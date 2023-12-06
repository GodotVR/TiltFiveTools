class_name T5ToolsScene
extends Node3D


signal scene_pre_exiting(user_data : Variant)
signal scene_exiting(user_data : Variant)
signal scene_loaded(user_data : Variant)
signal scene_visible(user_data : Variant)


## New Player Spawn Location
enum NewPlayerSpawn {
	LOAD,			## Spawn at the load location
	PLAYERS			## Near existing player (or load if none)
}

## Character scene
@export var character_scene : PackedScene

## How close to spawn characters
@export var spawn_padding : float = 1.0

## Spawn near other player
@export var spawn_location : NewPlayerSpawn = NewPlayerSpawn.PLAYERS


## Array of characters
var characters : Array[T5ToolsCharacter] = []


## Load spawn point
var _load_spawn : Transform3D = Transform3D.IDENTITY


func _ready():
	scene_loaded.connect(_on_scene_loaded)
	scene_exiting.connect(_on_scene_exiting)


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
	match typeof(spawn_position):
		TYPE_STRING:
			# Name of Node3D to spawn at
			var node = find_child(spawn_position)
			if node is Node3D:
				_load_spawn = node.global_transform

		TYPE_VECTOR3:
			# Vector3 to spawn at
			_load_spawn.origin = spawn_position

		TYPE_TRANSFORM3D:
			# Transform3D to spawn at
			_load_spawn = spawn_position

	# Spawn the existing player characters
	var players := T5ToolsStaging.instance.players
	var count = players.size()
	for index in count:
		# Pick the spawn location
		var location := _load_spawn
		var offset := Vector3.FORWARD * spawn_padding
		offset = offset.rotated(Vector3.UP, PI * 2 * index / count)
		location.origin += offset

		# Create the character
		_create_character(players[index], location)

	# Subscribe to player change signals
	T5ToolsStaging.instance.player_created.connect(_on_player_created)
	T5ToolsStaging.instance.player_removed.connect(_on_player_removed)


func _on_scene_exiting(_user_data : Variant) -> void:
	# Unsubscribe from player change signals
	T5ToolsStaging.instance.player_created.disconnect(_on_player_created)
	T5ToolsStaging.instance.player_removed.disconnect(_on_player_removed)


func _on_player_created(player : T5ToolsPlayer) -> void:
	# Pick the base location
	var location := _load_spawn
	if spawn_location == NewPlayerSpawn.PLAYERS and characters.size() > 0:
		location = characters.pick_random().global_transform

	# Offset the location by the spawn padding
	var offset := Vector3.FORWARD * spawn_padding
	offset = offset.rotated(Vector3.UP, randf_range(0.0, PI * 2))
	location.origin += offset

	# Create a new character at the location
	_create_character(player, location)


func _on_player_removed(player : T5ToolsPlayer) -> void:
	# Remove the character
	_remove_character(player)


# Create a character for a player
func _create_character(player : T5ToolsPlayer, location : Transform3D) -> void:
	# Construct the character
	var character : T5ToolsCharacter = character_scene.instantiate()
	character.player = player

	# Add the character to the scene
	add_child(character)
	characters.append(character)

	# Position the character
	character.global_transform = location


# Remove the character
func _remove_character(player : T5ToolsPlayer) -> void:
	# Find the character
	var character := get_player_character(player)
	if not character:
		return

	# Free the character
	characters.erase(character)
	character.queue_free()


## Get a players character
func get_player_character(player : T5ToolsPlayer) -> T5ToolsCharacter:
	# Search for a character associated with the player
	for character in characters:
		if character.player == player:
			return character

	# No character found
	return null


## Get the current scene
static func get_current() -> T5ToolsScene:
	# If we have an active stage then query it for the current scene
	if T5ToolsStaging.instance:
		return T5ToolsStaging.instance.current_scene

	# No current scene
	return null
