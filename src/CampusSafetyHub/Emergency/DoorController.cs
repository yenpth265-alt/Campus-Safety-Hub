//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Emergency;

public sealed class DoorController
{
    public bool IsLocked { get; private set; } = true;

    public void Unlock()
    {
        IsLocked = false;
        Console.WriteLine("[DOOR] Unlocked");
    }
}
