using Godot;

#nullable enable

public partial class Demo1CharacterBody : T5ToolsRigidBodyController
{
    /// <summary>
    /// Hit sound player
    /// </summary>
    private AudioStreamPlayer3D? _hitPlayer;

    /// <summary>
    /// Rolling sound player
    /// </summary>
    private AudioStreamPlayer3D? _rollingPlayer;

    /// <summary>
    /// Last hit test
    /// </summary>
    private bool _lastHit;

    public override void _Ready()
    {
        base._Ready();
        
        // Get the audio nodes
        _hitPlayer = GetNodeOrNull<AudioStreamPlayer3D>("HitPlayer");
        _rollingPlayer = GetNodeOrNull<AudioStreamPlayer3D>("RollingPlayer");
    }

    /// <summary>
    /// Check for collisions and speed to fire sound effects
    /// </summary>
    /// <param name="state">State information</param>
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        // Process the contacts for hits and fastest rolling speed
        var hit = false;
        var speed = 0.0f;
        for (var c = 0; c < state.GetContactCount(); c++)
        {
            // Test for a velocity striking into the contact surface
            var cNorm = state.GetContactLocalNormal(c);
            var cVel = state.GetContactLocalVelocityAtPosition(c);
            if (cNorm.Dot(cVel) < -2.0)
                hit = true;

            // Get the maximum speed at the contact point
            var pos = ToLocal(state.GetContactLocalPosition(c));
            var vel = state.GetVelocityAtLocalPosition(pos);
            speed = Mathf.Max(speed, vel.Length());
        }

        // Adjust rolling volume based on speed
        speed = Mathf.Clamp(speed / 5.0f, 0.0f, 1.0f);
        if (_rollingPlayer != null)
            _rollingPlayer.VolumeDb = Mathf.LinearToDb(speed);

        // Play hit sounds on hit
        if (hit && !_lastHit && _hitPlayer != null)
            _hitPlayer.Play();

        // Update the last hit
        _lastHit = hit;
    }
}