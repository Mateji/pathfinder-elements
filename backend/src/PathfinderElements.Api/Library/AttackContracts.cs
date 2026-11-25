using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace PathfinderElements.Api.Library;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record AttackSequenceRequest(string? Name, AttackDefinitionRequest?[]? Attacks)
{
    public void Validate(string path, Dictionary<string, string[]> errors)
    {
        LibraryValidation.Text(Name, $"{path}.name", errors, 256);
        LibraryValidation.Items(Attacks, $"{path}.attacks", errors, 1, 32,
            static (attack, itemPath, itemErrors) => attack.Validate(itemPath, itemErrors));
    }

    public AttackSequence ToDomain()
    {
        return new AttackSequence(Name!, [.. Attacks!.Select(attack => attack!.ToDomain())]);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[JsonNumberHandling(JsonNumberHandling.Strict)]
internal sealed record AttackDefinitionRequest(
    string? Name,
    string? Mode,
    [property: JsonRequired] bool IsNatural,
    [property: JsonRequired] int AttackBonus,
    [property: JsonRequired] bool TargetsTouchArmorClass,
    [property: JsonRequired] int CriticalThreatMinimum,
    [property: JsonRequired] int CriticalMultiplier,
    AttackDamageRequest?[]? Damage,
    string? Notes)
{
    public void Validate(string path, Dictionary<string, string[]> errors)
    {
        LibraryValidation.Text(Name, $"{path}.name", errors, 256);
        LibraryValidation.Text(Notes, $"{path}.notes", errors, 1024, required: false);
        if (Mode is not ("melee" or "ranged"))
        {
            errors[$"{path}.mode"] = ["invalidAttackMode"];
        }

        if (CriticalThreatMinimum is < 2 or > 20)
        {
            errors[$"{path}.criticalThreatMinimum"] = ["outOfRange"];
        }

        if (CriticalMultiplier is < 2 or > 10)
        {
            errors[$"{path}.criticalMultiplier"] = ["outOfRange"];
        }

        LibraryValidation.Items(Damage, $"{path}.damage", errors, 0, 16,
            static (damage, itemPath, itemErrors) => damage.Validate(itemPath, itemErrors));
    }

    public AttackDefinition ToDomain()
    {
        var mode = Mode == "melee" ? AttackMode.Melee : AttackMode.Ranged;
        return new AttackDefinition(Name!, mode, IsNatural, AttackBonus, TargetsTouchArmorClass,
            CriticalThreatMinimum, CriticalMultiplier, [.. Damage!.Select(damage => damage!.ToDomain())], Notes);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[JsonNumberHandling(JsonNumberHandling.Strict)]
internal sealed record AttackDamageRequest(
    [property: JsonRequired] int DiceCount,
    [property: JsonRequired] int DieSides,
    [property: JsonRequired] int Modifier,
    string? DamageType,
    [property: JsonRequired] bool MultipliesOnCritical)
{
    public void Validate(string path, Dictionary<string, string[]> errors)
    {
        LibraryValidation.Text(DamageType, $"{path}.damageType", errors, 256);
        if (DiceCount is < 0 or > 100)
        {
            errors[$"{path}.diceCount"] = ["outOfRange"];
        }

        if (DiceCount == 0 && DieSides != 0 || DiceCount > 0 && DieSides is < 2 or > 100)
        {
            errors[$"{path}.dieSides"] = ["invalidDieSides"];
        }
    }

    public AttackDamage ToDomain()
    {
        return new AttackDamage(DiceCount, DieSides, Modifier, DamageType!, MultipliesOnCritical);
    }
}
