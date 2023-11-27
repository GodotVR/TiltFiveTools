using System.Linq;
using Godot;

#nullable enable

public partial class Demo2Missile : Node3D
{
    /// <summary>
    /// Missile mesh instance
    /// </summary>
    private MeshInstance3D? _meshInstance;

    /// <summary>
    /// Explosion sound
    /// </summary>
    private AudioStreamPlayer3D? _explosionSound;

    /// <summary>
    /// Missile trail particles
    /// </summary>
    private GpuParticles3D? _trail;

    /// <summary>
    /// Missile explosion particles
    /// </summary>
    private GpuParticles3D? _explosion;

    /// <summary>
    /// Missile speed
    /// </summary>
    [Export]
    public float Speed { get; set; } = 3.0f;

    /// <summary>
    /// Kill radius
    /// </summary>
    [Export]
    public float Radius { get; set; } = 0.8f;

    /// <summary>
    /// Missile target
    /// </summary>
    [Export]
    public Vector3 Target { get; set; }
    
    public override void _Ready()
    {
        // Get the nodes
        _meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
        _explosionSound = GetNode<AudioStreamPlayer3D>("ExplosionSound");
        _trail = GetNode<GpuParticles3D>("Trail");
        _explosion = GetNode<GpuParticles3D>("Explosion");
    }

    public override void _Process(double delta)
    {
        // Step towards the target
        var dir = Target - GlobalPosition;
        if (dir.Length() > 0.2)
        {
            GlobalPosition += dir.Normalized() * Speed * (float)delta;
            return;
        }

        // Trigger the explosion
        SetProcess(false);
        _meshInstance?.Hide();
        _explosionSound?.Play();
        if (_trail != null) _trail.Emitting = false;
        if (_explosion != null) _explosion.Emitting = true;

        // Kill all enemies in the radius
        foreach (var enemy in GetTree().GetNodesInGroup("enemy").OfType<Node3D>())
            if (enemy.GlobalPosition.DistanceTo(GlobalPosition) < Radius)
                enemy.QueueFree();

        // Discard the missile after 3 seconds
        var dieTimer = GetTree().CreateTimer(3.0);
        dieTimer.Timeout += QueueFree;
    }
}