//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Emergency;

public sealed class Alarm
{
    public bool IsActive { get; private set; }

    public void Activate()
    {
        IsActive = true;
        Console.WriteLine("[ALARM] Activated");
    }
}
