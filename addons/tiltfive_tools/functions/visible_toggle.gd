extends Node


## Tilt Five Tools Visible Toggle Node
##
## This node supports toggling the visibility of its parent based on a wand
## button press.


## Initial visibility
enum Initial {
	HIDE,		## Hide at start
	SHOW,		## Show at start
	DEFAULT		## No change
}


## Wand number (0 for default)
@export var wand : int = 2

## Toggle button (T5 menu button for default)
@export var toggle_button : String = "button_t5"

## Target node (null for parent)
@export var target : Node3D

## Initial state
@export var initial : Initial = Initial.HIDE


## Target Node3D
var _target : Node3D


# Called when the node enters the scene tree for the first time.
func _ready():
	# Get the target node
	_target = target if target else get_parent()
	if not _target:
		push_warning("VisibleToggle %s could not get target" % self)
		return

	# Subscribe to button events
	var wand_node := _find_wand()
	if wand_node:
		wand_node.button_pressed.connect(_on_button_pressed)

	# Apply initial visibility
	if initial == Initial.HIDE:
		_target.visible = false
	elif initial == Initial.SHOW:
		_target.visible = true


# Handle wand button presses
func _on_button_pressed(p_name : String) -> void:
	if p_name == toggle_button:
		_target.visible = not _target.visible


# Find the wand
func _find_wand() -> T5Controller3D:
	# Find the player
	var player := _find_player()
	if not player:
		return null

	# Find the wand
	var wand_node := player.get_player_wand(wand)
	if wand_node:
		return wand_node

	# Report failure
	push_warning("VisibleToggle %s could not find wand" % self)
	return null


# Find the player node associated with this node
func _find_player() -> T5ToolsPlayer:
	# Test if this node is a child of a character
	var character := T5ToolsCharacter.find_instance(self)
	if character:
		return character.player

	# Test if this node is a child of a player
	var player := T5ToolsPlayer.find_instance(self)
	if player:
		return player

	# Report failure
	push_warning("VisibleToggle %s could not find player" % self)
	return null
