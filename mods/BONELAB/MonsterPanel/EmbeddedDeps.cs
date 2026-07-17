using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using MelonLoader;

namespace MonsterPanel
{
    /// <summary>
    /// Loads Npgsql (+ deps) from embedded resources so only MonsterPanel.dll is needed in Mods/.
    /// </summary>
    internal static class EmbeddedDeps
    {
        private static int _installed;
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Assembly> Cache =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> NameToResource =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Npgsql"] = "MonsterPanel.Embedded.Npgsql.dll",
                ["Microsoft.Extensions.Logging.Abstractions"] = "MonsterPanel.Embedded.Microsoft.Extensions.Logging.Abstractions.dll",
                ["Microsoft.Extensions.DependencyInjection.Abstractions"] = "MonsterPanel.Embedded.Microsoft.Extensions.DependencyInjection.Abstractions.dll",
            };

        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void AutoInstall() => Install();

        public static void Install()
        {
            if (Interlocked.Exchange(ref _installed, 1) != 0) return;

            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            lock (Gate)
            {
                foreach (string res in NameToResource.Values)
                    TryLoadResource_NoLock(res);
            }
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            try
            {
                string name = new AssemblyName(args.Name).Name;
                if (string.IsNullOrEmpty(name)) return null;

                lock (Gate)
                {
                    if (Cache.TryGetValue(name, out Assembly hit))
                        return hit;

                    if (!NameToResource.TryGetValue(name, out string res))
                        return null;

                    return TryLoadResource_NoLock(res);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Warning("EmbeddedDeps resolve: " + e.Message);
                return null;
            }
        }

        private static Assembly TryLoadResource_NoLock(string resourceName)
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
                Cache[asm.GetName().Name] = asm;
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
