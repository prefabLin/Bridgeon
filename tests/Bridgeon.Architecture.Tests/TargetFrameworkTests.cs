using System.Xml.Linq;
using FluentAssertions;

namespace Bridgeon.Architecture.Tests;

/// <summary>
/// The target framework is declared in every csproj rather than once in
/// Directory.Build.props, because Stryker's project analysis cannot see a
/// framework that lives only in the props file (decision 0001, amended). What
/// kept the props-file arrangement honest was having a single place to edit;
/// these tests replace that with a single place that cannot drift unnoticed.
/// </summary>
public class TargetFrameworkTests
{
    private const string TheOneTargetFramework = "net10.0";

    [Fact]
    public void EveryProjectDeclaresTheSameTargetFramework()
    {
        foreach (var project in AllProjects())
        {
            var declared = XDocument.Load(project.FullName)
                .Descendants("TargetFramework")
                .Select(e => e.Value)
                .ToArray();

            declared.Should().Equal([TheOneTargetFramework],
                "{0} must declare exactly the repository's one target framework",
                project.Name);
        }
    }

    [Fact]
    public void ThePropsFileDeclaresNoTargetFramework()
    {
        var props = XDocument.Load(RepoFile("Directory.Build.props").FullName);

        props.Descendants("TargetFramework").Should().BeEmpty(
            "a framework declared only in Directory.Build.props is invisible to "
            + "Stryker's project analysis, which silently disables the mutation gate");
        props.Descendants("TargetFrameworks").Should().BeEmpty();
    }

    private static IEnumerable<FileInfo> AllProjects()
    {
        var root = RepoFile("CLAUDE.md").Directory!;
        var projects = root.EnumerateFiles("*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains("StrykerOutput"))
            .ToArray();
        projects.Should().NotBeEmpty();
        return projects;
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
