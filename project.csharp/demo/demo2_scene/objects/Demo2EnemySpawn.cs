using System;
using Godot;

#nullable enable

public partial class Demo2EnemySpawn : Node3D
{
    /// <summary>
    /// Random source
    /// </summary>
    private readonly Random _random = new();

    /// <summary>
    /// Enemy scene
    /// </summary>
    private PackedScene? _enemyScene;

    /// <summary>
    /// Enemy spawn timer
    /// </summary>
    private Timer? _spawnTimer;

    /// <summary>
    /// Target node
    /// </summary>
    [Export]
    public Node3D? Target { get; set; }

    public override void _Ready()
    {
        // Load the enemy scene
        _enemyScene = GD.Load<PackedScene>("res://demo/demo2_scene/objects/enemy.tscn");

        // Get the spawn timer
        _spawnTimer = GetNode<Timer>("SpawnTimer");
        _spawnTimer.Timeout += OnSpawnTimerTimeout;

        // Hook when the scene is comping to a close
        var scene = T5ToolsScene.GetCurrent();
        if (scene != null)
            scene.ScenePreExiting += OnScenePreExiting;

        // Start the timer with a random delay
        var startTimer = GetTree().CreateTimer(0.1 + _random.NextSingle() * 0.9);
        startTimer.Timeout += () => _spawnTimer?.Start();
    }

    private void OnScenePreExiting(Variant userData)
    {
        _spawnTimer?.Stop();
    }

    private void OnSpawnTimerTimeout()
    {
        // Skip on invalid nodes
        if (_enemyScene == null || Target == null || _spawnTimer == null)
            return;

        // Create the enemy
        var enemy = _enemyScene.Instantiate<Demo2Enemy>();
        enemy.Target = Target;
        AddChild(enemy);
        enemy.Position = new Vector3(_random.NextSingle() * 0.8f - 0.4f, 0.0f, 0.0f);

        // Update the wait time
        _spawnTimer.WaitTime = Mathf.Clamp(_spawnTimer.WaitTime - _random.NextSingle() * 0.1f, 0.3f, 1.0f);
    }
}
