namespace PathfinderElements.Api.Library;

internal interface ILibraryTemplate
{
    Guid Id { get; }
    string OwnerUserId { get; }
    string Name { get; }
    CombatStatistics Statistics { get; }
    bool IsArchived { get; }
    Guid Version { get; }

    void Update(string name, CombatStatistics statistics);
    void SetArchived(bool isArchived);
}
