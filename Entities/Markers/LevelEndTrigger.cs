using Godot;
using System;

public partial class LevelEndTrigger : Area3D
{
	private bool _hasTriggered = false;
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
		if (_hasTriggered)
		{
			return;
		}

		if (body is not PlayerGround)
		{
			return;
		}

		_hasTriggered = true;

		// Prevent the Area3D from firing again while the transition happens.
		SetDeferred(Area3D.PropertyName.Monitoring, false);
		Node levelNode = GetTree().CurrentScene;

		if (levelNode is Level1 level)
		{
			level.CompleteLevel();
		}
		else
		{
			GD.PushError(
				"[LevelEndTrigger] Current scene does not implement expected level completion."
			);
		}
	}
}
