internal class ProjectVersion
{
    public ProjectVersion(int major, int minor, int patch, string preReleaseSuffix = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreReleaseSuffix = preReleaseSuffix;
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    public string PreReleaseSuffix { get; }

    public static ProjectVersion Parse(string version)
    {
        var versionParts = version.Split('.','-');

        return new ProjectVersion(
            int.Parse(versionParts[0]), 
            int.Parse(versionParts[1]), 
            int.Parse(versionParts[2]),
            versionParts.Length > 3 ? versionParts[3] : null);
    }

    public override string ToString()
    {
        string version = $"{Major}.{Minor}.{Patch}";
        return version;
    }
}
