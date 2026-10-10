# The mod implementation

Builds against **SPT 4.1.x** / .NET 10.

| File | Role |
|---|---|
| `FlexiGym/FlexiGymMod.cs` | `IOnLoad` entry point. Loads config and rewrites the muscle-pain duration in the hideout QTE table. |
| `FlexiGym/Config.cs` | Config (`enabled`, `gymResetMinutes`) with range clamping. No SPT dependency, so tests compile it. |
| `FlexiGym/FlexiGymMetadata.cs` | SPT mod manifest (GUID, version, SPT range, incompatibilities). |
| `FlexiGym/config.jsonc` | User-editable settings, shipped next to the DLL. |
| `FlexiGym/package.sh` | Test -> Release build -> zip in `SPT_Runtime/user/mods/` layout. |

## Conventions worth keeping

- **Config is parsed by hand** (`System.Text.Json` with comment/trailing-comma support) so `config.jsonc` can be documented inline.
- **`Sanitise()` clamps, never throws.** Every correction becomes a log warning.
- **Code defaults equal shipped defaults.** `tests/FlexiGym.Tests/ConfigCheck.cs` enforces it.
- **Failures disable, never crash.** `OnLoadAsync` returns early on a bad config.
- **Keep logic SPT-free where possible** (a `Foo.cs` with plain types), and link it into the test project with a `<Compile Include>`; only the `*Mod.cs` file touches SPT services.
- **Log with a `[FlexiGym]` tag** so lines are greppable in the server log.
- **Data files** (large JSON tables) live in `src/data/` and are linked into the output with `<Content Include=... Link=...>` in the csproj.

## Adding a feature

1. Add the setting to `Config.cs` (property, default, clamp in `Sanitise()`).
2. Document it in `config.jsonc` and `README.md`; the config test fails if defaults drift.
3. Put the logic in its own SPT-free class, add a `*Check.cs` under `tests/FlexiGym.Tests/` and call it from `Program.cs`.
4. Wire it into `FlexiGymMod.cs`.
5. Add an entry to `docs/forge/changelog.md`.
