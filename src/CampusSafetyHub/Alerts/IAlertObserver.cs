//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Alerts;

public interface IAlertObserver
{
    void Update(AlertEvent alertEvent);
}
