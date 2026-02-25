using Godot;
using System;

public partial class PlayerAir : CharacterBody3D
{
	[Export] private float _followBackDistance = 0.0f;
	[Export] private float _positionSmoothSpeed = 14.0f;
	[Export] private float _yawFollowSpeed = 16.0f;
	[Export] private float _maxLateralOffset = 4.5f; // what does this do?
	[Export] private float _lateralSmoothSpeed = 15.0f;
	[Export] private float _mouseSensitivity = 0.015f;
 	[Export] private PlayerGround _playerGround;
	private float _currentLateralOffset = 0f;
	private float _targetLateralOffset = 0f;

	public override void _Ready()
	{
		if (_playerGround != null)
		{
			// Match the position and rotation of the Player Air marker
			GlobalPosition = GetTargetFollowPosition();
			GlobalRotation = new Vector3(GlobalRotation.X, _playerGround.GlobalRotation.Y, GlobalRotation.Z);
		}
		else 
		{
			GD.Print("[Player Air] Player Ground not found");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// event is a reserved keyword
		// can use event by prefixing with @
		if (@event is InputEventMouseMotion mouseMotion)
		{
			_targetLateralOffset += mouseMotion.Relative.X * _mouseSensitivity;
			_targetLateralOffset = Mathf.Clamp(_targetLateralOffset, -_maxLateralOffset, _maxLateralOffset);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_playerGround != null)
		{
			float dt = (float)delta;

			// ───── Calculate independant lateral movement ─────
			float lateralBlend = 1f - Mathf.Exp(-_lateralSmoothSpeed * dt);
			_currentLateralOffset = Mathf.Lerp(_currentLateralOffset, _targetLateralOffset, lateralBlend);

			// ───── Auto-forward movement, shadowing Player Ground ─────
			Vector3 targetPos = GetTargetFollowPosition();
			float posBlend = 1f - Mathf.Exp(-_positionSmoothSpeed * dt);
			GlobalPosition = GlobalPosition.Lerp(targetPos, posBlend);

			// ───── Auto-rotation, shadowing Player Ground ─────
			Vector3 rotation = GlobalRotation;
			float targetYaw = _playerGround.GlobalRotation.Y;
			// Calculates the diff between Player Ground's rotation, the current rotation, and locks it to PI, -PI range
			// Important so when the angle loops, the object doesn't rotate the long way around
			float diff = Mathf.Wrap(targetYaw - rotation.Y, -Mathf.Pi, Mathf.Pi);
			// Converts the speed into a frame-scaled blend factor
			// The clamp keeps it betwee 0 and 1
			float yawStep = Mathf.Clamp(_yawFollowSpeed * dt, 0f, 1f);
			// Moves current yaw towards the target yaw
			rotation.Y +=  diff * yawStep;
			GlobalRotation = rotation;
		}
		else 
		{
			GD.Print("[Player Air] Player Ground not found");
		}
	}

	private Vector3 GetTargetFollowPosition()
	{
		// Uses PlayerGround orientation so the lateral movement stays relative to the other player's route
		Vector3 forward = _playerGround.ForwardDir;
		Vector3 right = _playerGround.RightDir;

		// Base position derrived from Node3D anchor in PlayerGround for PlayerAir position
		Vector3 basePos = _playerGround.AirAnchorPosition;
		basePos -= forward * _followBackDistance;

		// Apply independent lateral movement
		basePos += right * _currentLateralOffset;

		return basePos;
	}
}
