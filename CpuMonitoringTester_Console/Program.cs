using Lab7.CpuMonitoringLibrary;
using System;
using System.Threading;
using System.Linq;
using System.Data;


public class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("CPU Monitoring Test");
        Console.WriteLine($"Targeting: {(OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsLinux() ? "Linux" : OperatingSystem.IsMacOS() ? "macOS" : "Other OS")}");

        ICpuDataProvider cpuMonitor = new CpuDataProvider(); // 60 seconds default 

        Console.WriteLine($"History capacity: {cpuMonitor.HistoryCapacitySeconds} seconds.");
        cpuMonitor.StartMonitoring();

        try
        {
            for (int i = 0; i < 120; i++) // 2 minutes
            {
                Thread.Sleep(2000); // Sleep 2 seconds
                var history = cpuMonitor.GetCpuUsageHistory();

                string historyString = string.Join(", ", history.Select(h => $"{h:F1}%"));
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] CPU History ({history.Count} points): {historyString}");

                if (history.Any())
                {
                    Console.WriteLine($"  Current (approx): {history.Last():F1}%");
                }
            }
        }
        catch (ThreadInterruptedException)
        {
            Console.WriteLine("Monitoring interrupted by user.");
        }
        finally
        {
            Console.WriteLine("Stopping monitoring...");
            cpuMonitor.StopMonitoring();
            cpuMonitor.Dispose();
            Console.WriteLine("Monitoring stopped and resources disposed.");
        }
        Console.WriteLine("Test finished. Press any key to exit.");
        Console.ReadKey();
    }
}