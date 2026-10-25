# GymRat

**Custom gym cooldowns, hideout recovery crafts, exact buff timers, and active combat resets.**

In vanilla Tarkov, the hideout gym imposes an artificial 24-hour wall (`Severe Muscle Pain`) after every workout. You are left waiting around with zero in-game methods to accelerate recovery, and the game only mocks you with a vague hover tooltip: *"Less than a day left"*.

**GymRat** turns gym training into an engaging, dynamic part of your gameplay loop:
- **Tired of the 24-hour wait?** Tune the passive reset timer to whatever matches your raid rhythm (from 1 minute to 1 week).
- **No more blurry tooltips:** Replaces vanilla's vague *"Less than a day left"* hover text on chest muscle pain with exact hours and minutes remaining.
- **Worked up a sweat in raid?** Clear fatigue immediately through **Active Recovery** by eliminating a boss or securing 5+ PMC kills.
- **Utilize your Hideout:** Take a warm **Recovery Shower** in the Lavatory to wash away soreness, or prepare a targeted **Nutrition Regime** in the Nutrition Unit to accelerate recovery 6x.
- **Optional Food Resets:** Optionally configure food and beverage items (like Norvinsky Kvass) to reset your cooldown on consumption.

## Details {.tabset}

### Overview

#### Exact Health Effect Timers (BepInEx Client)

Vanilla EFT gives you no way to know whether your muscle pain has 23 hours or 10 minutes left—it just displays a blurry `"Less than a day left"`.

GymRat's client plugin patches the character health screen buff display to calculate and show the **exact remaining time**:
- **Chest Buff Tooltip:** Shows exact hours and minutes (e.g. `Severe muscle pain (14 hours 25 minutes left)` or `(42 minutes left)`).
- **Hideout Craft UI:** Adds custom visual headers, descriptive tooltips, and dedicated action buttons (**"TAKE SHOWER"** / **"START REGIME"**) directly into the Lavatory and Nutrition Unit production panels.

#### Hideout Recovery Crafts

Instead of waiting passively, you can use your Hideout stations to recover:

- **Recovery Shower (Lavatory lvl 3):**
  - Wash away workout fatigue completely using **Soap**, **Shampoo**, **Superwater**, and a **Fleece** towel.
  - *Cleans your PMC and completely wipes the muscle pain debuff on completion.*
  - **You keep your towel:** The Fleece fabric is returned to your inventory as the craft end-product!
- **Nutrition Regime (Nutrition Unit lvl 1):**
  - Prepare a targeted recovery meal using **Hot Rod**, **Multivitamins**, **Beef Stew**, and a **Zibbo** lighter.
  - *Accelerates recovery by 6x (configurable), cutting a 24-hour soreness down to just 4 hours.*
  - **You keep your lighter:** The Zibbo lighter is returned to your inventory as the craft end-product!

#### Active Combat Recovery

Turn high-stakes raids into gym resets! Pushing high-intensity combat rewards your PMC with full muscular recovery:

- **Boss Elimination:** Eliminating any Scav Boss, Rogue Boss, or Cultist Priest instantly clears workout fatigue post-raid.
- **PMC Slayer:** Securing 5 or more PMC kills in a single raid resets your gym cooldown.
- Surviving the raid is required to get the active recovery.

#### Consumable Food Resets (Optional)

If you prefer consumable resets, you can specify food or beverage items that immediately clear muscle pain when eaten or drank in the hideout:
- **Deactivated by default:** The default configuration ships with an empty list (`[]`) so you can experience the hideout crafts and active combat recovery as intended.
- **Food & Drink Only:** Only consumable food and drink items handled through the hideout consumption route (`Eat`) are supported (e.g. Norvinsky Kvass). Medical stims or injectors are not supported by this route.
- An example snippet with Norvinsky Kvass is provided commented out in `config.jsonc`.

#### Safe to Install & Clean Profile

- **Zero Profile Tampering:** Never injects custom or non-vanilla JSON fields into your `pmc.json` profile save.
- **Vanilla Invariants:** Cooldown resets and debuff removals only touch standard vanilla EFT health effect dictionaries (`Chest.Effects`).
- Safe to add to or remove from an existing save at any time.

---

### Configuration

Edit `user/mods/GymRat/config.jsonc`.

Out-of-range values are automatically clamped to safe boundaries with a note in the server log. If the configuration file cannot be parsed due to invalid syntax, the mod logs an error and safely disables itself instead of crashing your server.

#### 1. Core & Passive Cooldown

| Setting | Default | Range | What it does |
|---|---|---|---|
| `enabled` | `true` | `true` / `false` | Master switch. Disables the mod without uninstalling. |
| `gymResetMinutes` | `1440` | `1` – `10080` (1 week) | Minutes until workout muscle pain wears off naturally. `1440` = vanilla (24 h), `60` = 1 hour, `10` = rapid workout cycles. |

#### 2. Hideout Recovery Crafts

| Setting | Default | Range | What it does |
|---|---|---|---|
| `hideoutCraftsEnabled` | `true` | `true` / `false` | Master switch for both custom recovery crafts in the Hideout. |
| `showerCraftMinutes` | `30` | `1` – `10080` | Duration in minutes for the Recovery Shower craft. |
| `showerLavatoryLevel` | `3` | `1` – `3` | Required Lavatory station level to unlock the shower craft. |
| `showerRequireFir` | `true` | `true` / `false` | Require Found-in-Raid ingredients. Returns the reward as FIR. |
| `showerIngredients` | Soap, Shampoo, Superwater, Fleece | List of Item IDs | Item template IDs required for the shower craft (1 unit each). |
| `showerEndProduct` | Fleece (`5e2af477...`) | 24-char Hex ID | Item returned to the player upon taking the shower (returns your towel). |
| `nutritionCraftMinutes` | `15` | `1` – `10080` | Duration in minutes for the Nutrition Regime craft. |
| `nutritionUnitLevel` | `1` | `1` – `3` | Required Nutrition Unit level to unlock the craft. |
| `nutritionReductionFactor` | `6.0` | `1.0`+ | Factor dividing remaining soreness duration (e.g. `6.0` turns 24h into 4h). |
| `nutritionRequireFir` | `true` | `true` / `false` | Require Found-in-Raid ingredients for the nutrition meal. |
| `nutritionIngredients` | Hot Rod, Multivitamins, Beef Stew, Zibbo | List of Item IDs | Item template IDs required for the nutrition meal (1 unit each). |
| `nutritionEndProduct` | Zibbo (`56742c2e...`) | 24-char Hex ID | Item returned to the player upon completing the regime (returns your lighter). |

#### 3. Active Combat Recovery

| Setting | Default | Range | What it does |
|---|---|---|---|
| `activeRecoveryEnabled` | `true` | `true` / `false` | Master switch for clearing muscle pain via raid combat achievements. |
| `bossKillResets` | `true` | `true` / `false` | Eliminating any Scav Boss, Rogue Boss, or Cultist clears muscle pain post-raid. |
| `minPmcKillsForReset` | `5` | `0` – `50` (`0` = off) | Minimum PMC kills in a single raid to clear muscle pain. Set to `0` to disable. |
| `requireSurviveForActiveRecovery` | `true` | `true` / `false` | Player must survive or transit from the raid to earn active recovery. |
| `notifyOnActiveRecovery` | `true` | `true` / `false` | Therapist sends an in-game message confirming muscle fatigue was treated. |

#### 4. Consumable Food Resets (Optional)

| Setting | Default | Range | What it does |
|---|---|---|---|
| `resetItems` | `[]` *(empty)* | List of Item IDs | Consuming one of these food/beverage items in hideout resets the gym immediately. **Deactivated by default**. |
| `requireFoundInRaid` | `true` | `true` / `false` | Only Found-in-Raid items trigger the consumable reset when enabled. |

*Note: Consumable resets only work with food and beverage items handled by the game's out-of-raid `Eat` route. Medical stims and injectors are not supported.*

---

### Installation

1. Download the release archive `GymRat-<version>.zip`.
2. Extract the contents directly into your SPT root directory:
   - Server mod lands in: `SPT/user/mods/GymRat/`
   - Client mod lands in: `SPT/BepInEx/plugins/GymRatClient.dll`
3. Launch your SPT Server and Game Client.

**Requirements:**
- SPT **4.1.x**
- BepInEx **5.x**

---

### Compatibility & Alternatives

- **Server Value Modifier (SVM):**
  - If you *only* want to tweak the passive gym timer, SVM already has a slider under **Hideout > Muscle pain duration in minutes** (5–7200 min).
  - You only need GymRat if you want **hideout recovery crafts**, **exact buff tooltips**, **active combat recovery**, **FIR restrictions**, **food resets**, or timers under 5 minutes.
  - **Using both together:** Both mods work harmoniously. Whichever mod loads last during server startup sets the initial passive timer, while GymRat's crafts, exact tooltips, and active recovery continue to function normally.
- **Other Gym Mods:** Any mod altering gym cooldown modifies the same underlying QTE table. Whichever mod executes last determines the default passive cooldown.
- **FIKA (Co-op):** Works normally in co-op sessions.

---

### Credits & Technical Notes

- Built for **SPT 4.1.x** and **.NET 10**.
- For full architecture, route wrapping mechanics, and event hooks, see `docs/TECHNICAL.md`.

{.endtabset}
