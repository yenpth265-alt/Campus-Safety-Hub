//Phan Thị Hải Yến, MSSV 202419124
using System.Globalization;
using CampusSafetyHub.Alerts;
using CampusSafetyHub.Emergency;
using CampusSafetyHub.Observers;
using CampusSafetyHub.Sensors;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Console.WriteLine("CAMPUS SAFETY HUB - Adapter / Observer / Mediator");

var legacy = new LegacySmokeDetector(300);
ISensor sensor = new SmokeSensorAdapter(legacy);
var alarm = new Alarm();
var door = new DoorController();
var ventilation = new VentilationController();
IEmergencyMediator mediator = new EmergencyMediator(alarm, door, ventilation);

var monitor = new SensorMonitor();
var email = new EmailAlertObserver();
monitor.Attach(new DashboardObserver());
monitor.Attach(email);
monitor.Attach(new AuditLogObserver());
monitor.Attach(new EmergencyBridgeObserver(mediator));

Console.WriteLine("\n1. NORMAL: 300 -> 30.0% (no alerts)");
monitor.CheckSensor("Lab Smoke Sensor", sensor);

Console.WriteLine("\n2. WARNING: 600 -> 60.0% (no emergency actions)");
legacy.SetSmokeLevel(600);
monitor.CheckSensor("Lab Smoke Sensor", sensor);

Console.WriteLine("\n3. Attach the same email observer again; CRITICAL: 735 -> 73.5%");
monitor.Attach(email);
legacy.SetSmokeLevel(735);
monitor.CheckSensor("Lab Smoke Sensor", sensor);

Console.WriteLine("\n4. Detach email; CRITICAL again (no email output)");
monitor.Detach(email);
monitor.CheckSensor("Lab Smoke Sensor", sensor);

Console.WriteLine("\n5. Reattach email; CRITICAL again (email output restored)");
monitor.Attach(email);
monitor.CheckSensor("Lab Smoke Sensor", sensor);

Console.WriteLine(
    $"\nFinal state: alarm active = {alarm.IsActive}, " +
    $"door locked = {door.IsLocked}, ventilation running = {ventilation.IsRunning}");
