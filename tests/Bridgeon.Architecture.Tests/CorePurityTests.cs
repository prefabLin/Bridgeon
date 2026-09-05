using System.Xml.Linq;
using Bridgeon.Core.Scoring;
using FluentAssertions;
using NetArchTest.Rules;

namespace Bridgeon.Architecture.Tests;

/// <summary>
/// Bridgeon.Core holds the domain: contracts, scoring, rules and ranking. It must
/// stay free of I/O, hosting and persistence so that it is separately testable,
/// separately publishable, and cheap to reason about. These tests are the
/// enforcement — a convention nobody checks is not a boundary.
/// </summary>
public class CorePurityTests
{
    private static readonly System.Reflection.Assembly Core = typeof(ImpScale).Assembly;

    public static TheoryData<string> ForbiddenDependencies()
    {
        var data = new TheoryData<string>();
        foreach (var ns in new[]
                 {
                     "System.IO", "System.Net", "System.Text.Json", "System.Xml",
                     "System.Data", "System.Diagnostics.Process",
                     "Microsoft.AspNetCore", "Microsoft.Extensions", "Microsoft.Data",
                     "Microsoft.EntityFrameworkCore", "ModelContextProtocol",
                 })
        {
            data.Add(ns);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ForbiddenDependencies))]
    public void CoreDoesNotDependOn(string forbidden)
    {
        var result = Types.InAssembly(Core)
            .ShouldNot().HaveDependencyOn(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Bridgeon.Core must not depend on {0}, but these types do: {1}",
            forbidden, string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void CoreCarriesNoPackageReferences()
    {
        var packages = XDocument.Load(RepoFile("src/Bridgeon.Core/Bridgeon.Core.csproj").FullName)
            .Descendants("PackageReference")
            .Select(e => e.Attribute("Include")?.Value ?? "?")
            .ToArray();

        packages.Should().BeEmpty(
            "the domain compiles against the base class library alone; a dependency "
            + "here becomes a dependency for everyone who reuses the scoring library");
    }

    [Fact]
    public void CoreReferencesNoOtherBridgeonProject()
    {
        var references = XDocument.Load(RepoFile("src/Bridgeon.Core/Bridgeon.Core.csproj").FullName)
            .Descendants("ProjectReference")
            .Select(e => e.Attribute("Include")?.Value ?? "?")
            .ToArray();

        references.Should().BeEmpty("Bridgeon.Core sits at the bottom of the stack");
    }

    private static FileInfo RepoFile(string relative)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
            dir = dir.Parent;
        dir.Should().NotBeNull("the repository root should be above the test binaries");
        var file = new FileInfo(Path.Combine(dir!.FullName, relative));
        file.Exists.Should().BeTrue("{0} should exist", relative);
        return file;
    }
}
