using Godot;

#nullable enable

public partial class Demo3CharacterBody : T5ToolsCharacterBodyController
{
    /// <summary>
    /// Animation player
    /// </summary>
    private AnimationPlayer? _animation;

    public override void _Ready()
    {
        base._Ready();

        // Get the animation player
        _animation = GetNode<AnimationPlayer>("gobot/AnimationPlayer");

        // Subscribe to movement changed events
        MovementChanged += OnMovementChanged;
    }

    /// <summary>
    /// Handle movement changes
    /// </summary>
    /// <param name="state"></param>
    private void OnMovementChanged(MovementState state)
    {
        // Skip if no animation
        if (_animation == null)
            return;

        switch (state)
        {
            case MovementState.Idle:
                _animation.CurrentAnimation = "Idle";
                break;

            case MovementState.Walking:
                _animation.CurrentAnimation = "Walk";
                break;

            case MovementState.Running:
                _animation.CurrentAnimation = "Run";
                break;

            case MovementState.Jumping:
                _animation.CurrentAnimation = "Jump";
                break;

            case MovementState.Falling:
                _animation.CurrentAnimation = "Fall";
                break;

            case MovementState.Landed:
                _animation.CurrentAnimation = "Idle";
                break;

        }
    }
}