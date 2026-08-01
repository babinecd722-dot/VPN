using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using Epic.OnlineServices.Logging;
using Epic.OnlineServices.Platform;

namespace EosJoinProbe;

/// <summary>
/// Per-bot EOS identity. Reuse saved DeviceId/PUID by default; mint only when login fails.
/// </summary>
internal sealed class EosIdentity
{
    // U+00B7 MIDDLE DOT — same LinkFilter bypass as host lobby name
    // (reads as bonelab·fun; ZWSP-after-dot now shows "?" on Quest).
    public const string DefaultDisplayName = "bonelab\u00b7fun";

    /// <summary>Prefer BOT_NICK env (host ADMIN / farm nick) over baked default.</summary>
    public static string ActiveDisplayName =>
        Environment.GetEnvironmentVariable("BOT_NICK") is { Length: > 0 } n ? n : DefaultDisplayName;

    public string InstallId { get; set; }
    public string EncryptionKey { get; set; }
    public string DisplayName { get; set; }
    public string CacheDirectory { get; set; }
    public string ProductUserId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastLoginUtc { get; set; }
    public bool FreshMint { get; set; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static EosIdentity LoadOrMint(string dataDir, bool forceNew)
    {
        Directory.CreateDirectory(dataDir);
        string path = Path.Combine(dataDir, "eos-identity.json");
        if (!forceNew && File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<EosIdentity>(File.ReadAllText(path), JsonOpts);
                if (loaded != null && !string.IsNullOrEmpty(loaded.EncryptionKey) && !string.IsNullOrEmpty(loaded.CacheDirectory))
                {
                    loaded.DisplayName = ActiveDisplayName;
                    Directory.CreateDirectory(loaded.CacheDirectory);
                    Console.WriteLine($"[identity] reuse install={loaded.InstallId} puid={loaded.ProductUserId ?? "?"} nick={loaded.DisplayName}");
                    Persist(loaded, path);
                    return loaded;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("[identity] load failed: " + e.Message);
            }
        }
        return Mint(dataDir, path);
    }

    public static EosIdentity Mint(string dataDir, string path = null)
    {
        Directory.CreateDirectory(dataDir);
        path ??= Path.Combine(dataDir, "eos-identity.json");
        string install = Guid.NewGuid().ToString("N");
        string cache = Path.Combine(dataDir, "eos-cache", install);
        Directory.CreateDirectory(cache);
        var id = new EosIdentity
        {
            InstallId = install,
            EncryptionKey = RandomHex(64),
            DisplayName = ActiveDisplayName,
            CacheDirectory = cache,
            CreatedUtc = DateTime.UtcNow,
            FreshMint = true,
        };
        Persist(id, path);
        Console.WriteLine($"[identity] minted NEW install={install} nick={id.DisplayName}");
        return id;
    }

    public static void Persist(EosIdentity id, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(id, JsonOpts));
    }

    public static string IdentityPath(string dataDir) => Path.Combine(dataDir, "eos-identity.json");

    public static PlatformInterface CreatePlatform(EosIdentity id)
    {
        var init = new InitializeOptions { ProductName = "Fusion", ProductVersion = "0.0.1" };
        Result r = PlatformInterface.Initialize(ref init);
        if (r != Result.Success && r != Result.AlreadyConfigured)
            throw new InvalidOperationException("EOS_Initialize: " + r);

        LoggingInterface.SetLogLevel(LogCategory.AllCategories, LogLevel.Warning);
        Directory.CreateDirectory(id.CacheDirectory);
        var options = new Options
        {
            ProductId = Env("EOS_PRODUCT_ID", "29e074d5b4724f3bb01f26b7e33d2582"),
            SandboxId = Env("EOS_SANDBOX_ID", "26f32d66d87f4dfeb4a7449b776a41f1"),
            // Fusion 0.1.0+ release DLL credentials (git tag still ships old values).
            DeploymentId = Env("EOS_DEPLOYMENT_ID", "951363bef61a4b7cbd04902e570f80f1"),
            ClientCredentials = new ClientCredentials
            {
                ClientId = Env("EOS_CLIENT_ID", "xyza78915hKqxe2TNTavpq2sxBDvJ9AH"),
                ClientSecret = Env("EOS_CLIENT_SECRET", "4HJqOC7+zzdzWw8AsA4yvLe0Ea9CBco8PS+yzW/rhBE"),
            },
            Flags = PlatformFlags.DisableOverlay | PlatformFlags.DisableSocialOverlay,
            TickBudgetInMilliseconds = 0,
            CacheDirectory = id.CacheDirectory,
            EncryptionKey = id.EncryptionKey,
        };
        var platform = PlatformInterface.Create(ref options);
        if (platform == null) throw new InvalidOperationException("Platform.Create returned null");
        return platform;
    }

    /// <summary>
    /// Prefer reuse: CreateDeviceId (keep existing) → Login.
    /// Only DeleteDeviceId + new CreateUser when forceNew or login is broken.
    /// </summary>
    public static ProductUserId LoginDeviceAccount(PlatformInterface platform, EosIdentity id, bool forceNew, Action tick)
    {
        var connect = platform.GetConnectInterface()
            ?? throw new InvalidOperationException("Connect interface null");

        string model = $"PrimexLangFarm/{id.InstallId[..Math.Min(8, id.InstallId.Length)]}/{Sanitize(Environment.MachineName, 20)}";

        if (forceNew || id.FreshMint)
        {
            Console.WriteLine("[identity] force/fresh mint path");
            return MintAndLogin(connect, id, model, tick);
        }

        // --- reuse path ---
        Result create = CreateDeviceId(connect, model, tick);
        Console.WriteLine($"[identity] CreateDeviceId(reuse): {create}");
        if (create != Result.Success && create != Result.DuplicateNotAllowed)
        {
            Console.WriteLine("[identity] CreateDeviceId failed on reuse — reminting");
            return MintAndLogin(connect, id, model, tick);
        }

        var (login, user, continuance) = DoLogin(connect, id.DisplayName ?? ActiveDisplayName, tick);
        Console.WriteLine($"[identity] Login(reuse): {login}");

        if (login == Result.Success && user != null)
        {
            // Prefer same PUID if we had one saved
            string got = user.ToString();
            if (!string.IsNullOrEmpty(id.ProductUserId) &&
                !string.Equals(id.ProductUserId, got, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[identity] PUID changed {id.ProductUserId} → {got} (DeviceId slot shared) — keeping session");
            }
            id.ProductUserId = got;
            id.LastLoginUtc = DateTime.UtcNow;
            id.FreshMint = false;
            id.DisplayName = ActiveDisplayName;
            return user;
        }

        if (login == Result.InvalidUser && continuance != null)
        {
            // DeviceId exists but no user yet — create once (not a wipe)
            user = CreateUser(connect, continuance, tick);
            id.ProductUserId = user.ToString();
            id.LastLoginUtc = DateTime.UtcNow;
            id.FreshMint = false;
            id.DisplayName = ActiveDisplayName;
            return user;
        }

        Console.WriteLine($"[identity] reuse login failed ({login}) — reminting new account");
        return MintAndLogin(connect, id, model, tick);
    }

    private static ProductUserId MintAndLogin(ConnectInterface connect, EosIdentity id, string model, Action tick)
    {
        DeleteDeviceId(connect, tick);
        Result create = CreateDeviceId(connect, model + "/n", tick);
        Console.WriteLine($"[identity] CreateDeviceId(mint): {create}");
        if (create == Result.DuplicateNotAllowed)
        {
            DeleteDeviceId(connect, tick);
            create = CreateDeviceId(connect, model + "/n2", tick);
            Console.WriteLine($"[identity] CreateDeviceId(mint retry): {create}");
        }
        if (create != Result.Success && create != Result.DuplicateNotAllowed)
            throw new InvalidOperationException("CreateDeviceId failed: " + create);

        var (login, user, continuance) = DoLogin(connect, id.DisplayName ?? ActiveDisplayName, tick);
        Console.WriteLine($"[identity] Login(mint): {login}");

        if (login == Result.InvalidUser && continuance != null)
            user = CreateUser(connect, continuance, tick);
        else if (login != Result.Success || user == null)
            throw new InvalidOperationException("Login failed after mint: " + login);

        id.ProductUserId = user.ToString();
        id.LastLoginUtc = DateTime.UtcNow;
        id.FreshMint = false;
        id.DisplayName = ActiveDisplayName;
        return user;
    }

    private static (Result login, ProductUserId user, ContinuanceToken continuance) DoLogin(
        ConnectInterface connect, string displayName, Action tick)
    {
        ProductUserId user = null;
        ContinuanceToken continuance = null;
        Result login = Result.UnexpectedError;
        bool done = false;
        var loginOpts = new LoginOptions
        {
            Credentials = new Credentials { Type = ExternalCredentialType.DeviceidAccessToken, Token = "" },
            UserLoginInfo = new UserLoginInfo { DisplayName = displayName },
        };
        connect.Login(ref loginOpts, null, (ref LoginCallbackInfo d) =>
        {
            login = d.ResultCode;
            if (d.ResultCode == Result.Success) user = d.LocalUserId;
            else if (d.ResultCode == Result.InvalidUser) continuance = d.ContinuanceToken;
            done = true;
        });
        Pump(tick, () => done, 40);
        return (login, user, continuance);
    }

    private static ProductUserId CreateUser(ConnectInterface connect, ContinuanceToken continuance, Action tick)
    {
        bool done = false;
        Result cu = Result.UnexpectedError;
        ProductUserId user = null;
        var cuOpts = new CreateUserOptions { ContinuanceToken = continuance };
        connect.CreateUser(ref cuOpts, null, (ref CreateUserCallbackInfo d) =>
        {
            cu = d.ResultCode;
            if (d.ResultCode == Result.Success) user = d.LocalUserId;
            done = true;
        });
        Pump(tick, () => done, 40);
        Console.WriteLine($"[identity] CreateUser: {cu}");
        if (cu != Result.Success || user == null)
            throw new InvalidOperationException("CreateUser failed: " + cu);
        return user;
    }

    /// <summary>
    /// Intentionally a no-op on Linux EOSSDK — Connect.Logout is often missing/broken and
    /// SIGSEGVs the process after a successful farm cycle (exit 139).
    /// </summary>
    public static void Logout(PlatformInterface platform, ProductUserId user, Action tick)
    {
        Console.WriteLine("[identity] Logout skipped (Linux EOSSDK teardown unsafe)");
    }

    public static Result DeleteDeviceId(ConnectInterface connect, Action tick)
    {
        bool done = false;
        Result result = Result.UnexpectedError;
        var opts = new DeleteDeviceIdOptions();
        connect.DeleteDeviceId(ref opts, null, (ref DeleteDeviceIdCallbackInfo d) =>
        {
            result = d.ResultCode;
            done = true;
        });
        Pump(tick, () => done, 20);
        Console.WriteLine($"[identity] DeleteDeviceId: {result}");
        return result;
    }

    private static Result CreateDeviceId(ConnectInterface connect, string model, Action tick)
    {
        bool done = false;
        Result result = Result.UnexpectedError;
        var opts = new CreateDeviceIdOptions { DeviceModel = model };
        connect.CreateDeviceId(ref opts, null, (ref CreateDeviceIdCallbackInfo d) =>
        {
            result = d.ResultCode;
            done = true;
        });
        Pump(tick, () => done, 30);
        return result;
    }

    private static void Pump(Action tick, Func<bool> done, int timeoutSec)
    {
        var until = DateTime.UtcNow.AddSeconds(timeoutSec);
        while (!done() && DateTime.UtcNow < until)
        {
            try { tick(); } catch { /* native tick blip */ }
            Thread.Sleep(15);
        }
        if (!done())
            throw new TimeoutException("EOS identity callback timed out");
    }

    private static string Env(string k, string d)
        => Environment.GetEnvironmentVariable(k) is { Length: > 0 } v ? v : d;

    private static string RandomHex(int chars)
    {
        var buf = RandomNumberGenerator.GetBytes(chars / 2);
        var sb = new StringBuilder(chars);
        foreach (byte b in buf) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string Sanitize(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "host";
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_') sb.Append(c);
            if (sb.Length >= max) break;
        }
        return sb.Length > 0 ? sb.ToString() : "host";
    }
}
