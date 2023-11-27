using System;
using Godot;

#nullable enable

public partial class Demo2Ground : StaticBody3D
{
    /// <summary>
    /// Random source
    /// </summary>
    private readonly Random _random = new();

    private PackedScene? _missileScene;

    public override void _Ready()
    {
        // Load the missile scene
        _missileScene = GD.Load<PackedScene>("res://demo/demo2_scene/objects/missile.tscn");
    }

    public void PointerEvent(T5ToolsPointerEvent evt)
    {
        // Ignore anything that isn't a press
        if (evt.EventType != T5ToolsPointerEvent.Type.Pressed)
            return;

        // Instantiate and fire the missile
        var missile = _missileScene?.Instantiate<Demo2Missile>();
        if (missile == null) return;
        missile.Target = evt.Position;
        AddChild(missile);
        missile.GlobalPosition = evt.Position + new Vector3(
            _random.NextSingle() * 6.0f - 3.0f,
            4.0f,
            _random.NextSingle() * 6.0f - 3.0f);
    }
}
