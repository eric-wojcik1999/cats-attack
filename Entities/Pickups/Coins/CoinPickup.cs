using Godot;
using System;

public partial class CoinPickup : Area3D
{
	[Export(PropertyHint.Range, "1,999999,1")]
	public int _amount { get; set; } = 1;

	[Export]
	public PackedScene ModelScene { get; set; }

	[Export]
	public AudioStream PickupSfx { get; set; }
	private const float ROTATION_SPEED = 1f;

	public override void _Ready()
	{
		// Connect to built-in signals
		BodyEntered += OnPlayerEntered;

		// //  Initialise audio player
        // _audioPlayer = GetNodeOrNull<AudioStreamPlayer3D>("PickupAudio");
        // if (_audioPlayer == null)
        // {
        //     _audioPlayer = new AudioStreamPlayer3D();
        //     _audioPlayer.Name = "PickupAudio";
        //     AddChild(_audioPlayer);
        // }
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
			GD.Print($"[CoinPickup] has been picked up by the player");

			if (Global.Instance != null)
			{
				Global.Instance.AddCurrency(_amount);
			}
			else 
			{
				GD.PushError("[CoinPickup] Global.Instance is null. Is Global autoload configured?");
			}
			// // Play SFX if assigned (note: if you QueueFree immediately, you may cut sound)
			// if (PickupSfx != null)
			// {
			// 	_audioPlayer.Stream = PickupSfx;
			// 	_audioPlayer.GlobalPosition = GlobalPosition;
			// 	_audioPlayer.Play();
			// }

			QueueFree();
		}
	}
}
