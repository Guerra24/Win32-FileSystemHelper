using DotNet.Globbing;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Windows.Win32;

namespace Win32.FileSystemHelper;

public static partial class NativeExports
{
    private static List<FileSystemWatcher> watchers = new();

    private static ConcurrentQueue<string> events = new();

    private static ManualResetEventSlim eventing = new();

    private static ConcurrentDictionary<string, string> LongToShort = new();

    private static List<Glob> Filters = new();

    [UnmanagedCallersOnly(EntryPoint = "Initialize")]
    public static void Initialize(nint pathPtr, nint filtersPtr)
    {
        try
        {
            GlobOptions.Default.Evaluation.CaseInsensitive = true;

            var path = Marshal.PtrToStringAnsi(pathPtr)!;

            var filters = Marshal.PtrToStringAnsi(filtersPtr)!;

            foreach (Match match in FilterRegex().Matches(filters))
            {
                var filter = $"*.{match.Groups[1].Value.Replace("\\", "")}";
                Filters.Add(Glob.Parse($"**/{filter}"));
            }

            AddWatcher(path, filters);

            foreach (var f in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                var file = f.FS();
                foreach (var glob in Filters)
                    if (glob.IsMatch(file))
                        AddOrUpdateLongToShort(file);
            }

            foreach (var f in Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories))
            {
                var info = new FileInfo(f);
                if (!string.IsNullOrEmpty(info.LinkTarget))
                    AddWatcher(f, filters);
            }

        }
        catch (Exception e)
        {
            PrintException(e);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "Event")]
    public static nint Event()
    {
        if (events.TryDequeue(out var e))
        {
            return Marshal.StringToHGlobalAnsi(e);
        }
        return nint.Zero;
    }

    [UnmanagedCallersOnly(EntryPoint = "EventBlocking")]
    public static nint EventBlocking()
    {
        eventing.Wait();
        if (events.TryDequeue(out var e))
        {
            return Marshal.StringToHGlobalAnsi(e);
        }
        eventing.Reset();
        return nint.Zero;
    }

    [UnmanagedCallersOnly(EntryPoint = "Cleanup")]
    public static void Cleanup()
    {
        eventing.Dispose();
        watchers.ForEach(w => w.Dispose());
        watchers.Clear();
    }

    [UnmanagedCallersOnly(EntryPoint = "Interrupt")]
    public static void Interrupt()
    {
        eventing.Set();
    }

    [UnmanagedCallersOnly(EntryPoint = "FreeMemory")]
    public static void FreeMemory(nint ptr)
    {
        if (ptr != nint.Zero)
            Marshal.FreeHGlobal(ptr);
    }

    [UnmanagedCallersOnly(EntryPoint = "GetFullPath")]
    public static nint GetFullPath(nint pathPtr)
    {
        try
        {
            return Marshal.StringToHGlobalAnsi(Path.GetFullPath(Marshal.PtrToStringAnsi(pathPtr)!).FS());
        }
        catch (Exception e)
        {
            PrintException(e);
        }
        return nint.Zero;
    }

    [UnmanagedCallersOnly(EntryPoint = "GetShortPath")]
    public static nint GetShortPath(nint pathPtr)
    {
        try
        {
            var path = Marshal.PtrToStringAnsi(pathPtr)!;

            Span<char> buffer = stackalloc char[(int)PInvoke.MAX_PATH];
            var useMap = PInvoke.GetShortPathName(path, buffer) == 0;

            var shortPath = buffer.ToString().FS();

            if (useMap && LongToShort.TryGetValue(path, out shortPath))
            {
                return Marshal.StringToHGlobalAnsi(shortPath);
            }

            return Marshal.StringToHGlobalAnsi(shortPath);
        }
        catch (Exception e)
        {
            PrintException(e);
        }
        return nint.Zero;
    }

    private static void OnCreated(object sender, FileSystemEventArgs e)
    {
        var path = e.FullPath.FS();
        AddOrUpdateLongToShort(path);
        events.Enqueue($"{path}|create");
        eventing.Set();
    }

    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        events.Enqueue($"{e.FullPath.FS()}|modify");
        eventing.Set();
    }

    private static void OnDeleted(object sender, FileSystemEventArgs e)
    {
        events.Enqueue($"{e.FullPath.FS()}|delete");
        eventing.Set();
    }

    private static void OnRenamed(object sender, RenamedEventArgs e)
    {
        var path = e.FullPath.FS();
        events.Enqueue($"{path}|modify");
        AddOrUpdateLongToShort(path);
        eventing.Set();
    }

    private static void OnError(object sender, ErrorEventArgs e) =>
        PrintException(e.GetException());

    private static void PrintException(Exception? ex)
    {
        if (ex != null)
        {
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine("Stacktrace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();
            PrintException(ex.InnerException);
        }
    }

    private static void AddOrUpdateLongToShort(string file)
    {
        Span<char> buffer = new char[(int)PInvoke.MAX_PATH];
        PInvoke.GetShortPathName(file, buffer);
        LongToShort[file] = buffer.ToString().FS();
    }

    private static void AddWatcher(string path, string filters)
    {
        var watcher = new FileSystemWatcher(Path.GetFullPath(path));

        watcher.NotifyFilter = NotifyFilters.DirectoryName
                             | NotifyFilters.FileName;

        watcher.InternalBufferSize = 65536;

        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        foreach (Match match in FilterRegex().Matches(filters))
        {
            var filter = $"*.{match.Groups[1].Value.Replace("\\", "")}";
            watcher.Filters.Add(filter);
        }

        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;

        watchers.Add(watcher);
    }

    [GeneratedRegex(@"([\\.a-zA-Z0-9]+)[\)\|]")]
    private static partial Regex FilterRegex();

    private static string FS(this string path) => path.Replace('\\', '/');
}
