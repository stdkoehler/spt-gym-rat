# GymRat

**Set how long the gym takes to reset, in minutes, and reset it early via items or raid combat.**

By default the gym "resets" 24 hours after a workout (the muscle pain wears off). GymRat lets you
pick any time from 1 minute to one week in `config.jsonc`, prepares custom recovery crafts in the Hideout,
rewards high-intensity raids with **Active Recovery** (eliminating a boss or achieving >= 5 PMC kills clears muscle fatigue,
confirmed by Therapist in-game mail), and replaces vague health buff text with exact remaining hours and minutes.

## Features

- **Custom Passive Gym Timer:** Configure gym reset interval anywhere from 1 minute to 1 week (vanilla default: 24h).
- **Precise Muscle Pain Tooltips (BepInEx Client):** Replaces vanilla's blurry `"Less of a day left"` hover text on chest muscle pain with exact remaining hours and minutes (e.g. `Severe muscle pain (14 hours 25 minutes left)`).
- **Hideout Recovery Crafts:**
  - **Recovery Shower (Lavatory lvl 3):** Clean up with Soap, Shampoo, Superwater, and Fleece to completely wash away muscle pain and soreness. (Returns the Fleece towel).
  - **Nutrition Regime (Nutrition Unit lvl 1):** Prepare a recovery meal with Hot Rod, Vitamins, Beef Stew, and Zibbo to accelerate muscle recovery by 6x (e.g. 24h reduced to 4h). (Returns the Zibbo lighter).
  - Configurable FIR requirements, station levels, craft times, and ingredients.
- **Client UI Enhancement (BepInEx Client):**
  - **Hideout Craft Visuals:** Custom UI headers, clear action buttons ("TAKE SHOWER" / "START REGIME"), and informative tooltips in the Hideout craft menu.
- **Active Combat Recovery:** Clear muscle fatigue by eliminating a boss or securing 5+ PMC kills in a raid (confirmed by Therapist in-game mail).
- **Optional Consumable Food Resets:** Optionally configure food or drink items (like Norvinsky Kvass) to reset muscle pain immediately upon consumption in the hideout (disabled by default).

## Installation

1. Download the release archive `GymRat-<version>.zip`.
2. Extract it into your SPT root installation folder:
   - Server mod: `user/mods/GymRat/`
   - Client mod: `BepInEx/plugins/GymRatClient.dll`
3. Launch the SPT server and client.

Requires **SPT 4.1.x** and **BepInEx 5.x**.

## Configuration

Edit `user/mods/GymRat/config.jsonc` (comments are allowed). Out-of-range values are clamped and
reported in the server log; a file that cannot be parsed disables the mod instead of crashing the server.

| Setting | Default | Range | Meaning |
|---|---|---|---|
| `enabled` | `true` | `true` / `false` | Turn the mod off without uninstalling it. |
| `gymResetMinutes` | `1440` | `1` – `10080` | Minutes until the gym resets naturally (1440 = 24 h). |
| `hideoutCraftsEnabled` | `true` | `true` / `false` | Enable Hideout recovery crafts (Shower & Nutrition). |
| `showerCraftMinutes` | `30` | `1` – `10080` | Craft time in minutes for the Recovery Shower. |
| `showerLavatoryLevel` | `3` | `1` – `3` | Required Lavatory station level for the shower craft. |
| `showerRequireFir` | `true` | `true` / `false` | Require ingredients and reward FIR for Recovery Shower. |
| `showerIngredients` | Soap, Shampoo, Superwater, Fleece | List of IDs | Item template IDs consumed by the shower (1 each). |
| `showerEndProduct` | Fleece (`5e2af477...`) | 24-char Hex ID | Item returned upon shower completion (preserves towel). |
| `nutritionCraftMinutes` | `15` | `1` – `10080` | Craft time in minutes for the Nutrition Regime. |
| `nutritionUnitLevel` | `1` | `1` – `3` | Required Nutrition Unit station level for the nutrition craft. |
| `nutritionReductionFactor` | `6.0` | `1.0`+ | Factor by which Nutrition Regime reduces remaining soreness. |
| `nutritionRequireFir` | `true` | `true` / `false` | Require ingredients and reward FIR for Nutrition Regime. |
| `nutritionIngredients` | Hot Rod, Vitamins, Beef Stew, Zibbo | List of IDs | Item template IDs consumed by nutrition meal (1 each). |
| `nutritionEndProduct` | Zibbo (`56742c2e...`) | 24-char Hex ID | Item returned upon nutrition completion (preserves lighter). |
| `activeRecoveryEnabled` | `true` | `true` / `false` | Enable active gym recovery from intense raid combat. |
| `bossKillResets` | `true` | `true` / `false` | Eliminating any boss clears muscle pain post-raid. |
| `minPmcKillsForReset` | `5` | `0` – `50` | Minimum PMC kills in a raid to trigger recovery (0 disables). |
| `requireSurviveForActiveRecovery` | `true` | `true` / `false` | Player must survive or transit from the raid to earn recovery. |
| `notifyOnActiveRecovery` | `true` | `true` / `false` | Receive an in-game message from Therapist upon active recovery. |
| `resetItems` | `[]` *(empty)* | List of IDs | Item template IDs; consuming one resets gym. Disabled by default. Only food/drink supported. |
| `requireFoundInRaid` | `true` | `true` / `false` | Only found-in-raid items trigger the consumable reset. |

## Compatibility & Alternatives

- **Other mods changing reset time:** Any mod that modifies the gym workout reset duration modifies the same underlying `MusclePain` reward effect in `HideoutTable.Qte`. If multiple mods change this value, whichever mod loads last during SPT server startup will take precedence for the passive cooldown timer.
- **Server Value Modifier (SVM) as an alternative:** If you already use [Server Value Modifier (SVM)](https://github.com/GhostFenixx/svm-csharp) and *only* want to customize the passive reset timer, SVM already has this built-in under **Hideout > Muscle pain duration in minutes** (`5` to `7200` minutes). You do not need GymRat unless you also want:
  - Exact tooltip remaining time display on chest muscle pain debuffs.
  - Custom Hideout recovery crafts (Recovery Shower and Nutrition Regime) with custom UI buttons.
  - Active recovery from intense raid combat (boss kills or >= 5 PMC kills with Therapist in-game notification).
  - Instant cooldown resets by consuming designated food/drink items (e.g. kvass).
  - Found-in-raid restrictions on reset items.
  - Reset intervals shorter than SVM's 5-minute slider minimum (down to 1 minute).
- **Using both together:** If you run both GymRat and SVM (or another mod), GymRat's crafts, exact buff tooltips, and active recovery continue to work normally regardless of load order. Only the passive timer duration will be determined by whichever mod executed last.

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
| `src/GymRat/` | Server mod: entry point, config, metadata, packaging script. |
| `src/GymRat.Client/` | BepInEx client plugin: Hideout craft UI banners, buttons, and tooltips. |
| `tests/GymRat.Tests/` | SPT-free test runner; compiles the pure-logic sources directly. |
| `docs/` | Technical notes and Forge page text. |

## Development

### Prerequisites

- .NET 10 SDK
- Git Bash (for `package.sh`) on Windows

### Build

```bash
dotnet build GymRat.sln
```

Release output lands in `src/GymRat/bin/Release/GymRat/`; copy that folder into `SPT/user/mods/`.

### Test

```bash
cd tests/GymRat.Tests
dotnet run
```

### Package a release

Bump the version in `src/GymRat/GymRatMetadata.cs` , update `docs/forge/changelog.md`, then:

```bash
bash src/GymRat/package.sh
```

This runs the tests, builds Release and writes `src/GymRat/Package/GymRat-<version>.zip` containing
`BepInEx/plugins/GymRatClient.dll` and `SPT_Runtime/user/mods/GymRat/...`.

## License

MIT, see [LICENSE](LICENSE).
