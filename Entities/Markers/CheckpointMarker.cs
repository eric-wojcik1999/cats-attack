using Godot;
using System;

public partial class CheckpointMarker : Area3D
{
	[Export] public NodePath EditorOnlyTriggerArea;
    [Export] private int _checkpointId = 1;
	[Export] private Marker3D _respawnMarker;

    private bool _activated = false;

	public override void _Ready()
	{
        if (_respawnMarker == null)
        {
            GD.PushError($"[Checkpoint {_checkpointId}] RespawnMarker not found.");
            return;
        }

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
        if (body is not PlayerGround player)
        {
            return;
        }

        // Prevent unnecessary repeated activation while standing inside the same checkpoint.
        if (_activated)
        {
            return;
        }

        _activated = true;
        player.RegisterCheckpoint(_checkpointId, _respawnMarker.GlobalTransform);
        GD.Print($"[Checkpoint] Player reached checkpoint {_checkpointId}");
    }
}
