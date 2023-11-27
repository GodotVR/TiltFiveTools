using Godot;

public partial class T5ToolsCharacter : Node3D
{
    /// <summary>
    /// Player associated with this character
    /// </summary>
    [Export]
    public T5ToolsPlayer Player { get; set; }

    /// <summary>
    /// Find the T5ToolsCharacter from a child node
    /// </summary>
    /// <param name="node">Node to search from</param>
    /// <returns>Character or null</returns>
    public static T5ToolsCharacter FindInstance(Node node)
    {
        // Walk the node tree
        while (node != null)
        {
            // If we have the character then return it
            if (node is T5ToolsCharacter character)
                return character;

            // Walk up to the parent
            node = node.GetParent();
        }

        // Not found
        return null;
    }
}