namespace QPOPs2
{
    public class XML2JTConfiguration
    {
        public bool IncludeBranchesWithoutCAD { get; set; } = false;

        public bool ResourceSysRootJTFilesAreAssemblies { get; set; } = true;

        required public string SysRootPath { get; set; }

        public bool IncludeProduct { get; set; } = true;
        public bool IncludeResource { get; set; } = true;

        public Dictionary<string, string> AdditionalAttributes { get; set; } = new();

        required public string RootName { get; set; }
    }
}
