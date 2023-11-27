using Godot;

#nullable enable

public partial class Demo2Enemy : CharacterBody3D
{
    /// <summary>
    /// Exiting flag
    /// </summary>
    private bool _exiting;

    /// <summary>
    /// Move speed
    /// </summary>
    [Export]
    public float Speed = 0.5f;

    /// <summary>
    /// Target position
    /// </summary>
    [Export]
    public Node3D? Target { get; set; }

    public override void _Ready()
    {
        // Subscribe to scene events
        var scene = T5ToolsScene.GetCurrent();
        if (scene != null)
            scene.ScenePreExiting += OnScenePreExiting;
    }

    private void OnScenePreExiting(Variant userData)
    {
        // Set the exiting flag
        _exiting = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Skip if no target
        if (Target == null)
            return;

        var dir = Target.GlobalPosition - GlobalPosition;
        if (dir.Length() > 0.2)
        {
            dir = dir.Normalized() * Speed;
            Velocity = new Vector3(dir.X, Velocity.Y - 9.8f * (float)delta, dir.Z);
            MoveAndSlide();
            return;
        }

        if (!_exiting)
        {
            T5ToolsStaging.LoadScene("res://demo/main_menu/main_menu.tscn");
            _exiting = true;
        }
    }
}