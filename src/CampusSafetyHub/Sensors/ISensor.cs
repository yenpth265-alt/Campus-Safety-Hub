namespace CampusSafetyHub.Sensors;

public interface ISensor
{
    double ReadValue();
    string GetUnit();
}
