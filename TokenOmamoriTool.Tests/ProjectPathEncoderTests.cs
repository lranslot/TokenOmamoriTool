using TokenOmamoriTool.Services;

namespace TokenOmamoriTool.Tests;

public class ProjectPathEncoderTests
{
    [Fact]
    public void Encode_WindowsPath_MatchesObservedClaudeCodeEncoding()
    {
        // Encoding rule verified against a real ~/.claude/projects/ folder name.
        var result = ProjectPathEncoder.Encode(@"c:\Users\foo\source\repos\SampleProject");

        Assert.Equal("c--Users-foo-source-repos-SampleProject", result);
    }

    [Fact]
    public void Encode_UnixStylePath_ReplacesSlashesWithDash()
    {
        var result = ProjectPathEncoder.Encode("/Users/foo/dev/my-project");

        Assert.Equal("-Users-foo-dev-my-project", result);
    }
}
