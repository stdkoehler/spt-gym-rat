# Flexi Gym

**Set how long the gym takes to reset, in minutes.**

By default the gym "resets" 24 hours after a workout (the muscle pain wears off). Flexi Gym lets you
pick any time from 1 minute to one week in `config.jsonc`.

## Installation

1. Download the release archive.
2. Extract it into your SPT install so the folder ends up at `SPT/user/mods/FlexiGym/`.
3. Start the server. The log prints a `[FlexiGym] Gym resets every ... min` line when it worked.

Requires **SPT 4.1.x**.

## Configuration

Edit `user/mods/FlexiGym/config.jsonc` (comments are allowed). Out-of-range values are clamped and
reported in the server log; a file that cannot be parsed disables the mod instead of crashing the server.

| Setting | Default | Meaning |
|---|---|---|
| `enabled` | `true` | Turn the mod off without uninstalling it. |
| `gymResetMinutes` | `1440` | Minutes until the gym resets (1 - 10080). 1440 is vanilla (24 h). |

## Is it safe to add to an existing profile?

Yes. The mod only changes the duration in the in-memory hideout QTE table at startup and never
writes to profiles. Muscle pain already on a profile keeps its old timer; new workouts use the new one.

## Repository layout

| Path | Contents |
|---|---|
| `src/FlexiGym/` | The mod: entry point, config, metadata, packaging script. |
| `tests/FlexiGym.Tests/` | SPT-free test runner; compiles the pure-logic sources directly. |
| `docs/` | Technical notes and Forge page text. |

## Development

### Prerequisites

- .NET 10 SDK
- Git Bash (for `package.sh`) on Windows

### Build

```bash
dotnet build spt-flexi-gym.sln
```

Release output lands in `src/FlexiGym/bin/Release/FlexiGym/`; copy that folder into `SPT/user/mods/`.

### Test

```bash
cd tests/FlexiGym.Tests
dotnet run
```

### Package a release

Bump the version in `src/FlexiGym/FlexiGymMetadata.cs` , update `docs/forge/changelog.md`, then:

```bash
bash src/FlexiGym/package.sh
```

This runs the tests, builds Release and writes `src/FlexiGym/Package/FlexiGym-<version>.zip` containing
`SPT_Runtime/user/mods/FlexiGym/...`.

## License

MIT, see [LICENSE](LICENSE).
