using System;
using Godot;
using Godot.Collections;

/// <summary>
/// Tilt Five Tools Viewport2Din3D
/// </summary>
/// <description>
/// This script manages a 2D scene rendered as a texture on a 3D quad. The
/// Visible and Collision layers control visibility and collision for all
/// players; however if the Viewport2Din3D is a child of a player then the
/// players visibility and collision are automatically included. This allows
/// for UIs specific to a player by adding them to the custom player scene.
/// </description>
[Tool]
public partial class T5ToolsViewport2Din3D : Node3D
{
    /// <summary>
    /// Transparency mode
    /// </summary>
    public enum TransparencyMode
    {
        /// <summary>
        /// Render opaque
        /// </summary>
        Opaque,

        /// <summary>
        /// Render transparent
        /// </summary>
        Transparent,

        /// <summary>
        /// Render using alpha-scissor
        /// </summary>
        Scissor
    }

    /// <summary>
    /// Viewport update mode
    /// </summary>
    public enum ViewportUpdateMode
    {
        /// <summary>
        /// Update once (redraw triggered if set again to UpdateOnce
        /// </summary>
        UpdateOnce,

        /// <summary>
        /// Update on every frame
        /// </summary>
        UpdateAlways,

        /// <summary>
        /// Update at throttled rate
        /// </summary>
        UpdateThrottled
    }

    /// <summary>
    /// State dirty flags
    /// </summary>
    [Flags]
    private enum Dirty
    {
        Material = 1,
        Scene = 2,
        Size = 4,
        Albedo = 8,
        Update = 16,
        Transparency = 32,
        AlphaScissor = 64,
        Unshaded = 128,
        Filtered = 256,
        Surface = 512,
        Redraw = 1024,
        All = 2047
    }

    /// <summary>
    /// Default visual layer of 1:Everyone
    /// </summary>
    private const uint VisualDefault = 1U;

    /// <summary>
    /// Default physics layer of 21:Pointable
    /// </summary>
    private const uint PhysicsDefault = 0b0000_0000_0001_0000_0000_0000_0000_0000U;

    /// <summary>
    /// Physical screen size backing field
    /// </summary>
    private Vector2 _screenSize = new(3.0f, 2.0f);

    /// <summary>
    /// Collision layer backing field
    /// </summary>
    private uint _collisionLayer = PhysicsDefault;

    /// <summary>
    /// Scene backing field
    /// </summary>
    private PackedScene _scene;

    /// <summary>
    /// Viewport size backing field
    /// </summary>
    private Vector2 _viewportSize = new(300.0f, 200.0f);

    /// <summary>
    /// Viewport update mode backing field
    /// </summary>
    private ViewportUpdateMode _updateMode = ViewportUpdateMode.UpdateAlways;

    /// <summary>
    /// Visible layers backing field
    /// </summary>
    private uint _visibleLayers = VisualDefault;

    /// <summary>
    /// Custom material template backing field
    /// </summary>
    private StandardMaterial3D _material;

    /// <summary>
    /// Transparent mode backing field
    /// </summary>
    private TransparencyMode _transparent = TransparencyMode.Transparent;

    /// <summary>
    /// Alpha scissor threshold backing field
    /// </summary>
    private float _alphaScissorThreshold = 0.25f;

    /// <summary>
    /// Unshaded backing field
    /// </summary>
    private bool _unshaded;
    
    /// <summary>
    /// Filter backing field
    /// </summary>
    private bool _filter = true;

    /// <summary>
    /// Current scene node
    /// </summary>
    private Node _sceneNode;

    /// <summary>
    /// Viewport texture
    /// </summary>
    private ViewportTexture _viewportTexture;

    /// <summary>
    /// Time since the last update
    /// </summary>
    private double _timeSinceLastUpdate;

    /// <summary>
    /// Screen material
    /// </summary>
    private StandardMaterial3D _screenMaterial;

    /// <summary>
    /// Viewport instance
    /// </summary>
    private SubViewport _viewport;

    /// <summary>
    /// Screen mesh instance
    /// </summary>
    private MeshInstance3D _screenMesh;

    /// <summary>
    /// Screen quad mesh
    /// </summary>
    private QuadMesh _screenQuadMesh;

    /// <summary>
    /// Screen body
    /// </summary>
    private T5ToolsViewport2Din3DBody _screenBody;

    /// <summary>
    /// Screen collision shape
    /// </summary>
    private CollisionShape3D _screenShape;

    /// <summary>
    /// Screen box shape
    /// </summary>
    private BoxShape3D _screenBoxShape;

    /// <summary>
    /// Owning player (null if global)
    /// </summary>
    private T5ToolsPlayer _player;

    /// <summary>
    /// Dirty flags
    /// </summary>
    private Dirty _dirty = Dirty.All;

    /// <summary>
    /// Signal when pointer event occurs
    /// </summary>
    /// <param name="pointerEvent">Pointer event</param>
    [Signal]
    public delegate void PointerEventEventHandler(T5ToolsPointerEvent pointerEvent);


    /// Physical screen size property
    [ExportGroup("Physics")]
    [Export]
    public Vector2 ScreenSize
    {
        get => _screenSize;
        set => SetScreenSize(value);
    }

    /// <summary>
    /// Collision layer
    /// </summary>
    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionLayer
    {
        get => _collisionLayer;
        set => SetCollisionLayer(value);
    }

    /// <summary>
    /// Scene property
    /// </summary>
    [ExportGroup("Content")]
    [Export]
    public PackedScene Scene
    {
        get => _scene;
        set => SetScene(value);
    }

    /// <summary>
    /// Viewport size property
    /// </summary>
    [Export]
    public Vector2 ViewportSize
    {
        get => _viewportSize;
        set => SetViewportSize(value);
    }

    /// <summary>
    /// Update mode property
    /// </summary>
    [Export]
    public ViewportUpdateMode UpdateMode
    {
        get => _updateMode;
        set => SetUpdateMode(value);
    }

    /// <summary>
    /// Update throttle property
    /// </summary>
    [Export]
    public float ThrottleFps { get; set; } = 30.0f;

    /// <summary>
    /// Visible layers
    /// </summary>
    [ExportGroup("Rendering")]

    [Export(PropertyHint.Layers3DRender)]
    public uint VisibleLayers
    {
        get => _visibleLayers;
        set => SetVisibleLayers(value);
    }

    /// <summary>
    /// Custom material template
    /// </summary>
    [Export]
    public StandardMaterial3D Material
    {
        get => _material;
        set => SetMaterial(value);
    }

    /// <summary>
    /// Transparent property
    /// </summary>
    [Export]
    public TransparencyMode Transparent
    {
        get => _transparent;
        set => SetTransparent(value);
    }

    /// <summary>
    /// Alpha Scissor Threshold property (ignored when custom material provided)
    /// </summary>
    public float AlphaScissorThreshold
    {
        get => _alphaScissorThreshold;
        set => SetAlphaScissorThreshold(value);
    }

    /// <summary>
    /// Unshaded flag (ignored when custom material provided)
    /// </summary>
    public bool Unshaded
    {
        get => _unshaded;
        set => SetUnshaded(value);
    }

    /// <summary>
    /// Filtered flag (ignored when custom material provided)
    /// </summary>
    public bool Filtered
    {
        get => _filter;
        set => SetFilter(value);
    }

    /// <summary>
    /// Called when the node is "ready"
    /// </summary>
    public override void _Ready()
    {
        base._Ready();

        // Get the nodes
        _viewport = GetNode<SubViewport>("Viewport");
        _screenMesh = GetNode<MeshInstance3D>("Screen");
        _screenBody = GetNode<T5ToolsViewport2Din3DBody>("StaticBody3D");
        _screenShape = GetNode<CollisionShape3D>("StaticBody3D/CollisionShape3D");
        _screenQuadMesh = _screenMesh.Mesh as QuadMesh;
        _screenBoxShape = _screenShape.Shape as BoxShape3D;

        // Test if the viewport is under a player
        _player = T5ToolsPlayer.FindInstance(this);

        // Listen for pointer events on the screen body
        var body = GetNode<T5ToolsViewport2Din3DBody>("StaticBody3D");
        body.PointerEvent += OnPointerEvent;

        // Update enabled based on visibility
        VisibilityChanged += OnVisibilityChanged;

        // Apply physics properties
        UpdateScreenSize();
        UpdateVisibleLayers();
        UpdateCollisionLayer();
        UpdateEnabled();

        // Update the render objects
        UpdateRender();
    }

    public override Array<Dictionary> _GetPropertyList()
    {
        // Select visibility of properties
        var showAlphaScissor = _material == null && _transparent == TransparencyMode.Scissor;
        var showUnshaded = _material == null;
        var showFilter = _material == null;

        return new Array<Dictionary>
        {
            new()
            {
                { "name", "Rendering" },
                { "type", (int)Variant.Type.Nil },
                { "usage", (int)PropertyUsageFlags.Group }
            },
            new()
            {
                { "name", "AlphaScissorThreshold" },
                { "type", (int)Variant.Type.Float },
                { "usage", (int)(showAlphaScissor ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor) },
                { "hint", (int)PropertyHint.Range },
                { "hint_string", "0.0,1.0" }
            },
            new()
            {
                { "name", "Unshaded" },
                { "type", (int)Variant.Type.Bool },
                { "usage", (int)(showUnshaded ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor) }
            },
            new()
            {
                { "name", "Filter" },
                { "type", (int)Variant.Type.Bool },
                { "usage", (int)(showFilter ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor) }
            }
        };
    }

    /// <summary>
    /// Allow revert of custom properties
    /// </summary>
    /// <param name="property">Property name</param>
    /// <returns>True if can be reverted</returns>
    public override bool _PropertyCanRevert(StringName property)
    {
        if (property == "alpha_scissor_threshold") return true;
        if (property == "unshaded") return true;
        if (property == "filter") return true;
        return base._PropertyCanRevert(property);
    }

    /// <summary>
    /// Provide revert values for custom properties
    /// </summary>
    /// <param name="property">Property name</param>
    /// <returns>Property revert value</returns>
    public override Variant _PropertyGetRevert(StringName property)
    {
        if (property == "alpha_scissor_threshold") return 0.25f;
        if (property == "unshaded") return false;
        if (property == "filter") return true;
        return base._PropertyGetRevert(property);
    }

    /// <summary>
    /// Get the 2D scene instance
    /// </summary>
    /// <returns>2D scene instance</returns>
    public Node GetSceneInstance() => _sceneNode;

    /// <summary>
    /// Connect a 2D scene signal
    /// </summary>
    /// <param name="signal">Signal name</param>
    /// <param name="callback">Callback</param>
    /// <param name="flags">Connection flags</param>
    public void ConnectSceneSignal(string signal, Callable callback, uint flags = 0)
    {
        _sceneNode?.Connect(signal, callback, flags);
    }

    /// <summary>
    /// Handle pointer event from screen-body
    /// </summary>
    /// <param name="pointerEvent">Pointer event</param>
    private void OnPointerEvent(T5ToolsPointerEvent pointerEvent)
    {
        EmitSignal(SignalName.PointerEvent, pointerEvent);
    }

    /// <summary>
    /// Handler for input events
    /// </summary>
    /// <param name="event">Input event</param>
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton)
            _viewport.PushInput(@event);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // Process screen refreshing
        if (Engine.IsEditorHint())
        {
            _timeSinceLastUpdate += delta;
            if (_timeSinceLastUpdate > 1.0)
            {
                _timeSinceLastUpdate = 0.0;
                // Trigger material refresh
                _dirty |= Dirty.Material;
                UpdateRender();
            }
        }
        else if (_updateMode == ViewportUpdateMode.UpdateThrottled)
        {
            // Perform throttled updates of the viewport
            _timeSinceLastUpdate += delta;
            if (_timeSinceLastUpdate > 1.0 / ThrottleFps)
            {
                _timeSinceLastUpdate = 0.0;
                // Trigger update
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
            }
        }
        else
        {
            // This is no longer needed
            SetProcess(false);
        }
    }

    /// <summary>
    /// Handle visibility changed
    /// </summary>
    private void OnVisibilityChanged()
    {
        // Update enabled state
        UpdateEnabled();

        // Fire visibility changed in scene
        _sceneNode?.PropagateNotification(
            (int)CanvasItem.NotificationVisibilityChanged);
    }

    /// <summary>
    /// Handle setting screen size property
    /// </summary>
    /// <param name="size">New screen size</param>
    private void SetScreenSize(Vector2 size)
    {
        _screenSize = size;
        if (IsInsideTree())
            UpdateScreenSize();
    }

    /// <summary>
    /// Handle setting collision layer property
    /// </summary>
    /// <param name="layer">New collision layer</param>
    private void SetCollisionLayer(uint layer)
    {
        _collisionLayer = layer;
        if (IsInsideTree())
            UpdateCollisionLayer();
    }

    /// <summary>
    /// Handle setting scene property
    /// </summary>
    /// <param name="scene">New scene</param>
    private void SetScene(PackedScene scene)
    {
        _scene = scene;
        _dirty |= Dirty.Scene;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting viewport size property
    /// </summary>
    /// <param name="size">New viewport size</param>
    private void SetViewportSize(Vector2 size)
    {
        _viewportSize = size;
        _dirty |= Dirty.Size;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting update mode property
    /// </summary>
    /// <param name="mode">New update mode</param>
    private void SetUpdateMode(ViewportUpdateMode mode)
    {
        _updateMode = mode;
        _dirty |= Dirty.Update;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting visible layers property
    /// </summary>
    /// <param name="layers">Visible layers</param>
    private void SetVisibleLayers(uint layers)
    {
        _visibleLayers = layers;
        _dirty |= Dirty.Surface;
        if (IsInsideTree())
            UpdateVisibleLayers();
    }

    /// <summary>
    /// Handle setting material property
    /// </summary>
    /// <param name="material">New material</param>
    private void SetMaterial(StandardMaterial3D material)
    {
        _material = material;
        NotifyPropertyListChanged();
        _dirty |= Dirty.Material;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting transparency property
    /// </summary>
    /// <param name="transparent">New transparency mode</param>
    private void SetTransparent(TransparencyMode transparent)
    {
        _transparent = transparent;
        NotifyPropertyListChanged();
        _dirty |= Dirty.Transparency;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting alpha scissor threshold property
    /// </summary>
    /// <param name="threshold">New threshold</param>
    private void SetAlphaScissorThreshold(float threshold)
    {
        _alphaScissorThreshold = threshold;
        _dirty |= Dirty.AlphaScissor;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting unshaded property
    /// </summary>
    /// <param name="unshaded">New unshaded flag</param>
    private void SetUnshaded(bool unshaded)
    {
        _unshaded = unshaded;
        _dirty |= Dirty.Unshaded;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Handle setting filter property
    /// </summary>
    /// <param name="filter">New filter flag</param>
    private void SetFilter(bool filter)
    {
        _filter = filter;
        _dirty |= Dirty.Filtered;
        if (IsInsideTree())
            UpdateRender();
    }

    /// <summary>
    /// Screen size update handler
    /// </summary>
    private void UpdateScreenSize()
    {
        _screenQuadMesh.Size = _screenSize;
        _screenBody.ScreenSize = _screenSize;
        _screenBoxShape.Size = new Vector3(_screenSize.X, _screenSize.Y, 0.02f);
    }

    /// <summary>
    /// Enabled update handler
    /// </summary>
    private void UpdateEnabled()
    {
        // Ignore if in editor
        if (Engine.IsEditorHint())
            return;

        // Update the screen shape disabled flag
        _screenShape.Disabled = !IsVisibleInTree();
    }

    /// <summary>
    /// Collision layer update handler
    /// </summary>
    private void UpdateCollisionLayer()
    {
        // Ignore if in editor
        if (Engine.IsEditorHint())
            return;

        // Calculate the collision layer
        var layer = _collisionLayer;
        if (_player != null)
            layer |= _player.GetPlayerPhysicsLayer();

        // Update the collision layer
        _screenBody.CollisionLayer = layer;
    }

    /// <summary>
    /// Visible layers update handler
    /// </summary>
    private void UpdateVisibleLayers()
    {
        // Ignore if in editor
        if (Engine.IsEditorHint())
            return;

        // Calculate the visible layers
        var layers = _visibleLayers;
        if (_player != null)
            layers |= _player.GetPlayerVisibleLayer();

        // Update the visible layers
        _screenMesh.Layers = layers;
    }

    /// <summary>
    /// Update render objects based on dirty flags
    /// </summary>
    private void UpdateRender()
    {
        // Handle material change
        if (_dirty.HasFlag(Dirty.Material))
        {
            _dirty &= ~Dirty.Material;

            // Construct the new screen material
            if (_material != null)
            {
                // Copy custom material
                _screenMaterial = _material.Duplicate() as StandardMaterial3D;
            }
            else
            {
                // Create new local material
                _screenMaterial = new StandardMaterial3D();

                // Disable culling
                _screenMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

                // Ensure local material is configured
                _dirty |= Dirty.Transparency | Dirty.AlphaScissor | Dirty.Unshaded | Dirty.Filtered;
            }

            // Ensure new material renders viewport onto surface
            _dirty |= Dirty.Albedo | Dirty.Surface;
        }

        // If we have no screen material then skip everything else
        if (_screenMaterial == null)
            return;

        // Handle scene change
        if (_dirty.HasFlag(Dirty.Scene))
        {
            _dirty &= ~Dirty.Scene;

            // Out with the old
            if (GodotObject.IsInstanceValid(_sceneNode))
            {
                _viewport.RemoveChild(_sceneNode);
                _sceneNode?.QueueFree();
            }

            // In with the new
            if (_scene != null)
            {
                // Instantiate provided scene
                _sceneNode = _scene.Instantiate<Node>();
                _viewport.AddChild(_sceneNode);
            }
            else if (_viewport.GetChildCount() > 0)
            {
                // Use already-provided scene
                _sceneNode = _viewport.GetChild<Node>(0);
            }

            // Ensure the new scene is rendered at least once
            _dirty |= Dirty.Redraw;
        }

        // Handle viewport size change
        if (_dirty.HasFlag(Dirty.Size))
        {
            _dirty &= ~Dirty.Size;

            // Update the viewport size
            _viewport.Size = (Vector2I)_viewportSize;
            _screenBody.ViewportSize = _viewportSize;

            // Update our viewport texture, it will have changed
            _dirty |= Dirty.Albedo;
        }

        // Handle albedo change
        if (_dirty.HasFlag(Dirty.Albedo))
        {
            _dirty &= ~Dirty.Albedo;

            // Set the screen material to use the viewport for the albedo channel
            _viewportTexture = _viewport.GetTexture();
            _screenMaterial.AlbedoTexture = _viewportTexture;
        }

        // Handle update mode change
        if (_dirty.HasFlag(Dirty.Update))
        {
            _dirty &= ~Dirty.Update; 

            // Apply update rules
            if (Engine.IsEditorHint() || _updateMode == ViewportUpdateMode.UpdateThrottled)
            {
                // Update once. Process function used for periodic updates
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                SetProcess(true);
            }
            else if (_updateMode == ViewportUpdateMode.UpdateOnce)
            {
                // Update once. Process function not used
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
                SetProcess(false);
            }
            else if (_updateMode == ViewportUpdateMode.UpdateAlways)
            {
                // Update always. Process function not used
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
                SetProcess(false);
            }
        }

        // Handle transparency update
        if (_dirty.HasFlag(Dirty.Transparency))
        {
            _dirty &= ~Dirty.Transparency;

            // If using a temporary material then update transparency
            if (_material == null)
            {
                // Set material transparency
                _screenMaterial.Transparency = _transparent switch
                {
                    TransparencyMode.Opaque => BaseMaterial3D.TransparencyEnum.Disabled,
                    TransparencyMode.Scissor => BaseMaterial3D.TransparencyEnum.AlphaScissor,
                    TransparencyMode.Transparent => BaseMaterial3D.TransparencyEnum.Alpha,
                    _ => BaseMaterial3D.TransparencyEnum.Disabled
                };
            }

            // Set the viewport background transparency mode and force a redraw
            _viewport.TransparentBg = _transparent != TransparencyMode.Opaque;
            _dirty |= Dirty.Redraw;
        }

        // Handle alpha scissor update
        if (_dirty.HasFlag(Dirty.AlphaScissor))
        {
            _dirty &= ~Dirty.AlphaScissor;

            // If using a temporary material with alpha-scissor then update
            if (_material == null && _transparent == TransparencyMode.Scissor)
                _screenMaterial.AlphaScissorThreshold = _alphaScissorThreshold;
        }

        // Handle unshaded update
        if (_dirty.HasFlag(Dirty.Unshaded))
        {
            _dirty &= ~Dirty.Unshaded;

            // If using a temporary material with unshaded then update
            if (_material == null)
                _screenMaterial.ShadingMode = _unshaded ?
                    BaseMaterial3D.ShadingModeEnum.Unshaded : 
                    BaseMaterial3D.ShadingModeEnum.PerPixel;
        }

        // Handle filter update
        if (_dirty.HasFlag(Dirty.Filtered))
        {
            _dirty &= ~Dirty.Filtered;

            // If using a temporary material with filter then update
            if (_material == null)
                _screenMaterial.TextureFilter = _filter ?
                    BaseMaterial3D.TextureFilterEnum.Linear :
                    BaseMaterial3D.TextureFilterEnum.Nearest;
        }

        // Handle surface material update
        if (_dirty.HasFlag(Dirty.Surface))
        {
            _dirty &= ~Dirty.Surface;

            // Set the screen to render using the new screen material
            _screenMesh.SetSurfaceOverrideMaterial(0, _screenMaterial);
        }

        // Handle forced redraw of the viewport
        if (_dirty.HasFlag(Dirty.Redraw))
        {
            _dirty &= ~Dirty.Redraw;

            // Force a redraw of the viewport
            if (Engine.IsEditorHint() || _updateMode == ViewportUpdateMode.UpdateOnce)
                _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        }
    }
}