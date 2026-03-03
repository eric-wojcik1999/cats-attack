using Godot;
using System;

public partial class DirectionMarker : Area3D
{
	[Export] public bool oneShot = false;
	// Per-marker limit: how fast the player rotates (deg/sec) after colliding with marker
	// Examples: 180 (slow), 360 (normal), 720 (very fast)
	[Export(PropertyHint.Range, "30,1080,1")]
	public float _turnDegPerSecOverride = 360f;
    // These nodes are editor-only visuals that should be hidden during gameplay.
    [Export] public NodePath EditorOnlyVectorPath;
    [Export] public NodePath EditorOnlyFieldPath;

	public override void _Ready()
	{
		// Connect to built-in signals
		BodyEntered += OnPlayerEntered;

		// Hide editor-only visuals at runtime (but keep them in the scene)
        if (!Engine.IsEditorHint())
        {
            HideIfExists(EditorOnlyVectorPath);
            HideIfExists(EditorOnlyFieldPath);
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

	private void OnPlayerEntered(Node body) 
	{
		// Equal to doing: 
		// PlayerGround player = body as PlayerGround;
		// if (player == null)
		// 		return;
        if (body is PlayerGround player)
		{
			// GD.Print($"[Marker] ENTER: marker={Name} rotDeg={GlobalRotationDegrees}");

			// // IMPORTANT: direction comes from ROOT Area3D rotation (this node).
			// // Godot forward is -Z, so marker's forward direction is -GlobalBasis.Z
			var basis = GlobalTransform.Basis;
			Vector3 desiredForward = -basis.Z;
			desiredForward.Y = 0;

			if (desiredForward.LengthSquared() > 0.0001f)
			{
				player.SetDesiredForward(desiredForward.Normalized(), _turnDegPerSecOverride);
				float targetYaw = Mathf.Atan2(-desiredForward.X, -desiredForward.Z);
				
				// GD.Print($"[Marker] targetYawDeg={Mathf.RadToDeg(targetYaw)}");
			}

			if (oneShot)
			{
				QueueFree();
			}
		}
	}
}
