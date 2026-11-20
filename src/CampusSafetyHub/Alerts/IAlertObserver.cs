namespace CampusSafetyHub.Alerts;

public interface IAlertObserver
{
    void Update(AlertEvent alertEvent);
}
