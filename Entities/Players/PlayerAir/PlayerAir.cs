using Godot;
using System;

public partial class PlayerAir : CharacterBody3D
{
	[Export] private float _followBackDistance = 0.0f;
	[Export] private float _positionSmoothSpeed = 14.0f;
	[Export] private PlayerGround _playerGround;

	public override void _Ready()
	{
		if (_playerGround != null)
		{
			GlobalPosition = GetTargetFollowPosition();
			GlobalRotation = new Vector3(GlobalRotation.X, _playerGround.GlobalRotation.Y, GlobalRotation.Z);
		}
		else 
		{
			GD.Print("[Player Air] Player Ground not found");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_playerGround != null)
		{
			float dt = (float)delta;

			Vector3 targetPos = GetTargetFollowPosition();
			float posBlend = 1f - Mathf.Exp(-_positionSmoothSpeed * dt);
			GlobalPosition = GlobalPosition.Lerp(targetPos, posBlend);
		}
		else 
		{
			GD.Print("[Player Air] Player Ground not found");
		}
		// Vector3 velocity = Velocity;

		// // Add the gravity.
		// if (!IsOnFloor())
		// {
		// 	velocity += GetGravity() * (float)delta;
		// }

		// // Handle Jump.
		// if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		// {
		// 	velocity.Y = JumpVelocity;
		// }

		// // Get the input direction and handle the movement/deceleration.
		// // As good practice, you should replace UI actions with custom gameplay actions.
		// Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		// Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		// if (direction != Vector3.Zero)
		// {
		// 	velocity.X = direction.X * Speed;
		// 	velocity.Z = direction.Z * Speed;
		// }
		// else
		// {
		// 	velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		// 	velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		// }

		// Velocity = velocity;
		// MoveAndSlide();
	}

	private Vector3 GetTargetFollowPosition()
	{
		// Uses PlayerGround orientation so the lateral movement stays relative to the other player's route
		Vector3 forward = _playerGround.ForwardDir;
		Vector3 right = _playerGround.RightDir;

		// Base position derrived from Node3D anchor in PlayerGround for PlayerAir position
		Vector3 basePos = _playerGround.AirAnchorPosition;
		basePos -= forward * _followBackDistance;

		return basePos;
	}
}
