class_name T5ToolsScene
extends Node3D


signal scene_pre_exiting(user_data : Variant)
signal scene_exiting(user_data : Variant)
signal scene_loaded(user_data : Variant)
signal scene_visible(user_data : Variant)


## Character scene
@export var character_scene : PackedScene

## How close to spawn characters
@export var spawn_padding : float = 1.0


## Array of characters
var characters : Array[T5ToolsCharacter] = []


func _ready():
	scene_loaded.connect(_on_scene_loaded)


func _on_scene_loaded(user_data : Variant) -> void:
	# Skip if we dont have characters to instance
	if !character_scene:
		return

	# Get the spawn position data
	var spawn_position = user_data
	if typeof(spawn_position) == TYPE_OBJECT and \
		spawn_position.has_method("get_spawn_position"):
		spawn_position = spawn_position.get_spawn_postion(self)

	# Get the spawn transform
	var spawn_transform := Transform3D.IDENTITY
	match typeof(spawn_position):
		TYPE_STRING:
			# Name of Node3D to spawn at
			var node = find_child(spawn_position)
			if node is Node3D:
				spawn_transform = node.global_transform

		TYPE_VECTOR3:
			# Vector3 to spawn at
			spawn_transform.origin = spawn_position

		TYPE_TRANSFORM3D:
			# Transform3D to spawn at
			spawn_transform = spawn_position

	# Spawn the player characters
	var players := T5ToolsStaging.instance.players
	var count = players.size()
	for index in count:
		# Construct the character
		var character : T5ToolsCharacter = character_scene.instantiate()
		character.player = players[index]

		# Add the character to the scene
		add_child(character)

		# Calculate the spawn offset
		var offset := Vector3.FORWARD * spawn_padding
		offset = offset.rotated(Vector3.UP, PI * 2 * index / count)

		# Position the character
		character.global_transform = spawn_transform
		character.global_position += offset


## Get the current scene
static func get_current() -> T5ToolsScene:
	# If we have an active stage then query it for the current scene
	if T5ToolsStaging.instance:
		return T5ToolsStaging.instance.current_scene

	# No current scene
	return null
