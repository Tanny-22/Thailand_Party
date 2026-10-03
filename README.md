# Thailand Party

Unity multiplayer party-game project intended for collaborative development.

## Team setup

- **Project:** Thailand Party
- **Unity version:** 6000.3.24f1

Required workflow:

1. Clone the repository.
2. Open the project through Unity Hub using Unity 6000.3.24f1.
3. Allow Unity Package Manager to restore the packages in `Packages/manifest.json`.
4. Do not commit generated folders such as `Library`, `Temp`, `Logs`, or `Obj`.
5. Always commit each Unity asset together with its matching `.meta` file.
6. Pull the latest shared changes before starting work.
7. Avoid editing the same Scene or Prefab simultaneously where practical.

## Multiplayer stack

The project uses:

- Netcode for GameObjects
- Unity Transport
- Unity Multiplayer Services
- Relay
- Unity Authentication

Photon Fusion assets may exist as isolated third-party reference content, but Fusion is not the active multiplayer backend.

## Team Git workflow

`main` is the stable shared branch. Normal feature work should be performed on a focused branch rather than committed directly to `main`.

Example branch names:

- `feature/minigame-race`
- `feature/ui-mainmenu`
- `fix/lobby-ui`
- `feature/character-controller`

Recommended workflow:

1. Pull the latest `main`.
2. Create a feature or fix branch.
3. Work on one focused area.
4. Commit small, logical changes.
5. Push the branch.
6. Open a Pull Request.
7. Merge after review.
8. Pull the updated `main` before beginning the next task.

## Unity scene and prefab collaboration

- Avoid having two people edit the same `.unity` Scene at the same time.
- Avoid editing the same Prefab simultaneously where possible.
- Prefer Prefabs and ScriptableObjects for modular work.
- Coordinate scene ownership before starting overlapping work.

Possible ownership split:

- **Person A:** Main Menu and Lobby UI
- **Person B:** MiniGames
- **Person C:** Characters and controller
- **Person D:** Networking and services

These are coordination examples only and are not enforced by project code.
