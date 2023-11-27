using Godot;

/// <summary>
/// Tilt Five Tools Controller Script for RigidBody3D
/// </summary>
/// <description>
/// This script provides a basic controller for a character body based on
/// a RigidBody3D. These are usually spheres for ball-rolling games. This script
/// may work in some simple games; however it is intended as a starter script
/// and advanced movement will almost certainly require customization or
/// reimplementation.
///
/// The primary_pressed/primary_released signals can be used to detect when the
/// primary button has been pressed - for example to implement firing logic.
/// </description>
public partial class T5ToolsRigidBodyController : RigidBody3D
{
    /// <summary>
    /// Orientation of the controller
    /// </summary>
    public enum ControlOrientationMode
    {
        /// <summary>
        /// Vertical - wand pointing forwards
        /// </summary>
        Vertical,

        /// <summary>
        /// Horizontal - wand pointing to the left
        /// </summary>
        Horizontal
    }

    /// <summary>
    /// Control reference frame
    /// </summary>
    public enum ControlReferenceMode
    {
        /// <summary>
        /// Control input relative to player
        /// </summary>
        Player,

        /// <summary>
        /// Control input relative to world
        /// </summary>
        World
    }

    /// <summary>
    /// Signal for primary button pressed
    /// </summary>
    [Signal]
    public delegate void PrimaryPressedEventHandler();

    /// <summary>
    /// Signal for primary button released
    /// </summary>
    [Signal]
    public delegate void PrimaryReleasedEventHandler();

    /// <summary>
    /// Signal for button pressed
    /// </summary>
    /// <param name="name">Button name</param>
    [Signal]
    public delegate void ButtonPressedEventHandler(StringName name);

    /// <summary>
    /// Signal for button released
    /// </summary>
    /// <param name="name">Button name</param>
    [Signal]
    public delegate void ButtonReleasedEventHandler(StringName name);

    /// <summary>
    /// Player node
    /// </summary>
    private T5ToolsPlayer _player;

    /// <summary>
    /// Origin node
    /// </summary>
    private T5OriginCS _origin;

    /// <summary>
    /// Camera node
    /// </summary>
    private T5CameraCS _camera;

    /// <summary>
    /// Control vector
    /// </summary>
    private Vector2 _control = Vector2.Zero;

    /// <summary>
    /// Jump request flag
    /// </summary>
    private bool _jump;

    /// <summary>
    /// Center the character at the origin
    /// </summary>
    [ExportGroup("Centering", "Center")]
    [Export]
    public bool CenterCharacter { get; set; } = true;

    /// <summary>
    /// Center offset
    /// </summary>
    [Export]
    public Vector3 CenterOffset { get; set; } = new(0.0f, 2.0f, 0.0f);

    /// <summary>
    /// Orientation of the controller
    /// </summary>
    [ExportGroup("Controls", "Control")]
    [Export]
    public ControlOrientationMode ControlOrientation { get; set; } = ControlOrientationMode.Vertical;

    /// <summary>
    /// Control reference frame
    /// </summary>
    [Export]
    public ControlReferenceMode ControlReference { get; set; } = ControlReferenceMode.Player;

    /// <summary>
    /// Primary button
    /// </summary>
    [Export]
    public string ControlPrimaryButton { get; set; } = "trigger_click";

    /// <summary>
    /// Jump button
    /// </summary>
    [Export]
    public string ControlJump { get; set; } = "button_3";

    /// <summary>
    /// Movement force
    /// </summary>
    [ExportGroup("Movement", "Movement")]
    [Export]
    public float MovementForce { get; set; } = 7.0f;

    /// <summary>
    /// Jump velocity
    /// </summary>
    [Export]
    public float MovementJump { get; set; } = 5.0f;

    /// <summary>
    /// Flag indicating whether the player has control in the air
    /// </summary>
    [Export]
    public bool MovementAirControl { get; set; } = true;

    /// <summary>
    /// Called when the node becomes "ready"
    /// </summary>
    public override void _Ready()
    {
        // Get the player
        _player = T5ToolsCharacter.FindInstance(this).Player;
        _origin = _player.Origin;
        _camera = _player.Camera;

        // Subscribe to player wand events
        var controller = _player.Wand;
        controller?.Connect("button_pressed", Callable.From((StringName name) => OnButtonPressed(name)));
        controller?.Connect("button_released", Callable.From((StringName name) => OnButtonReleased(name)));
        controller?.Connect("input_vector2_changed", Callable.From((StringName name, Vector2 value) => OnInputVector2Changed(name, value)));
    }

    /// <summary>
    /// Called during the processing step of the main loop
    /// </summary>
    /// <param name="delta">Delta since previous process in seconds</param>
    public override void _Process(double delta)
    {
        if (CenterCharacter)
            _origin.GlobalPosition = GlobalPosition + CenterOffset;
    }

    /// <summary>
    /// Called during the physics processing step of the main loop
    /// </summary>
    /// <param name="delta">Delta since previous process in seconds</param>
    public override void _PhysicsProcess(double delta)
    {
        // Test if the player is on the ground
        var onGround = IsOnGround();

        // Allow jumping while on the ground
        if (onGround && _jump)
        {
            LinearVelocity = LinearVelocity with { Y = MovementJump };
            _jump = false;
        }

        // Apply movement force
        if (onGround || MovementAirControl)
            ApplyCentralForce(
                ControlToGlobal(_control) * MovementForce);
    }

    /// <summary>
    /// Handle button presses
    /// </summary>
    /// <param name="name">Button name</param>
    private void OnButtonPressed(StringName name)
    {
        // Report the button press
        EmitSignal(SignalName.ButtonPressed, name);

        // Handle known buttons
        if (name == ControlPrimaryButton)
            EmitSignal(SignalName.PrimaryPressed);
        else if (name == ControlJump)
            _jump = true;
    }

    /// <summary>
    /// Handle button releases
    /// </summary>
    /// <param name="name">Button name</param>
    private void OnButtonReleased(StringName name)
    {
        // Report the button release
        EmitSignal(SignalName.ButtonReleased, name);

        // Handle known buttons
        if (name == ControlPrimaryButton)
            EmitSignal(SignalName.PrimaryReleased);
        else if (name == ControlJump)
            _jump = false;
    }

    /// <summary>
    /// Handle joystick input
    /// </summary>
    /// <param name="name">Input name</param>
    /// <param name="value">Input value</param>
    private void OnInputVector2Changed(StringName name, Vector2 value)
    {
        // Handle known joysticks
        if (name == "stick")
            _control = value;
    }

    private bool IsOnGround()
    {
        // Probe down to see if there is any ground
        var collision = new KinematicCollision3D();
        if (!TestMove(GlobalTransform, Vector3.Down * 0.1f, collision, 0.01f, false, 3))
            return false;

        // Inspect all collisions
        for (var c = 0; c < collision.GetCollisionCount(); ++c)
        {
            // Skip if the contact is too steep to be called ground
            if (collision.GetAngle(c) > Mathf.DegToRad(60.0f))
                continue;

            // Test if moving up relative to the ground
            var groundVelocity = collision.GetColliderVelocity(c);
            var relativeVelocity = LinearVelocity - groundVelocity;
            if (relativeVelocity.Y > 0.1)
                continue;

            // Found a working collision
            return true;
        }

        // No valid collisions
        return false;
    }

    /// <summary>
    /// Convert control input to global vector
    /// </summary>
    /// <param name="control">Control input</param>
    /// <returns>Global control vector</returns>
    private Vector3 ControlToGlobal(Vector2 control)
    {
        // Get the oriented vector
        var vec = Vector3.Zero;
        switch (ControlOrientation)
        {
            case ControlOrientationMode.Vertical:
                vec = new Vector3(control.X, 0.0f, -control.Y);
                break;

            case ControlOrientationMode.Horizontal:
                vec = new Vector3(-control.Y, 0.0f, -control.X);
                break;
        }

        // Translate to reference frame
        if (ControlReference == ControlReferenceMode.Player)
        {
            // Get the frame Z vector (to-player, horizontal, normalized)
            var frameZ = (_camera.GlobalPosition - GlobalPosition).Slide(Vector3.Up).Normalized();
            var frame = new Basis(
                Vector3.Up.Cross(frameZ),
                Vector3.Up,
                frameZ);

            // Apply the reference frame
            vec = frame * vec;
        }

        // Return the vector
        return vec;
    }
}
