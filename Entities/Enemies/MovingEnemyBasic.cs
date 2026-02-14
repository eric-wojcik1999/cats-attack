using Godot;
using System;
using System.Threading.Tasks;

public partial class MovingEnemyBasic : CharacterBody3D
{
	[Export] private float _baseMovementSpeed = 5.0f;
	// (1, 0, 0) <- the positive 1 is 'left' on the x-axis
	[Export] private Vector3 _direction = new Vector3(-1, 0, 0);
	private bool _isTurning = false;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 newVelocity = Velocity;
		newVelocity.X = _baseMovementSpeed * _direction.X;
		newVelocity.Z = _baseMovementSpeed * _direction.Z;

		// Might need to disable temporarily
		if (!IsOnFloor())
		{
			newVelocity += GetGravity() * (float)delta;
		}

		Velocity = new Vector3(newVelocity.X, newVelocity.Y, newVelocity.Z);
		MoveAndSlide();

		// && _isTurning == false
		if (IsOnWall() && !_isTurning)
		{
			_ = TurnAroundAsync();
		}
	}

    private async Task TurnAroundAsync()
    {
        _isTurning = true;

        Vector3 currentDirection = _direction;
        _direction = Vector3.Zero;

		// Rotate the enemy to direction they are heading
		Tween turnTween = CreateTween();
		turnTween.TweenProperty(this, "rotation_degrees", new Vector3(0f, 180f, 0f), 0.6f).AsRelative();

        _direction = currentDirection;
        _direction.X *= -1f;
        _direction.Z *= -1f;
		
        await ToSignal(GetTree().CreateTimer(0.05f), SceneTreeTimer.SignalName.Timeout);

        _isTurning = false;
    }

}
