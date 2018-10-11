## Koikatu Drag and Drop
Adds drag and drop support to Koikatu, making it possible to load character cards and scenes by dragging them into the game window.

### System Requirements
- BepInEx v4.0 or newer

### Installation
- Copy the included BepInEx folder into the Koikatu folder.

### Usage
To load cards or scenes, drag them from Explorer into the game window.

### Features
- Drag and load character cards into the character editor.
  - Parts of a character (face, eyes, etc.) may be loaded individually by changing the settings at the bottom of the "Load character" window.
  - The sex of the loaded card is automatically converted as needed.
- Drag and load character cards into the scene editor.
  - They are added to the current scene.
  - If a character is selected, it will be replaced by the dropped card.
  - Supports loading multiple characters at the same time.
- Drag and load scenes into the scene editor.
  - The current scene will be replaced, so be sure to save any unsaved work beforehand.
  - Scenes and characters can be loaded at the same time. Scenes are loaded first and characters are loaded second.

### Limitations
- Only supports dragging from certain programs (Explorer, KoiCatalog, etc.).
- Coordinate cards are not supported.