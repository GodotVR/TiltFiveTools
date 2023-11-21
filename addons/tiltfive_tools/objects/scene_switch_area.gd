@tool
class_name SceneSwitchArea
extends Area3D


## Enabled flag
@export var enabled : bool = true

## Target scene
@export_file('*.tscn') var target_scene : String

## Target location
@export var location : String


func _ready() -> void:
	# Do not run if in the editor
	if Engine.is_editor_hint():
		return

	# Subscribe to body entered events
	body_entered.connect(_on_body_entered)


func _get_configuration_warnings() -> PackedStringArray:
	var warnings := PackedStringArray()

	# Verify the controller
	if not target_scene:
		warnings.append("Target scene must be specified")

	return warnings


func _on_body_entered(_body : Node3D) -> void:
	# Skip if not enabled
	if not enabled:
		return

	# Disable to prevent repeated invocation
	enabled = false
	T5ToolsStaging.load_scene(target_scene, location)
