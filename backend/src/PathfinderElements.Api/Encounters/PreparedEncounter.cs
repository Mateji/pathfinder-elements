using System.Text.RegularExpressions;
using PathfinderElements.Api.Library;

namespace PathfinderElements.Api.Encounters;

internal sealed class PreparedEncounter
{
    private PreparedEncounter()
    {
    }

    internal PreparedEncounter(string ownerUserId, string name)
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Rename(name);
    }

    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid Version { get; private set; }
    public int NextParticipantNumber { get; private set; } = 1;
    public ICollection<EncounterParticipant> Participants { get; } = new List<EncounterParticipant>();

    internal EncounterParticipant[] CreateParticipants(Guid templateId, bool isMonster, int quantity)
    {
        var participants = new EncounterParticipant[quantity];
        for (var index = 0; index < quantity; index++)
        {
            participants[index] = new EncounterParticipant(Id, templateId, isMonster, NextParticipantNumber);
            NextParticipantNumber = checked(NextParticipantNumber + 1);
        }
        MarkChanged();
        return participants;
    }

    internal void Rename(string name)
    {
        Name = name.Trim();
        MarkChanged();
    }

    internal void MarkChanged()
    {
        Version = Guid.NewGuid();
    }
}

internal sealed class EncounterParticipant
{
    private static readonly string[] DefaultColors =
    [
        "#C8192F", "#0057D9", "#FFC400", "#007A3D", "#7231B8", "#E86000", "#008EA1", "#C20876"
    ];

    private EncounterParticipant()
    {
    }

    internal EncounterParticipant(Guid encounterId, Guid templateId, bool isMonster, int number)
    {
        Id = Guid.NewGuid();
        EncounterId = encounterId;
        Number = number;
        Color = DefaultColors[(number - 1) % DefaultColors.Length];
        if (isMonster)
        {
            MonsterTemplateId = templateId;
        }
        else
        {
            CharacterTemplateId = templateId;
        }
    }

    public Guid Id { get; private set; }
    public Guid EncounterId { get; private set; }
    public int Number { get; private set; }
    public string Color { get; private set; } = null!;
    public Guid? CharacterTemplateId { get; private set; }
    public Guid? MonsterTemplateId { get; private set; }
    public CharacterTemplate? CharacterTemplate { get; private set; }
    public MonsterTemplate? MonsterTemplate { get; private set; }

    internal void ChangeColor(string color)
    {
        Color = color;
    }

    internal static string? NormalizeColor(string? color)
    {
        return color is not null && Regex.IsMatch(color, @"\A#[0-9A-Fa-f]{6}\z") ? color.ToUpperInvariant() : null;
    }
}
