using Godot;
using System;
using System.Collections.Generic;

public partial class BaseSpeedPad : Area3D
{

	[Export] private float _boostMultiplier = 1.5f;
	[Export] private float _boostDuration = 6.0f;
	// Prevents the same pad from triggering again due to body overlap
	[Export] private float _localRetriggerCooldown = 0.75f;

	private double _lastTriggerTime = -999.0;

	private readonly HashSet<PlayerGround> _playersInside = new();
	private readonly HashSet<PlayerGround> _playersTriggeredInsideThisEntry = new();

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
		BodyExited += OnBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		foreach (PlayerGround player in _playersInside)
		{
			if (!GodotObject.IsInstanceValid(player))
			{
				continue;
			}

			if (_playersTriggeredInsideThisEntry.Contains(player))
			{
				continue;
			}

			TryTriggerPlayer(player);
		}
	}

	private void OnBodyEntered(Node3D body) 
	{
		if (body is not PlayerGround player)
		{
			return;
		}

		_playersInside.Add(player);
		TryTriggerPlayer(player);
	}

	private void OnBodyExited(Node3D body)
	{
		if (body is not PlayerGround player)
		{
			return;
		}

		_playersInside.Remove(player);
		_playersTriggeredInsideThisEntry.Remove(player);
	}

	private void TryTriggerPlayer(PlayerGround player)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		if (now - _lastTriggerTime < _localRetriggerCooldown)
		{
			return;
		}

		if (!player.CanTriggerSpeedBoostPad())
		{
			return;
		}

		_lastTriggerTime = now;
		_playersTriggeredInsideThisEntry.Add(player);
		player.ActivateSpeedBoost(_boostMultiplier, _boostDuration);
	}
}
