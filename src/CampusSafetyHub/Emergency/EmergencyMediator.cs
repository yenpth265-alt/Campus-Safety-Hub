namespace CampusSafetyHub.Emergency;

public sealed class EmergencyMediator : IEmergencyMediator
{
    private readonly Alarm alarm;
    private readonly DoorController door;
    private readonly VentilationController ventilation;

    public EmergencyMediator(
        Alarm alarm, DoorController door, VentilationController ventilation)
    {
        this.alarm = alarm ?? throw new ArgumentNullException(nameof(alarm));
        this.door = door ?? throw new ArgumentNullException(nameof(door));
        this.ventilation = ventilation
            ?? throw new ArgumentNullException(nameof(ventilation));
    }

    public void Notify(string eventName)
    {
        if (eventName != EmergencyEvents.FireDetected)
        {
            return;
        }

        Console.WriteLine($"[MEDIATOR] {eventName}");
        alarm.Activate();
        door.Unlock();
        ventilation.Stop();
    }
}
