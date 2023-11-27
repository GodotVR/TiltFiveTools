using Godot;
using System;
using System.Collections.Generic;

public partial class T5ToolsScene : Node3D
{
    /// <summary>
    /// New Player Spawn Location
    /// </summary>
    public enum NewPlayerSpawn
    {
        /// <summary>
        /// Spawn at the load location
        /// </summary>
        Load,

        /// <summary>
        /// Near existing player (or load if none)
        /// </summary>
        Players
    };

    /// <summary>
    /// Load spawn point
    /// </summary>
    private Transform3D _loadSpawn;

    /// <summary>
    /// Signal emitted when the game starts to exit the scene
    /// </summary>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void ScenePreExitingEventHandler(Variant userData);

    /// <summary>
    /// Signal emitted when the game exits the scene
    /// </summary>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneExitingEventHandler(Variant userData);

    /// <summary>
    /// Signal emitted when the game loads this scene
    /// </summary>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneLoadedEventHandler(Variant userData);

    /// <summary>
    /// Signal emitted when the game shows this scene
    /// </summary>
    /// <param name="userData">Custom data</param>
    [Signal]
    public delegate void SceneVisibleEventHandler(Variant userData);

    /// <summary>
    /// Character scene
    /// </summary>
    [Export]
    public PackedScene CharacterScene { get; set; }

    /// <summary>
    /// How close to spawn characters
    /// </summary>
    [Export]
    public float SpawnPadding { get; set; } = 1.0f;

    /// <summary>
    /// Spawn location for new players
    /// </summary>
    [Export]
    public NewPlayerSpawn SpawnLocation { get; set; } = NewPlayerSpawn.Load;

    /// <summary>
    /// Array of characters
    /// </summary>
    public List<T5ToolsCharacter> Characters { get; } = new();

    /// <summary>
    /// Random number source
    /// </summary>
    private readonly Random _random = new();

    public override void _Ready()
    {
        // Call the base
        base._Ready();

        SceneLoaded += OnSceneLoaded;
        SceneExiting += OnSceneExiting;
    }

    protected virtual void OnSceneLoaded(Variant userData)
    {
        // Skip if no stage, or character scene
        if (T5ToolsStaging.Instance == null || CharacterScene == null)
            return; 

        // Get the spawn position data
        var spawnPosition = userData;
        if (userData.VariantType == Variant.Type.Object)
        {
            var obj = userData.AsGodotObject();
            if (obj.HasMethod("get_spawn_position"))
                spawnPosition = obj.Call("get_spawn_position");
        }

        // Get the spawn transform
        _loadSpawn = Transform3D.Identity;
        switch (spawnPosition.VariantType)
        {
            case Variant.Type.String:
                // Name of Node3D to spawn at
                var node = GetNodeOrNull<Node3D>(spawnPosition.AsString());
                if (node != null)
                    _loadSpawn = node.GlobalTransform;
                break;

            case Variant.Type.Vector3:
                // Vector3 to spawn at
                _loadSpawn = new Transform3D(Basis.Identity, spawnPosition.AsVector3());
                break;

            case Variant.Type.Transform3D:
                // Transform3D to spawn at
                _loadSpawn = spawnPosition.AsTransform3D();
                break;
        }

        // Spawn the existing player characters
        var players = T5ToolsStaging.Instance.Players;
        var count = players.Count;
        for (var i = 0; i < count; i++)
        {
            // Pick the spawn location
            var location = _loadSpawn;
            var offset = Vector3.Forward * SpawnPadding;
            offset = offset.Rotated(Vector3.Up, Mathf.Pi * 2.0f * i / count);
            location.Origin += offset;

            // Create the character
            CreateCharacter(players[i], location);
        }

        // Subscribe to the player change signals
        T5ToolsStaging.Instance.PlayerCreated += OnPlayerCreated;
        T5ToolsStaging.Instance.PlayerRemoved += OnPlayerRemoved;
    }

    protected virtual void OnSceneExiting(Variant userData)
    {
        // Skip if no stage, or character scene
        if (T5ToolsStaging.Instance == null)
            return;

        // Unsubscribe from the player change signals
        T5ToolsStaging.Instance.PlayerCreated -= OnPlayerCreated;
        T5ToolsStaging.Instance.PlayerRemoved -= OnPlayerRemoved;
    }

    /// <summary>
    /// Handle new player created
    /// </summary>
    /// <param name="player">New player</param>
    protected virtual void OnPlayerCreated(T5ToolsPlayer player)
    {
        // Pick the base location
        var location = _loadSpawn;
        if (SpawnLocation == NewPlayerSpawn.Players && Characters.Count > 0)
            location = Characters[_random.Next(Characters.Count)].GlobalTransform;

        // Offset the location by the spawn padding
        var offset = Vector3.Forward * SpawnPadding;
        offset = offset.Rotated(Vector3.Up, Mathf.Pi * 2.0f * _random.NextSingle());
        location.Origin += offset;

        // Create a new character at the location
        CreateCharacter(player, location);
    }

    /// <summary>
    /// Handle player removed from the scene
    /// </summary>
    /// <param name="player">Removed player</param>
    protected virtual void OnPlayerRemoved(T5ToolsPlayer player)
    {
        // Remove the character
        RemoveCharacter(player);
    }

    /// <summary>
    /// Create a character for the player
    /// </summary>
    /// <param name="player">Player</param>
    /// <param name="location">Character spawn location</param>
    protected virtual void CreateCharacter(T5ToolsPlayer player, Transform3D location)
    {
        // Skip if no character scene
        if (CharacterScene == null)
            return;

        // Construct the character
        var character = CharacterScene.Instantiate<T5ToolsCharacter>();
        character.Player = player;

        // Add the character to the scene
        AddChild(character);
        Characters.Add(character);

        // Position the character
        character.GlobalTransform = location;
    }

    /// <summary>
    /// Remove the character for the player
    /// </summary>
    /// <param name="player">Player</param>
    protected virtual void RemoveCharacter(T5ToolsPlayer player)
    {
        // Find the character
        var character = GetPlayerCharacter(player);
        if (character == null)
            return;

        // Remove the character
        Characters.Remove(character);
        character.QueueFree();
    }

    /// <summary>
    /// Get the character associated with a player
    /// </summary>
    /// <param name="player">Player</param>
    /// <returns>Character or null</returns>
    public T5ToolsCharacter GetPlayerCharacter(T5ToolsPlayer player)
    {
        // Find the character
        return Characters.Find(c => c.Player == player);
    }

    /// <summary>
    /// Get the current scene
    /// </summary>
    /// <returns>Current scene or null</returns>
    public static T5ToolsScene GetCurrent()
    {
        // If we have an active stage then query it for the current scene
        return T5ToolsStaging.Instance?.CurrentScene;
    }
}
