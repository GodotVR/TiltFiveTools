class_name T5ToolsRigidBodyController
extends RigidBody3D


## Tilt Five Tools Controller Script for RigidBody3D
##
## This script provides a basic controller for a character body based on
## a RigidBody3D. These are usually spheres for ball-rolling games. This script
## may work in some simple games; however it is intended as a starter script
## and advanced movement will almost certainly require customization or
## reimplementation.
##
## The primary_pressed/primary_released signals can be used to detect when the
## primary button has been pressed - for example to implement firing logic.


## Signal emitted when the primary button is pressed
signal primary_pressed()

## Signal emitted when the primary button is released
signal primary_released()

## Signal emitted when any button is pressed
signal button_pressed(name : String)

## Signal emitted when any button is released
signal button_released(name : String)


# Character Centering group
@export_group("Centering", "center_")

## Center the character at the origin
@export var center_character : bool = true

## Center offset
@export var center_offset : Vector3 = Vector3(0.0, 2.0, 0.0)


# Controls group
@export_group("Controls", "control_")

## Primary button
@export var control_primary : String = "trigger_click"

## Jump button
@export var control_jump : String = "button_3"

# Movement group
@export_group("Movement", "movement_")

## Movement force
@export var movement_force : float = 7.0

## Jump velocity
@export var movement_jump : float = 5.0

## Flag indicating whether the player has control in the air
@export var movement_air_control : bool = false


# The player
var _player : T5ToolsPlayer

# The camera origin
var _origin : T5Origin3D

# Control vector
var _control : Vector2

# Jump request flag
var _jump : bool


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	# Get the player from the character
	_player = T5ToolsCharacter.find_instance(self).player
	_origin = _player.get_player_origin()

	# Subscribe to player wand events
	var controller := _player.get_player_wand(0)
	controller.button_pressed.connect(_on_button_pressed)
	controller.button_released.connect(_on_button_released)
	controller.input_vector2_changed.connect(_on_input_vector2_changed)


# Called every frame.
func _process(_delta : float) -> void:
	if center_character:
		_origin.global_position = global_position + center_offset



func _physics_process(_delta : float) -> void:
	# Test if the player is on the ground
	var on_ground := _is_on_ground()

	# Allow jumping while on the ground
	if on_ground and _jump:
		linear_velocity.y = movement_jump
		_jump = false

	# Apply movement
	if on_ground or movement_air_control:
		apply_central_force(
			Vector3(_control.x, 0.0, -_control.y) * movement_force)


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


# Test if the character is on the ground
func _is_on_ground() -> bool:
	# Probe down to see if there is any ground
	var collision := KinematicCollision3D.new()
	if not test_move(
		global_transform,
		Vector3.DOWN * 0.1,
		collision):
		return false

	# Skip if the contact is too stepp to be called ground
	if collision.get_angle(0) > deg_to_rad(60):
		return false

	# Test if moving up relative to the ground
	var ground_velocity := collision.get_collider_velocity(0)
	var relative_velocity := linear_velocity - ground_velocity
	if relative_velocity.y > 0.1:
		return false

	# In contact with ground
	return true
