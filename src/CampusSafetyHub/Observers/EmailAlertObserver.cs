//Phan Thị Hải Yến, MSSV 202419124
using CampusSafetyHub.Alerts;

namespace CampusSafetyHub.Observers;

public sealed class EmailAlertObserver : IAlertObserver
{
    public void Update(AlertEvent alertEvent)
    {
        Console.WriteLine(
            $"[EMAIL - SIMULATED] {alertEvent.SensorName}: " +
            $"{alertEvent.Value:F1} {alertEvent.Unit} [{alertEvent.Severity}]");
    }
}
