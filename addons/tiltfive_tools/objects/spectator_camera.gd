class_name T5ToolsSpectatorCamera
extends Camera3D


## Vertical distance
@export var vertical : float = 0.5

## Horizontal distance
@export var horizontal : float = 1.0


func _ready() -> void:
	# Subscribe to scene loaded (reset camera)
	T5ToolsStaging.instance.scene_loaded.connect(_on_scene_loaded)


func _process(_delta : float) -> void:
	# Calculate the target
	var target = _target()

	# Calculate and correct relative position
	var relative := (global_position - target).slide(Vector3.UP)
	relative = relative.normalized() * horizontal
	relative.y = vertical

	# Position and look at the target
	global_position = target + relative
	look_at(target)


func _on_scene_loaded(_scene, _user_data) -> void:
	# Calculate the target
	var target = _target()

	# Position and look at the target
	global_position = target + Vector3(0, vertical, horizontal)
	look_at(target)


# Calculate the target (average of player origins)
func _target() -> Vector3:
	# Get the players
	var players := T5ToolsStaging.instance.players
	if players.is_empty():
		return Vector3.ZERO

	# Return the average of the origins
	var pos := Vector3.ZERO
	for player in players:
		pos += player.get_player_origin().global_position
	return pos / players.size()
