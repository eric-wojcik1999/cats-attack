using Godot;
using System;

public partial class CoinPickup : Area3D
{
	[Export]
	public CoinDefinition Definition { get; set; }

	[Export]
	public float SpinDegreesPerSecond { get; set; } = 90f;

	[Export]
	public NodePath VisualRootPath { get; set; } = "VisualRoot";

	private Node3D _visualRoot;
	private bool _collected = false;

	public override void _Ready()
	{
		BodyEntered += OnPlayerEntered;
		_visualRoot = GetNodeOrNull<Node3D>(VisualRootPath);
		
		ApplyDefinition();

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
		RotateY(Mathf.DegToRad(SpinDegreesPerSecond) * (float)delta);
	}

	private void ApplyDefinition()
	{
		if (Definition == null)
		{
			GD.PushWarning("[CoinPickup] No CoinDefinition assigned.");
			return;
		}

		if (_visualRoot == null)
		{
			GD.PushWarning("[CoinPickup] VisualRoot node is missing");
			return;
		}

		foreach (Node child in _visualRoot.GetChildren())
		{
			child.QueueFree();
		}

		if (Definition.ModelScene != null) 
		{
			// Applies custom resource model for coin using this script
			Node model = Definition.ModelScene.Instantiate();
			_visualRoot.AddChild(model);
		}
	}

	private void OnPlayerEntered(Node body) 
	{

		if (_collected)
		{
			return;
		}

		if (body is not PlayerGround)
		{
			return;
		}

		if (Definition == null)
		{
			GD.PushWarning("[CoinPickup] Cannot collect coin because Definition is null.");
			return;
		}

		if (Global.Instance == null)
		{
			GD.PushError("[CoinPickup] Global.Instance is null. Is Global autoload configured?");
			return;
		}

		_collected = true;
		Monitoring = false;

		bool addedCurrency = Global.Instance.AddCurrency(Definition.Amount);

		if (addedCurrency)
		{
			string message = BuildPickupMessage();

			Global.Instance.RequestGameMessage(message);

			GD.Print($"[CoinPickup] {message}");
		}

		PlayerPickupSfxDetached();

		QueueFree();
	}

	private string BuildPickupMessage()
	{
		return Definition.PickupMessage
			.Replace("{name}", Definition.DisplayName)
			.Replace("{amount}", Definition.Amount.ToString());
	}

	private void PlayerPickupSfxDetached()
	{
		if  (Definition == null || Definition.PickupSfx == null)
		{
			return;
		}

		AudioStreamPlayer3D audioPlayer = new AudioStreamPlayer3D
		{
			Stream = Definition.PickupSfx,
			GlobalPosition = GlobalPosition
		};

		Node parent = GetTree().CurrentScene ?? GetTree().Root;

		parent.AddChild(audioPlayer);

		// Finished expects a function call later
		audioPlayer.Finished += () => audioPlayer.QueueFree();
	}
}
