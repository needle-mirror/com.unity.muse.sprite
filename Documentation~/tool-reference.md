---
uid: tool-reference
---

# Tool reference

This page describes the UI elements and the keyboard shortcuts for the **Muse Sprite** tool.

## UI elements

The following table describes the various elements of the **Muse Sprite** window:

| UI elements| Description|
| --- | --- |
| Generation| Select the Sprite or Material Generator |
| Images| Set the number of images to be generated every time the Generate button is pressed. |
| Generate | Start generating sprites. Generated sprites will show up in the generations panel. |
| Prompt| Text input used to generate images. |
| Negative Prompt | Text that describes things to exclude from the generated images. |
| Remove Background | Indicates whether the background of the generated sprite should be removed.  |
| Style Strength | Determines how much influence the current style has on the results. The higher the value, the closer the results will look like the current style. (Requires an image or scribble in the Key Image field)|
| Seed| A unique number that can be used to generate a deterministic output.|
| R | Randomizes the seed.  |
| Tightness| Determines how closely the generated sprite’s silhouette follows the silhouette of the Input Image. The higher the value, the closer the results will follow the silhouette. (Requires an image in the Key Image field. Does not support Doodle)|
| Input Image | Image that can be used as input to Sprite generation. See Use as Scribble and Tightness for more information on how this image will influence generation. The buttons in this node are for: <br/> <ul><li>Painting a doodle</li><li>Erasing parts of the doodle </li><li>Clearing the doodle or reference image <li>Picking an image from the scene or the project view</li></ul>|

## Keyboards shortcuts

The following table describes the keyboard shortcuts for **Muse Sprite** tool:

| Keyboard shortcut | Description | 
| --- | --- |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>W</kbd> | Close the **Muse Sprite** window. |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>Space</kbd> | Generate a sprite. |


## Additional resources

* [Get started with Muse Sprite Generator](xref:get-started)
* [Generate sprites](xref:generate)
* [Refine a sprite](xref:refine)
* [Best practices](xref:best-practices)

