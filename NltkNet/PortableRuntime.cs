using Python.Runtime;
using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace NltkNet
{
    internal static class PortableRuntime
    {
        private static readonly object Sync = new object();
        private static bool initialized;
        private static IntPtr pythonLibrary;

        internal static void Initialize()
        {
            lock (Sync)
            {
                if (initialized)
                    return;
                if (PythonEngine.IsInitialized)
                    throw new InvalidOperationException("CPython is already initialized by another component. NltkNet requires its packaged runtime.");

                string rid = RuntimeInformation.RuntimeIdentifier;
                var (libraryPath, executablePath, standardLibrary, extensions) = rid switch
                {
                    "osx-arm64" => ("lib/libpython3.12.dylib", "bin/python3", "lib/python3.12", "lib/python3.12/lib-dynload"),
                    "linux-x64" => ("lib/libpython3.12.so.1.0", "bin/python3", "lib/python3.12", "lib/python3.12/lib-dynload"),
                    "win-x64" => ("python312.dll", "python.exe", "Lib", "DLLs"),
                    _ => throw new PlatformNotSupportedException($"NltkNet has no runtime for {rid}. Supported runtimes: osx-arm64, win-x64, linux-x64 (glibc; not Alpine/musl).")
                };
                string archive = Path.Combine(AppContext.BaseDirectory, "nltknet", "runtime", rid, "python.tar.gz");
                if (!File.Exists(archive))
                    throw new FileNotFoundException($"The NltkNet runtime for {rid} is missing. Reference the matching NltkNet.Runtime.{rid} NuGet package.", archive);

                string home = Extract(archive, libraryPath);
                string library = Path.Combine(home, libraryPath);
                if (!File.Exists(library))
                    throw new FileNotFoundException("The packaged CPython library is missing.", library);

                // Python.NET uses legacy initialization, which can select ASCII in a
                // C locale. CPython 3.12's UTF-8 flag must be set before initialization.
                pythonLibrary = NativeLibrary.Load(library);
                Marshal.WriteInt32(NativeLibrary.GetExport(pythonLibrary, "Py_UTF8Mode"), 1);
                Runtime.PythonDLL = library;
                PythonEngine.ProgramName = Path.Combine(home, executablePath);
                PythonEngine.PythonHome = home;
                PythonEngine.PythonPath = string.Join(Path.PathSeparator,
                    Path.Combine(home, standardLibrary),
                    Path.Combine(home, extensions),
                    Path.Combine(home, standardLibrary, "site-packages"));
                PythonEngine.SetNoSiteFlag();
                PythonEngine.Initialize(setSysArgv: false, initSigs: false);
                // Py_SetPath (used by PythonPath) leaves prefix/exec_prefix empty.
                using (Py.GIL())
                using (var sys = Py.Import("sys"))
                using (var prefix = new PyString(home))
                {
                    sys.SetAttr("prefix", prefix);
                    sys.SetAttr("exec_prefix", prefix);
                    sys.SetAttr("base_prefix", prefix);
                    sys.SetAttr("base_exec_prefix", prefix);
                }
                if (OperatingSystem.IsWindows())
                {
                    using var gil = Py.GIL();
                    using var scope = Py.CreateScope();
                    scope.Set("home", home);
                    // Keep the Windows DLL directory handles alive for native extension imports.
                    scope.Exec("import os, sys\nsys._nltknet_dll_directories = [os.add_dll_directory(home), os.add_dll_directory(os.path.join(home, 'DLLs'))]");
                }
                // Release the initialization thread's GIL so calls from worker threads can acquire it.
                PythonEngine.BeginAllowThreads();
                initialized = true;
            }
        }

        private static string Extract(string archive, string libraryPath)
        {
            string expected = File.ReadAllText(archive + ".sha256").Trim();
            using (var stream = File.OpenRead(archive))
            {
                string actual = Convert.ToHexString(SHA256.HashData(stream));
                if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The packaged Python archive failed its SHA-256 integrity check.");
            }

            string cache = Environment.GetEnvironmentVariable("NLTKNET_CACHE_DIRECTORY")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NltkNet", "runtimes");
            if (!Path.IsPathFullyQualified(cache))
                throw new ArgumentException("NLTKNET_CACHE_DIRECTORY must be an absolute writable directory.");
            CreateCacheDirectory(cache);
            string destination = Path.Combine(cache, expected.ToLowerInvariant());
            using var cacheLock = AcquireLock(destination + ".lock");
            if (File.Exists(Path.Combine(destination, ".complete")))
                return Path.Combine(destination, "python");

            string staging = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            CreateCacheDirectory(staging);
            try
            {
                using (var input = File.OpenRead(archive))
                using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                    TarFile.ExtractToDirectory(gzip, staging, overwriteFiles: false);

                if (!File.Exists(Path.Combine(staging, "python", libraryPath)))
                    throw new InvalidDataException("The Python archive does not contain the expected runtime.");
                File.WriteAllText(Path.Combine(staging, ".complete"), expected);
                Directory.Move(staging, destination);
            }
            finally
            {
                if (Directory.Exists(staging))
                    Directory.Delete(staging, recursive: true);
            }
            return Path.Combine(destination, "python");
        }

        private static void CreateCacheDirectory(string path)
        {
            if (OperatingSystem.IsWindows())
                Directory.CreateDirectory(path);
            else
                Directory.CreateDirectory(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private static FileStream AcquireLock(string path)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException) when (attempt < 120)
                {
                    Thread.Sleep(250);
                }
            }
        }
    }
}
