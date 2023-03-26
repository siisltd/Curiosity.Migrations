using FluentAssertions;
using Xunit;

namespace Curiosity.Migrations.UnitTests;

/// <summary>
/// Test for regexp patterns
/// </summary>
public class DbVersion_Should
{
    [Fact]
    public void TryParse_Numeric_Major_Version()
    {
        var parseResult = DbVersion.TryParse("1", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(1, "version contains only major version");
        version.Minor.Should().Be(0, "version contains only minor version");
    }

    [Fact]
    public void Pattern_Should_Handle_Numeric_Major_Minor_Version()
    {
        var parseResult = DbVersion.TryParse("1.2", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(1, "version contains major version");
        version.Minor.Should().Be(2, "version contains minor version");
    }

    [Fact]
    public void Pattern_Should_Handle_Numeric_Major_Minor_2_Version()
    {
        var parseResult = DbVersion.TryParse("10001.020", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(10001, "version contains major version");
        version.Minor.Should().Be(20, "version contains minor version");
    }

    [Fact]
    public void Pattern_Should_Handle_Yyymmdd_Version()
    {
        var parseResult = DbVersion.TryParse("20201208", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(20201208, "version contains major version");
        version.Minor.Should().Be(0, "version contains only major version");
    }

    [Fact]
    public void Pattern_Should_Handle_Yyymmdd_hhmm_Version()
    {
        var parseResult = DbVersion.TryParse("20201208-1850", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(202012081850, "version contains major version");
        version.Minor.Should().Be(0, "version contains only major version");
    }

    [Fact]
    public void Pattern_Should_Handle_Yyymmdd_hhmm_minor_Version()
    {
        var parseResult = DbVersion.TryParse("20201208-1850.02", out var version);

        parseResult.Should().BeTrue("it's correct version");
        version.Major.Should().Be(202012081850, "version contains major version");
        version.Minor.Should().Be(2, "version contains minor version");
    }
}
