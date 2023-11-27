extends Node3D


# Enemy scene
const ENEMY := preload("res://demo/demo2_scene/objects/enemy.tscn")


## Target node
@export var target : Node3D


func _ready():
	# Hook when the scene is coming to a close
	var scene := T5ToolsScene.get_current()
	scene.scene_pre_exiting.connect(_on_scene_pre_exiting)

	# Wait a random amount of time then start the spawn timer
	await get_tree().create_timer(randf_range(0.1, 1.0)).timeout
	$SpawnTimer.start()


func _on_scene_pre_exiting(_user_data) -> void:
	$SpawnTimer.stop()


func _on_spawn_timer_timeout() -> void:
	# Create the enemy
	var enemy : Enemy = ENEMY.instantiate()
	enemy.target = target
	add_child(enemy)
	enemy.position.x = randf_range(-0.4, 0.4)

	# Update the wait time
	$SpawnTimer.wait_time = clamp($SpawnTimer.wait_time - randf_range(0.0, 0.1), 0.3, 1.0)
