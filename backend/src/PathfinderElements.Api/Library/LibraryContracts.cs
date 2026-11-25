using System.Text.Json.Serialization;

namespace PathfinderElements.Api.Library;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record TemplateWriteRequest(string? Name, TemplateStatisticsRequest? Statistics)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors["name"] = ["required"];
        }
        else if (Name.Length > 256)
        {
            errors["name"] = ["tooLong"];
        }
        else if (Name.Contains('\0'))
        {
            errors["name"] = ["invalidCharacters"];
        }

        if (Statistics is null)
        {
            errors["statistics"] = ["required"];
        }
        else if (Statistics.MaxHitPoints <= 0)
        {
            errors["statistics.maxHitPoints"] = ["mustBePositive"];
        }

        return errors;
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

internal sealed record TemplateResponse(Guid Id, string Name, CombatStatistics Statistics, bool IsArchived);
