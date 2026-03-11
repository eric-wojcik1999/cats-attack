using Godot;
using System;
using System.Threading.Tasks;

public partial class MovingEnemyBasic : CharacterBody3D
{
	[Export] private float _baseMovementSpeed = 5.0f;
	// (1, 0, 0) <- the positive 1 is 'left' on the x-axis
	[Export] private Vector3 _direction = new Vector3(-1, 0, 0);
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	
	[Export] public NodePath _topDetectionPath = "TopDetection";
	[Export] public NodePath _detectFloorRayCastPath = "DetectFloorRayCast";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 1;
	private bool _isTurning = false;
	private Area3D _sideDetection;
	private Area3D _topDetection;
	private RayCast3D _detectFloorRayCast;

	public override void _Ready()
	{
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_sideDetection.BodyEntered += OnSideDectionPlayerEntered;
		_topDetection = GetNode<Area3D>(_topDetectionPath);
		_topDetection.BodyEntered += OnTopDectionPlayerEntered;
		_detectFloorRayCast = GetNode<RayCast3D>(_detectFloorRayCastPath);
	}

	public override void _PhysicsProcess(double delta)
	{	
		Vector3 newVelocity = Velocity;

		// ───── Horizontal movement based on current facing direction ─────
		// In Godot, -Z is forward
		Vector3 forward = -GlobalTransform.Basis.Z;
		newVelocity.X = forward.X * _baseMovementSpeed;
		newVelocity.Z = forward.Z * _baseMovementSpeed;


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

		if (!_detectFloorRayCast.IsColliding() && IsOnFloor() && !_isTurning)
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
		turnTween.TweenProperty(this, "rotation_degrees", new Vector3(0f, 180f, 0f), 0.3f).AsRelative();
		await ToSignal(turnTween, Tween.SignalName.Finished);

        _direction = currentDirection;
        _direction.X *= -1f;
        _direction.Z *= -1f;
        _isTurning = false;
    }

	private void OnSideDectionPlayerEntered(Node3D body) 
	{
		if (body != null)
		{
			// Disable top collision when side ccollision entered to prevent double collision
			_topDetection.SetCollisionMaskValue(1, false);

			if (body is PlayerGround player)
			{
				player.TakeDamage(_damageAmount);
				player.BounceBackFromPosition(GlobalPosition, 4f);
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[MovingEnemyBasic] player body is null.");
		}
	}

	private void OnTopDectionPlayerEntered(Node3D body) 
	{
		if (body != null)
		{
			// Disable side collision when top ccollision entered to prevent double collision
			_sideDetection.SetCollisionMaskValue(1, false);

			if (body is PlayerGround player)
			{
				player.BounceUp();
				Global.Instance.AddCurrency(_currencyAmount);
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[MovingEnemyBasic] player body is null.");
		}
	}

	public void Die()
	{
		GD.Print("Killed enemy!");
		QueueFree();
	}

}
