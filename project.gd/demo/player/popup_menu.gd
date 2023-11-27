extends Control


func _on_back_pressed():
	T5ToolsStaging.load_scene("res://demo/main_menu/main_menu.tscn")


func _on_quit_pressed():
	get_tree().quit()
