using Godot;

#nullable enable

public partial class MainMenu2D : Control
{
    public void OnQuitPressed()
    {
        GetTree().Quit();
    }

    public void OnDemoPressed(int demoNumber)
    {
        switch (demoNumber)
        {
            case 1:
                T5ToolsStaging.LoadScene("res://demo/demo1_scene/demo1_scene.tscn");
                break;

            case 2:
                T5ToolsStaging.LoadScene("res://demo/demo2_scene/demo2_scene.tscn");
                break;

            case 3:
                T5ToolsStaging.LoadScene("res://demo/demo3_scene/demo3_scene.tscn");
                break;

            case 4:
                T5ToolsStaging.LoadScene("res://demo/demo4_scene/demo4_scene.tscn");
                break;

        }
    }
}