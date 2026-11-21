//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Sensors;

public interface ISensor
{
    double ReadValue();
    string GetUnit();
}
