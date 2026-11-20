using CampusSafetyHub.Sensors;

namespace CampusSafetyHub.Alerts;

public sealed class SensorMonitor
{
    private readonly List<IAlertObserver> observers = new();

    public void Attach(IAlertObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
        }
    }

    public void Detach(IAlertObserver observer) => observers.Remove(observer);

    public void CheckSensor(string sensorName, ISensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);
        double value = sensor.ReadValue();
        string? severity = value switch
        {
            >= 70 => "CRITICAL",
            >= 50 => "WARNING",
            _ => null
        };

        if (severity is null)
        {
            return;
        }

        var alertEvent = new AlertEvent(
            sensorName, value, sensor.GetUnit(), severity, DateTime.Now);
        Notify(alertEvent);
    }

    private void Notify(AlertEvent alertEvent)
    {
        // Snapshot allows observers to attach/detach during a notification.
        foreach (IAlertObserver observer in observers.ToArray())
        {
            try
            {
                observer.Update(alertEvent);
            }
            catch (Exception ex)
            {
                // One failed recipient must not prevent emergency handling.
                Console.Error.WriteLine(
                    $"[OBSERVER ERROR] {observer.GetType().Name}: {ex.Message}");
            }
        }
    }
}
