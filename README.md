# Flexi Gym

**Set how long the gym takes to reset, in minutes, and reset it early via items or raid combat.**

By default the gym "resets" 24 hours after a workout (the muscle pain wears off). Flexi Gym lets you
pick any time from 1 minute to one week in `config.jsonc`, optionally lets the player consume
an item (kvass by default) in the hideout to reset it at once, and rewards high-intensity raids with
**Active Recovery** (eliminating a boss or achieving >= 5 PMC kills clears muscle fatigue, confirmed by a message from Therapist).

## Features

- **Custom Passive Gym Timer:** Configure gym reset interval anywhere from 1 minute to 1 week (vanilla default: 24h).
- **Hideout Recovery Crafts:**
  - **Recovery Shower (Lavatory lvl 3):** Clean up with Shampoo, Superwater, and Fleece to completely wash away muscle pain and soreness. (Returns the Fleece).
  - **Nutrition Regime (Nutrition Unit lvl 1):** Prepare a recovery meal with Hot Rod, Vitamins, Beef Stew, and Zibbo to accelerate muscle recovery by 6x (e.g. 24h reduced to 4h). (Returns the Zibbo).
  - Configurable FIR requirements and rewards for both crafts.
- **Client UI Enhancement (BepInEx):**
  - **Hideout Craft Visuals:** Custom UI banners, clear action buttons ("TAKE SHOWER" / "START REGIME"), and informative tooltips in the Hideout craft menu so crafts are intuitive.
  - **Precise Muscle Pain Tooltips:** Replaces vanilla's vague `"Less of a day left"` hover text on chest muscle pain with exact remaining hours and minutes (e.g. `14 hours 25 minutes left`).
- **Active Combat Recovery:** Clear muscle fatigue by eliminating a boss or securing 5+ PMC kills in a raid (confirmed by Therapist in-game mail).
- **Consumable Item Resets:** Optionally consume designated items (like Kvass) in hideout to reset the gym instantly.

## Installation

1. Download the release archive `FlexiGym-<version>.zip`.
2. Extract it into your SPT root installation folder:
   - Server mod: `user/mods/FlexiGym/`
   - Client mod: `BepInEx/plugins/FlexiGymClient.dll`
3. Launch the SPT server and client.

Requires **SPT 4.1.x** and **BepInEx 5.x**.

## Configuration

Edit `user/mods/FlexiGym/config.jsonc` (comments are allowed). Out-of-range values are clamped and
reported in the server log; a file that cannot be parsed disables the mod instead of crashing the server.

| Setting | Default | Meaning |
|---|---|---|
| `enabled` | `true` | Turn the mod off without uninstalling it. |
| `gymResetMinutes` | `1440` | Minutes until the gym resets (1 - 10080). 1440 is vanilla (24 h). |
| `showerCraftEnabled` | `true` | Enable the Recovery Shower craft in Lavatory lvl 3. |
| `showerRequireFir` | `true` | Require ingredients and reward FIR for Recovery Shower. |
| `nutritionCraftEnabled` | `true` | Enable the Nutrition Regime craft in Nutrition Unit lvl 1. |
| `nutritionRequireFir` | `true` | Require ingredients and reward FIR for Nutrition Regime. |
| `nutritionRecoveryAccelerationFactor` | `6.0` | Factor by which Nutrition Regime reduces remaining muscle soreness. |
| `resetItems` | `["5e8f3423fd7471236e6e3b64"]` | Item template ids; consuming one resets the gym immediately. Default is kvass. |
| `requireFoundInRaid` | `true` | Only found-in-raid items trigger the consumable reset. |
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
| `src/FlexiGym/` | Server mod: entry point, config, metadata, packaging script. |
| `src/FlexiGym.Client/` | BepInEx client plugin: Hideout craft UI banners, buttons, and tooltips. |
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
`BepInEx/plugins/FlexiGymClient.dll` and `SPT_Runtime/user/mods/FlexiGym/...`.

## License

MIT, see [LICENSE](LICENSE).
