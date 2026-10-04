using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class UpdateVersionTests
{
    [Theory]
    [InlineData("v1.0.1", "v1.0.0")]
    [InlineData("v1.1.0", "v1.0.9")]
    [InlineData("v2.0.0", "v1.99.99")]
    [InlineData("1.0.1", "1.0.0")]  // tags without the v prefix still compare
    public void IsNewer_HigherRelease_IsAnUpdate(string candidate, string current)
    {
        Assert.True(UpdateService.IsNewer(candidate, current));
    }

    [Theory]
    [InlineData("v1.0.0", "v1.0.0")]
    [InlineData("v1.0.0", "v1.0.1")]
    [InlineData("v1.0.0", "v2.0.0")]
    public void IsNewer_SameOrOlderRelease_IsNot(string candidate, string current)
    {
        Assert.False(UpdateService.IsNewer(candidate, current));
    }

    [Fact]
    public void IsNewer_InstallationOnABareCommit_TreatsAnyReleaseAsNewer()
    {
        // "git describe --always" on a branch gives a short commit id, which is not a version -
        // offering the release is the useful answer there.
        Assert.True(UpdateService.IsNewer("v1.0.0", "34e3b73"));
    }

    [Fact]
    public void IsNewer_PreReleaseSuffix_ComparesTheVersionPart()
    {
        Assert.True(UpdateService.IsNewer("v1.2.0-rc1", "v1.1.0"));
        Assert.False(UpdateService.IsNewer("v1.0.0-rc1", "v1.1.0"));
    }

    [Fact]
    public void IsNewer_UnparsableCandidate_IsNeverOffered()
    {
        Assert.False(UpdateService.IsNewer("nightly", "v1.0.0"));
        Assert.False(UpdateService.IsNewer("", "v1.0.0"));
    }
}
