using SPTarkov.Server.Core.Models.Spt.Mod;

namespace FlexiGym;

public record FlexiGymMetadata : IModMetadata
{
    // Must be globally unique and never change after the first release.
    public string ModGuid { get; init; } = "com.styrr.flexigym";
    public string Name { get; init; } = "Flexi Gym";
    public string Author { get; init; } = "Styrr";
    public List<string>? Contributors { get; init; }
    // Bump before running package.sh - the archive name is taken from this line.
    public SemanticVersioning.Version Version { get; init; } = new("0.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    // GUIDs of mods that must not be loaded together with this one.
    public List<string>? Incompatibilities { get; init; }
    // GUID -> version range of mods that must be present.
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }
}
