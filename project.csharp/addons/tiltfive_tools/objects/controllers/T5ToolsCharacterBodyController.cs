using Godot;

/// <summary>
/// Tilt Five Tools Controller Script for CharacterBody3D
/// </summary>
/// <description>
/// This script provides a basic controller for a character body based on
/// a CharacterBody3D (usually with a capsule collider). This script may work in
/// some simple games; however it is intended as a starter script and advanced
/// movement will almost certainly require customization or reimplementation.
/// 
/// The movement_changed signal can be used to set animations on a character
/// animation player. The primary_pressed/primary_released signals can be used
/// to detect when the primary button has been pressed - for example to implement
/// firing logic.
/// </description>
public partial class T5ToolsCharacterBodyController : CharacterBody3D
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
    /// Movement state
    /// </summary>
    public enum MovementState
    {
        /// <summary>
        /// Character is idle
        /// </summary>
        Idle,

        /// <summary>
        /// Character is walking
        /// </summary>
        Walking,

        /// <summary>
        /// Character is running
        /// </summary>
        Running,

        /// <summary>
        /// Character is jumping
        /// </summary>
        Jumping,

        /// <summary>
        /// Character is falling
        /// </summary>
        Falling,

        /// <summary>
        /// Character has landed
        /// </summary>
        Landed
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
    /// Signal for character movement state changed
    /// </summary>
    /// <param name="state"></param>
    [Signal]
    public delegate void MovementChangedEventHandler(MovementState state);

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
    /// Movement state
    /// </summary>
    private MovementState _state = MovementState.Idle;

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
    /// Movement speed
    /// </summary>
    [ExportGroup("Movement", "Movement")]
    [Export]
    public float MovementSpeed { get; set; } = 5.0f;

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
        controller.Connect("button_pressed", Callable.From((StringName name) => OnButtonPressed(name)));
        controller.Connect("button_released", Callable.From((StringName name) => OnButtonReleased(name)));
        controller.Connect("input_vector2_changed", Callable.From((StringName name, Vector2 value) => OnInputVector2Changed(name, value)));
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
        // Always apply gravity to the player
        var gravityState = PhysicsServer3D.BodyGetDirectState(GetRid());
        var gravity = gravityState.TotalGravity;
        Velocity += gravity * (float)delta;

        // Handle floor state
        if (IsOnFloor())
        {
            // Process floor states
            if (_state is MovementState.Jumping or MovementState.Falling)
            {
                // Landed on floor
                SetState(MovementState.Landed);
            }
            else if (_jump)
            {
                // Execute jump request
                SetState(MovementState.Jumping);
                Velocity = Velocity with { Y = MovementJump };
                _jump = false;
            }
        }
        else if (Velocity.Y <= 0.0f && _state != MovementState.Falling)
        {
            // Detect falling
            SetState(MovementState.Falling);
        }

        // Get the input direction and handle the movement/deceleration
        var controlVel = ControlToGlobal(_control) * MovementSpeed;
        var direction = controlVel.Normalized();

        // Face in the desired direction
        if (!direction.IsZeroApprox())
            LookAt(GlobalPosition + direction, Vector3.Up, true);

        // Apply the control
        if (MovementAirControl || IsOnFloor())
        {
            if (!direction.IsZeroApprox())
                Velocity = Velocity with
                {
                    X = controlVel.X,
                    Z = controlVel.Z
                };
            else
                Velocity = Velocity with
                {
                    X = Mathf.MoveToward(Velocity.X, 0.0f, MovementSpeed),
                    Z = Mathf.MoveToward(Velocity.Z, 0.0f, MovementSpeed)
                };
        }

        // Move the character
        MoveAndSlide();

        // Handle floor states
        if (IsOnFloor())
        {
            // Get the ground speed
            var groundSpeed = Velocity.Slide(Vector3.Up).Length();
            if (groundSpeed < MovementSpeed * 0.05f)
            {
                SetState(MovementState.Idle);
            }
            else if (groundSpeed < MovementSpeed * 0.5f)
            {
                SetState(MovementState.Walking);
            }
            else
            {
                SetState(MovementState.Running);
            }
        }
    }

    /// <summary>
    /// Get the current movement state
    /// </summary>
    /// <returns>Current movement state</returns>
    public MovementState GetMovementState() => _state;

    /// <summary>
    /// Set the movement state
    /// </summary>
    /// <param name="state">Movement state</param>
    private void SetState(MovementState state)
    {
        // Skip if no change
        if (_state == state)
            return;

        // Save and report the new state
        _state = state;
        EmitSignal(SignalName.MovementChanged, (int)_state);
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
