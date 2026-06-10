using Godot;
using System;

public partial class BaseJumpPad : Area3D
{
	[Export] private float _upVelocity = 16.0f;
	[Export] private float _forwardVelocity = 14.0f;
	// Prevents adjacent pads from being triggered if one has already been used
	// This var is to prevent overlapping body entered weirdness
	[Export] private float _localRetriggerCooldown;

	private double _lastTriggerTime = -999.0f;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body) 
	{
		if (body is not PlayerGround player)
		{
			return;
		}

		// Pad usage cooldown
		double now = Time.GetTicksMsec() / 1000.0;

		if (now - _lastTriggerTime < _localRetriggerCooldown)
		{
			return;
		}

		if (!player.CanTriggerJumpPad())
		{
			return;
		}

		_lastTriggerTime = now;

		Vector3 launchDirection = GetPlayerLaunchDirection(player);
		player.LaunchFromJumpPad(launchDirection, _forwardVelocity, _upVelocity);
	}

	private Vector3 GetPlayerLaunchDirection(PlayerGround player)
	{
		Vector3 direction = player.ForwardDir;
		direction.Y = 0f;

		if (direction.LengthSquared() < 0.0001f)
		{
			direction = Vector3.Forward;
		}

		return direction.Normalized();
	}
}
