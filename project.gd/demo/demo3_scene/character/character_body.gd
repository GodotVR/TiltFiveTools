extends T5ToolsCharacterBodyController


# Animation player
var _animation : AnimationPlayer


func _ready() -> void:
	super()

	# Get the animation player
	_animation = $gobot/AnimationPlayer

	# Subscribe to movement changed event
	movement_changed.connect(_on_movement_changed)


# Handle movement changes
func _on_movement_changed(state : MovementState) -> void:
	match state:
		MovementState.IDLE:
			_animation.current_animation = "Idle"
		MovementState.WALKING:
			_animation.current_animation = "Walk"
		MovementState.RUNNING:
			_animation.current_animation = "Run"
		MovementState.JUMPING:
			_animation.current_animation = "Jump"
		MovementState.FALLING:
			_animation.current_animation = "Fall"
		MovementState.LANDED:
			_animation.current_animation = "Idle"
