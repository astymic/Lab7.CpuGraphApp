using Lab7.CpuMonitoringLibrary;
using Lab7.CpuMonitoringTester_Console.Windows;

namespace Lab7.CpuMonitoringTester_Console;

public static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var os = "Other OS";

        if (OperatingSystem.IsWindows())
        {
            os = "Windows";
        }
        else if (OperatingSystem.IsLinux())
        {
            os = "Linux";
        }
        else if (OperatingSystem.IsMacOS())
        {
            os = "MacOS";
        }

        Console.WriteLine("CPU Monitoring Test");
        Console.WriteLine($"Targeting: {os}");

        ICpuDataProvider cpuMonitor = new CpuDataProvider(); // 60 seconds default

        cpuMonitor.StartMonitoring();
        using var window = new ChartWindow(cpuMonitor);

        window.Run();
    }
}
