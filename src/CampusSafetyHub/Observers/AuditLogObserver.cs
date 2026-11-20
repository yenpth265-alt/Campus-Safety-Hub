using CampusSafetyHub.Alerts;

namespace CampusSafetyHub.Observers;

public sealed class AuditLogObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[AUDIT] {alertEvent.OccurredAt:O} | {alertEvent.SensorName} | " +
            $"{alertEvent.Value:F1} {alertEvent.Unit} | {alertEvent.Severity}");
    }
}
