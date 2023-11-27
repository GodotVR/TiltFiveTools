extends Node


## Button triggering zoom-in
@export var zoom_in_button := "button_a"

## Button triggering zoom-out
@export var zoom_out_button := "button_y"

## Minimum zoom level
@export var zoom_min := 4.0

## Maximum zoom level
@export var zoom_max := 20.0

## Zoom step on each button press
@export var zoom_step := 1.2


# Origin node
var _origin : T5Origin3D


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	# Get the origin
	_origin = T5ToolsPlayer.find_instance(self).get_origin()

	# Bind to the parent wand controller inputs
	var controller = get_parent() as T5Controller3D
	controller.button_pressed.connect(_on_button_pressed)


# Handle controller button presses
func _on_button_pressed(p_name : String) -> void:
	# Process the button
	match p_name:
		zoom_in_button:
			var zoom := _origin.gameboard_scale
			zoom = clamp(zoom / zoom_step, zoom_min, zoom_max)
			_origin.gameboard_scale = zoom

		zoom_out_button:
			var zoom := _origin.gameboard_scale
			zoom = clamp(zoom * zoom_step, zoom_min, zoom_max)
			_origin.gameboard_scale = zoom
