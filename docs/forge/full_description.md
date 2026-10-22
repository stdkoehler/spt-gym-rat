# Flexi Gym

**Set how long the gym takes to reset, in minutes.**

By default the gym resets 24 hours after a workout. Flexi Gym lets you choose any time from
1 minute to one week.

## Details {.tabset}

### Overview

- **Default:** 1440 minutes, identical to vanilla.
- **Shorter:** e.g. 60 for a one-hour reset, or 10 for near-continuous training.
- **Item reset:** consume kvass (found in raid by default) to reset the gym immediately.

#### Compatibility & Alternatives

- SPT 4.1.x
- FIKA: not tested
- **Other mods changing reset time:** Any mod that modifies gym reset time changes the same QTE muscle pain duration. Whichever mod loads last during server startup determines the passive timer.
- **Server Value Modifier (SVM) as an alternative:** If you already run SVM and *only* want to tweak the passive cooldown, SVM has a built-in setting under **Hideout > Muscle pain duration in minutes** (5–7200 min). Flexi Gym is only needed if you want consumable item resets, FIR restrictions, or timers down to 1 minute.
- **Using both:** Flexi Gym's consumable item reset feature functions normally regardless of load order.

### Configuration

Set `gymResetMinutes` in `config.jsonc` (1 - 10080). `resetItems` lists the item ids that reset the
gym when consumed (default: kvass) and `requireFoundInRaid` controls whether they must be FIR.

### Installation

Extract the archive into your SPT folder so `user/mods/FlexiGym/` exists, then start the server.
