namespace Unity.Muse.Sprite.UIComponents
{
    static class TextContent
    {
        public static readonly string undoTooltip = "Undo";
        public static readonly string redoTooltip = "Redo";
        public static readonly string saveTooltip = "Save selected generation(s) into project.";
        public static readonly string refineTooltip = "Refine image in canvas.";

        public static readonly string doodleStartTooltip = "Scribble or Drag & Drop reference image here.";
        public static readonly string doodleBrushTooltip = "Scribble Tool. (B)\n'[' or ']' decreases/increases brush size.";
        public static readonly string doodleEraserTooltip = "Erases the scribbles. (E)\n'[' or ']' decreases/increases brush size.";
        public static readonly string doodleTooltipDisabled = "Clear reference image to use Scribble/Eraser Tools";
        public static readonly string doodleClearTooltip = "Clear Tool clears all the scribbles and/or image reference.";
        public static readonly string doodleSelectorTooltip = "Sprite Picker Tool picks sprites from the Scene view or the Project window.";
        public static readonly string styleSelectionTooltip = "Style used for generation. Train new styles in Menu > Muse > Style Trainer.";

        public static readonly string operatorPromptTooltip = "Generates images based on text.";
        public static readonly string operatorNegativePromptTooltip = "Generates images excluding from this text.";
        public static readonly string operatorStyleTooltip = "Influences the style of the generation.";
        public static readonly string operatorStrengthTooltip = "Determines how much influence the selected style and prompts have on the results. The higher the value, the closer the results will look like the selected style and prompts.";
        public static readonly string operatorTightnessTooltip = "Determines how closely the generation follows the outline of the scribble or image reference or the mask. The higher the value, the closer the generations take teh shape of the reference outlines.";
        public static readonly string operatorRemoveBackgroundTooltip = "Removes the background from the selected image.";

        public static readonly string controlMaskBrushToolTooltip = "Masks out areas for regenerations.";
        public static readonly string controlMaskEraserToolTooltip = "Erases parts of the masks.";
        public static readonly string controlMaskClearToolTooltip = "Clears all the masks.";
        public static readonly string backButtonTooltip = "Snaps back the Generations Tray, hides the canvas and exits Refinement mode.";
        public static readonly string customSeed = "Custom Seed";
        public static readonly string randomSeed = "Random Seed";
    }
}
