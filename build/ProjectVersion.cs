internal class ProjectVersion
{
    public ProjectVersion(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public static ProjectVersion Parse(string version)
    {
        var versionParts = version.Split('.','-');

        return new ProjectVersion(
            int.Parse(versionParts[0]), 
            int.Parse(versionParts[1]), 
            int.Parse(versionParts[2]));
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
