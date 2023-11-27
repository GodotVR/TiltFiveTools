extends T5ToolsRigidBodyController


# Last hit test
var _last_hit := false


# Check for collisions and speed to fire sound effects
func _integrate_forces(state : PhysicsDirectBodyState3D) -> void:
	# Process the contacts for hits and fastest rolling speed
	var hit := false
	var speed := 0.0
	for c in state.get_contact_count():
		# Test for a velocity striking into the contact surface
		var cnorm := state.get_contact_local_normal(c)
		var cvel := state.get_contact_local_velocity_at_position(c)
		if cnorm.dot(cvel) < -2.0:
			hit = true

		# Get the maximum speed at the contact point
		var pos := to_local(state.get_contact_local_position(c))
		var vel := state.get_velocity_at_local_position(pos)
		speed = max(speed, vel.length())

	# Adjust rolling volume based on speed
	speed = clamp(speed / 5.0, 0.0, 1.0)
	$RollingPlayer.volume_db = linear_to_db(speed)

	# Play hit sounds on hit
	if hit and not _last_hit:
		$HitPlayer.play()
	_last_hit = hit
