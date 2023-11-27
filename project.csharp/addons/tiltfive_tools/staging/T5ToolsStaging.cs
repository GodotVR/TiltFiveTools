using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Tilt Five Tools Staging Base
/// </summary>
/// <description>
/// ## This node manages transitions between scenes. It can be accessed globally
/// ## using "T5ToolsStagingBase.instance".
/// </description>
public partial class T5ToolsStaging : Node3D
{
    /// <summary>
    /// Signal emitted when a player is created
    /// </summary>
    /// <param name="player">New player</param>
    [Signal]
    public delegate void PlayerCreatedEventHandler(T5ToolsPlayer player);

    /// <summary>
    /// Signal emitted when a player is removed
    /// </summary>
    /// <param name="player">Removed player</param>
    [Signal]
    public delegate void PlayerRemovedEventHandler(T5ToolsPlayer player);

    /// <summary>
    /// Signal emitted when the game starts to exit the scene
    /// </summary>
    /// <param name="scene">Scene starting to exit</param>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void ScenePreExitingEventHandler(T5ToolsScene scene, Variant userData);

    /// <summary>
    /// Signal emitted when the game exits the scene
    /// </summary>
    /// <param name="scene">Scene exited</param>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneExitingEventHandler(T5ToolsScene scene, Variant userData);

    /// <summary>
    /// Signal emitted when the game loads this scene
    /// </summary>
    /// <param name="scene">Scene loaded</param>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneLoadedEventHandler(T5ToolsScene scene, Variant userData);

    /// <summary>
    /// Signal emitted when the game shows this scene
    /// </summary>
    /// <param name="scene">Scene visible</param>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneVisibleEventHandler(T5ToolsScene scene, Variant userData);

    /// <summary>
    /// Start scene
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")]
    public string StartScene { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary to hold general game-data
    /// </summary>
    public Godot.Collections.Dictionary<Variant, Variant> Data { get; } = new();

    /// <summary>
    /// List of players
    /// </summary>
    public List<T5ToolsPlayer> Players { get; } = new();

    /// <summary>
    /// The current scene
    /// </summary>
    public T5ToolsScene CurrentScene { get; private set; }

    /// <summary>
    /// The fade tween
    /// </summary>
    private Tween _fadeTween;

    /// <summary>
    /// Fade mesh
    /// </summary>
    private MeshInstance3D _fadeMesh;

    /// <summary>
    /// Node to hold scenes
    /// </summary>
    private Node3D _scene;

    /// <summary>
    /// Instance of the staging
    /// </summary>
    public static T5ToolsStaging Instance { get; private set; }

    /// <summary>
    /// Called when the node enters the scene tree for the first time.
    /// </summary>
    public override void _EnterTree()
    {
        base._EnterTree();

        // Save as the staging instance
        Instance = this;
    }

    /// <summary>
    /// Called when the node is about to leave the scene tree.
    /// </summary>
    public override void _ExitTree()
    {
        base._ExitTree();

        // Clear the staging instance
        Instance = null;
    }

    /// <summary>
    /// Called when the node is "ready".
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Save nodes
        _fadeMesh = GetNode<MeshInstance3D>("Fade");
        _scene = GetNode<Node3D>("Scene");

        // Do not initialise if in the editor
        if (Engine.IsEditorHint())
            return;

        // Connect T5Manager signals
        var manager = GetNode<T5Manager>("T5Manager");
        manager.XRRigWasAdded += OnXRRigWasAdded;
        manager.XRRigWillBeRemoved += OnXRRigWillBeRemoved;

        // Start by loading the start scene
        if (!string.IsNullOrEmpty(StartScene))
            DoLoadScene(StartScene);
    }

    protected async Task DoLoadScene(string scenePath, Variant userData = new())
    {
        // Log request
        GD.Print($"T5ToolsStaging: Request to load {scenePath}");

        // Start background loading of the resource
        ResourceLoader.LoadThreadedRequest(scenePath);

        // Start by unloading the current scene
        if (CurrentScene != null)
        {
            // Report about to exit the current scene
            GD.Print("T5ToolsStaging: Reporting ScenePreExiting");
            EmitSignal(SignalName.ScenePreExiting, CurrentScene, userData);
            CurrentScene.EmitSignal(SignalName.ScenePreExiting, userData);

            // Fade to black
            GD.Print("T5ToolsStaging: Fading out");
            _fadeTween?.Kill();
            _fadeTween = GetTree().CreateTween();
            _fadeTween.TweenMethod(Callable.From((float fade) => SetFade(fade)), 0.0f, 1.0f, 1.0f);
            await ToSignal(_fadeTween, "finished");

            // Report the exit of the current scene
            GD.Print("T5ToolsStaging: Reporting SceneExiting");
            EmitSignal(SignalName.SceneExiting, CurrentScene, userData);
            CurrentScene.EmitSignal(SignalName.SceneExiting, userData);

            // Discard the current scene
            GD.Print("T5ToolsStaging: Discarding old scene");
            _scene.RemoveChild(CurrentScene);
            CurrentScene.QueueFree();
            CurrentScene = null;

            // Zero all player origins. The new scene can choose to relocate
            // but its safest to just zero in case
            foreach (var origin in Players.Select(p => p.Origin))
                origin.GlobalTransform = Transform3D.Identity;
        }

        // Load the new scene
        GD.Print("T5ToolsStaging: Loading new scene");
        var newScene = ResourceLoader.Load<PackedScene>(scenePath);

        // Instantiate the scene
        GD.Print("T5ToolsStaging: Instantiating new scene");
        CurrentScene = newScene.Instantiate<T5ToolsScene>();
        _scene.AddChild(CurrentScene);

        // Report the new scene is loaded
        GD.Print("T5ToolsStaging: Reporting SceneLoaded");
        EmitSignal(SignalName.SceneLoaded, CurrentScene, userData);
        CurrentScene.EmitSignal(SignalName.SceneLoaded, userData);

        // Fade to visible
        GD.Print("T5ToolsStaging: Fading in");
        _fadeTween?.Kill();
        _fadeTween = GetTree().CreateTween();
        _fadeTween.TweenMethod(Callable.From((float fade) => SetFade(fade)), 1.0f, 0.0f, 1.0f);
        await ToSignal(_fadeTween, "finished");

        // Report the new scene is visible
        GD.Print("T5ToolsStaging: Reporting SceneVisible");
        EmitSignal(SignalName.SceneVisible, CurrentScene, userData);
        CurrentScene.EmitSignal(SignalName.SceneVisible, userData);
    }

    private void SetFade(float fade)
    {
        if (fade <= 0.0f)
        {
            _fadeMesh.Visible = false;
        }
        else
        {
            var material = _fadeMesh.GetSurfaceOverrideMaterial(0) as ShaderMaterial;
            material?.SetShaderParameter("alpha", fade);
            _fadeMesh.Visible = true;
        }
    }

    protected void OnXRRigWasAdded(T5XRRig rig)
    {
        // Ignore if the rig isn't a player
        if (rig is not T5ToolsPlayer player)
            return;

        // Add the player
        GD.Print($"T5ToolsStaging: Player {player} added");
        Players.Add(player);
        EmitSignal(SignalName.PlayerCreated, player);
    }

    protected void OnXRRigWillBeRemoved(T5XRRig rig)
    {
        // Ignore if the rig isn't a player
        if (rig is not T5ToolsPlayer player)
            return;

        GD.Print($"T5ToolsStaging: Player {player} removed");
        Players.Remove(player);
        EmitSignal(SignalName.PlayerRemoved, player);
    }

    /// <summary>
    /// Load the requested scene
    /// </summary>
    /// <param name="scenePath">Scene path</param>
    /// <param name="userData">Custom data</param>
    public static void LoadScene(string scenePath, Variant userData = new())
    {
        Instance.DoLoadScene(scenePath, userData);
    }
}
