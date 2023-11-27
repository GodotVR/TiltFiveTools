class_name T5ToolsStaging
extends Node3D


## Tilt Five Tools Staging Base
##
## This node manages transitions between scenes. It can be accessed globally
## using "T5ToolsStagingBase.instance".


signal player_created(player : T5ToolsPlayer)
signal player_removed(player : T5ToolsPlayer)
signal scene_pre_exiting(scene : T5ToolsScene, user_data : Variant)
signal scene_exiting(scene : T5ToolsScene, user_data : Variant)
signal scene_loaded(scene : T5ToolsScene, user_data : Variant)
signal scene_visible(scene : T5ToolsScene, user_data : Variant)


## Start scene
@export_file('*.tscn') var start_scene : String


## Variable to hold general game-data
var data : Dictionary = {}

## Array of players
var players : Array[T5ToolsPlayer] = []

# The current scene
var current_scene : T5ToolsScene


# The fade tween
var _fade_tween : Tween


## Instance of the staging
static var instance : T5ToolsStaging


func _enter_tree():
	# Save as the staging instance
	instance = self


func _exit_tree():
	# Clear the staging instance
	instance = null


func _ready() -> void:
	# Do not initialise if in the editor
	if Engine.is_editor_hint():
		return

	# Connect player events
	$T5Manager.xr_rig_was_added.connect(_on_xr_rig_was_added)
	$T5Manager.xr_rig_will_be_removed.connect(_on_xr_rig_will_be_removed)

	# Start by loading the start scene
	do_load_scene(start_scene, null)


# Load a scene
func do_load_scene(p_scene_path : String, user_data : Variant) -> void:
	# Log request
	print_verbose("T5ToolsStaging: Request to load %s" % p_scene_path)

	# Start background loading of the resource
	ResourceLoader.load_threaded_request(p_scene_path)

	# Start by unloading the current scene
	if current_scene:
		# Report about to exit the current scene
		print_verbose("T5ToolsStaging: Reporting scene_pre_exiting")
		scene_pre_exiting.emit(current_scene, user_data)
		current_scene.scene_pre_exiting.emit(user_data)

		# Fade to black
		print_verbose("T5ToolsStaging: Fading out")
		if _fade_tween: _fade_tween.kill()
		_fade_tween = get_tree().create_tween()
		_fade_tween.tween_method(_set_fade, 0.0, 1.0, 1.0)
		await _fade_tween.finished

		# Report the exit of the current scene
		print_verbose("T5ToolsStaging: Reporting scene_exiting")
		scene_exiting.emit(current_scene, user_data)
		current_scene.scene_exiting.emit(user_data)

		# Discard the current scene
		print_verbose("T5ToolsStaging: Discarding old scene")
		$Scene.remove_child(current_scene)
		current_scene.queue_free()
		current_scene = null

		# Zero all player origins. The new scene can choose to relocate
		# but it's safest to just zero in case
		for player in players:
			player.get_origin().global_transform = Transform3D.IDENTITY

	# Load the new scene
	print_verbose("T5ToolsStaging: Loading new scene")
	var new_scene : PackedScene = ResourceLoader.load_threaded_get(p_scene_path)

	# Instantiate the scene
	print_verbose("T5ToolsStaging: Instantiating new scene")
	current_scene = new_scene.instantiate()
	$Scene.add_child(current_scene)

	# Report the new scene is loaded
	print_verbose("T5ToolsStaging: Reporting scene_loaded")
	current_scene.scene_loaded.emit(user_data)
	scene_loaded.emit(current_scene, user_data)

	# Fade to visible
	print_verbose("T5ToolsStaging: Fading in")
	if _fade_tween: _fade_tween.kill()
	_fade_tween = get_tree().create_tween()
	_fade_tween.tween_method(_set_fade, 1.0, 0.0, 1.0)
	await _fade_tween.finished

	# Report the new scene is visible
	print_verbose("T5ToolsStaging: Reporting scene_visible")
	current_scene.scene_visible.emit(user_data)
	scene_visible.emit(current_scene, user_data)


func _set_fade(p_fade : float) -> void:
	if p_fade == 0.0:
		$Fade.visible = false
	else:
		var material : ShaderMaterial = $Fade.get_surface_override_material(0)
		if material:
			material.set_shader_parameter("alpha", p_fade)
		$Fade.visible = true


# Handle player added
func _on_xr_rig_was_added(rig : T5XRRig) -> void:
	# Ignore if the rig isn't a player
	var player = rig as T5ToolsPlayer
	if not player:
		return

	print_verbose("T5ToolsStaging: Player %s added" % player)
	players.append(player)
	player_created.emit(player)


# Handle player removed
func _on_xr_rig_will_be_removed(rig : T5XRRig) -> void:
	# Ignore if the rig isn't a player
	var player = rig as T5ToolsPlayer
	if not player:
		return

	print_verbose("T5ToolsStaging: Player %s removed" % player)
	players.erase(player)
	player_removed.emit(player)


## Load the requested scene
static func load_scene(p_scene_path : String, user_data : Variant = null) -> void:
	instance.do_load_scene(p_scene_path, user_data)
