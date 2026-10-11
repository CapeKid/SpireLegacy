namespace SpireLegacy;

/// <summary>Fail closed before any test-profile or family-save access.</summary>
public static class TestIsolation
{
    public static bool Validate(string? mode,string userDirectory,string? dataDirectory,string? root)
    {
        if(string.IsNullOrEmpty(mode)||mode=="0")return false;
        if(mode!="1")throw new InvalidOperationException("HEIR_TEST_MODE must be 1, 0 or unset.");
        if(!System.Text.RegularExpressions.Regex.IsMatch(userDirectory.Replace('\\','/').TrimEnd('/').Split('/').Last(),"^SpireLegacy2PortLab[A-Za-z0-9_-]*$"))
            throw new InvalidOperationException("Test mode requires a dedicated SpireLegacy2PortLab Godot user directory.");
        if(string.IsNullOrWhiteSpace(root)||string.IsNullOrWhiteSpace(dataDirectory)||!Path.IsPathFullyQualified(root)||!Path.IsPathFullyQualified(dataDirectory))
            throw new InvalidOperationException("Test mode requires absolute HEIR_TEST_ROOT and HEIR_DATA_DIR paths.");
        var physicalRoot=PhysicalPath(root);var physicalData=PhysicalPath(dataDirectory);
        var relative=Path.GetRelativePath(physicalRoot,physicalData);
        if(relative=="."||Path.IsPathRooted(relative)||relative==".."||relative.StartsWith(".."+Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Test family directory must be a child of HEIR_TEST_ROOT, including link targets.");
        return true;
    }
    private static string PhysicalPath(string path)
    {
        var full=Path.GetFullPath(path);var current=Path.GetPathRoot(full)!;
        foreach(var part in full[current.Length..].Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar))
        {
            if(part.Length==0)continue;
            current=Path.Combine(current,part);
            var dir=new DirectoryInfo(current);
            if(dir.Exists&&dir.Attributes.HasFlag(FileAttributes.ReparsePoint))current=dir.ResolveLinkTarget(true)!.FullName;
        }
        return Path.GetFullPath(current);
    }
}
