//Phan Thị Hải Yến, MSSV 202419124
namespace CampusSafetyHub.Alerts;

public sealed record AlertEvent(
    string SensorName,
    double Value,
    string Unit,
    string Severity,
    DateTime OccurredAt);
