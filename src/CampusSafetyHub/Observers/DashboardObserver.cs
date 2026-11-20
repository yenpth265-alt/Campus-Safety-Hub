using CampusSafetyHub.Alerts;

namespace CampusSafetyHub.Observers;

public sealed class DashboardObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[DASHBOARD] {alertEvent.SensorName}: " +
            $"{alertEvent.Value:F1} {alertEvent.Unit} [{alertEvent.Severity}]");
    }
}
