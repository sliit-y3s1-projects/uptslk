using System.Security.Claims;
using api.Services.AgentRecovery;
using Xunit;

namespace api.Tests.AgentRecovery;

public sealed class RecoverySecurityTests
{
    [Fact]
    public void TryCreate_AllowsAdminWithoutCentreClaim()
    {
        var principal = CreatePrincipal("Admin", null);

        var created = RecoveryAccessScope.TryCreate(principal, out var scope);

        Assert.True(created);
        Assert.True(scope.IsAdmin);
        Assert.True(scope.CanAccess(Guid.NewGuid()));
    }

    [Fact]
    public void TryCreate_RestrictsCentreUserToClaimedCentre()
    {
        var centreId = Guid.NewGuid();
        var principal = CreatePrincipal("CentreManager", centreId);

        var created = RecoveryAccessScope.TryCreate(principal, out var scope);

        Assert.True(created);
        Assert.False(scope.IsAdmin);
        Assert.True(scope.CanAccess(centreId));
        Assert.False(scope.CanAccess(Guid.NewGuid()));
    }

    [Fact]
    public void TryCreate_RejectsOperationalUserWithoutCentreClaim()
    {
        var principal = CreatePrincipal("Dispatcher", null);

        var created = RecoveryAccessScope.TryCreate(principal, out _);

        Assert.False(created);
    }

    [Fact]
    public void TryNormalizeObjective_TrimsValidInput()
    {
        var valid = RecoveryTextValidator.TryNormalizeObjective(
            "  Restore the delayed service safely.  ",
            out var normalized,
            out var error);

        Assert.True(valid);
        Assert.Equal("Restore the delayed service safely.", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalizeObjective_RejectsUnsupportedControlCharacters()
    {
        var valid = RecoveryTextValidator.TryNormalizeObjective(
            "Restore service\u0001now",
            out _,
            out var error);

        Assert.False(valid);
        Assert.Contains("control characters", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryNormalizeDecisionNote_RejectsExcessiveLines()
    {
        var note = string.Join('\n', Enumerable.Range(1, 13).Select(number => $"Line {number}"));

        var valid = RecoveryTextValidator.TryNormalizeDecisionNote(note, out _, out var error);

        Assert.False(valid);
        Assert.Contains("12 lines", error);
    }

    [Theory]
    [InlineData('\r')]
    [InlineData('\u2028')]
    [InlineData('\u2029')]
    public void TryNormalizeDecisionNote_RejectsAlternateThirteenLineSeparators(char separator)
    {
        var note = string.Join(separator.ToString(), Enumerable.Range(1, 13).Select(number => $"Line {number}"));

        var valid = RecoveryTextValidator.TryNormalizeDecisionNote(note, out _, out var error);

        Assert.False(valid);
        Assert.Contains("12 lines", error);
    }

    [Fact]
    public void TryNormalizeDecisionNote_CountsCrLfAsSingleLineSeparator()
    {
        var note = string.Join("\r\n", Enumerable.Range(1, 12).Select(number => $"Line {number}"));

        var valid = RecoveryTextValidator.TryNormalizeDecisionNote(note, out _, out var error);

        Assert.True(valid);
        Assert.Null(error);
    }

    [Fact]
    public void SanitizeForPrompt_RemovesControlCharactersAndAppliesLengthLimit()
    {
        var sanitized = RecoveryTextValidator.SanitizeForPrompt("abc\u0001def", 5);

        Assert.Equal("abcde", sanitized);
    }

    private static ClaimsPrincipal CreatePrincipal(string role, Guid? centreId)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, role) };
        if (centreId.HasValue) claims.Add(new Claim("centre_id", centreId.Value.ToString()));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
