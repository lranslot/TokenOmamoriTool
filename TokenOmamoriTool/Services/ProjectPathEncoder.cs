namespace TokenOmamoriTool.Services;

public static class ProjectPathEncoder
{
    public static string Encode(string absoluteProjectPath)
    {
        return absoluteProjectPath
            .Replace(':', '-')
            .Replace('\\', '-')
            .Replace('/', '-');
    }
}
