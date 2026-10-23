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

### Hideout recovery crafts
Hideout crafting recipes are injected in-memory into `HideoutTable.Production.Recipes` during `OnLoad`:
- **Recovery Shower (Lavatory lvl 3):** Requires Found-In-Raid ingredients (default: 1x Soap, 1x Shampoo, 1x Canister with purified water (superwater), 1x Fleece fabric; returns 1x Fleece fabric). Completely resets workout muscle pain.
- **Nutrition Regime (Nutrition Unit lvl 1):** Requires Found-In-Raid ingredients (default: 1x Can of Hot Rod energy, 1x Bottle of OLOLO multivitamins, 1x Can of beef stew small, 1x Zibbo lighter; returns 1x Zibbo lighter). Reduces remaining muscle pain cooldown by factor 6 (`Time = Math.Round(Time / 6.0)`).

The live `HideoutTakeProduction` route in `HideoutItemEventRouter` is wrapped via reflection (`HookTakeProductionRoute`). When the player clicks "Get Items", the completed recipe ID is identified:
- The produced items dynamically match the recipe's FIR configuration: if the recipe requires FIR ingredients, the returned item is marked Found-In-Raid; if the recipe does not require FIR, the returned item is non-FIR (preventing unintended infinite craft loops when FIR is off).
- The chest muscle pain effects are cleared or scaled down on `pmcData.Health`.
- The real-time health state is synchronized into `output.ProfileChanges[sessionId].Health` so the client UI updates immediately without needing to exit the hideout or wait for health polling.
- The profile is committed to disk via `SaveServer.SaveProfileAsync`.

### Client-side UI enhancements (`FlexiGymClient.dll`)
To solve the discovery and clarity problem of Hideout recovery crafts without touching profile items or modifying vanilla templates in the database:
- `FlexiGymClient` is a lightweight BepInEx plugin running on the client via Harmony (`src/FlexiGym.Client/`).
- It hooks `ProduceView.Show` and `ProduceView.UpdateButtonsActiveness`.
- When rendering the Recovery Shower or Nutrition Regime recipes:
  - Injects a styled card banner across the top: `[RECOVERY SHOWER] • Resets Workout Muscle Soreness` or `[NUTRITION REGIME] • Accelerates Muscle Recovery (6x faster)`.
  - Replaces the generic `START` button label with `TAKE SHOWER` / `START REGIME` with detailed action tooltips.
  - Updates the `GET ITEMS` button label to `TAKE SHOWER` / `FINISH REGIME`.
  - Updates the result item tile caption to `SHOWER` (cyan) or `NUTRITION` (gold).
- Packaged automatically into `BepInEx/plugins/FlexiGymClient.dll` alongside the server mod in `package.sh`.

### Compatibility & load order
Any mod modifying gym reset times (such as Server Value Modifier / SVM via `Hideout.MusclePainTime`) writes to the same `HideoutTable.Qte` template values. Whichever mod runs its `OnLoad` last sets the effective passive timer duration. Flexi Gym's item consumption hook (`HookEatRoute`), active recovery hook (`HookRaidEnd`), and hideout craft hook (`HookTakeProductionRoute`) are completely decoupled from this and continue to operate regardless of load order.

For players who only wish to adjust the passive cooldown and already use SVM, SVM serves as an alternative without needing Flexi Gym. Flexi Gym differentiates itself through:
- In-hideout FIR crafting recipes (Recovery Shower in Lavatory and Nutrition Regime in Nutrition Unit).
- Active recovery earned through boss kills or high PMC kill raids with lore-friendly in-game notification.
- Dynamic item-based resets in hideout (with FIR filtering).
- Lower minimum timer thresholds (down to 1 minute vs. SVM's 5-minute slider minimum).

## 3. Invariants

- The only profile write is removing `MildMusclePain` / `SevereMusclePain` or updating its `Time` in `pmcData.Health.BodyParts["Chest"].Effects`. Never inject custom properties into `pmc.json`.
- A bad config disables the mod; out-of-range values are clamped (`ConfigCheck`).

## 4. Testing

`tests/FlexiGym.Tests` covers config parsing, defaults and clamping. The table patch, hideout recipes, and router hooks can only be
verified in a running server: check the `[FlexiGym]` log line and train in the gym.

