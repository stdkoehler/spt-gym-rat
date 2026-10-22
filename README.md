# Flexi Gym

**Set how long the gym takes to reset, in minutes, and reset it early via items or raid combat.**

By default the gym "resets" 24 hours after a workout (the muscle pain wears off). Flexi Gym lets you
pick any time from 1 minute to one week in `config.jsonc`, optionally lets the player consume
an item (kvass by default) in the hideout to reset it at once, and rewards high-intensity raids with
**Active Recovery** (eliminating a boss or achieving >= 5 PMC kills clears muscle fatigue, confirmed by a message from Therapist).

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
| `resetItems` | `["5e8f3423fd7471236e6e3b64"]` | Item template ids; consuming one resets the gym immediately. Default is kvass. Empty list turns the feature off. |
| `requireFoundInRaid` | `true` | Only found-in-raid items trigger the reset. |
| `activeRecoveryEnabled` | `true` | Enable active gym recovery from intense raid combat. |
| `bossKillResets` | `true` | Eliminating any boss clears muscle pain post-raid. |
| `minPmcKillsForReset` | `5` | Minimum PMC kills required in a raid to trigger recovery (0 disables). |
| `requireSurviveForActiveRecovery` | `true` | Player must survive or transit from the raid to earn active recovery. |
| `notifyOnActiveRecovery` | `true` | Receive an in-game message from Therapist when active recovery triggers. |

## Compatibility & Alternatives

- **Other mods changing reset time:** Any mod that modifies the gym workout reset duration modifies the same underlying `MusclePain` reward effect in `HideoutTable.Qte`. If multiple mods change this value, whichever mod loads last during SPT server startup will take precedence for the passive cooldown timer.
- **Server Value Modifier (SVM) as an alternative:** If you already use [Server Value Modifier (SVM)](https://github.com/GhostFenixx/svm-csharp) and *only* want to customize the passive reset timer, SVM already has this built-in under **Hideout > Muscle pain duration in minutes** (`5` to `7200` minutes). You do not need Flexi Gym unless you also want:
  - Active recovery from intense raid combat (boss kills or >= 5 PMC kills with Therapist in-game notification).
  - Instant cooldown resets by consuming items (e.g. kvass, energy drinks, stims).
  - Found-in-raid restrictions on reset items.
  - Reset intervals shorter than SVM's 5-minute slider minimum (down to 1 minute).
- **Using both together:** If you run both Flexi Gym and SVM (or another mod), Flexi Gym's item-reset and active recovery features continue to work normally regardless of load order. Only the passive timer duration will be determined by whichever mod executed last.

## Is it safe to add to an existing profile?

Yes. At startup the mod only changes the duration in the in-memory hideout QTE table. The only
profile write is removing the chest muscle pain effect when a reset item is consumed or when
active recovery triggers, which is standard vanilla EFT health data. It never injects custom properties
into `pmc.json`. Muscle pain already on a profile keeps its old timer; new workouts use the new one.
Removing the mod leaves profiles 100% clean and vanilla-compatible.

## Known quirks

- The item reset wraps the server's `Eat` route and active recovery hooks `MatchStaticRouter` at startup.
  If SPT changes its internals the mod logs an error and keeps the configurable timer working without them.

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
