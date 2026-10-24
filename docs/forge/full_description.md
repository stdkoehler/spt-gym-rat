# GymRat

**Set how long the gym takes to reset, in minutes, and reset it early via items or raid combat.**

By default the gym resets 24 hours after a workout. GymRat lets you choose any time from
1 minute to one week, consume items to reset immediately, or earn active recovery through high-intensity raids.

## Details {.tabset}

### Overview

- **Default:** 1440 minutes, identical to vanilla.
- **Shorter:** e.g. 60 for a one-hour reset, or 10 for near-continuous training.
- **Hideout Recovery Crafts:**
  - **Recovery Shower (Lavatory lvl 3):** Shampoo + Superwater + Fleece -> completely wipes muscle soreness (returns Fleece).
  - **Nutrition Regime (Nutrition Unit lvl 1):** Hot Rod + Vitamins + Beef Stew + Zibbo -> accelerates recovery by 6x (returns Zibbo).
  - Includes a lightweight BepInEx client mod providing in-game craft banners, custom action buttons ("TAKE SHOWER" / "START REGIME"), and tooltips.
- **Active recovery:** eliminate any boss or >= 5 PMCs in a raid (survive required by default) to clear muscle fatigue, confirmed with an in-game mail message from Therapist.
- **Item reset:** consume kvass (found in raid by default) in the hideout to reset the gym immediately.

#### Compatibility & Alternatives

- SPT 4.1.x
- BepInEx 5.x
- FIKA: not tested
- **Other mods changing reset time:** Any mod that modifies gym reset time changes the same QTE muscle pain duration. Whichever mod loads last during server startup determines the passive timer.
- **Server Value Modifier (SVM) as an alternative:** If you already run SVM and *only* want to tweak the passive cooldown, SVM has a built-in setting under **Hideout > Muscle pain duration in minutes** (5–7200 min). GymRat is only needed if you want recovery crafts, active raid recovery, consumable item resets, FIR restrictions, or timers down to 1 minute.
- **Using both:** GymRat's hideout crafts, active recovery, and consumable item reset features function normally regardless of load order.

### Configuration

See `config.jsonc` to configure gym reset timer, craft toggles, FIR requirements, acceleration factor (default: 6x), active combat recovery, and consumable item IDs.

### Installation

Extract the archive into your SPT root folder so that:
- Server mod goes to `user/mods/GymRat/`
- Client mod goes to `BepInEx/plugins/GymRatClient.dll`
Then start the server and client.
