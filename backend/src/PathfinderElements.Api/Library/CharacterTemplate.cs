namespace PathfinderElements.Api.Library;

public sealed class CharacterTemplate
{
    private CharacterTemplate()
    {
    }

    public CharacterTemplate(string ownerUserId, string name, CombatStatistics statistics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(statistics);

        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Name = name;
        Statistics = statistics;
    }

    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public CombatStatistics Statistics { get; private set; } = null!;
}
