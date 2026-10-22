# Technical Details

## How it works

At server start the mod rewrites the duration of the muscle pain applied after every gym workout
(vanilla 86400 s) in the hideout quick-time-event table. The gym "reset" is that muscle pain.

## What it reads and writes

- Reads: `config.jsonc`, the hideout QTE table.
- Writes: the in-memory QTE table, and, when a reset item is consumed, removes the muscle pain effect from that profile's chest.

## Performance

One pass over a handful of table entries at startup; nothing runs afterwards.
