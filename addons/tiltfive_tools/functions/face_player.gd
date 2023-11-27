extends Node


## Target node (null for parent)
@export var target : Node3D

## Allow tilt
@export var tilt : bool = false

## Scale to player
@export var player_scale : bool = true

## Rotation rate
@export var rate := 1.0


## Player camera
var _camera : T5Camera3D

## Player origin
var _origin : T5Origin3D

## Target Node3D
var _target : Node3D


# Called when the node enters the scene tree for the first time.
func _ready():
	# Get the target node
	_target = target if target else get_parent()
	if not _target:
		push_warning("FacePlayer %s could not get target" % self)
		return

	# Find the player
	var player := _find_player()
	if not player:
		return

	# Get the camera and origin
	_camera = player.get_camera()
	_origin = player.get_origin()

	# Perform the initial facing
	_target_transform(1.0)


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta : float) -> void:
	_target_transform(rate * delta)


# Update the target transform
func _target_transform(weight : float) -> void:
	# Get the old basis
	var b_old := _target.transform.basis.orthonormalized()

	# Construct the new basis looking at the camera
	var dir_local := _target.to_local(_camera.global_position)
	var b_new := b_old.looking_at(dir_local, Vector3.UP, true)

	# If tilt is not permitted then snap the Y to vertical
	if not tilt:
		b_new.y = Vector3.UP
		b_new.z = b_new.x.cross(Vector3.UP)
		b_new.x = Vector3.UP.cross(b_new.z)
		b_new = b_new.orthonormalized()

	# Blend based on weight
	b_new = b_old.slerp(b_new, weight)

	# If player-scaled then scale to counteract origin scale
	if player_scale:
		var scale := _origin.gameboard_scale
		b_new = b_new.scaled(Vector3(scale, scale, scale))

	# Set the target transform
	_target.transform.basis = b_new


# Find the player node associated with this node
func _find_player() -> T5ToolsPlayer:
	# Test if this node is a child of a character
	var character := T5ToolsCharacter.find_instance(self)
	if character:
		return character.player

	# Test if this node is a child of a player
	var player := T5ToolsPlayer.find_instance(self)
	if player:
		return player

	# Report failure
	push_warning("FacePlayer %s could not find player" % self)
	return null
