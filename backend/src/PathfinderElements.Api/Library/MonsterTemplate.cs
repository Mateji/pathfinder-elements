using System.Collections.Immutable;

namespace PathfinderElements.Api.Library;

public sealed class MonsterTemplate : ILibraryTemplate
{
    private MonsterTemplate()
    {
    }

    public MonsterTemplate(string ownerUserId, string name, CombatStatistics statistics,
        ImmutableArray<AttackSequence>? attackSequences = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUserId);
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Update(name, statistics, attackSequences);
    }

    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public CombatStatistics Statistics { get; private set; } = null!;
    public ImmutableArray<AttackSequence> AttackSequences { get; private set; } = [];
    public bool IsArchived { get; private set; }
    public Guid Version { get; private set; }

    public void Update(string name, CombatStatistics statistics)
    {
        Update(name, statistics, attackSequences: null);
    }

    public void Update(string name, CombatStatistics statistics, ImmutableArray<AttackSequence>? attackSequences)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(statistics);

        Name = name;
        Statistics = statistics;
        if (attackSequences.HasValue)
        {
            AttackSequences = attackSequences.Value.IsDefault ? [] : attackSequences.Value;
        }

        Version = Guid.NewGuid();
    }

    public void SetArchived(bool isArchived)
    {
        IsArchived = isArchived;
        Version = Guid.NewGuid();
    }
}
