using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerGround : CharacterBody3D
{

	[Signal] public delegate void HealthChangedEventHandler(int amount);
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void PercyPowerupCollectedEventHandler();
	[Export] private float _baseMovementSpeed = 5.0f;
	[Export] private float _autoForwardSpeed = 6.0f;
	[Export] private float _baseJumpVelocity = 4.5f;
	// Configures decleration (higher = faster deceleration)
	[Export] private float _lateralDeceleration = 2.0f; 
	[Export] private float _cameraLag = 0.075f;
	[Export] public float _defaultTurnDegPerSec = 360f;
	[Export] public float _cameraYawFollowSpeed = 8.0f;
	[Export] private int _health = 6;
	[Export] private int _maxHealth = 6;
	[Export] private float _knockbackTime = 0.75f;
	private Vector3 _desiredForward = Vector3.Forward;
	private bool _hasDesiredForward = false;
	private float _currentTurnDegPerSec;
	private Node3D _cameraController;
	private bool _isInvulnerable = false;
	private float _knockbackTimer = 0f;
	private Vector3 _knockbackHorizontal = Vector3.Zero;
	[Export] public int _collideDamageAmount = 1;
	[Export] public NodePath _playerMeshNodePath = "PlayerMeshTemp";
	
	[Export] public NodePath _droneSpawnLeftPath = "PercyDroneSpawnLeft";
	[Export] public NodePath _droneSpawnRightPath = "PercyDroneSpawnRight";
	[Export] public NodePath _droneDespawnPath = "PercyDroneDespawn";

	private bool _hasDied = false;
	
	[Export] private Node3D _playerAirPositionMarker;
	[Export] public PackedScene _percyDroneScene;
	private Node3D _spawnLeft;
	private Node3D _spawnRight;
	private Node3D _despawnMarker;


	// Getters 
	public int Health => _health;
	public int MaxHealth => _maxHealth;
	public Vector3 ForwardDir => -GlobalTransform.Basis.Z;
	public Vector3 RightDir => GlobalTransform.Basis.X;
	public bool IsKnockbackActive => _knockbackTimer > 0f;
	public Vector3 AirAnchorPosition => _playerAirPositionMarker != null ? _playerAirPositionMarker.GlobalPosition : GlobalPosition;

	public override void _Ready() 
	{
		_currentTurnDegPerSec = _defaultTurnDegPerSec;
		_cameraController = GetNode<Node3D>("CameraController");
		_spawnLeft = GetNode<Node3D>(_droneSpawnLeftPath);
		_spawnRight = GetNode<Node3D>(_droneSpawnRightPath);
		_despawnMarker = GetNode<Node3D>(_droneDespawnPath);
	}
    
	public override void _PhysicsProcess(double delta)
	{

		if (_hasDied)
		{
			return;
		}

		float dt = (float)delta;

		// ───── Update Player Rotation ─────
		UpdateYawTurning(dt);

		bool onFloor = IsOnFloor();
		float horizontalInput = Input.GetActionStrength("move_ground_right") - Input.GetActionStrength("move_ground_left");

		// Start from current velocity and build next frame's velocity
		Vector3 newVelocity = Velocity;

		// ───── Apply gravity ─────
		if (!onFloor)
		{
			newVelocity += GetGravity() * dt;
		}

		// ───── Shared horizontal basis vectors ─────
		Vector3 forwardDir = -Transform.Basis.Z;
		Vector3 rightDir = Transform.Basis.X;

		// Lateral input vector
		Vector3 lateralInputVelocity = Vector3.Zero;
		if (Mathf.Abs(horizontalInput) >= 0.01f)
		{
			lateralInputVelocity = rightDir * horizontalInput * _baseMovementSpeed;
		}

		// if: knowckback timer > 0, then KNOCKBACK STUN
		// else: NORMAL MOVEMENT
		if (_knockbackTimer > 0f)
		{
			_knockbackTimer -= dt;
			if (_knockbackTimer < 0f)
				_knockbackTimer = 0f;

			// During knockback:
			// - allow lateral steering
			// - block normal auto-forward progression
			// - smoothly hand off from knockback -> player lateral control
			float decayT = Mathf.Clamp(10f * dt, 0f, 1f); // tune 6..14
			_knockbackHorizontal = _knockbackHorizontal.Lerp(lateralInputVelocity, decayT);

			newVelocity.X = _knockbackHorizontal.X;
			newVelocity.Z = _knockbackHorizontal.Z;
		}
		else
		{
			// ───── Jumping ─────
			if (Input.IsActionJustPressed("jump") && onFloor)
			{
				newVelocity.Y = _baseJumpVelocity;
			}

			// ───── Auto-forward movement ─────
			Vector3 forwardVelocity = forwardDir * _autoForwardSpeed;

			// ───── Lateral (left/right) movement ─────
			// If: player is not laterlly moving, slow to a stop
			// Else: move the player left/right
			if (Mathf.Abs(horizontalInput) < 0.01f)
			{
				// Smoothly converge back to auto-forward when no lateral input
				float blend = 1f - Mathf.Exp(-_lateralDeceleration * dt);
				newVelocity.X = Mathf.Lerp(newVelocity.X, forwardVelocity.X, blend);
				newVelocity.Z = Mathf.Lerp(newVelocity.Z, forwardVelocity.Z, blend);
			}
			else
			{
				newVelocity.X = forwardVelocity.X + lateralInputVelocity.X;
				newVelocity.Z = forwardVelocity.Z + lateralInputVelocity.Z;
			}
		}

		// ───── Commit movement once ─────
		Velocity = newVelocity;
		MoveAndSlide();

		// ───── Post-move collision reactions ─────
		TryApplyWallKnockback();

		// ───── Match camera to position and rotation of the player ─────
		UpdateCameraYaw(dt);
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

	private void UpdateCameraYaw(float delta) 
	{
		_cameraController.GlobalPosition = _cameraController.GlobalPosition.Lerp(GlobalPosition, _cameraLag);
		Vector3 cameraRotation = _cameraController.GlobalRotation;
		float cameraYaw = cameraRotation.Y;
		float targetYaw = GlobalRotation.Y; // Player's rotation
		float diff = Mathf.Wrap(targetYaw - cameraYaw, -Mathf.Pi, Mathf.Pi);
		float step = _cameraYawFollowSpeed * delta;
		cameraRotation.Y += diff * Mathf.Clamp(step, 0f, 1f);
		_cameraController.GlobalRotation = cameraRotation;
	}

	public void TakeDamage(int amount)
	{
		if (amount < 0)
		{
			GD.PushWarning("[PlayerGround] damage amount provided < 0");
		}
		else 
		{
			// if (isInvulnerable == true || health <= 0 ) -> then don't do the rest

			_ = ApplyHealthDeltaAsync(-amount);

			// // i-frame stuff -> for the future
			// if (_health >= 0)
			// {
			// 	_ = StartIframesAsync();
			// }
		}
	}
	private async Task ApplyHealthDeltaAsync(int delta)
	{
		int prevHealth = _health;
		_health = Mathf.Clamp(_health + delta, 0, _maxHealth);

		GD.Print($"[PlayerGround] health is: {_health})");

		if (_health != prevHealth)
		{
			// Emit signal to update UI health value
			EmitSignal(SignalName.HealthChanged, _health);
		}

		// Handling death
		if (_health == 0 && prevHealth > 0)
		{
			_hasDied = true;
			EmitSignal(SignalName.Died);
			MeshInstance3D playerMesh = GetNode<MeshInstance3D>(_playerMeshNodePath);
			playerMesh.Visible = false;
			await ToSignal(GetTree().CreateTimer(3), SceneTreeTimer.SignalName.Timeout);
			GD.Print("[PlayerGround] reload level!");
			// TO DO: Reload level which would be stored in Global
		}
	}

	public void BounceUp()
	{
		Velocity = new Vector3(Velocity.X, _baseJumpVelocity * 0.7f, Velocity.Z);
	}

	public void BounceBackFromPosition(Vector3 entityPos, float knockbackMultp = 1f)
	{
		Vector3 away = GlobalPosition - entityPos;
		away.Y = 0f;
		BounceBackFrom(away, knockbackMultp);
	}

	public void BounceBackFromNormal(Vector3 wallNormal, float knockbackMultp = 1f)
	{
		// Wall normal points away from wall face - push player in this direction
		Vector3 away = wallNormal;
		BounceBackFrom(wallNormal, knockbackMultp);
	}

	private void BounceBackFrom(Vector3 vector, float knockbackMultp = 1f)
	{
		// Fallback if source vector is too vertical or degenerate
		if (vector.LengthSquared() < 0.0001f)
		{
			vector = -Transform.Basis.Z;
		}

		vector = vector.Normalized();

		// Horizontal push
		_knockbackHorizontal = vector * 6.0f * knockbackMultp;

		// Upwards push
		Velocity = new Vector3(Velocity.X, _baseJumpVelocity * 0.4f, Velocity.Z);

		// Lock player briefly
		_knockbackTimer = _knockbackTime;
	}

	public void TryApplyWallKnockback()
	{
		if (_knockbackTimer <= 0f)
		{
			Vector3 forward = -GlobalTransform.Basis.Z; 
			forward.Y = 0f;

			if (forward.LengthSquared() >= 0.000f)
			{
				int collisionCount = GetSlideCollisionCount();
				for (int i = 0; i < collisionCount; i++)
				{
					KinematicCollision3D wallHit = GetSlideCollision(i);

					if (wallHit != null) 
					{
						Vector3 normal = wallHit.GetNormal().Normalized();

						// Ignore floor/ceiling-ish surfaces
						// (walls should have small Y in their normal)
						if (Mathf.Abs(normal.Y) > 0.4f)
						{
							continue;
						}

						// Check if wall is facing the player
						// A front-facing wall should face opposite player's forward dir
						float facingDot = (-forward).Dot(normal);

						if (facingDot >= 0.7f)
						{
							BounceBackFromNormal(normal, 6.0f);
							TakeDamage(_collideDamageAmount);
							break; 
						}
					}

				}
			}
		}
	}

	public async Task SpawnPercyDrone()
	{
		if (_percyDroneScene == null)
		{
			GD.PushError("[PlayerGround] Percy drone has not been assigned");
			return;
		}

		EmitSignal(SignalName.PercyPowerupCollected);
		await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

		Node3D spawnMarker = GD.Randf() < 0.5f ? _spawnLeft : _spawnRight;

		PercyDrone droneNode = _percyDroneScene.Instantiate<PercyDrone>();

		// Add bullet to scene (use current scene root)
		GetTree().CurrentScene.AddChild(droneNode);

		droneNode.Activate(this, spawnMarker.GlobalPosition, _despawnMarker.GlobalPosition);
	}

}
