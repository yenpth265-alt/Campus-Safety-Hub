namespace CampusSafetyHub.Sensors;

public sealed class SmokeSensorAdapter : ISensor
{
    private readonly LegacySmokeDetector legacy;

    public SmokeSensorAdapter(LegacySmokeDetector legacy)
    {
        this.legacy = legacy ?? throw new ArgumentNullException(nameof(legacy));
    }

    public double ReadValue() => legacy.GetSmokeLevel() / 10.0;

    public string GetUnit() => "% smoke";
}
