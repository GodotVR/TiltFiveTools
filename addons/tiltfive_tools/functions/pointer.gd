@tool
class_name T5ToolsPointer
extends Node3D


## TiltFive Pointer
##
## This node implements a laser-pointer capable of interacting with bodies
## and areas.


## Signal for pointer entered valid target
signal pointer_entered(target : Node3D, at : Vector3)

## Signal for pointer moved on valid target
signal pointer_moved(target : Node3D, from : Vector3, to : Vector3)

## Signal for pointer exited valid target
signal pointer_exited(target : Node3D, at : Vector3)

## Signal for pointer pressed on valid target
signal pointer_pressed(target : Node3D, at : Vector3)

## Signal for pointer released on valid target
signal pointer_released(target : Node3D, at : Vector3)

## Signal for pointing event
signal pointing_event(event : T5ToolsPointerEvent)


## Default collison mask (world + 21:pointable)
const COLLISION_MASK := 0b0000_0000_0001_0000_0000_0000_1111_1111

## Default valid mask (21:pointable)
const VALID_MASK := 0b0000_0000_0001_0000_0000_0000_0000_0000


@export_group("General")

## Pointer length
@export var length : float = 1.0 : set = _set_length

## Pointer angle
@export var angle : float = 25.0 : set = _set_angle

## Visible layers
@export_flags_3d_render var visible_layers : int = 2 : set = _set_visible_layers

## Action button
@export var button : String = "trigger_click"

@export_group("Arc")

## Arc length when not colliding
@export_range(0.01, 1.0, 0.01, "or_greater") var not_colliding_distance : float = 0.5

## Bezier strength
@export_range(0.1, 1.0, 0.05) var bezier_strength : float = 0.5

## Arc radius
@export var arc_radius : float = 0.01 : set = _set_arc_radius

## Arc pointer color
@export var arc_color : Color = Color(0.0, 0.0, 1.0) : set = _set_arc_color

## Arc pointer hit color
@export var arc_hit_color : Color = Color(0.5, 0.5, 1.0) : set = _set_arc_hit_color

@export_group("Target")

## Target radius
@export var target_radius : float = 0.05 : set = _set_target_radius

## Target color
@export var target_color : Color = Color(0.5, 0.5, 1.0, 0.5) : set = _set_target_color

@export_group("Collision")

## Pointer collision mask
@export_flags_3d_physics var collision_mask : int = COLLISION_MASK : set = _set_collision_mask

## Pointer valid mask
@export_flags_3d_physics var valid_mask : int = VALID_MASK : set = _set_valid_mask

## Enable pointer collision with bodies
@export var collide_with_bodies : bool = true : set = _set_collide_with_bodies

## Enable pointer collision with areas
@export var collide_with_areas : bool = false : set = _set_collide_with_areas


# Player
var _player : T5ToolsPlayer

# Controller
var _controller : T5Controller3D

# Valid mask including player
var _player_valid_mask : int

# Locked target
var _locked_target : Node3D

# Last target
var _last_target : Node3D

# Last valid
var _last_valid : bool

# Last collision point
var _last_at : Vector3

# Enabled flag
var _enabled : bool


# RayCast node
@onready var _raycast : RayCast3D = $RayCast

# Arc node
@onready var _arc_mesh : MeshInstance3D = $Arc

# Arc material
@onready var _arc_material : ShaderMaterial = $Arc.material_override

# Target node
@onready var _target_mesh : MeshInstance3D = $Target

# Target material
@onready var _target_material : StandardMaterial3D = $Target.material_override


func _ready() -> void:
	# Do not initialise if in the editor
	if Engine.is_editor_hint():
		return

	# Handle visibility changes
	visibility_changed.connect(_update_enabled)

	# Find the player
	_player = T5ToolsPlayer.find_instance(self)

	# Get the parent wand controller
	_controller = get_parent() as T5Controller3D
	_controller.button_pressed.connect(_on_button_pressed)
	_controller.button_released.connect(_on_button_released)

	# Update the pointer
	_update_enabled()
	_update_visible_layers()
	_update_ray()
	_update_target()
	_update_collision()


func _get_configuration_warnings() -> PackedStringArray:
	var warnings := PackedStringArray()

	# Verify the controller
	if not (get_parent() is T5Controller3D):
		warnings.append("Pointer must be a child of a T5Controller3D")

	return warnings


func _physics_process(_delta : float) -> void:
	# Do not run if in the editor
	if Engine.is_editor_hint() or !is_inside_tree():
		return

	# Handle targets being deleted
	if _locked_target and not is_instance_valid(_locked_target):
		_locked_target = null
	if _last_target and not is_instance_valid(_last_target):
		_last_target = null
		_visible_miss()

	# find the new target
	var new_target : Node3D
	var new_valid : bool
	var new_at : Vector3
	if _enabled and _controller.get_is_active() and _raycast.is_colliding():
		new_at = _raycast.get_collision_point()
		if _locked_target:
			# Locked to 'target'
			new_target = _locked_target
		else:
			# Use raycast target
			new_target = _raycast.get_collider()

		# Test if the object is a valid hit
		if not is_instance_valid(new_target) or not "collision_layer" in new_target:
			new_target = null
		else:
			new_valid = (new_target.collision_layer & _player_valid_mask) != 0

	# Skip if no current and previous collision
	if not new_target and not _last_target:
		return

	# Handle pointer changes
	if new_target and not _last_target:
		# If valid, report events on new_target
		if new_valid:
			_report_entered(new_target, new_at)
			_report_moved(new_target, new_at, new_at)

		# Update visible artifacts for hit
		_visible_hit(new_valid, new_at)
	elif not new_target and _last_target:
		# If valid, report exited _last_target
		if _last_valid:
			_report_exited(_last_target, _last_at)

		# Update visible artifacts for miss
		_visible_miss()
	elif new_target != _last_target:
		# If valid, report exiting _last_target
		if _last_valid:
			_report_exited(_last_target, _last_at)

		# If valid, report entered new_target
		if new_valid:
			_report_entered(new_target, new_at)
			_report_moved(new_target, new_at, new_at)

		# Update visible artifacts for hit
		_visible_hit(new_valid, new_at)
	elif new_at != _last_at:
		# If valid, report moved on target
		if new_valid:
			_report_moved(new_target, new_at, _last_at)

		# Update visible artifacts for move
		_visible_move(new_at)

	# Update last values
	_last_target = new_target
	_last_valid = new_valid
	_last_at = new_at


func _set_length(p_length : float) -> void:
	length = p_length
	if is_inside_tree():
		_update_ray()


func _set_angle(p_angle : float) -> void:
	angle = p_angle
	if is_inside_tree():
		_update_ray()


func _set_visible_layers(p_visible_layers : int) -> void:
	visible_layers = p_visible_layers
	if is_inside_tree():
		_update_visible_layers()


func _set_arc_radius(p_arc_radius : float) -> void:
	arc_radius = p_arc_radius
	if is_inside_tree():
		_update_arc()


func _set_arc_color(p_arc_color : Color) -> void:
	arc_color = p_arc_color
	if is_inside_tree():
		_update_arc()


func _set_arc_hit_color(p_arc_hit_color : Color) -> void:
	arc_hit_color = p_arc_hit_color
	if is_inside_tree():
		_update_arc()


func _set_target_radius(p_target_radius : float) -> void:
	target_radius = p_target_radius
	if is_inside_tree():
		_update_target()


func _set_target_color(p_target_color : Color) -> void:
	target_color = p_target_color
	if is_inside_tree():
		_update_target()


func _set_collision_mask(p_collision_mask : int) -> void:
	collision_mask = p_collision_mask
	if is_inside_tree():
		_update_collision()


func _set_valid_mask(p_valid_mask : int) -> void:
	valid_mask = p_valid_mask
	if is_inside_tree():
		_update_collision()


func _set_collide_with_bodies(p_collide_with_bodies : bool) -> void:
	collide_with_bodies = p_collide_with_bodies
	if is_inside_tree():
		_update_collision()


func _set_collide_with_areas(p_collide_with_areas : bool) -> void:
	collide_with_areas = p_collide_with_areas
	if is_inside_tree():
		_update_collision()


func _update_enabled() -> void:
	_enabled = is_visible_in_tree()
	_update_arc()
	_update_target()


func _update_visible_layers() -> void:
	var layers := visible_layers | _player.get_player_visible_layer()
	_arc_mesh.layers = layers
	_target_mesh.layers = layers


func _update_ray() -> void:
	_raycast.rotation_degrees.x = -angle
	_raycast.target_position.z = -length
	_update_arc()


func _update_arc() -> void:
	_arc_mesh.mesh.top_radius = arc_radius
	_arc_mesh.mesh.bottom_radius = arc_radius

	if _enabled and _last_target:
		_visible_hit(_last_valid, _last_at)
	else:
		_visible_miss()


func _update_target() -> void:
	_target_mesh.mesh.radius = target_radius
	_target_mesh.mesh.height = target_radius * 2.0
	_target_material.albedo_color = target_color


func _update_collision():
	# Get the player-specific layer
	var player_layer := _player.get_player_physics_layer()

	# Update the valid mask with player specific layers
	_player_valid_mask = valid_mask | player_layer

	# Set the raycast to collide with the collision with player-specific layers
	_raycast.collision_mask = collision_mask | player_layer
	_raycast.collide_with_bodies = collide_with_bodies
	_raycast.collide_with_areas = collide_with_areas


func _update_arc_active_color(hit : bool) -> void:
	_arc_material.set_shader_parameter(
		"color",
		arc_hit_color if hit else arc_color)


func _report_entered(target : Node3D, at : Vector3) -> void:
	pointer_entered.emit(target, at)
	T5ToolsPointerEvent.entered(_player, self, target, at)


func _report_moved(target : Node3D, to : Vector3, from : Vector3) -> void:
	pointer_moved.emit(target, from, to)
	T5ToolsPointerEvent.moved(_player, self, target, to, from)


func _report_exited(target : Node3D, at : Vector3) -> void:
	pointer_exited.emit(target, at)
	T5ToolsPointerEvent.exited(_player, self, target, at)


func _report_pressed(target : Node3D, at : Vector3) -> void:
	pointer_pressed.emit(target, at)
	T5ToolsPointerEvent.pressed(_player, self, target, at)


func _report_released(target : Node3D, at : Vector3) -> void:
	pointer_released.emit(target, at)
	T5ToolsPointerEvent.released(_player, self, target, at)


func _visible_hit(valid : bool, at : Vector3) -> void:
	# Show the target
	_target_mesh.global_position = at
	_target_mesh.visible = valid

	# Update the arc
	_update_arc_active_color(valid)
	_update_arc_curve(at)


func _visible_move(at : Vector3) -> void:
	# Update the target
	_target_mesh.global_position = at

	# Update the arc
	_update_arc_curve(at)


func _visible_miss() -> void:
	# Hide the target
	_target_mesh.visible = false

	# Calculate a fake "at" vector
	var at := _raycast.to_global(Vector3(0, 0, -length))

	# Update the arc
	_update_arc_active_color(false)
	_update_arc_curve(at)


func _update_arc_curve(at : Vector3) -> void:
	var raycast_transform := _raycast.global_transform

	var distance := at.distance_to(raycast_transform.origin)

	# Mix target up with raycast direction
	var forward := Vector3(0, -1, 0)
	var up := (Vector3.UP + raycast_transform.basis.z).normalized()

	var inv := _arc_mesh.global_transform.affine_inverse()
	var target := inv * at
	var target_up := inv.basis * up
	target_up.z -= abs(target_up.x)
	target_up.x = 0.0

	_arc_material.set_shader_parameter("forward", forward * distance * bezier_strength)
	_arc_material.set_shader_parameter("target", target)
	_arc_material.set_shader_parameter("target_up", target_up * distance * bezier_strength)


func _on_button_pressed(p_name : String) -> void:
	# Ignore if not the active button or no target
	if not _last_target or p_name != button:
		return

	# Lock the target and report the press
	_locked_target = _last_target
	_report_pressed(_locked_target, _last_at)


func _on_button_released(p_name : String) -> void:
	# Ignore if not the active button or no target
	if not _locked_target or p_name != button:
		return

	# Report the release and unlock the target
	_report_released(_locked_target, _last_at)
	_locked_target = null
