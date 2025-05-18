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

        var chartSize = 600; // dots.
        var updateInterval = 30; // ms.

        ICpuDataProvider cpuMonitor = new CpuDataProvider(chartSize, updateInterval);

        cpuMonitor.StartMonitoring();
        using var window = new ChartWindow(cpuMonitor);

        window.Run();
    }
}
