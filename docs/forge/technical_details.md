# Technical Details

## How it works

1. **Passive Workout Cooldown:**
   At server startup, GymRat inspects the hideout Quick-Time-Event table (`hideoutTable.Qte`). It rewrites the duration of the `MusclePain` reward effect (vanilla default: 86,400 seconds / 24 hours) to your configured `gymResetMinutes`.

2. **Hideout Recovery Crafts:**
   - Injects custom craft recipes into the server's `hideoutTable.Production` for the Lavatory (lvl 3) and Nutrition Unit (lvl 1).
   - Wraps the server's `TakeProduction` route (`/client/game/profile/items/moving`) to clear or accelerate muscle soreness upon claiming craft results.
   - On the client side, `GymRatClient.dll` patches `ProduceView` to display custom banners and action buttons (`TAKE SHOWER`, `START REGIME`), while `RecoveryHelper` synchronizes the local `OfflineHealthController` so client UI updates without restarting or reloading the hideout.

3. **Exact Muscle Pain Buff Tooltip:**
   - In vanilla EFT, `BuffDescription.TimeLeftApproximatelyDisplay` groups all durations over 1 hour into `"Less of a day left"`.
   - `GymRatClient.dll` intercepts the getter for muscle pain buffs on the chest and formats exact remaining time (e.g. `Severe muscle pain (14 hours 25 minutes left)`).

4. **Active Combat Recovery:**
   - Wraps the post-raid transition route (`/client/match/local/end`).
   - Parses the raid's kill statistics for boss eliminations (`IsBoss`) and PMC kills (`IsPmc`).
   - If conditions are met and the player survived/transited, removes the chest muscle soreness and triggers a mail message via `MailSendService` from Therapist (Trader ID `54cb57776803fa99248b456e`).

5. **Consumable Item Resets (Optional):**
   - Intercepts the live `Eat` action on `HealthItemEventRouter`.
   - When an item matching `resetItems` is consumed out-of-raid, clears the chest muscle pain effect and updates `output.ProfileChanges[sessionId].Health`.
   - Disabled by default (`[]`). Only consumable food and drink items handled via the `Eat` route are supported.

## Profile Safety & Cleanliness

- **Zero Schema Pollution:** The mod never injects custom fields or non-vanilla JSON structures into your `pmc.json` profile save.
- **Vanilla Health Data:** Cooldown resets and debuff reductions only manipulate the standard EFT health effect dictionary (`pmcData.Health.BodyParts["Chest"].Effects`).
- **Clean Removal:** Uninstalling the mod leaves your profile 100% vanilla compatible with zero leftover data or side effects.

## Performance

- Single-pass QTE table patch at server boot.
- Synchronous route wrappers with zero background polling loops.
- Lightweight client Harmony patches that only execute when viewing the hideout craft panel or health screen.

