# Technical Details

## How it works

1. At server start the mod rewrites the duration of the muscle pain applied after every gym workout
(vanilla 86400 s) in the hideout quick-time-event table. The gym "reset" is that muscle pain.
2. The item consumption hook wraps the live `Eat` route to clear muscle pain immediately when a configured item is consumed.
3. Active recovery hooks the post-raid route (`/client/match/local/end`) to evaluate boss and PMC kills, clearing muscle pain and sending a message from Therapist.

## What it reads and writes

- Reads: `config.jsonc`, the hideout QTE table, post-raid kill statistics.
- Writes: the in-memory QTE table, and removes the vanilla muscle pain effect from `pmcData.Health.BodyParts["Chest"].Effects` upon item consumption or post-raid active recovery. Never adds custom schema properties to profile.

## Performance

Startup table patch and lightweight event routing. No background polling loops.

