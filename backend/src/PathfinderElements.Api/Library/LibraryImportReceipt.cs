namespace PathfinderElements.Api.Library;

internal sealed record LibraryImportReceipt(string OwnerUserId, string Fingerprint)
{
    internal const string PrimaryKeyName = "pk_library_import_receipts";
}
