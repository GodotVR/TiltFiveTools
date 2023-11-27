using Godot;

public partial class T5ToolsSpectatorCamera : Camera3D
{
    /// <summary>
    /// Vertical distance
    /// </summary>
    [Export]
    public float Vertical { get; set; } = 0.5f;

    /// <summary>
    /// Horizontal distance
    /// </summary>
    [Export]
    public float Horizontal { get; set; } = 1.0f;

    public override void _Ready()
    {
        base._Ready();

        // Subscribe to scene loaded (reset camera)
        var staging = T5ToolsStaging.Instance;
        if (staging != null)
            staging.SceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// Called during the processing step of the main loop
    /// </summary>
    /// <param name="delta">Delta since previous process in seconds</param>
    public override void _Process(double delta)
    {
        base._Process(delta);

        // Calculate the target
        var target = Target();

        // Calculate and correct relative position
        var relative = (GlobalPosition - target).Slide(Vector3.Up);
        relative = relative.Normalized() * Horizontal;
        relative.Y = Vertical;

        // Position and look at the target
        GlobalPosition = target + relative;
        LookAt(target);
    }

    /// <summary>
    /// Handle scene loaded
    /// </summary>
    /// <param name="scene">New scene</param>
    /// <param name="userData">Custom data</param>
    private void OnSceneLoaded(T5ToolsScene scene, Variant userData)
    {
        // Calculate the target
        var target = Target();

        // Position and look at the target
        GlobalPosition = target + new Vector3(0, Vertical, Horizontal);
        LookAt(target);
    }

    /// <summary>
    /// Calculate the target (average of player origins)
    /// </summary>
    /// <returns>Target position</returns>
    private static Vector3 Target()
    {
        // Get the players
        var players = T5ToolsStaging.Instance?.Players;
        if (players == null || players.Count == 0)
            return Vector3.Zero;

        // Return the average of the origins
        var pos = Vector3.Zero;
        foreach (var player in players)
            pos += player.Origin.GlobalTransform.Origin;
        return pos / players.Count;
    }
}