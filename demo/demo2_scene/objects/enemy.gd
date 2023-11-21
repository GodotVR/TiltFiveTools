class_name Enemy
extends CharacterBody3D


## Move speed
@export var speed := 0.5

## Target position
@export var target : Node3D


var _exiting := false


func _ready() -> void:
	# Subscribe to scene events
	var scene := T5ToolsScene.get_current()
	scene.scene_pre_exiting.connect(_on_pre_exiting)


func _on_pre_exiting(_user_data) -> void:
	_exiting = true


func _physics_process(delta : float) -> void:
	var dir := target.global_position - global_position
	if dir.length() > 0.2:
		dir = dir.normalized() * speed
		velocity.x = dir.x
		velocity.y -= 9.8 * delta
		velocity.z = dir.z
		move_and_slide()
		return

	if not _exiting:
		T5ToolsStaging.load_scene("res://demo/main_menu/main_menu.tscn")
		_exiting = true
