# Thailand Party — Multiplayer Framework

## Architecture

The framework is data-driven and split into Core, Networking, Players, MiniGames, Match, Settings, Persistence, Input, and focused UI controllers. `00_Bootstrap` creates the persistent `ManagersRoot`; later scenes contain only scene-specific presentation and controllers. Persistent services reject duplicate initialization.

## Networking stack

- Netcode for GameObjects 2.7.0
- Unity Transport 2.7.4 (resolved dependency)
- Unity Multiplayer Services 1.2.0 Sessions API
- Unity Authentication anonymous sign-in
- Relay host/client topology; no dedicated server

`SessionManager` initializes services, signs in anonymously, creates or joins an MPS Session, and uses `WithRelayNetwork`. Public rooms are queryable; private rooms are excluded from queries but remain joinable by code. Session properties contain compact string IDs/state only—never Unity object references.

## Scene flow

`00_Bootstrap → 01_MainMenu → 02_Lobby → 03_MiniGameRandomizer → MiniGame_* → 04_Results`.

The host owns room state, MiniGame selection, NGO scene changes, result reporting, wins, and match completion. Clients only display synchronized state or submit requests.

## Managers

- `SceneFlowManager`: local and NGO scene transitions.
- `SessionManager`: UGS initialization, Sessions/Relay, room metadata, public query, join code, kick/ban hooks, and idempotent cleanup.
- `MiniGameManager`: authoritative selection by registry ID and selected scene loading.
- `MatchManager`: authoritative standings, result broadcast, and wins-required completion.
- `SettingsManager`: audio/graphics application, unique resolutions, persistence.
- `InputManager`: New Input System lifecycle and binding override save/reset/rebind.
- `SaveManager`: encapsulated PlayerPrefs profile/settings storage.

## Room creation, visibility, and joining

Create Room validates name, capacity (2–10), wins, and at least one MiniGame ID. Public rooms appear in controlled browser refreshes and have a join code. Private rooms do not appear in queries but accept their join code. Joining is allowed only while `WaitingInLobby`. Backend failures are logged in detail and surfaced through concise UI messages.

## Kick, ban, and disconnects

Only the host-side `SessionManager` invokes removal. UI visibility is not treated as authority. The room ban set is in-memory and lasts only for the active host-owned room; stable Authentication player IDs are the intended keys. Host departure closes the room—there is no host migration—and remaining clients return to Main Menu. Cleanup is safe to call repeatedly.

## Adding a MiniGame

1. Duplicate `Data/MiniGames/MiniGame_Template.asset` and assign a unique ID, display name, thumbnail, and scene name.
2. Duplicate `Scenes/MiniGames/MiniGame_Template.unity` as `MiniGame_Example.unity`.
3. Put gameplay under `[MINIGAME]`, add spawn points, and derive its authority controller from `MiniGameController`.
4. Add the scene to Build Settings and the definition to `MiniGameRegistry`.
5. Report the winner on the server. Core selection code requires no edits.

## Adding a character

1. Create a `CharacterDefinition` under `Data/Characters`.
2. Assign a stable ID, display name, thumbnail, and optional character prefab.
3. Add it to `CharacterRegistry`.
4. Character cards use the registry; selected IDs persist locally and synchronize through `NetworkPlayer`.

## Replacing placeholders

Assign new thumbnails/prefabs directly in the definition assets. Reusable UI entry prefabs are under `Prefabs/UI`; editing one is the intended way to update every future generated entry.

## Default room settings

Edit `Data/Config/GameConfig.asset`. It owns default capacity, minimum players, wins limits, name limits, scene names, and development logging. Runtime `RoomSettingsData` is separate from designer assets.

## Settings and input

Master/Music/SFX values are converted from linear slider values to logarithmic AudioMixer dB. Graphics supports V-Sync, fullscreen/windowed, and de-duplicated monitor resolutions. Input uses `Input/PartyGameInput.asset`; rebinding overrides are saved as JSON and can be reset. Pause uses unscaled UI and never changes `Time.timeScale`.

## Important Inspector references

- `GameConfig.asset`
- `MiniGameRegistry.asset`
- `CharacterRegistry.asset`
- `ManagersRoot.prefab` (`NetworkManager` player prefab, manager data references)
- `NetworkPlayer.prefab`
- UI controller references in Main Menu, Lobby, Randomizer, and Results scenes

Use **Party Game → Build or Repair Framework** to regenerate baseline assets and **Party Game → Validate Registries and Scenes** to audit required assets.

## Testing multiplayer

Multiplayer Play Mode 1.3.3 is installed. A Unity Cloud project must be linked and Authentication, Sessions/Lobby, and Relay enabled.

1. Open **Window → Multiplayer → Multiplayer Play Mode** and activate Player 2.
2. Open `Scenes/Core/00_Bootstrap.unity` and enter Play Mode.
3. In Player 1, use **START**, select at least one MiniGame, and create a public or private room.
4. In Player 2, use **JOIN**. Select the public-room entry or enter the displayed Join Code.
5. Confirm both profiles appear in Lobby. The host can edit settings, Start, Kick, or Ban the other entry.
6. Start the match. Both players should see the authoritative randomizer result and load `MiniGame_Template`.
7. Use the host-only **DEVELOPMENT TEST UI** winner button. Both players should load Results with identical wins.
8. With Wins Required set above one, use **CONTINUE** to repeat. At the threshold, Results displays **MATCH WINNER** and Continue returns to Lobby.
9. To test host disconnect, have Player 1 leave or stop. Player 2 should return to Main Menu with `Host disconnected. Room closed.`

The editor helper menu **Party Game → Testing** can activate Player 2 and configure the opt-in automated smoke tags. `PartyGameMppmSmokeDriver` is compiled only in the Editor and is inert unless those tags are present. Clear the tags after automated testing.

## Current Completion Status

### Completed

- Dynamic Create Room MiniGame cards, validation, loading state, and modal errors.
- Dynamic public-room browser entries with search, refresh, full-room filtering, and selected-session Join.
- Join Code flow, public/private session behavior, and Relay-backed NGO startup.
- Dynamic Customize character cards, local persistence, and connected profile updates.
- Dynamic Lobby roster, host badges, Kick/Ban controls, and host room-settings editor.
- Audio, Controls, and Graphics category panels; only one category is shown at a time.
- Input rebinding presentation, cancel/reset, saved overrides, and duplicate-binding warning.
- Local Pause navigation without pausing the multiplayer simulation.
- Host-authoritative randomizer presentation and synchronized scene destination.
- Host-only development winner controls in `MiniGame_Template`.
- Dynamic Results rows, stable sorting, last-winner highlight, and host-controlled continuation.
- Host close/removal messages and cleanup paths.

### Tested in this project

- Framework compilation: zero C# compiler errors.
- All six required scenes open with no Missing Script components.
- Bootstrap contains one `NetworkManager`; Unity Transport and `NetworkPlayer` are assigned.
- Main Menu and Create Room were visually exercised at 3840×2160, including empty-name validation.
- A real two-player MPPM run passed: public query, selected-session join, two NGO clients, authoritative Randomizer, synchronized MiniGame scene, winner report, Results scene, and replicated final standings/match completion.
- Both host and Player 2 emitted independent end-to-end PASS markers after the final fix, with no PartyGame/NGO runtime error in that run.

### Requires targeted manual testing

- Join-by-code end to end.
- Kick, Ban, banned-player rejoin rejection, manual client leave, and host-disconnect presentation.
- Host settings changes observed live by Player 2.
- Duplicate-click and network-loss timing cases.
- UI review at 1920×1080 and a smaller 16:9 resolution; the current pass used the Editor Game view at 3840×2160.

### Manual AudioMixer setup

No AudioMixer asset is currently assigned. To enable real mixer routing:

1. Create `Assets/_PartyGame/Audio/Mixer/PartyGameAudioMixer.mixer`.
2. Create child groups `Music` and `SFX` under `Master`.
3. Expose the volume controls exactly as `MasterVolume`, `MusicVolume`, and `SFXVolume`.
4. Assign the mixer asset to `ManagersRoot.prefab → SettingsManager → Audio Mixer`.
5. Route project AudioSources to the appropriate mixer groups.

The sliders already persist and use logarithmic linear-to-dB conversion; without this assignment they save correctly but do not affect routed audio.

## Known limitations

- No host migration.
- No reconnect flow.
- No mid-game joining.
- Ban is room-local and expires with the room.
- The AudioMixer asset/reference and AudioSource routing require the exact manual setup above.
- Kick/Ban/disconnect and join-code paths are implemented but were not end-to-end MPPM tested in this completion pass.
- Results continuation is host-controlled; clients never choose the next scene independently.
- Reusable UI prefabs are functional placeholders rather than final presentation.
- Placeholder UI/art is intentionally replaceable.
- Pause is local UI only; multiplayer simulation continues.

## Final QA Matrix

Final hardening pass performed October 3, 2026. `PASS` below means the behavior was actually observed at runtime; code inspection alone is not counted.

| Scenario | Status | How Tested | Notes |
|---|---|---|---|
| Public Room Join | PASS | Two-player MPPM public query and selected-session join | Host observed two Session members, two NGO clients, and two Lobby rows. |
| Join Code | PASS | Player 2 entered the host's exact Join Code through `JoinCodeInput` and invoked the real Join UI button | Player 2 reached Lobby and connected through NGO; host independently observed both players. |
| Invalid Code Recovery | PASS | Player 2 submitted `INVALID-CODE`, observed loading/button recovery and a user-facing modal, then joined with the valid code without restarting Play Mode | Join panel now subscribes directly to operation failures. |
| Kick | NOT TESTED | — | MPPM became unavailable after the editor restart required to clear its Asset Database refresh counter. |
| Kick then Rejoin | NOT TESTED | — | Requires the blocked Kick runtime scenario. |
| Ban | NOT TESTED | — | Requires a restored two-player MPPM session. |
| Ban Rejoin Rejection | NOT TESTED | — | Requires the blocked Ban runtime scenario. |
| Client Leave | NOT TESTED | — | Implementation remains present; no runtime observation in this pass. |
| Host Leave | NOT TESTED | — | Implementation remains present; no runtime observation in this pass. |
| Host Disconnect during gameplay | NOT TESTED | — | Could not reliably simulate after MPPM startup became unavailable. |
| Settings | NOT TESTED | — | Persistence code exists, but the complete Audio/Graphics runtime regression was not observed. |
| Rebinding | NOT TESTED | — | Presentation and persistence exist; rebind/cancel/reset/reload were not executed in this pass. |
| Customize synchronization | NOT TESTED | — | Connected profile update was not executed in this pass. |
| Randomizer | PASS | Previously completed two-player MPPM core regression | Both players reached the same selected MiniGame and scene. |
| Results | PASS | Previously completed two-player MPPM core regression | Both players received replicated standings. |
| Match Winner | PASS | Wins Required = 1 in the completed end-to-end smoke run | Both players reported the same finished match and standings. |
| 3840×2160 | PASS | Runtime Game-view screenshots and interactive Main Menu/Create Room inspection | Placeholder layout was usable at this resolution. |
| 1920×1080 | NOT TESTED | — | Editor became unavailable before resolution testing. |
| 1280×720 | NOT TESTED | — | Editor became unavailable before resolution testing. |

### Final QA hardening fixes

- Create/Join continuations now check Unity object lifetime after scene transitions, preventing `MissingReferenceException` when Main Menu unloads before an awaited operation resumes.
- Join UI now handles `SessionManager.OperationFailed` locally, guaranteeing its loading overlay is dismissed and its modal is shown for invalid codes and selected-room failures.
- The editor-only MPPM QA driver contains opt-in Join Code, moderation, and leave scenarios. It remains inert unless explicit QA tags are assigned and is excluded from players by `UNITY_EDITOR`.

### Final QA environment limitation

The source assemblies compile successfully through Unity's generated Roslyn response files with no C# diagnostics. Further MPPM execution is pending because, after the required editor restart, direct Unity startup stopped after licensing/project-path initialization and Unity Hub did not reopen the project through the restricted automation context. Reopen `Thailand_Party` from the Unity Hub UI before continuing the remaining `NOT TESTED` rows. Do not reinterpret those rows as passing based on implementation alone.
