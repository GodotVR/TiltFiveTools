using Godot;

public partial class T5ToolsFacePlayer : Node
{
    /// <summary>
    /// Player camera
    /// </summary>
    private T5CameraCS _camera;

    /// <summary>
    /// Player origin
    /// </summary>
    private T5OriginCS _origin;

    /// <summary>
    /// Target node
    /// </summary>
    private Node3D _target;

    /// <summary>
    /// Target node (null for parent)
    /// </summary>
    [Export]
    public Node3D Target { get; set; }

    /// <summary>
    /// Allow tilt
    /// </summary>
    [Export]
    public bool Tilt { get; set; }

    /// <summary>
    /// Scale to player
    /// </summary>
    [Export]
    public bool PlayerScale { get; set; } = true;

    /// <summary>
    /// Rotation rate
    /// </summary>
    [Export]
    public float Rate { get; set; } = 1.0f;

    /// <summary>
    /// Called when the node is "ready"
    /// </summary>
    public override void _Ready()
    {
        // Get the target
        _target = Target ?? GetParentOrNull<Node3D>();
        if (_target == null)
        {
            GD.PushWarning($"T5ToolsFacePlayer<{this}>: No target node found");
            return;
        }

        // Find the player
        var player = FindPlayer();
        if (player == null)
            return;

        // Get the camera and origin
        _camera = player.Camera;
        _origin = player.Origin;

        // Perform the initial facing
        TargetTransform(1.0f);
    }

    /// <summary>
    /// Called during the processing step of the main loop
    /// </summary>
    /// <param name="delta">Delta since previous process in seconds</param>
    public override void _Process(double delta)
    {
        TargetTransform(Rate * (float)delta);
    }

    /// <summary>
    /// Apply the target transform with a slew weight
    /// </summary>
    /// <param name="weight">Slew weight [0=none .. 1=full]</param>
    private void TargetTransform(float weight)
    {
        // Get the camera position local to the target
        var dirLocal = _target.ToLocal(_camera.GlobalPosition);
        if (dirLocal.IsZeroApprox())
            return;

        // Get the old basis
        var bOld = _target.Transform.Basis.Orthonormalized();

        // Construct the new basis looking at the camera
        var bNew = Basis.LookingAt(dirLocal, Vector3.Up, true);

        // If tilt is not permitted then snap the Y to vertical
        if (!Tilt)
        {
            bNew.Y = Vector3.Up;
            bNew.Z = bNew.X.Cross(Vector3.Up);
            bNew.X = Vector3.Up.Cross(bNew.Z);
            bNew = bNew.Orthonormalized();
        }

        // Blend based on weight
        bNew = bOld.Slerp(bNew, weight);

        // If player-scaled then scale to counteract origin scale
        if (PlayerScale)
        {
            var scale = _origin.GameboardScale;
            bNew = bNew.Scaled(new Vector3(scale, scale, scale));
        }

        // Set the target transform
        _target.Transform = _target.Transform with { Basis = bNew };
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
        GD.PushWarning($"T5ToolsFacePlayer<{this}>: No player found");
        return null;
    }
}