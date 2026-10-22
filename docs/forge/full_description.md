# Flexi Gym

**Set how long the gym takes to reset, in minutes.**

By default the gym resets 24 hours after a workout. Flexi Gym lets you choose any time from
1 minute to one week.

## Details {.tabset}

### Overview

- **Default:** 1440 minutes, identical to vanilla.
- **Shorter:** e.g. 60 for a one-hour reset, or 10 for near-continuous training.

#### Compatibility

- SPT 4.1.x
- FIKA: not tested
- Known conflicts: other mods that change gym muscle pain duration

- **Item reset:** consume kvass (found in raid by default) to reset the gym immediately.

### Configuration

Set `gymResetMinutes` in `config.jsonc` (1 - 10080). `resetItems` lists the item ids that reset the
gym when consumed (default: kvass) and `requireFoundInRaid` controls whether they must be FIR.

### Installation

Extract the archive into your SPT folder so `user/mods/FlexiGym/` exists, then start the server.
