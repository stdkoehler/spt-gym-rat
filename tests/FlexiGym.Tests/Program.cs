using FlexiGym.Tests;

// Dependency-free test runner: exit code 0 = all checks passed. package.sh refuses to
// build a release when this fails. Add one *Check class per area and call it here.
static string FindRepoRoot()
{
    foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var dir = new DirectoryInfo(startPath); dir != null; dir = dir.Parent)
        {
            if (dir.GetFiles("*.sln").Length > 0) return dir.FullName;
        }
    }

    return Directory.GetCurrentDirectory();
}

var repoRoot = FindRepoRoot();
var configPath = Path.Combine(repoRoot, "src", "FlexiGym", "config.jsonc");

int pass = 0, fail = 0;

Console.WriteLine("Config:");
var (cp, cf) = ConfigCheck.Run(configPath);
pass += cp; fail += cf;

Console.WriteLine(fail == 0 ? $"\nALL {pass} CHECKS PASSED" : $"\n{fail} FAILED, {pass} passed");
return fail == 0 ? 0 : 1;
