using System;
using System.Collections.Generic;
using Godot;

[Tool]
public partial class T5ToolsPointer : Node3D
{
    /// <summary>
    /// Signal for pointer entered valid target
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Pointer location</param>
    [Signal]
    public delegate void PointerEnteredEventHandler(Node3D target, Vector3 at);

    /// <summary>
    /// Signal for pointer moved on valid target
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="from">From location</param>
    /// <param name="to">To location</param>
    [Signal]
    public delegate void PointerMovedEventHandler(Node3D target, Vector3 from, Vector3 to);

    /// <summary>
    /// Signal for pointer exited valid target
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Pointer location</param>
    [Signal]
    public delegate void PointerExitedEventHandler(Node3D target, Vector3 at);

    /// <summary>
    /// Signal for pointer pressed on valid target
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Press location</param>
    [Signal]
    public delegate void PointerPressedEventHandler(Node3D target, Vector3 at);

    /// <summary>
    /// Signal for pointer released on valid target
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Release location</param>
    [Signal]
    public delegate void PointerReleasedEventHandler(Node3D target, Vector3 at);

    /// <summary>
    /// Signal for pointing event
    /// </summary>
    /// <param name="event">Pointer event</param>
    [Signal]
    public delegate void PointingEventEventHandler(T5ToolsPointerEvent @event);

    /// <summary>
    /// Default collision mask (world + 21:pointable)
    /// </summary>
    private const uint DefaultCollisionMask = 0b0000_0000_0001_0000_0000_0000_1111_1111;

    /// <summary>
    /// Default valid mask (21:pointable)
    /// </summary>
    private const uint DefaultValidMask = 0b0000_0000_0001_0000_0000_0000_0000_0000;

    /// <summary>
    /// Pointer length backing field
    /// </summary>
    private float _length = 1.0f;

    /// <summary>
    /// Pointer angle backing field
    /// </summary>
    private float _angle = 25.0f;

    /// <summary>
    /// Visible layers backing field
    /// </summary>
    private uint _visibleLayers = 2;

    /// <summary>
    /// Arc radius backing field
    /// </summary>
    private float _arcRadius = 0.01f;

    /// <summary>
    /// Arc pointer color backing field
    /// </summary>
    private Color _arcColor = new(0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Arc pointer hit color backing field
    /// </summary>
    private Color _arcHitColor = new(0.5f, 0.5f, 1.0f);

    /// <summary>
    /// Target radius backing field
    /// </summary>
    private float _targetRadius = 0.05f;

    /// <summary>
    /// Target color backing field
    /// </summary>
    private Color _targetColor = new(0.5f, 0.5f, 1.0f, 0.5f);

    /// <summary>
    /// Collision mask backing field
    /// </summary>
    private uint _collisionMask = DefaultCollisionMask;

    /// <summary>
    /// Valid mask backing field
    /// </summary>
    private uint _validMask = DefaultValidMask;

    /// <summary>
    /// Collide with bodies backing field
    /// </summary>
    private bool _collideWithBodies = true;

    /// <summary>
    /// Collide with areas backing field
    /// </summary>
    private bool _collideWithAreas;

    /// <summary>
    /// Player
    /// </summary>
    private T5ToolsPlayer _player;

    /// <summary>
    /// Controller
    /// </summary>
    private T5ControllerCS _controller;

    /// <summary>
    /// Valid mask including player
    /// </summary>
    private uint _playerValidMask;

    /// <summary>
    /// Locked target
    /// </summary>
    private Node3D _lockedTarget;

    /// <summary>
    /// Last target
    /// </summary>
    private Node3D _lastTarget;

    /// <summary>
    /// Last valid
    /// </summary>
    private bool _lastValid;

    /// <summary>
    /// Last at
    /// </summary>
    private Vector3 _lastAt;

    /// <summary>
    /// Enabled
    /// </summary>
    private bool _enabled;

    /// <summary>
    /// RayCast node
    /// </summary>
    private RayCast3D _rayCast;

    /// <summary>
    /// Arc Mesh instance
    /// </summary>
    private MeshInstance3D _arcMesh;

    /// <summary>
    /// Arc cylinder mesh
    /// </summary>
    private CylinderMesh _arcCylinderMesh;

    /// <summary>
    /// Arc material
    /// </summary>
    private ShaderMaterial _arcMaterial;

    /// <summary>
    /// Target mesh instance
    /// </summary>
    private MeshInstance3D _targetMesh;

    /// <summary>
    /// Target sphere mesh
    /// </summary>
    private SphereMesh _targetSphereMesh;

    /// <summary>
    /// Target material
    /// </summary>
    private StandardMaterial3D _targetMaterial;

    /// <summary>
    /// Pointer length
    /// </summary>
    [ExportGroup("General")]
    [Export]
    public float Length
    {
        get => _length;
        set => SetLength(value);
    }

    /// <summary>
    /// Pointer angle
    /// </summary>
    [Export]
    public float Angle
    {
        get => _angle;
        set => SetAngle(value);
    }

    /// <summary>
    /// Visible layers
    /// </summary>
    [Export(PropertyHint.Layers3DRender)]
    public uint VisibleLayers
    {
        get => _visibleLayers;
        set => SetVisibleLayers(value);
    }

    /// <summary>
    /// Action button
    /// </summary>
    [Export]
    public string Button { get; set; } = "trigger_click";

    /// <summary>
    /// Bezier strength
    /// </summary>
    [Export(PropertyHint.Range, "0.1,1.0,0.05")]
    public float BezierStrength { get; set; } = 0.5f;

    /// <summary>
    /// Arc radius
    /// </summary>
    [Export]
    public float ArcRadius
    {
        get => _arcRadius;
        set => SetArcRadius(value);
    }

    /// <summary>
    /// Arc pointer color
    /// </summary>
    [Export]
    public Color ArcColor
    {
        get => _arcColor;
        set => SetArcColor(value);
    }

    /// <summary>
    /// Arc hit color
    /// </summary>
    [Export]
    public Color ArcHitColor
    {
        get => _arcHitColor;
        set => SetArcHitColor(value);
    }

    /// <summary>
    /// Target radius
    /// </summary>
    [ExportGroup("Target")]
    [Export]
    public float TargetRadius
    {
        get => _targetRadius;
        set => SetTargetRadius(value);
    }

    /// <summary>
    /// Target color
    /// </summary>
    [Export]
    public Color TargetColor
    {
        get => _targetColor;
        set => SetTargetColor(value);
    }

    /// <summary>
    /// Pointer collision mask
    /// </summary>
    [ExportGroup("Collision")]
    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask
    {
        get => _collisionMask;
        set => SetCollisionMask(value);
    }

    /// <summary>
    /// Pointer valid mask
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint ValidMask
    {
        get => _validMask;
        set => SetValidMask(value);
    }

    /// <summary>
    /// Enable pointer collision with bodies
    /// </summary>
    [Export]
    public bool CollideWithBodies
    {
        get => _collideWithBodies;
        set => SetCollideWithBodies(value);
    }

    /// <summary>
    /// Enable pointer collision with areas
    /// </summary>
    [Export]
    public bool CollideWithAreas
    {
        get => _collideWithAreas;
        set => SetCollideWithAreas(value);
    }

    public override void _Ready()
    {
        // Do not initialise if in the editor
        if (Engine.IsEditorHint())
            return;

        // Get the nodes
        _rayCast = GetNode<RayCast3D>("RayCast");
        _arcMesh = GetNode<MeshInstance3D>("Arc");
        _arcMaterial = _arcMesh.MaterialOverride as ShaderMaterial;
        _targetMesh = GetNode<MeshInstance3D>("Target");
        _targetMaterial = _targetMesh.MaterialOverride as StandardMaterial3D;
        _arcCylinderMesh = _arcMesh.Mesh as CylinderMesh;
        _targetSphereMesh = _targetMesh.Mesh as SphereMesh;

        // Handle visibility changes
        VisibilityChanged += UpdateEnabled;

        // Find the player
        _player = T5ToolsPlayer.FindInstance(this);

        // Get the parent wand controller
        _controller = GetParent<T5ControllerCS>();
        _controller.Connect("button_pressed", Callable.From((StringName name) => OnButtonPressed(name)));
        _controller.Connect("button_released", Callable.From((StringName name) => OnButtonReleased(name)));

        // Update the pointer
        UpdateEnabled();
        UpdateVisibleLayers();
        UpdateRay();
        UpdateTarget();
        UpdateCollision();
    }

    /// <summary>
    /// Gets the configuration warnings with this node
    /// </summary>
    /// <returns>Configuration warnings</returns>
    public override string[] _GetConfigurationWarnings()
    {
        // Get the warnings
        var warnings = new List<string>();

        // Verify the controller
        if (GetParent() is not T5ControllerCS)
            warnings.Add("Pointer must be a child of T5ControllerCS");

        // Return warnings
        return warnings.ToArray();
    }

    /// <summary>
    /// Called during the physics processing step of the main loop
    /// </summary>
    /// <param name="delta">Delta since previous process in seconds</param>
    public override void _PhysicsProcess(double delta)
    {
        // Do not run if in the editor
        if (Engine.IsEditorHint() || !IsInsideTree())
            return;

        // Handle deletion of locked target
        if (_lockedTarget != null && !GodotObject.IsInstanceValid(_lockedTarget))
        {
            _lockedTarget = null;
        }

        // Handle deletion of last target
        if (_lastTarget != null && !GodotObject.IsInstanceValid(_lastTarget))
        {
            _lastTarget = null;
            VisibleMiss();
        }

        // Find the new target
        Node3D newTarget = null;
        var newValid = false;
        var newAt = Vector3.Zero;
        if (_enabled && _controller.Call("get_is_active").AsBool() && _rayCast.IsColliding())
        {
            // Get the ray cast collision
            newAt = _rayCast.GetCollisionPoint();
            newTarget = _lockedTarget ?? _rayCast.GetCollider() as Node3D;

            // Clear if not valid
            if (newTarget != null && GodotObject.IsInstanceValid(newTarget))
                newValid = (newTarget.Get("collision_layer").As<uint>() & _playerValidMask) != 0U;
            else
                newTarget = null;
        }

        // Skip if no current and previous targets
        if (newTarget == null && _lastTarget == null)
            return;

        // Handle pointer changes
        if (newTarget != null && _lastTarget == null)
        {
            // If valid, report events on newTarget
            if (newValid)
            {
                ReportEntered(newTarget, newAt);
                ReportMoved(newTarget, newAt, newAt);
            }

            // Update visible artifacts for hit
            VisibleHit(newValid, newAt);
        }
        else if (newTarget == null)
        {
            // If valid, report exited _lastTarget
            if (_lastValid)
                ReportExited(_lastTarget, _lastAt);

            // Update visible artifacts for miss
            VisibleMiss();
        }
        else if (newTarget != _lastTarget)
        {
            // If valid, report exited _lastTarget
            if (_lastValid)
                ReportExited(_lastTarget, _lastAt);
            
            // If valid, report entered newTarget
            if (newValid)
            {
                ReportEntered(newTarget, newAt);
                ReportMoved(newTarget, newAt, newAt);
            }

            // Update visible artifacts for hit
            VisibleHit(newValid, newAt);
        }
        else if (newAt != _lastAt)
        {
            // If valid, report moved on target
            if (newValid)
                ReportMoved(newTarget, newAt, _lastAt);

            // Update visible artifacts for move
            VisibleMove(newAt);
        }

        // Update last values
        _lastTarget = newTarget;
        _lastValid = newValid;
        _lastAt = newAt;
    }

    /// <summary>
    /// Handle setting length
    /// </summary>
    /// <param name="length">New length</param>
    private void SetLength(float length)
    {
        _length = length;
        if (IsInsideTree())
            UpdateRay();
    }

    /// <summary>
    /// Handle setting angle
    /// </summary>
    /// <param name="angle">New angle</param>
    private void SetAngle(float angle)
    {
        _angle = angle;
        if (IsInsideTree())
            UpdateRay();
    }

    /// <summary>
    /// Handle setting visible layers
    /// </summary>
    /// <param name="visibleLayers">New visible layers</param>
    private void SetVisibleLayers(uint visibleLayers)
    {
        _visibleLayers = visibleLayers;
        if (IsInsideTree())
            UpdateVisibleLayers();
    }

    /// <summary>
    /// Handle setting arc radius
    /// </summary>
    /// <param name="arcRadius">New arc radius</param>
    private void SetArcRadius(float arcRadius)
    {
        _arcRadius = arcRadius;
        if (IsInsideTree())
            UpdateArc();
    }

    /// <summary>
    /// Handle setting arc color
    /// </summary>
    /// <param name="arcColor">New arc color</param>
    private void SetArcColor(Color arcColor)
    {
        _arcColor = arcColor;
        if (IsInsideTree())
            UpdateArc();
    }

    /// <summary>
    /// Handle setting arc hit color
    /// </summary>
    /// <param name="arcHitColor">New arc hit color</param>
    private void SetArcHitColor(Color arcHitColor)
    {
        _arcHitColor = arcHitColor;
        if (IsInsideTree())
            UpdateArc();
    }

    /// <summary>
    /// Handle setting target radius
    /// </summary>
    /// <param name="targetRadius">New target radius</param>
    private void SetTargetRadius(float targetRadius)
    {
        _targetRadius = targetRadius;
        if (IsInsideTree())
            UpdateTarget();
    }

    /// <summary>
    /// Handle setting target color
    /// </summary>
    /// <param name="targetColor">New target color</param>
    private void SetTargetColor(Color targetColor)
    {
        _targetColor = targetColor;
        if (IsInsideTree())
            UpdateTarget();
    }

    /// <summary>
    /// Handle setting collision mask
    /// </summary>
    /// <param name="collisionMask">New collision mask</param>
    private void SetCollisionMask(uint collisionMask)
    {
        _collisionMask = collisionMask;
        if (IsInsideTree())
            UpdateCollision();
    }

    /// <summary>
    /// Handle setting valid mask
    /// </summary>
    /// <param name="validMask">New valid collision mask</param>
    private void SetValidMask(uint validMask)
    {
        _validMask = validMask;
        if (IsInsideTree())
            UpdateCollision();
    }

    /// <summary>
    /// Handle setting collide with bodies
    /// </summary>
    /// <param name="collideWithBodies">New collide with bodies flag</param>
    private void SetCollideWithBodies(bool collideWithBodies)
    {
        _collideWithBodies = collideWithBodies;
        if (IsInsideTree())
            UpdateCollision();
    }

    /// <summary>
    /// Handle setting collide with areas
    /// </summary>
    /// <param name="collideWithAreas">New collide with areas flag</param>
    private void SetCollideWithAreas(bool collideWithAreas)
    {
        _collideWithAreas = collideWithAreas;
        if (IsInsideTree())
            UpdateCollision();
    }

    /// <summary>
    /// Handle updating enabled state
    /// </summary>
    private void UpdateEnabled()
    {
        _enabled = IsVisibleInTree();
        UpdateArc();
        UpdateTarget();
    }

    /// <summary>
    /// Update visible layers
    /// </summary>
    private void UpdateVisibleLayers()
    {
        // Calculate the visible layers (including the player)
        var layers = _visibleLayers | (_player?.GetPlayerVisibleLayer() ?? 0U);

        // Set the mesh visible layers
        _arcMesh.Layers = layers;
        _targetMesh.Layers = layers;
    }

    /// <summary>
    /// Update the ray
    /// </summary>
    private void UpdateRay()
    {
        _rayCast.RotationDegrees = _rayCast.RotationDegrees with { X = -_angle };
        _rayCast.TargetPosition = _rayCast.TargetPosition with { Z = -_length };
        UpdateArc();
    }

    /// <summary>
    /// Update the arc
    /// </summary>
    private void UpdateArc()
    {
        // Update cylinder
        _arcCylinderMesh.TopRadius = _arcRadius;
        _arcCylinderMesh.BottomRadius = _arcRadius;

        // Update visible artifacts
        if (_enabled && _lastTarget != null)
            VisibleHit(_lastValid, _lastAt);
        else
            VisibleMiss();
    }

    /// <summary>
    /// Update the pointer target
    /// </summary>
    private void UpdateTarget()
    {
        // Update sphere
        _targetSphereMesh.Radius = _targetRadius;
        _targetSphereMesh.Height = _targetRadius * 2.0f;
        _targetMaterial.AlbedoColor = _targetColor;
    }

    /// <summary>
    /// Update collision settings
    /// </summary>
    private void UpdateCollision()
    {
        // Get the player-specific layer
        var playerLayer = _player?.GetPlayerPhysicsLayer() ?? 0U;

        // Update the valid mask with player specific layers
        _playerValidMask = _validMask | playerLayer;

        // Update the ray cast
        _rayCast.CollisionMask = _collisionMask | playerLayer;
        _rayCast.CollideWithBodies = _collideWithBodies;
        _rayCast.CollideWithAreas = _collideWithAreas;
    }

    /// <summary>
    /// Update the arc active color
    /// </summary>
    /// <param name="hit">Hit state</param>
    private void UpdateArcActiveColor(bool hit)
    {
        // Update the arc material
        _arcMaterial?.SetShaderParameter("color", hit ? _arcHitColor : _arcColor);
    }

    /// <summary>
    /// Report pointer entered
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Enter position</param>
    private void ReportEntered(Node3D target, Vector3 at)
    {
        EmitSignal(SignalName.PointerEntered, target, at);
        T5ToolsPointerEvent.Entered(_player, this, target, at);
    }

    /// <summary>
    /// Report pointer moved
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="to">New position</param>
    /// <param name="from">Old position</param>
    private void ReportMoved(Node3D target, Vector3 to, Vector3 from)
    {
        EmitSignal(SignalName.PointerMoved, target, from, to);
        T5ToolsPointerEvent.Moved(_player, this, target, to, from);
    }

    /// <summary>
    /// Report pointer exited
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Exit position</param>
    private void ReportExited(Node3D target, Vector3 at)
    {
        EmitSignal(SignalName.PointerExited, target, at);
        T5ToolsPointerEvent.Exited(_player, this, target, at);
    }

    /// <summary>
    /// Report pointer pressed
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Pressed position</param>
    private void ReportPressed(Node3D target, Vector3 at)
    {
        EmitSignal(SignalName.PointerPressed, target, at);
        T5ToolsPointerEvent.Pressed(_player, this, target, at);
    }

    /// <summary>
    /// Report pointer released
    /// </summary>
    /// <param name="target">Target node</param>
    /// <param name="at">Released position</param>
    private void ReportReleased(Node3D target, Vector3 at)
    {
        EmitSignal(SignalName.PointerReleased, target, at);
        T5ToolsPointerEvent.Released(_player, this, target, at);
    }

    /// <summary>
    /// Update visible objects for a hit
    /// </summary>
    /// <param name="valid">Valid flag</param>
    /// <param name="at">Hit position</param>
    private void VisibleHit(bool valid, Vector3 at)
    {
        // Show the target
        _targetMesh.GlobalPosition = at;
        _targetMesh.Visible = valid;

        // Update the arc
        UpdateArcActiveColor(valid);
        UpdateArcCurve(at);
    }

    /// <summary>
    /// Update visible objects for a move
    /// </summary>
    /// <param name="at">Hit position</param>
    private void VisibleMove(Vector3 at)
    {
        // Update the target
        _targetMesh.GlobalPosition = at;
        
        // Update the arc
        UpdateArcCurve(at);
    }

    /// <summary>
    /// Update visible objects for a miss
    /// </summary>
    private void VisibleMiss()
    {
        // Update the target
        _targetMesh.Visible = false;

        // Calculate a fake "at" vector
        var at = _rayCast?.ToGlobal(new Vector3(0, 0, -_length)) ?? Vector3.Zero;

        // Update the arc
        UpdateArcActiveColor(false);
        UpdateArcCurve(at);
    }

    /// <summary>
    /// Update the arc curve
    /// </summary>
    /// <param name="at">Curve target</param>
    private void UpdateArcCurve(Vector3 at)
    {
        var rayCastTransform = _rayCast.GlobalTransform;
        var distance = at.DistanceTo(rayCastTransform.Origin);

        // Mix target up with ray cast direction
        var forward = new Vector3(0.0f, -1.0f, 0.0f);
        var up = (Vector3.Up + rayCastTransform.Basis.Z).Normalized();

        var inv = _arcMesh.GlobalTransform.AffineInverse();
        var target = inv * at;
        var targetUp = inv.Basis * up;
        targetUp.Z -= Math.Abs(targetUp.X);
        targetUp.X = 0.0f;

        _arcMaterial.SetShaderParameter("forward", forward * distance * BezierStrength);
        _arcMaterial.SetShaderParameter("target", target);
        _arcMaterial.SetShaderParameter("target_up", targetUp * distance * BezierStrength);
    }

    /// <summary>
    /// Handle wand button press
    /// </summary>
    /// <param name="name">Button name</param>
    private void OnButtonPressed(StringName name)
    {
        // Ignore if not the active button or no target
        if (_lastTarget == null || name != Button)
            return;

        // Lock the target and report the press
        _lockedTarget = _lastTarget;
        ReportPressed(_lockedTarget, _lastAt);
    }

    private void OnButtonReleased(StringName name)
    {
        // Ignore if not the active button or no target
        if (_lockedTarget == null || name != Button)
            return;

        // Unlock the target and report the release
        ReportReleased(_lockedTarget, _lastAt);
        _lockedTarget = null;
    }
}
