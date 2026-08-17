using Godot;
using System;

public partial class PercyPowerup : Area3D
{
	[Export]
	public PackedScene ModelScene { get; set; }

	[Export]
	public AudioStream PickupSfx { get; set; }
	private const float ROTATION_SPEED = 1f;

	public override void _Ready()
	{
		// Connect to built-in signals
		BodyEntered += OnPlayerEntered;
	}

	public override void _Process(double delta)
	{
		 // Simple spin effect
		RotateY(Mathf.DegToRad(ROTATION_SPEED));
	}

	private void OnPlayerEntered(Node body) 
	{
        if (body is PlayerGround player)
		{
			GD.Print($"[PercyPowerup] has been picked up by the player");
			PlayPickupSfxDetached();
			_ = player.SpawnPercyDrone();
			QueueFree();
		}
	}

	private void PlayPickupSfxDetached()
	{
		if (PickupSfx == null)
		{
			return;
		}

		AudioStreamPlayer player = new AudioStreamPlayer
		{
			Stream = PickupSfx,
			Bus = "Sfx"
		};

		Node parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}
}
