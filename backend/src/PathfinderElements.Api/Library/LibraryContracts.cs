using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace PathfinderElements.Api.Library;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal record TemplateWriteRequest(string? Name, TemplateStatisticsRequest? Statistics)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        ValidateFields("", errors);
        return errors;
    }

    public virtual void ValidateFields(string prefix, Dictionary<string, string[]> errors)
    {
        LibraryValidation.Text(Name, $"{prefix}name", errors, 256);
        if (Statistics is null)
        {
            errors[$"{prefix}statistics"] = ["required"];
        }
        else if (Statistics.MaxHitPoints <= 0)
        {
            errors[$"{prefix}statistics.maxHitPoints"] = ["mustBePositive"];
        }
    }

    public CharacterTemplate ToCharacterTemplate(string ownerUserId)
    {
        return new CharacterTemplate(ownerUserId, Name!, Statistics!.ToDomain());
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record MonsterWriteRequest(string? Name, TemplateStatisticsRequest? Statistics, AttackSequenceRequest?[]? AttackSequences)
    : TemplateWriteRequest(Name, Statistics)
{
    public override void ValidateFields(string prefix, Dictionary<string, string[]> errors)
    {
        base.ValidateFields(prefix, errors);
        if (AttackSequences is not null)
        {
            LibraryValidation.Items(AttackSequences, $"{prefix}attackSequences", errors, 0, 32,
                static (sequence, path, itemErrors) => sequence.Validate(path, itemErrors));
        }
    }

    public ImmutableArray<AttackSequence>? ToAttackSequences()
    {
        if (AttackSequences is null)
        {
            return null;
        }

        return [.. AttackSequences.Select(sequence => sequence!.ToDomain())];
    }

    public MonsterTemplate ToMonsterTemplate(string ownerUserId)
    {
        return new MonsterTemplate(ownerUserId, Name!, Statistics!.ToDomain(), ToAttackSequences());
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[JsonNumberHandling(JsonNumberHandling.Strict)]
internal sealed record TemplateStatisticsRequest(
    [property: JsonRequired] int MaxHitPoints,
    [property: JsonRequired] int InitiativeModifier,
    [property: JsonRequired] int ArmorClass,
    [property: JsonRequired] int TouchArmorClass,
    [property: JsonRequired] int FlatFootedArmorClass,
    [property: JsonRequired] int FortitudeSave,
    [property: JsonRequired] int ReflexSave,
    [property: JsonRequired] int WillSave)
{
    public CombatStatistics ToDomain()
    {
        return new CombatStatistics(MaxHitPoints, InitiativeModifier, ArmorClass, TouchArmorClass,
            FlatFootedArmorClass, FortitudeSave, ReflexSave, WillSave);
    }
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record TemplateArchiveRequest([property: JsonRequired] bool IsArchived);

internal sealed record TemplateResponse(
    Guid Id,
    string Name,
    CombatStatistics Statistics,
    bool IsArchived,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ImmutableArray<AttackSequence>? AttackSequences = null);
