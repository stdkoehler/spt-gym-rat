# Flexi Gym

**Set how long the gym takes to reset, in minutes, and reset it early via items or raid combat.**

By default the gym resets 24 hours after a workout. Flexi Gym lets you choose any time from
1 minute to one week, consume items to reset immediately, or earn active recovery through high-intensity raids.

## Details {.tabset}

### Overview

- **Default:** 1440 minutes, identical to vanilla.
- **Shorter:** e.g. 60 for a one-hour reset, or 10 for near-continuous training.
- **Active recovery:** eliminate any boss or >= 5 PMCs in a raid (survive required by default) to clear muscle fatigue, confirmed with an in-game mail message from Therapist.
- **Item reset:** consume kvass (found in raid by default) in the hideout to reset the gym immediately.

#### Compatibility & Alternatives

- SPT 4.1.x
- FIKA: not tested
- **Other mods changing reset time:** Any mod that modifies gym reset time changes the same QTE muscle pain duration. Whichever mod loads last during server startup determines the passive timer.
- **Server Value Modifier (SVM) as an alternative:** If you already run SVM and *only* want to tweak the passive cooldown, SVM has a built-in setting under **Hideout > Muscle pain duration in minutes** (5–7200 min). Flexi Gym is only needed if you want active raid recovery, consumable item resets, FIR restrictions, or timers down to 1 minute.
- **Using both:** Flexi Gym's active recovery and consumable item reset features function normally regardless of load order.

### Configuration

Set `gymResetMinutes` in `config.jsonc` (1 - 10080). `resetItems` lists the item ids that reset the
gym when consumed (default: kvass) and `requireFoundInRaid` controls whether they must be FIR.
`activeRecoveryEnabled`, `bossKillResets`, `minPmcKillsForReset`, `requireSurviveForActiveRecovery`, and `notifyOnActiveRecovery` control combat-based resets and Therapist notifications.

### Installation

Extract the archive into your SPT folder so `user/mods/FlexiGym/` exists, then start the server.
