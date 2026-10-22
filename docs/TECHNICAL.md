# Technical documentation

Design notes for contributors. Keep user-facing text in the README and `docs/forge/`.

## 1. The problem

The gym has no server-side timer. The 24 h "reset" is the muscle pain applied when a workout
finishes: `hideout/qte.json` gives the `MusclePain` reward of each gym QTE's `finishEffect`
a `time` of 86400 s, and `HideoutController.HandleMusclePain` copies it onto the profile.

## 2. The approach

### Reset timer
At `OnLoad` (PostLoad) the mod sets `Time = gymResetMinutes * 60` on every `MusclePain` reward in
`HideoutTable.Qte`. The server and the client (which fetches the same table via
`client/hideout/qte/list`) both use the patched value. No update loop.

Because this only modifies the in-memory template constant:
- **No retroactive profile edits:** Muscle pain debuffs applied *prior* to installing the mod retain their original timestamp/duration on the profile.
- **New workouts:** Only workouts completed after the server starts with the mod active will receive the newly configured duration.
- **Clean removal:** Uninstalling the mod simply restores the vanilla constant on next launch; existing profile timers will continue until they naturally expire.

### Item reset
The item reset wraps the live `Eat` route of `HealthItemEventRouter`: `ItemEventCallbacks` uses the
first router that handles an action and `HealthItemEventRouter` is sealed, so a second router or a
subclass cannot take over. The wrapper reads the consumed item (template, `Upd.SpawnedInSession`)
before the original runs (a fully eaten item is gone afterwards), then removes `MildMusclePain` /
`SevereMusclePain` from the Chest. It sets the init-only `ItemRouteAction.Action` via reflection and
fails soft: any problem logs an error and leaves the timer feature intact.

## 3. Invariants

- The only profile write is removing `MildMusclePain` / `SevereMusclePain` from the Chest after a configured reset item is consumed.
- A bad config disables the mod; out-of-range values are clamped (`ConfigCheck`).

## 4. Testing

`tests/FlexiGym.Tests` covers config parsing, defaults and clamping. The table patch can only be
verified in a running server: check the `[FlexiGym]` log line and train in the gym.
