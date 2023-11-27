using Godot;

/// <summary>
/// Tilt Five Tools Pointer Event
/// </summary>
public partial class T5ToolsPointerEvent : GodotObject
{
    /// <summary>
    /// Types of pointer events
    /// </summary>
    public enum Type
    {
        /// <summary>
        /// Pointer entered target
        /// </summary>
        Entered,

        /// <summary>
        /// Pointer exited target
        /// </summary>
        Exited,

        /// <summary>
        /// Pointer pressed target
        /// </summary>
        Pressed,

        /// <summary>
        /// Pointer released target
        /// </summary>
        Released,

        /// <summary>
        /// Pointer moved on target
        /// </summary>
        Moved
    }

    /// <summary>
    /// Initializes a new instance of the T5ToolsPointerEvent class
    /// </summary>
    /// <param name="eventType">Type of pointer event</param>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="position">Position</param>
    /// <param name="lastPosition">Last position</param>
    public T5ToolsPointerEvent(Type eventType, T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 position, Vector3 lastPosition)
    {
        EventType = eventType;
        Player = player;
        Pointer = pointer;
        Target = target;
        Position = position;
        LastPosition = lastPosition;
    }

    /// <summary>
    /// Gets the type of pointer event
    /// </summary>
    public Type EventType { get; init; }

    /// <summary>
    /// Gets the player
    /// </summary>
    public T5ToolsPlayer Player { get; init; }

    /// <summary>
    /// Gets the pointer
    /// </summary>
    public Node3D Pointer { get; init; }

    /// <summary>
    /// Gets the target
    /// </summary>
    public Node3D Target { get; init; }

    /// <summary>
    /// Gets the position
    /// </summary>
    public Vector3 Position { get; init; }

    /// <summary>
    /// Gets the last position
    /// </summary>
    public Vector3 LastPosition { get; init; }

    /// <summary>
    /// Report a pointer entered event
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="at">Entered position</param>
    public static void Entered(T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 at)
    {
        Report(
            new T5ToolsPointerEvent(
                Type.Entered,
                player,
                pointer,
                target,
                at,
                at));
    }

    /// <summary>
    /// Report a pointer moved event
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="to">To position</param>
    /// <param name="from">From position</param>
    public static void Moved(T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 to, Vector3 from)
    {
        Report(
            new T5ToolsPointerEvent(
                Type.Moved,
                player,
                pointer,
                target,
                to,
                from));
    }

    /// <summary>
    /// Report a pointer pressed event
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="at">Pressed position</param>
    public static void Pressed(T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 at)
    {
        Report(
            new T5ToolsPointerEvent(
                Type.Pressed,
                player,
                pointer,
                target,
                at,
                at));
    }

    /// <summary>
    /// Report a pointer released event
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="at">Released position</param>
    public static void Released(T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 at)
    {
        Report(
            new T5ToolsPointerEvent(
                Type.Released,
                player,
                pointer,
                target,
                at,
                at));
    }

    /// <summary>
    /// Report a pointer exited event
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="pointer">Pointer</param>
    /// <param name="target">Target</param>
    /// <param name="last">Exited position</param>
    public static void Exited(T5ToolsPlayer player, Node3D pointer, Node3D target, Vector3 last)
    {
        Report(
            new T5ToolsPointerEvent(
                Type.Exited,
                player,
                pointer,
                target,
                last,
                last));
    }

    /// <summary>
    /// Report pointer event
    /// </summary>
    /// <param name="pointerEvent">Pointer event</param>
    public static void Report(T5ToolsPointerEvent pointerEvent)
    {
        if (GodotObject.IsInstanceValid(pointerEvent.Pointer))
        {
            if (pointerEvent.Pointer.HasSignal("PointingEvent"))
                pointerEvent.Pointer.EmitSignal("PointingEvent", pointerEvent);
        }

        if (GodotObject.IsInstanceValid(pointerEvent.Target))
        {
            if (pointerEvent.Target.HasSignal("PointerEvent"))
                pointerEvent.Target.EmitSignal("PointerEvent", pointerEvent);
            else if (pointerEvent.Target.HasMethod("PointerEvent"))
                pointerEvent.Target.Call("PointerEvent", pointerEvent);
        }
    }
}