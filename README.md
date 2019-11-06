### Warning: This plugin has been superseeded by [IllusionMods/DragAndDrop](https://github.com/IllusionMods/DragAndDrop) and is no longer actively supported. Use that version instead.

## Koikatu Drag and Drop
Adds drag and drop support to Koikatu, making it possible to load character cards and scenes by dragging them into the game window.

### System Requirements
- BepInEx v4.0 or newer

### Installation
- Download the latest version from the [releases](https://github.com/im-mi/KoikatuDragAndDrop/releases) page.
- Extract it.
- Copy the included DragAndDrop.dll file into your `Koikatu\BepInEx` folder.

### Usage
To load cards or scenes, drag them from Explorer into the game window.

### Features
- Drag and load character cards into the character editor.
  - Parts of a character (face, eyes, etc.) may be loaded individually by changing the settings at the bottom of the "Load character" window.
  - The sex of the loaded card is automatically converted as needed.
- Drag and load character cards into the scene editor.
  - They are added to the current scene.
  - If a character is selected, it will be replaced by the dropped card. Hold shift while dropping to always add new character, not replace.
  - Supports loading multiple characters at the same time.
- Drag and load scenes into the scene editor.
  - The current scene will be replaced, so be sure to save any unsaved work beforehand. Hold shift while dropping to import the new scene(s), not replace.
  - Load multiple scenes at the same time by holding Shift (they get imported).
  - Scenes and characters can be loaded at the same time. Scenes are loaded first and characters are loaded second.

### Limitations
- Only supports dragging from certain programs (Explorer, KoiCatalog, etc.).
- The game can not be running as administrator.
- Coordinate cards are not supported.

### More Information
[Koikatu Drag and Drop on GitHub](https://github.com/im-mi/KoikatuDragAndDrop)
