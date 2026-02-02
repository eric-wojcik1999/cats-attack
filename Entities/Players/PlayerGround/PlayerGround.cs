using Godot;
using System;

public partial class PlayerGround : CharacterBody3D
{
	private const float _baseMovementSpeed = 5.0f;
	private const float _baseJumpVelocity = 4.5f;

	// to do:
	// get basic controller working with jump and tighter controls 
	// put into state machine for jumping and going left and right

	public override void _PhysicsProcess(double delta)
	{
		Vector3 newVelocity = Velocity;

		float horizontalInput = Input.GetActionStrength("move_ground_right") - Input.GetActionStrength("move_ground_left");

		// TO IMPLEMENT:
		// // Preserve Y (gravity/jump)
		// float y = v.Y;

		// TO IMPLEMENT:
		// // Forward movement
		// Vector3 forward = -Transform.Basis.Z * autoSpeed;

		// Lateral velocity: either target strafe speed, or smoothly return to 0
		if (Mathf.Abs(horizontalInput) < 0.01f)
		{
			// TO DO: Make this slow burn a bit
			newVelocity.X = Mathf.MoveToward(Velocity.X, 0, _baseMovementSpeed);
			newVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, _baseMovementSpeed);


			// newVelocity = newVelocity.MoveToward(forward, _baseMovementSpeed * (float)delta);
			// or: v = v.Lerp(forward, 1.0f - Mathf.Exp(-strafeDamp * (float)delta));
		}
		else
		{
			// Lateral (left/right) movement
			Vector3 lateral = Transform.Basis.X * horizontalInput * _baseMovementSpeed;
			// newVelocity = forward + lateral;
			newVelocity = lateral;
		}

		Velocity = new Vector3(newVelocity.X, 0, newVelocity.Z);
		MoveAndSlide();
	}
}
