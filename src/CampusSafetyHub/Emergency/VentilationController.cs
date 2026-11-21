//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Emergency;

public sealed class VentilationController
{
    public bool IsRunning { get; private set; } = true;

    public void Stop()
    {
        IsRunning = false;
        Console.WriteLine("[VENTILATION] Stopped");
    }
}
