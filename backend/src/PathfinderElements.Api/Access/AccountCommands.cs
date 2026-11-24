using Microsoft.AspNetCore.Identity;

namespace PathfinderElements.Api.Access;

internal static class AccountCommands
{
    // A fixed key enforces the single-account limit even for concurrent create commands.
    private const string AccountId = "admin";

    internal static async Task<int> ExecuteAsync(string[] arguments, UserManager<IdentityUser> users, TextReader input, TextWriter errors)
    {
        if (arguments is not ["account", "create", _] and not ["account", "reset-password"])
        {
            await errors.WriteLineAsync("Usage: account create <username> | account reset-password. Read password from stdin.");
            return 2;
        }

        var account = await users.FindByIdAsync(AccountId);
        var create = arguments[1] == "create";
        if (create == (account is not null))
        {
            await errors.WriteLineAsync(create ? "The account already exists." : "The account does not exist.");
            return 1;
        }

        var password = await input.ReadLineAsync();
        if (string.IsNullOrEmpty(password) || password.Length > 256)
        {
            await errors.WriteLineAsync("Provide a password of at most 256 characters on stdin.");
            return 2;
        }

        if (create)
        {
            var result = await users.CreateAsync(new IdentityUser { Id = AccountId, UserName = arguments[2] }, password);
            return await ReportAsync(result, errors);
        }

        var existingAccount = account!;
        existingAccount.AccessFailedCount = 0;
        existingAccount.LockoutEnd = null;
        var token = await users.GeneratePasswordResetTokenAsync(existingAccount);
        var reset = await users.ResetPasswordAsync(existingAccount, token, password);
        return await ReportAsync(reset, errors);
    }

    private static async Task<int> ReportAsync(IdentityResult result, TextWriter errors)
    {
        foreach (var error in result.Errors)
        {
            await errors.WriteLineAsync(error.Description);
        }

        return result.Succeeded ? 0 : 1;
    }
}
