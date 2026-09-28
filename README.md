# Dragon Arena

A small Unity 2.5D dragon battle prototype created for the DexHigh Junior Unity Developer technical assessment.

## Project Setup

- **Unity:** `6000.3.9f1`
- **Render pipeline:** Universal Render Pipeline `17.3.0`
- **Navigation:** AI Navigation `2.0.10`
- **Input:** Input System `1.18.0`
- **Main scene:** [SampleScene.unity](Assets/Scenes/SampleScene.unity)

Open the project with the Unity version above, open `SampleScene`, and press Play. The sample scene is already enabled in the build scene list. To make a Windows build, use **File > Build Profiles**, select Windows, and build with `SampleScene` included.

## Controls

- **Right-click ground:** move to the clicked point.
- **Right-click enemy dragon:** approach and basic-attack it.
- **Q:** arm Fire Attack.
- **W:** arm Fly Attack.
- **E:** arm Tail Attack.
- **Left-click after arming:** cast the selected ability toward the clicked point.
- **Escape:** cancel ability targeting.
- Ability icons display the Q/W/E key badges and cooldown overlays.

Movement uses right-click orders rather than WASD. Ability casts use a two-step key-then-click targeting flow.

## Gameplay

The player and enemy have health, three special abilities, and a basic attack. Fire is a forward area attack, Tail is a close-range attack, and Fly travels along a reachable NavMesh path toward the target, up to its configured range, then deals area damage around its landing point. The AI uses an Idle/Chase/Attack state machine, selects abilities by distance, and can fall back to basic attacks. Damage triggers a material flash and floating damage number. When either dragon dies, the Winner Screen identifies the winner and offers a scene restart.

The camera follows its assigned dragon target. The arena uses a baked NavMesh; rebake it after changing obstacle colliders or the arena layout.

## Asset Sources

Assets are imported into the project under `Assets/UnityAssetStore/`:

- **Four Evil Dragons HP**: dragon meshes, prefabs, materials, and animation clips. See [the imported package folder](Assets/UnityAssetStore/FourEvilDragonsHP/).
- **Polytope Studio — Lowpoly Environments**: environment models and prefabs, including the rock, tree, and grass variants used in the arena. See [the imported package folder](Assets/UnityAssetStore/Polytope%20Studio/Lowpoly_Environments/).
- **Leohpaz — RPG Essentials Free**: battle sound effects used by the ability assets. See [the imported package folder](Assets/UnityAssetStore/Leohpaz/RPG_Essentials_Free/).

Scene-ready prefab variants are organized in [Assets/Prefabs](Assets/Prefabs/), and ability data assets are in [Assets/Scriptables](Assets/Scriptables/). The project copy does not include source product URLs or license documents; check the corresponding Unity Asset Store listings for current attribution and redistribution terms before distributing the project or a build.

## Assessment Notes

This repository contains the Unity project source. A Windows build and gameplay video should be produced from the final scene and shared separately with the submission. See [AI_USAGE_NOTE.md](AI_USAGE_NOTE.md) for the required AI workflow disclosure.
