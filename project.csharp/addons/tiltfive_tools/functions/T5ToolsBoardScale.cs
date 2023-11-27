using System;
using Godot;

public partial class T5ToolsBoardScale : Node
{
    /// <summary>
    /// Origin node
    /// </summary>
    private T5OriginCS _origin;

    /// <summary>
    /// Button triggering zoom-in
    /// </summary>
    [Export]
    public string ZoomInButton { get; set; } = "button_a";

    /// <summary>
    /// Button triggering zoom-out
    /// </summary>
    [Export]
    public string ZoomOutButton { get; set; } = "button_y";

    /// <summary>
    /// Minimum zoom level
    /// </summary>
    [Export]
    public float ZoomMin { get; set; } = 4.0f;

    /// <summary>
    /// Maximum zoom level
    /// </summary>
    [Export]
    public float ZoomMax { get; set; } = 20.0f;

    /// <summary>
    /// Zoom step on each button press
    /// </summary>
    [Export]
    public float ZoomStep { get; set; } = 1.2f;

    /// <summary>
    /// Called when the node is "ready"
    /// </summary>
    public override void _Ready()
    {
        // Get the origin
        _origin = T5ToolsPlayer.FindInstance(this).Origin;

        // Bind to the parent wand controller inputs
        var controller = GetParent<T5ControllerCS>();
        controller?.Connect("button_pressed", Callable.From((StringName name) => OnButtonPressed(name)));
    }

    /// <summary>
    /// Handle controller button presses
    /// </summary>
    /// <param name="name">Button name</param>
    private void OnButtonPressed(StringName name)
    {
        // Zoom in
        if (name == ZoomInButton)
        {
            var zoom = _origin.GameboardScale;
            zoom = Math.Clamp(zoom / ZoomStep, ZoomMin, ZoomMax);
            _origin.GameboardScale = zoom;
        }
        else if (name == ZoomOutButton)
        {
            var zoom = _origin.GameboardScale;
            zoom = Math.Clamp(zoom * ZoomStep, ZoomMin, ZoomMax);
            _origin.GameboardScale = zoom;
        }
    }
}