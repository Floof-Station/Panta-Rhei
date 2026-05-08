using Content.Shared._DV.MedicalRecords; // DeltaV - Medical Records
using Content.Shared._Shitmed.Targeting; // Shitmed Change
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public readonly NetEntity? TargetEntity;
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;
    public Dictionary<TargetBodyPart, TargetIntegrity>? Body; // Shitmed Change
    public NetEntity? Part; // Shitmed Change
    public MedicalRecord? MedicalRecord; // DeltaV - Medical Records
    public bool Printable; // Frontier

    public HealthAnalyzerScannedUserMessage(NetEntity? targetEntity, float temperature, float bloodLevel, bool? scanMode, bool? bleeding, bool? unrevivable,
        Dictionary<TargetBodyPart, TargetIntegrity>? body, // Shitmed Change
        MedicalRecord? medicalRecord = null, NetEntity? part = null, // DeltaV - Medical Records
        bool printable = false // Frontier
        )
    {
        TargetEntity = targetEntity;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        Body = body; // Shitmed Change
        Part = part; // Shitmed Change
        MedicalRecord = medicalRecord; // DeltaV - Medical Records
        Printable = printable; // Frontier
    }
}

// Shitmed Change Start
[Serializable, NetSerializable]
public sealed class HealthAnalyzerPartMessage(NetEntity? owner, TargetBodyPart? bodyPart) : BoundUserInterfaceMessage
{
    public readonly NetEntity? Owner = owner;
    public readonly TargetBodyPart? BodyPart = bodyPart;

}
// Shitmed Change End
