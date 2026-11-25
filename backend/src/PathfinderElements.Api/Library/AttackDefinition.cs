using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace PathfinderElements.Api.Library;

public sealed record AttackSequence(string Name, ImmutableArray<AttackDefinition> Attacks);

public sealed record AttackDefinition(
    string Name,
    AttackMode Mode,
    bool IsNatural,
    int AttackBonus,
    bool TargetsTouchArmorClass,
    int CriticalThreatMinimum,
    int CriticalMultiplier,
    ImmutableArray<AttackDamage> Damage,
    string? Notes);

[JsonConverter(typeof(JsonStringEnumConverter<AttackMode>))]
public enum AttackMode
{
    [JsonStringEnumMemberName("melee")]
    Melee,
    [JsonStringEnumMemberName("ranged")]
    Ranged
}

public sealed record AttackDamage(int DiceCount, int DieSides, int Modifier, string DamageType, bool MultipliesOnCritical);
