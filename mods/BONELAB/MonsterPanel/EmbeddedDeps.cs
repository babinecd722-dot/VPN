using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;

namespace MonsterPanel
{
    /// <summary>
    /// Loads Npgsql (+ deps) from embedded resources so only MonsterPanel.dll is needed in Mods/.
    /// </summary>
    internal static class EmbeddedDeps
    {
        private static bool _installed;
        private static readonly Dictionary<string, Assembly> Cache =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        private static readonly string[] ResourceDlls =
        {
            "MonsterPanel.Embedded.Npgsql.dll",
            "MonsterPanel.Embedded.Microsoft.Extensions.Logging.Abstractions.dll",
            "MonsterPanel.Embedded.Microsoft.Extensions.DependencyInjection.Abstractions.dll",
        };

        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void AutoInstall()
        {
            Install();
        }

        public static void Install()
        {
            if (_installed) return;
            _installed = true;

            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            // Preload so MelonLoader / first Npgsql use doesn't race.
            foreach (string res in ResourceDlls)
                TryLoadResource(res);
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(name)) return null;

                if (Cache.TryGetValue(name, out Assembly hit))
                    return hit;

                string res =
                    name.Equals("Npgsql", StringComparison.OrdinalIgnoreCase)
                        ? "MonsterPanel.Embedded.Npgsql.dll"
                        : name.Equals("Microsoft.Extensions.Logging.Abstractions", StringComparison.OrdinalIgnoreCase)
                            ? "MonsterPanel.Embedded.Microsoft.Extensions.Logging.Abstractions.dll"
                            : name.Equals("Microsoft.Extensions.DependencyInjection.Abstractions", StringComparison.OrdinalIgnoreCase)
                                ? "MonsterPanel.Embedded.Microsoft.Extensions.DependencyInjection.Abstractions.dll"
                                : null;

                if (res == null) return null;
                return TryLoadResource(res);
            }
            catch (Exception e)
            {
                MelonLogger.Warning("EmbeddedDeps resolve: " + e.Message);
                return null;
            }
        }

        private static Assembly TryLoadResource(string resourceName)
        {
            try
            {
                Assembly self = typeof(EmbeddedDeps).Assembly;
                using Stream stream = self.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MelonLogger.Warning("EmbeddedDeps: missing resource " + resourceName);
                    return null;
                }

                byte[] data = new byte[stream.Length];
                int read = 0;
                while (read < data.Length)
                {
                    int n = stream.Read(data, read, data.Length - read);
                    if (n <= 0) break;
                    read += n;
                }

                Assembly asm = Assembly.Load(data);
                string simple = asm.GetName().Name;
                Cache[simple] = asm;
                return asm;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("EmbeddedDeps load " + resourceName + ": " + e.Message);
                return null;
            }
        }
    }
}
