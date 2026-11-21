//Phan Thị Hải Yến, MSSV 202419124
using System.Globalization;
using CampusSafetyHub.Alerts;
using CampusSafetyHub.Emergency;
using CampusSafetyHub.Observers;
using CampusSafetyHub.Sensors;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var tests = new List<(string Name, Action Run)>();

tests.Add(("Adapter converts 735 to 73.5 and reports the required unit", () =>
{
    ISensor sensor = new SmokeSensorAdapter(new LegacySmokeDetector(735));
    Equal(73.5, sensor.ReadValue());
    Equal("% smoke", sensor.GetUnit());
}));

tests.Add(("Adapter reads live legacy state instead of copying it", () =>
{
    var legacy = new LegacySmokeDetector(735);
    ISensor sensor = new SmokeSensorAdapter(legacy);
    legacy.SetSmokeLevel(820);
    Equal(82.0, sensor.ReadValue());
    legacy.SetSmokeLevel(-1);
    Equal(0.0, sensor.ReadValue());
    legacy.SetSmokeLevel(1001);
    Equal(100.0, sensor.ReadValue());
}));

foreach (var (raw, expectedSeverity) in new (int, string?)[]
{
    (0, null), (499, null), (500, "WARNING"), (699, "WARNING"),
    (700, "CRITICAL"), (735, "CRITICAL"), (1000, "CRITICAL")
})
{
    tests.Add(($"Threshold: {raw} -> {expectedSeverity ?? "no alert"}", () =>
    {
        var monitor = new SensorMonitor();
        var observer = new RecordingObserver();
        monitor.Attach(observer);
        var before = DateTime.Now;
        monitor.CheckSensor("Lab A", new SmokeSensorAdapter(new LegacySmokeDetector(raw)));
        var after = DateTime.Now;
        Equal(expectedSeverity is null ? 0 : 1, observer.Events.Count);
        if (expectedSeverity is not null)
        {
            var alert = observer.Events.Single();
            Equal(expectedSeverity, alert.Severity);
            Equal("Lab A", alert.SensorName);
            Equal(raw / 10.0, alert.Value);
            Equal("% smoke", alert.Unit);
            Check(alert.OccurredAt >= before && alert.OccurredAt <= after,
                "Timestamp must represent the sensor check.");
        }
    }));
}

tests.Add(("Monitor accepts an alternative ISensor implementation", () =>
{
    var monitor = new SensorMonitor();
    var observer = new RecordingObserver();
    monitor.Attach(observer);
    monitor.CheckSensor("Alternative sensor", new FixedSensor(60, "test unit"));
    Equal("test unit", observer.Events.Single().Unit);
    Equal("WARNING", observer.Events.Single().Severity);
}));

tests.Add(("Duplicate attachment of the same instance delivers once", () =>
{
    var monitor = new SensorMonitor();
    var observer = new RecordingObserver();
    monitor.Attach(observer);
    monitor.Attach(observer);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, observer.Events.Count);
}));

tests.Add(("Different instances of the same observer class both receive", () =>
{
    var monitor = new SensorMonitor();
    var first = new RecordingObserver();
    var second = new RecordingObserver();
    monitor.Attach(first);
    monitor.Attach(second);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, first.Events.Count);
    Equal(1, second.Events.Count);
    Check(ReferenceEquals(first.Events[0], second.Events[0]),
        "Recipients must receive the same immutable event.");
}));

tests.Add(("Detach stops delivery and reattach restores delivery", () =>
{
    var monitor = new SensorMonitor();
    var observer = new RecordingObserver();
    monitor.Attach(observer);
    monitor.Detach(observer);
    monitor.Detach(observer); // Removing an absent observer is harmless.
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(0, observer.Events.Count);
    monitor.Attach(observer);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, observer.Events.Count);
}));

tests.Add(("An observer can detach itself without interrupting notification", () =>
{
    var monitor = new SensorMonitor();
    var selfRemoving = new SelfRemovingObserver(monitor);
    var remaining = new RecordingObserver();
    monitor.Attach(selfRemoving);
    monitor.Attach(remaining);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, selfRemoving.Count);
    Equal(2, remaining.Events.Count);
}));

tests.Add(("Detaching another observer takes effect after the current snapshot", () =>
{
    var monitor = new SensorMonitor();
    var removed = new RecordingObserver();
    monitor.Attach(new CallbackObserver(_ => monitor.Detach(removed)));
    monitor.Attach(removed);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, removed.Events.Count);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, removed.Events.Count);
}));

tests.Add(("An observer attached during notification starts on the next check", () =>
{
    var monitor = new SensorMonitor();
    var added = new RecordingObserver();
    monitor.Attach(new CallbackObserver(_ => monitor.Attach(added)));
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(0, added.Events.Count);
    monitor.CheckSensor("Lab", new FixedSensor(70));
    Equal(1, added.Events.Count);
}));

tests.Add(("A failing observer cannot block later recipients or change their data", () =>
{
    var monitor = new SensorMonitor();
    var recipient = new RecordingObserver();
    var mediator = new RecordingMediator();
    monitor.Attach(new CallbackObserver(alert =>
    {
        var changedCopy = alert with { Value = 0, Severity = "WARNING" };
        Equal(0.0, changedCopy.Value);
        throw new InvalidOperationException("Simulated observer failure");
    }));
    monitor.Attach(recipient);
    monitor.Attach(new EmergencyBridgeObserver(mediator));
    var captured = Capture(() => monitor.CheckSensor("Lab", new FixedSensor(73.5)));
    Equal(73.5, recipient.Events.Single().Value);
    Equal("CRITICAL", recipient.Events.Single().Severity);
    Equal(EmergencyEvents.FireDetected, mediator.Events.Single());
    Contains("Simulated observer failure", captured.Error);
}));

tests.Add(("Bridge ignores WARNING and forwards CRITICAL as FIRE_DETECTED", () =>
{
    var mediator = new RecordingMediator();
    var bridge = new EmergencyBridgeObserver(mediator);
    var warning = new AlertEvent("Lab", 60, "% smoke", "WARNING", DateTime.Now);
    bridge.Update(warning);
    Equal(0, mediator.Events.Count);
    bridge.Update(warning with { Value = 70, Severity = "CRITICAL" });
    Equal(EmergencyEvents.FireDetected, mediator.Events.Single());
}));

tests.Add(("Mediator ignores unknown events and coordinates all three devices", () =>
{
    var alarm = new Alarm();
    var door = new DoorController();
    var ventilation = new VentilationController();
    IEmergencyMediator mediator = new EmergencyMediator(alarm, door, ventilation);
    var ignored = Capture(() => mediator.Notify("UNKNOWN"));
    Equal(string.Empty, ignored.Output);
    Check(!alarm.IsActive && door.IsLocked && ventilation.IsRunning,
        "Unknown events must preserve device states.");
    var handled = Capture(() => mediator.Notify(EmergencyEvents.FireDetected));
    Check(alarm.IsActive && !door.IsLocked && !ventilation.IsRunning,
        "FIRE_DETECTED must coordinate all three devices.");
    Contains("[ALARM] Activated", handled.Output);
    Contains("[DOOR] Unlocked", handled.Output);
    Contains("[VENTILATION] Stopped", handled.Output);
}));

tests.Add(("Full integration: normal, warning, critical, duplicate registration, detach", () =>
{
    var legacy = new LegacySmokeDetector(499);
    ISensor sensor = new SmokeSensorAdapter(legacy);
    var alarm = new Alarm();
    var door = new DoorController();
    var ventilation = new VentilationController();
    var mediator = new EmergencyMediator(alarm, door, ventilation);
    var monitor = new SensorMonitor();
    var email = new EmailAlertObserver();
    monitor.Attach(new DashboardObserver());
    monitor.Attach(email);
    monitor.Attach(new AuditLogObserver());
    monitor.Attach(new EmergencyBridgeObserver(mediator));
    monitor.Attach(email);

    var normal = Capture(() => monitor.CheckSensor("Lab", sensor));
    Equal(string.Empty, normal.Output);
    legacy.SetSmokeLevel(500);
    var warning = Capture(() => monitor.CheckSensor("Lab", sensor));
    Contains("[WARNING]", warning.Output);
    Check(!warning.Output.Contains("[MEDIATOR]"), "WARNING must not trigger emergency actions.");
    Check(!alarm.IsActive && door.IsLocked && ventilation.IsRunning,
        "WARNING must preserve initial device states.");

    legacy.SetSmokeLevel(735);
    var critical = Capture(() => monitor.CheckSensor("Lab", sensor));
    Contains("[DASHBOARD] Lab: 73.5 % smoke [CRITICAL]", critical.Output);
    Contains("[AUDIT]", critical.Output);
    Contains("FIRE_DETECTED", critical.Output);
    Equal(1, critical.Output.Split("[EMAIL - SIMULATED]").Length - 1);
    Check(alarm.IsActive && !door.IsLocked && !ventilation.IsRunning,
        "CRITICAL must reach the actual devices.");

    monitor.Detach(email);
    var detached = Capture(() => monitor.CheckSensor("Lab", sensor));
    Check(!detached.Output.Contains("[EMAIL"), "Detached email must receive nothing.");
    Contains("[DASHBOARD]", detached.Output);
    Contains("[AUDIT]", detached.Output);
    Contains("[MEDIATOR]", detached.Output);
}));

int failures = 0;
foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

Console.WriteLine($"\n{tests.Count - failures}/{tests.Count} tests passed.");
return failures == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
}

static void Check(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Contains(string expected, string actual) =>
    Check(actual.Contains(expected, StringComparison.Ordinal), $"Missing output: {expected}");

static (string Output, string Error) Capture(Action action)
{
    var originalOutput = Console.Out;
    var originalError = Console.Error;
    using var output = new StringWriter(CultureInfo.InvariantCulture);
    using var error = new StringWriter(CultureInfo.InvariantCulture);
    try
    {
        Console.SetOut(output);
        Console.SetError(error);
        action();
        return (output.ToString(), error.ToString());
    }
    finally
    {
        Console.SetOut(originalOutput);
        Console.SetError(originalError);
    }
}

sealed class FixedSensor(double value, string unit = "% smoke") : ISensor
{
    public double ReadValue() => value;
    public string GetUnit() => unit;
}

sealed class RecordingObserver : IAlertObserver
{
    public List<AlertEvent> Events { get; } = new();
    public void Update(AlertEvent alertEvent) => Events.Add(alertEvent);
}

sealed class CallbackObserver(Action<AlertEvent> callback) : IAlertObserver
{
    public void Update(AlertEvent alertEvent) => callback(alertEvent);
}

sealed class SelfRemovingObserver(SensorMonitor monitor) : IAlertObserver
{
    public int Count { get; private set; }

    public void Update(AlertEvent alertEvent)
    {
        Count++;
        monitor.Detach(this);
    }
}

sealed class RecordingMediator : IEmergencyMediator
{
    public List<string> Events { get; } = new();
    public void Notify(string eventName) => Events.Add(eventName);
}
