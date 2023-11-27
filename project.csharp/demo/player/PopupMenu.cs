using Godot;

#nullable enable

public partial class PopupMenu : Control
{
    public void OnBackPressed()
    {
        T5ToolsStaging.LoadScene("res://demo/main_menu/main_menu.tscn");
    }

    public void OnQuitPressed()
    {
        GetTree().Quit();
    }
}