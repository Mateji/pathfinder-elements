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
    public ICollection<EncounterParticipant> Participants { get; } = new List<EncounterParticipant>();

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
    private EncounterParticipant()
    {
    }

    internal EncounterParticipant(Guid encounterId, Guid templateId, bool isMonster)
    {
        Id = Guid.NewGuid();
        EncounterId = encounterId;
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
    public Guid? CharacterTemplateId { get; private set; }
    public Guid? MonsterTemplateId { get; private set; }
    public CharacterTemplate? CharacterTemplate { get; private set; }
    public MonsterTemplate? MonsterTemplate { get; private set; }
}
