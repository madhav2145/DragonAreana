# AI Usage Note

## Tool Used

GitHub Copilot in Visual Studio Code was used during development. Unity Editor was used to inspect the scene, configure imported assets, and review gameplay behavior.

## How It Was Used

Copilot assisted with implementation and debugging across the player and enemy controllers, ability casting and cooldowns, health and damage feedback, camera behavior, and UI. It also helped trace issues through Unity scene data and serialized ability assets, and supported validation with builds of Unity's generated C# assemblies.

All generated suggestions were reviewed against the project code and Unity configuration. Changes affecting gameplay or scene behavior were checked in the Unity Editor where practical; Copilot was used as an aid, not as a substitute for understanding or testing the implementation.

## Suggestions Reviewed and Corrected

- **Fly Attack and obstacles:** An early implementation moved the dragon directly toward its target with straight-line transform interpolation. That ignored obstacles represented by the baked NavMesh. The movement was changed to require a complete NavMesh path, limit travel to the ability's configured range, and skip landing damage when no valid route exists.
- **Basic Attack fallback:** The first enemy fallback only attempted a basic attack inside the separate `tailRange` threshold. This prevented attacks at distances where the Basic Attack asset itself was in range. The range check was changed to use the asset's `range` value (`5` in the current asset), and the AI now closes distance to that range when needed.
- **Sound volume:** An early diagnosis focused on the click sound's low volume setting. Unity's playback scale is `0` to `1`, so values such as `100` do not provide a valid louder setting. The project now uses a configurable `0` to `1` volume and `PlayOneShot` through the camera AudioSource. If a clip remains quiet at full scale, its source audio or mixer gain still needs adjustment.
- **Pink materials:** The initial advice treated pink materials as a general render-pipeline conversion issue. Inspection showed the project uses URP `17.3.0` while the imported environment pack contains custom shaders. The correction was to identify the exact shader/pipeline mismatch and recommend the pack's matching URP materials or URP-compatible replacements. A complete material conversion was not verified as part of this work, so those assets still need visual validation in the Unity Editor.

## Impact

Copilot accelerated repetitive code changes and helped narrow debugging to the relevant controllers, ability assets, and scene settings. Reviewing the generated code against the actual project and correcting the Fly pathing issue helped keep the final behavior aligned with the game's NavMesh and ability configuration.
