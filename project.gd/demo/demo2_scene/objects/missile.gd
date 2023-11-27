class_name Missile
extends Node3D


## Missile speed
@export var speed : float = 3.0

## Kill radius
@export var radius : float = 0.8

## Missile target
@export var target : Vector3


func _process(delta : float) -> void:
	# Step towards the target
	var dir := target - global_position
	if dir.length() > 0.2:
		global_position += dir.normalized() * speed * delta
		return

	# Trigger the explosion
	set_process(false)
	$MeshInstance3D.visible = false
	$ExplosionSound.play()
	$Trail.emitting = false
	$Explosion.emitting = true

	# Kill all enemy in the radius
	var enemies := get_tree().get_nodes_in_group("enemy")
	for enemy in enemies:
		if enemy.global_position.distance_to(global_position) < radius:
			enemy.queue_free()

	# Wait for the explision to finish
	await get_tree().create_timer(3.0).timeout

	# Discard the missile
	queue_free()
