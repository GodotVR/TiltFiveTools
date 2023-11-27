using System.Collections.Generic;
using Godot;

[Tool]
public partial class T5ToolsSceneSwitchArea : Area3D
{
    /// <summary>
    /// Enable flag
    /// </summary>
    [Export]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Target scene
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")]
    public string TargetScene { get; set; } = string.Empty;

    /// <summary>
    /// Target location
    /// </summary>
    [Export]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Called when the node is "ready"
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Do not run if in the editor
        if (Engine.IsEditorHint())
            return;

        // Subscribe to body entered events
        BodyEntered += OnBodyEntered;
    }

    /// <summary>
    /// Gets the configuration warnings with this node
    /// </summary>
    /// <returns>Configuration warnings</returns>
    public override string[] _GetConfigurationWarnings()
    {
        // Construct the warnings
        var warnings = new List<string>();

        // Verify the target
        if (string.IsNullOrEmpty(TargetScene))
            warnings.Add("Target scene must be specified");

        // Return the warnings
        return warnings.ToArray();
    }

    /// <summary>
    /// Handle body entered
    /// </summary>
    /// <param name="body">Body entering this area</param>
    private void OnBodyEntered(Node3D body)
    {
        // Skip if not enabled
        if (!Enabled || string.IsNullOrEmpty(TargetScene))
            return;

        // Disable to prevent repeated notifications
        Enabled = false;

        // Trigger loading the scene
        T5ToolsStaging.LoadScene(TargetScene, Location);
    }
}