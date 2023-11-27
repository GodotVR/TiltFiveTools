using Godot;

public partial class T5ToolsVisibleToggle : Node
{
    /// <summary>
    /// Initial Visibility
    /// </summary>
    public enum InitialVisibility
    {
        /// <summary>
        /// Hide at start
        /// </summary>
        Hide,

        /// <summary>
        /// Show at start
        /// </summary>
        Show,

        /// <summary>
        /// No change
        /// </summary>
        Default
    }

    /// <summary>
    /// Target node
    /// </summary>
    private Node3D _target;

    /// <summary>
    /// Toggle button (T5 menu button for default)
    /// </summary>
    [Export]
    public string ToggleButton { get; set; } = "button_t5";

    /// <summary>
    /// Target node (null for parent)
    /// </summary>
    [Export]
    public Node3D Target { get; set; }

    /// <summary>
    /// Initial visibility
    /// </summary>
    [Export]
    public InitialVisibility Initial { get; set; } = InitialVisibility.Hide;

    public override void _Ready()
    {
        // Get the target node
        _target = Target ?? GetParentOrNull<Node3D>();
        if (_target == null)
        {
            GD.PushWarning($"T5ToolsVisibleToggle<{this}>: No target node found");
            return;
        }

        // Subscribe to button events
        var wandNode = FindWand();
        wandNode?.Connect("button_pressed", Callable.From((StringName name) => OnButtonPressed(name)));

        // Apply initial visibility
        switch (Initial)
        {
            case InitialVisibility.Hide:
                _target.Visible = false;
                break;

            case InitialVisibility.Show:
                _target.Visible = true;
                break;

            case InitialVisibility.Default:
                // No change
                break;
        }
    }

    /// <summary>
    /// Handle button presses
    /// </summary>
    /// <param name="name">Button name</param>
    private void OnButtonPressed(StringName name)
    {
        // Toggle visibility if toggle button pressed
        if (name == ToggleButton)
            _target.Visible = !_target.Visible;
    }

    /// <summary>
    /// Find the wand node
    /// </summary>
    /// <returns>Wand node or null</returns>
    private T5ControllerCS FindWand()
    {
        // Find the player
        var player = FindPlayer();
        if (player == null)
            return null;

        // Find the wand
        var wandNode = player.Wand;
        if (wandNode != null)
            return wandNode;

        // Report failure
        GD.PushWarning($"T5ToolsVisibleToggle<{this}>: No wand found");
        return null;
    }
    
    /// <summary>
    /// Find the associated player
    /// </summary>
    /// <returns>Player node or null</returns>
    private T5ToolsPlayer FindPlayer()
    {
        // Test if this node is a child of a character
        var character = T5ToolsCharacter.FindInstance(this);
        if (character != null)
            return character.Player;

        // Test if this node is a child of a player
        var player = T5ToolsPlayer.FindInstance(this);
        if (player != null)
            return player;

        // Report failure
        GD.PushWarning($"T5ToolsVisibleToggle<{this}>: No player found");
        return null;
    }
}