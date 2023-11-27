extends StaticBody3D


## The missile scene
const MISSILE := preload("res://demo/demo2_scene/objects/missile.tscn")


func pointer_event(event : T5ToolsPointerEvent) -> void:
	# Ignore anything that isn't a press
	if event.event_type != T5ToolsPointerEvent.Type.PRESSED:
		return

	# Instantiate and fire the missile
	var missile := MISSILE.instantiate()
	missile.target = event.position
	add_child(missile)
	missile.global_position = event.position + Vector3(
		randf_range(-3.0, 3.0),
		4.0,
		randf_range(-3.0, 3.0))
