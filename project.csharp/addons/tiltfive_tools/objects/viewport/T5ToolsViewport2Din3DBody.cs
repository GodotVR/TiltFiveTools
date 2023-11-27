using System.Collections.Generic;
using System.Linq;
using Godot;

[Tool]
public partial class T5ToolsViewport2Din3DBody : StaticBody3D
{
    /// <summary>
    /// Signal when pointer event occurs on body
    /// </summary>
    /// <param name="pointerEvent">Pointer event</param>
    [Signal]
    public delegate void PointerEventEventHandler(T5ToolsPointerEvent pointerEvent);

    /// <summary>
    /// Screen size
    /// </summary>
    [Export]
    public Vector2 ScreenSize { get; set; } = new(3.0f, 2.0f);

    /// <summary>
    /// Viewport size
    /// </summary>
    public Vector2 ViewportSize { get; set; } = new(100.0f, 100.0f);

    /// <summary>
    /// Current mouse mask
    /// </summary>
    private int _mouseMask;

    /// <summary>
    /// Viewport node
    /// </summary>
    private Viewport _viewport;

    /// <summary>
    /// Collision shape
    /// </summary>
    private CollisionShape3D _collisionShape;

    /// <summary>
    /// Dictionary of pointers to touch-index
    /// </summary>
    private readonly Dictionary<Node3D, int> _touches = new();

    /// <summary>
    /// List of pressed pointers
    /// </summary>
    private readonly Dictionary<Node3D, bool> _presses = new();

    /// <summary>
    /// Dominant pointer (index == 0)
    /// </summary>
    private Node3D _dominant;

    /// <summary>
    /// Mouse pointer
    /// </summary>
    private Node3D _mouse;

    /// <summary>
    /// Last mouse position
    /// </summary>
    private Vector2 _mouseLast = Vector2.Zero;

    public override void _Ready()
    {
        base._Ready();

        // Get the viewport node
        _viewport = GetNode<Viewport>("../Viewport");
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D");

        // Subscribe to pointer events
        PointerEvent += OnPointerEvent;
    }

    /// <summary>
    /// Convert intersection point to screen coordinate
    /// </summary>
    /// <param name="at">Intersection point (global)</param>
    /// <returns>Screen X/Y position</returns>
    public Vector2 GlobalToViewport(Vector3 at)
    {
        // Convert to local
        var t = _collisionShape.GlobalTransform;
        var atLocal = t.AffineInverse() * at;

        // Convert to screen-space
        atLocal.X = ((atLocal.X / ScreenSize.X) + 0.5f) * ViewportSize.X;
        atLocal.Y = (0.5f - (atLocal.Y / ScreenSize.Y)) * ViewportSize.Y;

        // Return screen-space position
        return new Vector2(atLocal.X, atLocal.Y);
    }

    /// <summary>
    /// Pointer event handler
    /// </summary>
    /// <param name="pointerEvent">Pointer event</param>
    private void OnPointerEvent(T5ToolsPointerEvent pointerEvent)
    {
        // Ignore if we have no viewport
        if (!GodotObject.IsInstanceValid(_viewport))
            return;

        // Get the pointer and event type
        var pointer = pointerEvent.Pointer;
        var type = pointerEvent.EventType;

        // Get the touch index [0..]
        var index = _touches.TryGetValue(pointer, out var touch) ? touch : -1;

        // Create a new touch-index if necessary
        if (index < 0 || type == T5ToolsPointerEvent.Type.Entered)
        {
            // Clear any stale pointer information
            _touches.Remove(pointer);
            _presses.Remove(pointer);

            // Assign a new touch-index for the pointer
            index = NextTouchIndex();
            _touches[pointer] = index;

            // Detect dominant pointer
            if (index == 0)
                _dominant = pointer;
        }

        // Get the viewport positions
        var at = GlobalToViewport(pointerEvent.Position);
        var last = GlobalToViewport(pointerEvent.LastPosition);

        // Get/update pressed state
        bool pressed;
        switch (type)
        {
            case T5ToolsPointerEvent.Type.Pressed:
                _presses[pointer] = true;
                pressed = true;
                break;

            case T5ToolsPointerEvent.Type.Released:
                _presses.Remove(pointer);
                pressed = false;
                break;

            default:
                pressed = _presses.ContainsKey(pointer);
                break;
        }

        // Dispatch touch events
        switch (type)
        {
            case T5ToolsPointerEvent.Type.Pressed:
                ReportTouchDown(index, at);
                break;

            case T5ToolsPointerEvent.Type.Released:
                ReportTouchUp(index, at);
                break;

            case T5ToolsPointerEvent.Type.Moved:
                ReportTouchMove(index, pressed, last, at);
                break;
        }

        // If the current mouse isn't pressed then consider switching to a new one
        if (_mouse == null || !_presses.ContainsKey(_mouse))
        {
            if (type == T5ToolsPointerEvent.Type.Pressed && pointer is T5ToolsPointer)
            {
                // Switch to pressed laser-pointer
                _mouse = pointer;
            }
            else if (type == T5ToolsPointerEvent.Type.Exited && pointer == _mouse)
            {
                // Current mouse leaving, switch to dominant
                _mouse = _dominant;
            }
            else if (_mouse == null && _dominant != null)
            {
                // No mouse, pick the dominant
                _mouse = _dominant;
            }
        }

        // Fire mouse events
        if (pointer == _mouse)
        {
            switch (type)
            {
                case T5ToolsPointerEvent.Type.Pressed:
                    ReportMouseDown(at);
                    break;

                case T5ToolsPointerEvent.Type.Released:
                    ReportMouseUp(at);
                    break;

                case T5ToolsPointerEvent.Type.Moved:
                    ReportMouseMove(pressed, last, at);
                    break;
            }
        }

        // Clear pointer information on exit
        if (type == T5ToolsPointerEvent.Type.Exited)
        {
            // Clear pointer information
            _touches.Remove(pointer);
            _presses.Remove(pointer);
            if (pointer == _dominant)
                _dominant = null;
            if (pointer == _mouse)
                _mouse = null;
        }
    }

    /// <summary>
    /// Report touch-down event
    /// </summary>
    /// <param name="index">Touch index</param>
    /// <param name="at">Touch position</param>
    private void ReportTouchDown(int index, Vector2 at)
    {
        _viewport?.PushInput(new InputEventScreenTouch
        {
            Index = index,
            Position = at,
            Pressed = true,
        });
    }

    /// <summary>
    /// Report touch-up event
    /// </summary>
    /// <param name="index">Touch index</param>
    /// <param name="at">Touch position</param>
    private void ReportTouchUp(int index, Vector2 at)
    {
        _viewport?.PushInput(new InputEventScreenTouch
        {
            Index = index,
            Position = at,
            Pressed = false,
        });
    }

    /// <summary>
    /// Report touch-move event
    /// </summary>
    /// <param name="index">Touch index</param>
    /// <param name="pressed">Pressed flag</param>
    /// <param name="from">From position</param>
    /// <param name="to">To position</param>
    private void ReportTouchMove(int index, bool pressed, Vector2 from, Vector2 to)
    {
        _viewport?.PushInput(new InputEventScreenDrag
        {
            Index = index,
            Position = to,
            Pressure = pressed ? 1.0f : 0.0f,
            Relative = to - from,
        });
    }

    /// <summary>
    /// Report mouse-down event
    /// </summary>
    /// <param name="at">Mouse position</param>
    private void ReportMouseDown(Vector2 at)
    {
        _viewport?.PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = true,
            Position = at,
            GlobalPosition = at,
            ButtonMask = MouseButtonMask.Left
        });
    }

    /// <summary>
    /// Report mouse-up event
    /// </summary>
    /// <param name="at">Mouse position</param>
    private void ReportMouseUp(Vector2 at)
    {
        _viewport?.PushInput(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = false,
            Position = at,
            GlobalPosition = at,
            ButtonMask = 0
        });
    }

    /// <summary>
    /// Report mouse-move event
    /// </summary>
    /// <param name="pressed">Pressed flag</param>
    /// <param name="from">From position</param>
    /// <param name="to">To position</param>
    private void ReportMouseMove(bool pressed, Vector2 from, Vector2 to)
    {
        _viewport?.PushInput(new InputEventMouseMotion
        {
            Position = to,
            GlobalPosition = to,
            Relative = to - from,
            ButtonMask = pressed ? MouseButtonMask.Left : 0,
            Pressure = pressed ? 1.0f : 0.0f,
        });
    }

    /// <summary>
    /// Find the next free touch index
    /// </summary>
    /// <returns></returns>
    private int NextTouchIndex()
    {
        // Get the current touches
        var current = _touches.Values.OrderBy(x => x).ToArray();

        // Look for a hole
        for (var t = 0; t < current.Length; t++)
            if (current[t] != t)
                return t;

        // No hole so add to end
        return current.Length;
    }
}
