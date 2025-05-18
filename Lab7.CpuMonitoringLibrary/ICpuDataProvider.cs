using System;
using System.Collections.Generic;

namespace Lab7.CpuMonitoringLibrary
{
    /// <summary>
    /// Defines the contract for a CPU usage data.
    /// Provides a history of CPU utilization percentages, updated periodically.
    /// </summary>
    public interface ICpuDataProvider : IDisposable
    {
        public int HistoryCapacity { get; }
        public int UpdateInterval { get; }

        /// <summary>
        /// Starts the periodic CPU usage data collection.
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// Stops the periodic collection of CPU usage data and realeases associated resources.
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Retrieves a snapshot of the CPU usage history.
        /// The list contains the CPU utilization percentages (0-100).
        /// The most recent value is at the end of the list.
        /// </summary>
        /// <returns>A list of floats representing CPU usage history.</returns>
        List<float> GetCpuUsageHistory();

        //event Action<float> NewCpuValueRecorded;
        //event Action<List<float>> HistoryUpdated;
    }
}
