using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PathfinderElements.Api.Library;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record LibraryImportDocument(
    int SchemaVersion, TemplateWriteRequest?[]? Characters, MonsterWriteRequest?[]? Monsters)
{
    private const int SupportedSchemaVersion = 1;
    private const int MaximumTemplates = 250;
    private static readonly JsonDocumentOptions DocumentOptions = new() { AllowDuplicateProperties = false };
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public string ComputeFingerprint()
    {
        // Keep the canonical representation stable for version 1 across serializer and DTO changes.
        var canonical = this with
        {
            Characters = Characters ?? [],
            Monsters = (Monsters ?? []).Select(request =>
            {
                var monster = request!;
                return monster with { AttackSequences = monster.AttackSequences ?? [] };
            }).ToArray()
        };
        return Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(canonical, SerializerOptions)));
    }

    public static async Task<(LibraryImportDocument? Document, Dictionary<string, string[]> Errors)> ReadAsync(
        Stream input, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        try
        {
            using var json = await JsonDocument.ParseAsync(input, DocumentOptions, cancellationToken);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors["document"] = ["expectedObject"];
                return (null, errors);
            }

            if (!json.RootElement.TryGetProperty("schemaVersion", out var version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schemaVersion))
            {
                errors["schemaVersion"] = ["requiredInteger"];
                return (null, errors);
            }

            if (schemaVersion != SupportedSchemaVersion)
            {
                errors["schemaVersion"] = ["unsupportedVersion"];
                return (null, errors);
            }

            var document = json.RootElement.Deserialize<LibraryImportDocument>(SerializerOptions)!;
            document.Validate(errors);
            return errors.Count == 0 ? (document, errors) : (null, errors);
        }
        catch (JsonException exception)
        {
            var path = exception.Path is { Length: > 2 and <= 256 } jsonPath && jsonPath.StartsWith("$.", StringComparison.Ordinal)
                ? jsonPath[2..]
                : "document";
            errors[path] = ["invalidJson"];
            return (null, errors);
        }
    }

    private void Validate(Dictionary<string, string[]> errors)
    {
        var templateCount = (Characters?.Length ?? 0) + (Monsters?.Length ?? 0);
        if (templateCount is < 1 or > MaximumTemplates)
        {
            errors["document"] = [templateCount == 0 ? "atLeastOneTemplate" : "tooManyTemplates"];
            return;
        }

        LibraryValidation.Items(Characters ?? [], "characters", errors, 0, MaximumTemplates,
            static (character, path, itemErrors) => character.ValidateFields($"{path}.", itemErrors));
        LibraryValidation.Items(Monsters ?? [], "monsters", errors, 0, MaximumTemplates,
            static (monster, path, itemErrors) => monster.ValidateFields($"{path}.", itemErrors));
    }
}
