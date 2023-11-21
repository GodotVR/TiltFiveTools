class_name T5ToolsCharacter
extends Node3D


## Player associated with this character
@export var player : T5ToolsPlayer


## Find the T5ToolsCharacter from a child node
static func find_instance(node : Node) -> T5ToolsCharacter:
	# Walk the node tree
	while node:
		# If we have the character then return it
		if node is T5ToolsCharacter:
			return node

		# Walk up to the parent
		node = node.get_parent()

	# Not found
	return null
