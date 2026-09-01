using Godot;
using System;

public partial class FallDeathVolume : Area3D
{
	[Export] public NodePath EditorOnlyTriggerArea;
	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		// Hide editor-only visuals at runtime (but keep them in the scene)
        if (!Engine.IsEditorHint())
        {
            HideIfExists(EditorOnlyTriggerArea);
        }
	}

	private void HideIfExists(NodePath path) 
	{
		if (path != null) 
		{
			var node = GetNodeOrNull<Node3D>(path);
			
			if (node is Node3D node3D) 
			{
				node3D.Visible = false;
			}
			else 
			{
				GD.Print("Node is not Node3D");
			}
		}
		else 
		{
			GD.Print("Path is null");
		}
	}

    private void OnBodyEntered(Node3D body)
    {
        if (body is PlayerGround player)
        {
            player.KillFromFall();
            return;
        }

        if (body is MovingEnemyBasic enemy)
        {
            enemy.KillFromFall();
        }
    }
}
