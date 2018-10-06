## Koikatu Drag and Drop
Adds drag and drop support for Koikatu, making it possible to load character cards and scenes by dragging them into the game window.

### How to use
Download latest version from releases and put it in your BepInEx folder. BepInEx v4.0 or newer is needed.

To load cards and scenes, drag them from explorer over the main game/studio window.

### Abilities
- Load character cards in character maker.
  - Sex of the card is automatically converted as needed.
  - Partial loading is possible by changing settings under the "Load character" list in maker.
- Load character cards in studio.
  - They are added to the current scene.
  - Can load multiple characters at the same time.
  - If a character is selected, it will be replaced by the dropped card.
- Load scenes in studio.
  - They are loaded as if you used the "Load" command.
  - Can load scenes and characters at the same time. Scene is loaded first, then the characters.

### Limitations
- Only supports dragging from Explorer and programs compatible with dragging to explorer.
- Doesn't support coordinate cards.
