using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Win32.FileSystemHelper;

public static class NativeExports
{
    private static FileSystemWatcher watcher = null!;

    private static ConcurrentQueue<string> events = new();

    private static ManualResetEventSlim eventing = new();

    [UnmanagedCallersOnly(EntryPoint = "Initialize")]
    public static void Initialize(nint pathPtr)
    {
        var path = Marshal.PtrToStringAnsi(pathPtr);

        watcher = new FileSystemWatcher(Path.GetFullPath(path!));

        watcher.NotifyFilter = NotifyFilters.DirectoryName
                             | NotifyFilters.FileName;

        watcher.InternalBufferSize = 65536;

        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;

        watcher.Filters.Add("*.zip");
        watcher.Filters.Add("*.rar");
        watcher.Filters.Add("*.7z");
        watcher.Filters.Add("*.tar");
        watcher.Filters.Add("*.tar.gz");
        watcher.Filters.Add("*.lzma");
        watcher.Filters.Add("*.xz");
        watcher.Filters.Add("*.cbz");
        watcher.Filters.Add("*.cbr");
        watcher.Filters.Add("*.cb7");
        watcher.Filters.Add("*.cbt");
        watcher.Filters.Add("*.pdf");
        watcher.Filters.Add("*.epub");
        watcher.Filters.Add("*.tar.zst");
        watcher.Filters.Add("*.zst");

        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
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
        watcher.Dispose();
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
        var path = Marshal.PtrToStringAnsi(pathPtr);
        path = Path.GetFullPath(path!).Replace('\\', '/');
        return Marshal.StringToHGlobalAnsi(path);
    }

    private static void OnCreated(object sender, FileSystemEventArgs e)
    {
        eventing.Set();
        events.Enqueue($"{e.FullPath}|create");
    }

    private static void OnChanged(object sender, FileSystemEventArgs e)
    {
        eventing.Set();
        events.Enqueue($"{e.FullPath}|modify");
    }

    private static void OnDeleted(object sender, FileSystemEventArgs e)
    {
        eventing.Set();
        events.Enqueue($"{e.FullPath}|delete");
    }

    private static void OnRenamed(object sender, RenamedEventArgs e)
    {
        eventing.Set();
        events.Enqueue($"{e.FullPath}|modify");
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
}
