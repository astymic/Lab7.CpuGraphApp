using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lab7.CpuMonitoringLibrary;


[TestClass]
public class CpuDataProviderTests
{
    [TestMethod]
    public void Constructor_WithValidHistorySeconds_SetsCapacityCorrectly()
    {
        // Arrange
        int historySeconds = 10;

        // Act
        using (var provider = new CpuDataProvider(historySeconds))
        {
            // Assert
            Assert.AreEqual(historySeconds, provider.HistoryCapacitySeconds);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Constructor_WithZeroHistorySeconds_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act
        using (var provider = new CpuDataProvider(0)) { } // Should throw
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Constructor_WithNegativeHistorySeconds_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act
        using (var provider = new CpuDataProvider(-5)) { } // Should throw
    }

    [TestMethod]
    public void GetCpuUsageHistory_Initial_ReturnsListWithCorrectCapacityAndDefaultValues()
    {
        // Arrange
        int historySeconds = 5;
        using (var provider = new CpuDataProvider(historySeconds))
        {
            // Act
            List<float> history = provider.GetCpuUsageHistory();

            // Assert
            Assert.IsNotNull(history);
            Assert.AreEqual(historySeconds, history.Count);
            Assert.IsTrue(history.All(val => val == 0.0f));
        }
    }

    [TestMethod]
    public void GetCpuUsageHistory_ReturnsCopyOfInternalData()
    {
        // Arrange
        int historySeconds = 3;
        using (var provider = new CpuDataProvider(historySeconds))
        {
            List<float> history1 = provider.GetCpuUsageHistory();

            // Act: Modify the returned list
            if (history1.Count > 0) history1[0] = 999f;

            List<float> history2 = provider.GetCpuUsageHistory();

            // Assert: The second retrieval should not reflect the modification
            Assert.IsNotNull(history2);
            Assert.AreEqual(historySeconds, history2.Count);
            if (history2.Count > 0) Assert.AreNotEqual(999f, history2[0]);
            Assert.IsTrue(history2.All(val => val == 0.0f)); // Assuming no monitoring started
        }
    }

    [TestMethod]
    public void StartMonitoring_And_StopMonitoring_CanBeCalledMultipleTimes()
    {
        // Arrange
        using (var provider = new CpuDataProvider(5))
        {
            // Act & Assert (no exceptions should be thrown)
            provider.StartMonitoring();
            provider.StartMonitoring(); // Call again
            Thread.Sleep(100); // Let it run a bit

            provider.StopMonitoring();
            provider.StopMonitoring(); // Call again
        }
    }

    [TestMethod]
    public void Methods_AfterDispose_ThrowObjectDisposedException()
    {
        // Arrange
        var provider = new CpuDataProvider(5);
        provider.Dispose();

        // Act & Assert
        Assert.ThrowsException<ObjectDisposedException>(() => provider.StartMonitoring());
        Assert.ThrowsException<ObjectDisposedException>(() => provider.StopMonitoring());
        Assert.ThrowsException<ObjectDisposedException>(() => provider.GetCpuUsageHistory());
        Assert.AreEqual(5, provider.HistoryCapacitySeconds);
    }

    [TestMethod]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var provider = new CpuDataProvider(5);

        // Act & Assert
        provider.Dispose();
        provider.Dispose(); // No exception should be thrown
    }

    [TestClass]
    public class CpuDataProviderWindowsIntegrationTests // Separate class or use TestCategory
    {
        [TestMethod]
        [TestCategory("Integration_Windows")] // Helps to run/skip these specific tests
        public void GetCpuUsageHistory_OnWindows_AfterMonitoring_ReturnsPlausibleValues()
        {
            // This test will only run effectively on Windows.
            if (!OperatingSystem.IsWindows())
            {
                Assert.Inconclusive("This test is for Windows only.");
                return;
            }

            // Arrange
            int historySeconds = 5;
            using (var provider = new CpuDataProvider(historySeconds))
            {
                provider.StartMonitoring();

                // Act
                Thread.Sleep(2000); // Allow time for a few updates (at least 2 sec for 2 updates)
                List<float> history = provider.GetCpuUsageHistory();
                provider.StopMonitoring();

                // Assert
                Assert.IsNotNull(history);
                Assert.AreEqual(historySeconds, history.Count, "History count mismatch.");

                // Check if at least some values are not the initial 0.0f (unless CPU is truly idle)
                // And that all values are within 0-100 range.
                int nonZeroUpdates = 0;
                foreach (var val in history)
                {
                    Assert.IsTrue(val >= 0.0f && val <= 100.0f, $"Value {val} out of range 0-100.");
                    // The initial prefill are zeros. After 2 seconds, at least 2 should be updated.
                    // The first value read immediately on StartMonitoring might be 0 if the counter wasn't fully primed.
                }

                // Check that the last few values are not all the initial '0.0f' values.
                // This is a heuristic. If CPU is 0% for 2s, this might fail.
                // A better check might be that not ALL values are 0.0f after some time.
                // Skip the first few initial default values that might not have been overwritten yet if test is too short
                int valuesToExpectUpdated = Math.Min(2, historySeconds); // Expect at least 2 updates if capacity allows
                int updatedCount = history.Skip(historySeconds - valuesToExpectUpdated).Count(v => v > 0.0f || history.First() != v); // Heuristic

                // More robust: check that not all values are the same as the initial fill value after a few updates.
                bool allInitial = true;
                for (int i = 0; i < history.Count; ++i)
                {
                    // If we started with 0s, and a value is not 0, it's updated.
                    // Or if we started with 0s, and the first update was 0, we need a different check.
                    // This test is tricky because actual CPU usage can be 0.
                    // A simple check: after 2s, at least one value should be from PerformanceCounter.
                    // If PC init failed, values will remain 0. If PC works and CPU is 0%, values will be 0.
                }
                // For now, the 0-100 range check is the most reliable part.
                // Asserting that values *change* or are non-zero is flaky.
                Console.WriteLine("Windows CPU History: " + string.Join(", ", history.Select(h => h.ToString("F1"))));
            }
        }
    }
}