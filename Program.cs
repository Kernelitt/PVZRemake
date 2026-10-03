using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using System.Runtime.InteropServices;

namespace PVZRemake;

internal class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    static void Main(string[] args)
    {
#if DEBUG
                Console.WriteLine("Debug Build!");
#endif

#if !DEBUG
        ShowWindow(GetConsoleWindow(), 0);  // Скрыть консоль
#endif
        var gameWindowSettings = new GameWindowSettings { UpdateFrequency = 75.0 };
        var nativeWindowSettings = new NativeWindowSettings
        {
            ClientSize = new(1600, 900),
            Title = "PlantsVsZombies Remake",
            APIVersion = new Version(4, 6),
            Profile = ContextProfile.Core,
            WindowBorder = WindowBorder.Resizable,
            StartVisible = true,
            AspectRatio = (16, 9),
            Icon = new WindowIcon()
        };

        var window = new LawnApp(gameWindowSettings, nativeWindowSettings);
        window.Run();
        
    }
}
