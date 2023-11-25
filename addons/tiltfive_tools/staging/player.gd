class_name T5ToolsPlayer
extends T5XRRig


## Tilt Five Tools Player Node
##
## This node is a T5XRRig with additional capabilities to work with the
## Tilt Five Tools Staging, Scene, and Character features. Applications should
## create a custom player scene extending from this scene/node to customize the
## player - for example to adjust the visible layers or add pointers.


## Player visibility layers
@export_flags_3d_render var visible_layers : int = 5 : set = _set_visible_layers

## Player number [0..3] (set by Staging on load)
@export var player_number : int = -1 : set = _set_player_number


## Array of all players (used for player numbering)
static var _players : Array[T5ToolsPlayer] = []


func _enter_tree():
	# Assign the next free player number
	for n in 4:
		if _players.all(func(p : T5ToolsPlayer) -> bool: return p.player_number != n):
			player_number = n
			break;

	# Save as a player
	_players.append(self)


func _exit_tree():
	# Remove from the list of players
	_players.erase(self)


func _ready():
	# Update the visibility mask
	_update_camera_cull_mask()


## Get the player viewport
func get_player_viewport() -> SubViewport:
	return self


## Get the player origin
func get_player_origin() -> T5Origin3D:
	return $Origin


## Get the player camera
func get_player_camera() -> T5Camera3D:
	return $Origin/Camera


## Get the player wand
func get_player_wand(wand_num : int) -> T5Controller3D:
	return get_node_or_null("Origin/Wand_%d" % (wand_num + 1))


## Get the visible layer mask associated with this player
func get_player_visible_layer() -> int:
	# Handle invalid player number
	if player_number < 0:
		return 0

	# Return the unique player layer [11..14]
	return 1024 << player_number


## Get the physics layer mask associated with this player
func get_player_physics_layer() -> int:
	# Handle invalid player number
	if player_number < 0:
		return 0

	# Return the unique player layer [11..14]
	return 1024 << player_number


## Find the T5ToolsPlayer from a child node
static func find_instance(node : Node) -> T5ToolsPlayer:
	# Search the ancestors for a player node
	while node:
		# If we have the player then return it
		if node is T5ToolsPlayer:
			return node

		# Walk up to the parent
		node = node.get_parent()

	# Could not find play
	return null


func _to_string() -> String:
	return "T5ToolsPlayer:<#%d|#%d>" % [player_number, get_instance_id()]


func _set_visible_layers(p_visible_layers : int) -> void:
	visible_layers = p_visible_layers
	if is_inside_tree():
		_update_camera_cull_mask()


func _set_player_number(p_player_number : int) -> void:
	player_number = p_player_number
	if is_inside_tree():
		_update_camera_cull_mask()


func _update_camera_cull_mask() -> void:
	# Set the camera cull mask to see the selected visible layers as well as
	# the layer specific to this player.
	$Origin/Camera.cull_mask = visible_layers | get_player_visible_layer()
