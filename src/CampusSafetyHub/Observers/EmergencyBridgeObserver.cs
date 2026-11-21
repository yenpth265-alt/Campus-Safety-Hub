//Phan Thị Hải Yến, MSSV 202419124
using CampusSafetyHub.Alerts;
using CampusSafetyHub.Emergency;

namespace CampusSafetyHub.Observers;

public sealed class EmergencyBridgeObserver : IAlertObserver
{
    private readonly IEmergencyMediator mediator;

    public EmergencyBridgeObserver(IEmergencyMediator mediator)
    {
        this.mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public void Update(AlertEvent alertEvent)
    {
        if (alertEvent.Severity == "CRITICAL")
        {
            mediator.Notify(EmergencyEvents.FireDetected);
        }
    }
}
