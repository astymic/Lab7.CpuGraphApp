using System.Diagnostics;


namespace Lab7.CpuMonitoringLibrary
{
    public class CpuDataProvider : ICpuDataProvider
    {
        private readonly object _historyLock = new object();
        private readonly Queue<float> _cpuUsageHistory;

        private System.Timers.Timer? _updateTimer;
        private bool _isDisposed = false;

        // OS specific resources
#if NET5_0_OR_GREATER // for OperatingSystem.IsWindows()
        private PerformanceCounter? _windowsCpuCounter;
#elif NETFRAMEWORK
        private PerformanceCounter? _windowsCpuCounter;
#endif
        // For Linux/MacOS use other libraries or APIs


        public int HistoryCapacity { get; }
        public int UpdateInterval { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CpuDataProvider"/> class.
        /// </summary>
        /// <param name="historyDotsToStore">The number of seconds of CPU usage history to store. Defaults to 60.</param>
        public CpuDataProvider(int historyDotsToStore = 60, int updateTimeSpan = 1000)
        {
            if (historyDotsToStore <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(historyDotsToStore), "History capacity must be greater than zero.");
            }

            HistoryCapacity = historyDotsToStore;

            // Initialize history with default values
            _cpuUsageHistory = new Queue<float>(Enumerable.Repeat(0.0f, HistoryCapacity));
            UpdateInterval = updateTimeSpan;
        }

        public void StartMonitoring()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CpuDataProvider));

            if (_updateTimer != null && _updateTimer.Enabled)
                return;

            // Initialize Os specific resources
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsWindows())
            {
                if (_windowsCpuCounter == null)
                {
                    try
                    {
                        _windowsCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                        _windowsCpuCounter.NextValue(); // Prime the counter
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to initialize PerformanceCounter: {ex.Message}");
                        _windowsCpuCounter?.Dispose();
                        _windowsCpuCounter = null;
                    }
                }
            }
            // else if (OperatingSystem.IsLinux())
            // else if (OperatingSystem.IsMacOS())
#elif NETFRAMEWORK
            if (_windowsCpuCounter == null)
                {
                    try
                    {
                        _windowsCpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                        _windowsCpuCounter.NextValue(); // Prime the counter
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to initialize PerformanceCounter: {ex.Message}");
                        _windowsCpuCounter?.Dispose();
                        _windowsCpuCounter = null; 
                    }
                }
#endif

            UpdateCpuData();

            _updateTimer = new System.Timers.Timer(UpdateInterval); // Update interval
            _updateTimer.Elapsed += OnTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
        }

        public void StopMonitoring()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CpuDataProvider));

            _updateTimer?.Stop();
            if (_updateTimer != null)
            {
                _updateTimer.Elapsed -= OnTimerElapsed;
                _updateTimer?.Dispose();
            }
            _updateTimer = null;

#if NET5_0_OR_GREATER || NETFRAMEWORK
            _windowsCpuCounter?.Dispose();
            _windowsCpuCounter = null;
#endif
        }

        public List<float> GetCpuUsageHistory()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CpuDataProvider));

            lock (_historyLock)
            {
                return new List<float>(_cpuUsageHistory); // Return a copy 
            }
        }

        public void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateCpuData();
        }
        private void UpdateCpuData()
        {
            if (_isDisposed) return;

            float currentLoad = 0.0f;

            try
            {
#if NET5_0_OR_GREATER
                if (OperatingSystem.IsWindows())
                {
                    if (_windowsCpuCounter != null)
                    {
                        currentLoad = _windowsCpuCounter?.NextValue() ?? 0.0f;
                    }
                    else
                    {
                        currentLoad = 0.0f;
                    }
                }
                // else if (OperatingSystem.IsLinux()) 
                // else if (OperatingSystem.IsMacOS())
                else { currentLoad = -1f; /* Unsupported OS */ }
#elif NETFRAMEWORK
                if (_windowsCpuCounter != null)
                {
                    currentLoad = _windowsCpuCounter.NextValue();
                }
                else
                {
                    currentLoad = 0.0f;
                }
#else
                currentLoad = -2f;
#endif
            }
            catch (Exception ex)
            {
                // Handle exceptions related to performance counter access
                Debug.WriteLine($"Error accessing CPU usage data: {ex.Message}");
                currentLoad = 0.0f;
            }

            currentLoad = Math.Clamp(Math.Max(0.0f, currentLoad), 0.0f, 100.0f);

            lock (_historyLock)
            {
                if (_cpuUsageHistory.Count >= HistoryCapacity)
                {
                    _cpuUsageHistory.Dequeue();
                }
                _cpuUsageHistory.Enqueue(currentLoad);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            if (disposing)
            {
                StopMonitoring();
            }

            _isDisposed = true;
        }
    }
}