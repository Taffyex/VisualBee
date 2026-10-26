using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace MusicBeePlugin
{
    internal static class EmbeddedResolver
    {
        private static int _initialized;

        public static void EnsureRegistered()
        {
            if (Interlocked.Exchange(ref _initialized, 1) != 0)
                return;

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        // Assembly.Load(byte[]) loads into no binding context, so the runtime never caches it:
        // every AssemblyResolve would otherwise create a NEW copy of SharpDX, and
        // SharpDX.Direct3D11 would bind to a different SharpDX than the plugin
        // (type identity mismatch -> MissingMethodException). Cache one instance per name.
        private static readonly Dictionary<string, Assembly> _loaded =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                var asmName = new AssemblyName(args.Name).Name;
                if (!asmName.StartsWith("SharpDX", StringComparison.OrdinalIgnoreCase))
                    return null;

                lock (_loaded)
                {
                    Assembly cached;
                    if (_loaded.TryGetValue(asmName, out cached))
                        return cached;
                    Assembly loaded = LoadEmbedded(asmName);
                    if (loaded != null)
                        _loaded[asmName] = loaded;
                    return loaded;
                }
            }
            catch
            {
                return null;
            }
        }

        private static Assembly LoadEmbedded(string asmName)
        {
            string resName = "MusicBeePlugin.Embedded." + asmName + ".dll";
            var currentAsm = typeof(EmbeddedResolver).Assembly;

            using (var stream = currentAsm.GetManifestResourceStream(resName))
            {
                if (stream == null)
                    return null;

                byte[] buffer = new byte[stream.Length];
                int totalRead = 0;
                while (totalRead < buffer.Length)
                {
                    int read = stream.Read(buffer, totalRead, buffer.Length - totalRead);
                    if (read <= 0) break;
                    totalRead += read;
                }

                return Assembly.Load(buffer);
            }
        }
    }
}
