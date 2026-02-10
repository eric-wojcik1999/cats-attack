using Godot;
using System;

public partial class PlayerGround : CharacterBody3D
{
	[Export] private float _baseMovementSpeed = 5.0f;
	[Export] private float _autoForwardSpeed = 6.0f;
	[Export] private float _baseJumpVelocity = 4.5f;
	// Configures decleration (higher = faster deceleration)
	[Export] private float _lateralDeceleration = 2.0f; 
	[Export] private float _cameraLag = 0.075f;
	[Export] public float _defaultTurnDegPerSec = 360f;
	private Vector3 _desiredForward = Vector3.Forward;
	private bool _hasDesiredForward = false;
	private float _currentTurnDegPerSec;


	private Node3D cameraController;

	public override void _Ready() 
	{
		_currentTurnDegPerSec = _defaultTurnDegPerSec;
		cameraController = GetNode<Node3D>("CameraController");
	}


	public override void _PhysicsProcess(double delta)
	{

		// ───── Update Player Rotation ─────
		float dt = (float)delta;
		UpdateYawTurning(dt);

		bool onFloor = IsOnFloor();
		Vector3 newVelocity = Velocity;
		float horizontalInput = Input.GetActionStrength("move_ground_right") - Input.GetActionStrength("move_ground_left");

		// ───── Falling when in air ─────
        if (!onFloor)
        {
            Vector3 gravity = GetGravity();
            newVelocity += gravity * (float)delta;
        }

		// ───── Jumping ─────
		if (Input.IsActionJustPressed("jump") && (onFloor)) 
		{
			newVelocity.Y = _baseJumpVelocity;
		} 

		// ───── Auto-forward movement ─────
		Vector3 forward = -Transform.Basis.Z * _autoForwardSpeed;
		newVelocity.X = forward.X;
    	newVelocity.Z = forward.Z;

		// ───── Lateral (left/right) movement ─────
		// If: player is not laterlly moving, slow to a stop
		// Else: move the player left/right
		if (Mathf.Abs(horizontalInput) < 0.01f)
		{
			newVelocity.X = Mathf.Lerp(newVelocity.X, forward.X, 1f - Mathf.Exp(-_lateralDeceleration * (float)delta));
			newVelocity.Z = Mathf.Lerp(newVelocity.Z, forward.Z, 1f - Mathf.Exp(-_lateralDeceleration * (float)delta));
		}
		else
		{
			Vector3 lateral = Transform.Basis.X * horizontalInput * _baseMovementSpeed;
			newVelocity.X = forward.X + lateral.X;
			newVelocity.Z = forward.Z + lateral.Z;
		}

		Velocity = new Vector3(newVelocity.X, newVelocity.Y, newVelocity.Z);
		MoveAndSlide();

		// ───── Match camera to position of player ─────
		cameraController.GlobalPosition = cameraController.GlobalPosition.Lerp(GlobalPosition, _cameraLag);
	}

	public void SetDesiredForward(Vector3 forward, float turnDegPerSecOverride)
	{
		forward.Y = 0f;
		if (forward.LengthSquared() < 0.0001f) 
		{
			GD.Print("Forward is less than 0.0001f");
		}
		else 
		{
			_desiredForward = forward.Normalized();
			_hasDesiredForward = true;

			// If turning override greater than 0 is true, use override. Otherwise use default.
			_currentTurnDegPerSec = turnDegPerSecOverride > 0f ? turnDegPerSecOverride : _defaultTurnDegPerSec;

			float targetYaw = Mathf.Atan2(-_desiredForward.X, -_desiredForward.Z);

			// GD.Print($"[Player] SetDesiredForward -> desiredForward={_desiredForward}, targetYawDeg={Mathf.RadToDeg(targetYaw):F2}, turn={_currentTurnDegPerSec}");
		}
	}

	private void UpdateYawTurning(float delta)
	{
		if (_hasDesiredForward) 
		{
			float targetYaw = Mathf.Atan2(-_desiredForward.X, -_desiredForward.Z);
			float currentYaw = GlobalRotation.Y;
			float maxStep = Mathf.DegToRad(_currentTurnDegPerSec) * delta;

			// Normalize shortest angle
			float diff = Mathf.Wrap(targetYaw - currentYaw, -Mathf.Pi, Mathf.Pi);

			// GD.Print($"[Player] currentYaw={Mathf.RadToDeg(currentYaw):F2}, targetYaw={Mathf.RadToDeg(targetYaw):F2}, diff={Mathf.RadToDeg(diff):F2}");

			// Player has reached target yaw set by direction marker
			if (Mathf.Abs(diff) <= maxStep)
			{
				var rotation = GlobalRotation;
				rotation.Y = targetYaw;
				GlobalRotation = rotation;
				_hasDesiredForward = false;
			} 
			else 
			{
				var rotation = GlobalRotation;
				rotation.Y += Mathf.Sign(diff) * maxStep;
				GlobalRotation = rotation;
			}
		}
	}

}
