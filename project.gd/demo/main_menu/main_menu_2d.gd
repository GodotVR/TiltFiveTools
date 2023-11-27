extends Control


func _on_quit_pressed() -> void:
	get_tree().quit()


func _on_demo_pressed(demo : int) -> void:
	match demo:
		1:
			T5ToolsStaging.load_scene("res://demo/demo1_scene/demo1_scene.tscn")

		2:
			T5ToolsStaging.load_scene("res://demo/demo2_scene/demo2_scene.tscn")

		3:
			T5ToolsStaging.load_scene("res://demo/demo3_scene/demo3_scene.tscn")

		4:
			T5ToolsStaging.load_scene("res://demo/demo4_scene/demo4_scene.tscn")
