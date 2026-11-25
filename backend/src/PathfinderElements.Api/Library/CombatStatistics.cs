namespace PathfinderElements.Api.Library;

public sealed record CombatStatistics(
    int MaxHitPoints,
    int InitiativeModifier,
    int ArmorClass,
    int TouchArmorClass,
    int FlatFootedArmorClass,
    int FortitudeSave,
    int ReflexSave,
    int WillSave)
{
    public int MaxHitPoints { get; private init; } = RequirePositiveHitPoints(MaxHitPoints);

    private static int RequirePositiveHitPoints(int maxHitPoints)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHitPoints);
        return maxHitPoints;
    }
}
