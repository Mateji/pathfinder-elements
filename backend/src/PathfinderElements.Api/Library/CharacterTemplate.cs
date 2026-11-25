namespace PathfinderElements.Api.Library;

public sealed class CharacterTemplate : ILibraryTemplate
{
    private CharacterTemplate()
    {
    }

    public CharacterTemplate(string ownerUserId, string name, CombatStatistics statistics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Update(name, statistics);
    }

    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public CombatStatistics Statistics { get; private set; } = null!;
    public bool IsArchived { get; private set; }
    public Guid Version { get; private set; }

    public void Update(string name, CombatStatistics statistics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(statistics);

        Name = name;
        Statistics = statistics;
        Version = Guid.NewGuid();
    }

    public void SetArchived(bool isArchived)
    {
        IsArchived = isArchived;
        Version = Guid.NewGuid();
    }
}
