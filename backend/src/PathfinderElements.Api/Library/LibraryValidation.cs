namespace PathfinderElements.Api.Library;

internal static class LibraryValidation
{
    public static void Text(string? value, string path, Dictionary<string, string[]> errors, int maximumLength,
        bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(value))
        {
            errors[path] = ["required"];
        }
        else if (value?.Length > maximumLength)
        {
            errors[path] = ["tooLong"];
        }
        else if (value?.Contains('\0') == true)
        {
            errors[path] = ["invalidCharacters"];
        }
    }

    public static void Items<T>(IReadOnlyList<T?>? items, string path, Dictionary<string, string[]> errors,
        int minimumCount, int maximumCount, Action<T, string, Dictionary<string, string[]>> validate)
        where T : class
    {
        if (items is null || items.Count < minimumCount)
        {
            errors[path] = ["required"];
            return;
        }

        if (items.Count > maximumCount)
        {
            errors[path] = ["tooManyItems"];
            return;
        }

        for (var index = 0; index < items.Count; index++)
        {
            var itemPath = $"{path}[{index}]";
            if (items[index] is { } item)
            {
                validate(item, itemPath, errors);
            }
            else
            {
                errors[itemPath] = ["required"];
            }
        }
    }
}
