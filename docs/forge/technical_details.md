# Technical Details

## How it works

At server start the mod rewrites the duration of the muscle pain applied after every gym workout
(vanilla 86400 s) in the hideout quick-time-event table. The gym "reset" is that muscle pain.

## What it reads and writes

- Reads: `config.jsonc`, the hideout QTE table.
- Writes: the in-memory QTE table only. Profiles are never touched.

## Performance

One pass over a handful of table entries at startup; nothing runs afterwards.
