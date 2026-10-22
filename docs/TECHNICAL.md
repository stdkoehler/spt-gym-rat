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
`SevereMusclePain` from the Chest. It also syncs the cleared effects directly into the router's
response (`output.ProfileChanges[sessionId].Health`) so the client paperdoll and debuff icons update
immediately in real time without waiting for a subsequent health poll. It sets the init-only
`ItemRouteAction.Action` via reflection and fails soft: any problem logs an error and leaves the timer
feature intact.

### Active recovery (raid combat)
Active recovery hooks the `OnAfterAction` event of `MatchStaticRouter` for `"/client/match/local/end"`.
`HttpServer` (singleton) is used to locate the active `MatchStaticRouter` instance through `SptHttpListener` -> `HttpRouter`.
When a raid ends:
1. Verifies the raid was a PMC session (not Scav) and verifies player survival / transit (`IsPlayerSurvived() || IsMapToMapTransfer()`) if `RequireSurviveForActiveRecovery` is enabled.
2. Inspects `raidEnd.Results.Profile.Stats.Eft.Victims` using `BotHelper` to identify boss eliminations (`botHelper.IsBotBoss`) and PMC eliminations (`botHelper.IsBotPmc` or side `Usec`/`Bear`).
3. If criteria are met (`BossKillResets` on boss kill or `MinPmcKillsForReset > 0` reached) and the player has active `MildMusclePain` or `SevereMusclePain` on their chest, muscle pain is removed from `pmcData.Health` and saved to disk via `SaveServer.SaveProfileAsync`.
4. If `NotifyOnActiveRecovery` is enabled, `MailSendService.SendDirectNpcMessageToPlayer` delivers an in-game dialogue message from Therapist (`Traders.THERAPIST`) explaining that intense physical combat has flushed lactic acid and cleared muscle fatigue.

### Compatibility & load order
Any mod modifying gym reset times (such as Server Value Modifier / SVM via `Hideout.MusclePainTime`) writes to the same `HideoutTable.Qte` template values. Whichever mod runs its `OnLoad` last sets the effective passive timer duration. Flexi Gym's item consumption hook (`HookEatRoute`) and active recovery hook (`HookRaidEnd`) are completely decoupled from this and continue to clear muscle pain regardless of load order.

For players who only wish to adjust the passive cooldown and already use SVM, SVM serves as an alternative without needing Flexi Gym. Flexi Gym differentiates itself through:
- Active recovery earned through boss kills or high PMC kill raids with lore-friendly in-game notification.
- Dynamic item-based resets in hideout (with FIR filtering).
- Lower minimum timer thresholds (down to 1 minute vs. SVM's 5-minute slider minimum).

## 3. Invariants

- The only profile write is removing `MildMusclePain` / `SevereMusclePain` from `pmcData.Health.BodyParts["Chest"].Effects` (either via consumed reset item or post-raid active recovery). Never inject custom properties into `pmc.json`.
- A bad config disables the mod; out-of-range values are clamped (`ConfigCheck`).

## 4. Testing

`tests/FlexiGym.Tests` covers config parsing, defaults and clamping. The table patch and router hooks can only be
verified in a running server: check the `[FlexiGym]` log line and train in the gym.
