class_name T5ToolsCharacterBodyController
extends CharacterBody3D


## Tilt Five Tools Controller Script for CharacterBody3D
##
## This script provides a basic controller for a character body based on
## a CharacterBody3D (usually with a capsule collider). This script may work in
## some simple games; however it is intended as a starter script and advanced
## movement will almost certainly require customization or reimplementation.
##
## The movement_changed signal can be used to set animations on a character
## animation player. The primary_pressed/primary_released signals can be used
## to detect when the primary button has been pressed - for example to implement
## firing logic.


## Signal emitted when the primary button is pressed
signal primary_pressed()

## Signal emitted when the primary button is released
signal primary_released()

## Signal emitted when any button is pressed
signal button_pressed(name : String)

## Signal emitted when any button is released
signal button_released(name : String)

## Signal emitted when the character movement changes
signal movement_changed(state : MovementState)


## Orientation of the controller
enum ControlOrientation {
	VERTICAL,		## Vertical - wand pointing forwards
	HORIZONTAL		## Horizontal - wand pointnig to the left
}

## Control reference frame
enum ControlReference {
	PLAYER,			## Control input relative to player
	WORLD			## Control input relative to world
}

## Movement State
enum MovementState {
	IDLE,		## Character is idle
	WALKING,	## Character is walking
	RUNNING,	## Character is running
	JUMPING,	## Character is jumping
	FALLING,	## Character is falling
	LANDED		## Character landed
}


# Character Centering group
@export_group("Centering", "center_")

## Center the character at the origin
@export var center_character : bool = true

## Center offset
@export var center_offset : Vector3 = Vector3(0.0, 2.0, 0.0)


# Controls group
@export_group("Controls", "control_")

## Orientation of the controller
@export var control_orientation : ControlOrientation = ControlOrientation.VERTICAL

## Control reference frame
@export var control_reference : ControlReference = ControlReference.PLAYER

## Primary button
@export var control_primary : String = "trigger_click"

## Jump button
@export var control_jump : String = "button_3"

# Movement group
@export_group("Movement", "movement_")

## Movement speed
@export var movement_speed : float = 5.0

## Jump velocity
@export var movement_jump : float = 5.0

## Flag indicating whether the player has control in the air
@export var movement_air_control : bool = true

# The player
var _player : T5ToolsPlayer

# The camera origin
var _origin : T5Origin3D

# The camera
var _camera : T5Camera3D

# Movement state
var _state : MovementState = MovementState.IDLE

# Control vector
var _control : Vector2

# Jump request flag
var _jump : bool


# Called when the node enters the scene tree for the first time.
func _ready():
	# Get the player from the character
	_player = T5ToolsCharacter.find_instance(self).player
	_origin = _player.get_origin()
	_camera = _player.get_camera()

	# Subscribe to player wand events
	var controller := _player.get_wand()
	controller.button_pressed.connect(_on_button_pressed)
	controller.button_released.connect(_on_button_released)
	controller.input_vector2_changed.connect(_on_input_vector2_changed)


# Called every frame.
func _process(_delta : float) -> void:
	if center_character:
		_origin.global_position = global_position + center_offset


# Called every physics frame.
func _physics_process(delta : float) -> void:
	# Always apply gravity to the player
	var gravity_state := PhysicsServer3D.body_get_direct_state(get_rid())
	var gravity := gravity_state.total_gravity
	velocity += gravity * delta

	# Handle floor state
	if is_on_floor():
		# Process floor states
		if _state == MovementState.JUMPING or _state == MovementState.FALLING:
			# Landed on floor
			_set_state(MovementState.LANDED)
		elif _jump:
			# Execute jump request
			_set_state(MovementState.JUMPING)
			velocity.y = movement_jump
			_jump = false
	elif velocity.y <= 0.0 and _state != MovementState.FALLING:
		# Detect falling
		_set_state(MovementState.FALLING)

	# Get the input direction and handle the movement/deceleration.
	var control_vel := _control_to_global(_control) * movement_speed
	var direction := control_vel.normalized()

	# Face in the desired direction
	if direction:
		look_at(global_position + direction, Vector3.UP, true)

	# Apply the control
	if movement_air_control or is_on_floor():
		if direction:
			velocity.x = control_vel.x
			velocity.z = control_vel.z
		else:
			velocity.x = move_toward(velocity.x, 0, movement_speed)
			velocity.z = move_toward(velocity.z, 0, movement_speed)

	# Move the player
	move_and_slide()

	# Handle floor state
	if is_on_floor():
		# Get the ground speed
		var ground_speed := velocity.slide(Vector3.UP).length()
		if ground_speed < movement_speed * 0.05:
			_set_state(MovementState.IDLE)
		elif ground_speed < movement_speed * 0.5:
			_set_state(MovementState.WALKING)
		else:
			_set_state(MovementState.RUNNING)


## Get the current movement state
func get_movement_state() -> MovementState:
	return _state


# Handle changing the state
func _set_state(state : MovementState) -> void:
	# Skip if no change
	if state == _state:
		return

	# Save and report the new state
	_state = state
	movement_changed.emit(state)


# Handle button presses
func _on_button_pressed(p_name : String) -> void:
	# Report the button press
	button_pressed.emit(p_name)

	# Handle known buttons
	if p_name == control_primary:
		primary_pressed.emit()
	elif p_name == control_jump:
		_jump = true


# Handle button releases
func _on_button_released(p_name : String) -> void:
	# Report the button release
	button_released.emit(p_name)

	# Handle known buttons
	if p_name == control_primary:
		primary_released.emit()
	elif p_name == control_jump:
		_jump = false


# Handle joystick input
func _on_input_vector2_changed(p_name : String, p_value : Vector2) -> void:
	# Handle known joysticks
	if p_name == "stick":
		_control = p_value


# Convert control input to global vector
func _control_to_global(control : Vector2) -> Vector3:
	# Get the oriented vector
	var vec : Vector3
	match control_orientation:
		ControlOrientation.VERTICAL:
			vec = Vector3(control.x, 0.0, -control.y)
		ControlOrientation.HORIZONTAL:
			vec = Vector3(-control.y, 0.0, -control.x)

	# Translate to reference frame
	if control_reference == ControlReference.PLAYER:
		# Get the frame Z vector (to-player, horizontal, normalized)
		var frame_z := (_camera.global_position - global_position).slide(Vector3.UP).normalized()
		var frame := Basis(
			Vector3.UP.cross(frame_z),
			Vector3.UP,
			frame_z)

		# Apply the reference frame
		vec = frame * vec

	# Return the vector
	return vec
