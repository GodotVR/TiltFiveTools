class_name T5ToolsPointerEvent


## Types of pointer events
enum Type {
	## Pointer entered target
	ENTERED,

	## Pointer exited target
	EXITED,

	## Pointer pressed target
	PRESSED,

	## Pointer released target
	RELEASED,

	## Pointer moved on target
	MOVED
}


## Type of pointer event
var event_type : Type

## Player
var player : T5ToolsPlayer

## Pointer generating event
var pointer : Node3D

## Target of pointer
var target : Node3D

## Point position
var position : Vector3

## Last point position
var last_position : Vector3


## Initialize a new instance of the T5ToolsPointerEvent class
func _init(
		p_event_type : Type,
		p_player : T5ToolsPlayer,
		p_pointer : Node3D,
		p_target : Node3D,
		p_position : Vector3,
		p_last_position : Vector3) -> void:
	event_type = p_event_type
	player = p_player
	pointer = p_pointer
	target = p_target
	position = p_position
	last_position = p_last_position


## Report a pointer entered event
static func entered(
		player : T5ToolsPlayer,
		pointer : Node3D,
		target : Node3D,
		at : Vector3) -> void:
	report(
		T5ToolsPointerEvent.new(
			Type.ENTERED,
			player,
			pointer,
			target,
			at,
			at))


## Report pointer moved event
static func moved(
		player : T5ToolsPlayer,
		pointer : Node3D,
		target : Node3D,
		to : Vector3,
		from : Vector3) -> void:
	report(
		T5ToolsPointerEvent.new(
			Type.MOVED,
			player,
			pointer,
			target,
			to,
			from))


## Report pointer pressed event
static func pressed(
		player : T5ToolsPlayer,
		pointer : Node3D,
		target : Node3D,
		at : Vector3) -> void:
	report(
		T5ToolsPointerEvent.new(
			Type.PRESSED,
			player,
			pointer,
			target,
			at,
			at))


## Report pointer released event
static func released(
		player : T5ToolsPlayer,
		pointer : Node3D,
		target : Node3D,
		at : Vector3) -> void:
	report(
		T5ToolsPointerEvent.new(
			Type.RELEASED,
			player,
			pointer,
			target,
			at,
			at))


## Report a pointer exited event
static func exited(
		player : T5ToolsPlayer,
		pointer : Node3D,
		target : Node3D,
		last : Vector3) -> void:
	report(
		T5ToolsPointerEvent.new(
			Type.EXITED,
			player,
			pointer,
			target,
			last,
			last))


## Report a pointer event
static func report(event : T5ToolsPointerEvent) -> void:
	# Fire event on pointer
	if is_instance_valid(event.pointer):
		if event.pointer.has_signal("pointing_event"):
			event.pointer.emit_signal("pointing_event", event)

	# Fire event/method on the target if it's valid
	if is_instance_valid(event.target):
		if event.target.has_signal("pointer_event"):
			event.target.emit_signal("pointer_event", event)
		elif event.target.has_method("pointer_event"):
			event.target.pointer_event(event)
