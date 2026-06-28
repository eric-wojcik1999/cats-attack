using Godot;
using System;
using System.Threading.Tasks;

public partial class PlayerGround : CharacterBody3D
{
	[Signal] public delegate void HealthChangedEventHandler(int amount);
	[Signal] public delegate void DiedEventHandler();
	[Signal] public delegate void PercyPowerupCollectedEventHandler();
	[Signal] public delegate void PercyPowerupActivatedEventHandler(float duration);
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
	[Export] private float _percyPowerupDuration = 15f;

	private bool _hasDied = false;
	
	[Export] private Node3D _playerAirPositionMarker;
	[Export] public PackedScene _percyDroneScene;
	private Node3D _spawnLeft;
	private Node3D _spawnRight;
	private Node3D _despawnMarker;

	// Getters 
	public int Health => _health;
	public int MaxHealth => _maxHealth;
	public Vector3 ForwardDir 
	{
		get 
		{
			Vector3 forward = -GlobalTransform.Basis.Z;
			forward.Y = 0f;

			if (forward.LengthSquared() < 0.0001f)
			{
				return Vector3.Forward;
			}

			return forward.Normalized();
		}
	}
	public Vector3 RightDir
	{
		get{
			Vector3 forward = ForwardDir;

			// Horizontal right vector derived from forward.
			Vector3 right = new Vector3(-forward.Z, 0f, forward.X);

			if (right.LengthSquared() < 0.0001f)
			{
				return Vector3.Right;
			}

			return right.Normalized();
		}
	}
	public bool IsKnockbackActive => _knockbackTimer > 0f;
	public Vector3 AirAnchorPosition => _playerAirPositionMarker != null ? _playerAirPositionMarker.GlobalPosition : GlobalPosition;

	// Jump Pad vars
	[Export] private float _jumpPadControlLockTime = 0.05f;
	[Export] private float _jumpPadBlendBackTime = 7f;
	[Export] private float _jumpPadAirSteeringStrength = 1.35f;
	// Keep upward motion close to normal but make the falling different/softer.
	[Export] private float _jumpPadUpGravityModifier = 0.9f;
	[Export] private float _jumpPadFallGravityModifier = 1.5f;
	// Prevents fall from becoming too fast/abrupt near the end
	[Export] private float _jumpPadMaxFallSpeed = 55.0f;
	[Export] private bool _disableFloorSnappingDuringJumpPad = true;
	[Export] private float _jumpPadTriggerLockoutTime = 0.75f;
	private float _externalLaunchTimer = 0f;
	private float _jumpPadBlendBackTimer = 0f;
	private float _jumpPadTriggerLockoutTimer = 0f;
	private bool _jumpPadArcActive = false;
	private float _defaultFloorSnapLength = 0f;

	// Jump pad - landing impact
	[Export] private float _jumpPadBounceMinImpactSpeed = 2.0f;
	[Export] private float _jumpPadBounceVelocityMultipler = 0.16f;
	[Export] private float _jumpPadMaxBounceVelocity = 2.5f;
	[Export] private int _jumpPadMaxBounces = 4;
	[Export] private float _jumpPadLandingHorizontalRetention = 0.88f;
	private int _jumpPadBouncesRemaining = 0;

	// Jump pad - skidding
	[Export] private float _jumpPadSkidTime = 0f;
	[Export] private float _jumpPadSkidFriction = 22.0f;
	[Export] private float _jumpPadSkidSteeringStrength = 0.95f;
	private float _jumpPadSkidTimer = 5f;
	private Vector3 _jumpPadSkidVelocity = Vector3.Zero;

	// Speed pad vars
	[Export] private float _speedBoostAccelerationTime = 0.35f;
	[Export] private float _speedBoostDecelerationTime = 0.75f;
	[Export] private float _speedBoostTriggerLockoutTime = 0.75f;
	[Export] private NodePath _cameraNodePath = "CameraController/CameraTarget/Camera3D";
	[Export] private float _speedBoostFovIncrease = 12.0f;
	private float _speedBoostTimer = 0f;
	private float _speedBoostElapsedTimer = 0f;
	private float _speedBoostTotalDuration = 0f;
	private float _speedBoostTargetMultiplier = 1f;
	private float _currentSpeedBoostMultiplier = 1f;
	private float _speedBoostTriggerLockoutTimer = 0f;
	private Camera3D _camera;
	private float _baseCameraFov = 75.0f;

	// Path guide vars
	[ExportGroup("Path Guide")]
	[Export] private bool _usePathGuide = true;
	[Export] private NodePath _pathGuideNodePath = "";
	[Export] private float _pathLookAheadDistance = 4.0f;
	[Export] private float _pathTurnDegPerSec = 10.0f; // Maybe set to 30.0f default?
	[Export] private bool _snapYawToPathOnReady = false;

	// Likely need to only have ONE path for the entire level
	private Path3D _pathGuide;
	private Curve3D _pathCurve;

	public override void _Ready() 
	{
		_currentTurnDegPerSec = _defaultTurnDegPerSec;
		_cameraController = GetNode<Node3D>("CameraController");
		_spawnLeft = GetNode<Node3D>(_droneSpawnLeftPath);
		_spawnRight = GetNode<Node3D>(_droneSpawnRightPath);
		_despawnMarker = GetNode<Node3D>(_droneDespawnPath);
		_camera = GetNodeOrNull<Camera3D>(_cameraNodePath);

		_defaultFloorSnapLength = FloorSnapLength;

		if (_camera != null)
		{
			_baseCameraFov = _camera.Fov;
		}
		else
		{
			GD.PushWarning("[PlayerGround] Camera3D not found. Check _cameraNodePath.");
		}

		InitialisePathGuide();

		if (_snapYawToPathOnReady)
		{
			if (TryGetPathGuideForward(out Vector3 initialForward))
			{
				SnapYawToForward(initialForward);
			}
		}
	}
    
	public override void _PhysicsProcess(double delta)
	{

		if (_hasDied)
		{
			return;
		}

		float dt = (float)delta;

		// ───── Jump Pad Handling ─────
		if (_externalLaunchTimer > 0f)
		{
			_externalLaunchTimer -= dt;

			if(_externalLaunchTimer < 0f) 
			{
				_externalLaunchTimer = 0f;
			}
		}

		if (_jumpPadBlendBackTimer > 0f)
		{
			_jumpPadBlendBackTimer -= dt;
			if (_jumpPadBlendBackTimer < 0f)
			{
				_jumpPadBlendBackTimer = 0f;
			}
		}

		if (_jumpPadTriggerLockoutTimer > 0f)
		{
			_jumpPadTriggerLockoutTimer -= dt;
			if (_jumpPadTriggerLockoutTimer < 0f)
			{
				_jumpPadTriggerLockoutTimer = 0f;
			}
		}

		// ───── Speed Boost Handling ─────

		UpdateSpeedBoost(dt);
		UpdateSpeedBoostFov();

		if (_speedBoostTriggerLockoutTimer > 0f)
		{
			_speedBoostTriggerLockoutTimer -= dt;

			if (_speedBoostTriggerLockoutTimer < 0f)
			{
				_speedBoostTriggerLockoutTimer = 0f;
			}
		}

		// ───── Guide Path Direction ─────
		// Replaces direction markers by continously choosing a desired forward from the nearest point on the Path3D guide curve
		UpdatePathGuideForward();

		// ───── Update Player Rotation ─────
		UpdateYawTurning(dt);

		bool onFloor = IsOnFloor();
		float horizontalInput = Input.GetActionStrength("move_ground_right") - Input.GetActionStrength("move_ground_left");

		// Start from current velocity and build next frame's velocity
		Vector3 newVelocity = Velocity;

		// ───── Apply gravity ─────
		if (!onFloor)
		{
			Vector3 gravity = GetGravity();

			if (_jumpPadArcActive)
			{
				if (newVelocity.Y > 0f)
				{
					// Going up - keep gravity mostly the same
					gravity *= _jumpPadUpGravityModifier;
				}
				else
				{
					// Going down - make gravity a bit softer
					// ACTUALLY maybe want to tweak with hard gravity here so can skid around, not necessarily padded
					gravity *= _jumpPadFallGravityModifier;
				}
			}

			newVelocity += gravity * dt;

			if (_jumpPadArcActive && _jumpPadMaxFallSpeed > 0f)
			{
				newVelocity.Y = Mathf.Max(newVelocity.Y, -_jumpPadMaxFallSpeed);
			}
		}

		// ───── Shared horizontal basis vectors ─────
		// Vector3 forwardDir = -Transform.Basis.Z;
		// Vector3 rightDir = Transform.Basis.X;
		// This means that auto-forward and lateral movement uses the player's world facing direction
		Vector3 forwardDir = ForwardDir;
		Vector3 rightDir = RightDir;

		float movementSpeedMultiplier = _currentSpeedBoostMultiplier;
		
		// Lateral input vector
		Vector3 lateralInputVelocity = Vector3.Zero;
		if (Mathf.Abs(horizontalInput) >= 0.01f)
		{
			lateralInputVelocity = rightDir * horizontalInput * (_baseMovementSpeed * movementSpeedMultiplier);
		}

		if (_jumpPadArcActive)
		{
			if (_externalLaunchTimer <= 0f)
			{
				// Preserve strong launch and add small amount of air steering
				newVelocity.X += lateralInputVelocity.X * _jumpPadAirSteeringStrength * dt;
				newVelocity.Z += lateralInputVelocity.Z * _jumpPadAirSteeringStrength * dt;
			}
		}
		else if (_jumpPadSkidTimer > 0f && onFloor) 
		{
			_jumpPadSkidTimer -= dt;

			if (_jumpPadSkidTimer < 0f)
			{
				_jumpPadSkidTimer = 0f;
			}

			Vector3 forwardVelocity = forwardDir * (_autoForwardSpeed * movementSpeedMultiplier);
			Vector3 targetHorizontalVelocity = forwardVelocity + lateralInputVelocity;

			// Allow limited steering while skidding
			_jumpPadSkidVelocity += lateralInputVelocity * _jumpPadSkidSteeringStrength * dt;

			float skidSpeed = _jumpPadSkidVelocity.Length();

			if (skidSpeed > 0.001f)
			{
				Vector3 skidDir = _jumpPadSkidVelocity / skidSpeed;

				// Friction to reduce sliding
				skidSpeed = Mathf.MoveToward(skidSpeed, 0f, _jumpPadSkidFriction * dt);

				Vector3 skidHorizontalVelocity = skidDir * skidSpeed;

				// At end of skid blend back into normal movement
				float skidElapsedT = 1f - (_jumpPadSkidTimer / Mathf.Max(_jumpPadSkidTime, 0.001f));
				float controlBlend = Mathf.InverseLerp(0.65f, 1.0f, skidElapsedT);
				controlBlend = SmoothStep01(controlBlend);

				Vector3 finalHorizontalVelocity = skidHorizontalVelocity.Lerp(targetHorizontalVelocity, controlBlend);

				newVelocity.X = finalHorizontalVelocity.X;
				newVelocity.Z = finalHorizontalVelocity.Z;
				_jumpPadSkidVelocity = skidHorizontalVelocity;
			}
			else
			{
				newVelocity.X = targetHorizontalVelocity.X;
				newVelocity.Z = targetHorizontalVelocity.Z;
				_jumpPadSkidTimer = 0f;
			}

		}
		else if (_knockbackTimer > 0f)
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
			Vector3 forwardVelocity = forwardDir * (_autoForwardSpeed * movementSpeedMultiplier);

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

		float preMoveVerticalVelocity = newVelocity.Y;
		Vector3 preMoveHorizontalVelocity = new Vector3(newVelocity.X, 0f, newVelocity.Z);

		// ───── Commit movement once ─────
		Velocity = newVelocity;
		MoveAndSlide();

		if (_jumpPadArcActive && IsOnFloor() && preMoveVerticalVelocity <= 0f)
		{
			HandleJumpPadLanding(-preMoveVerticalVelocity, preMoveHorizontalVelocity);
		}

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
		EmitSignal(SignalName.PercyPowerupActivated, _percyPowerupDuration);
		await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);

		Node3D spawnMarker = GD.Randf() < 0.5f ? _spawnLeft : _spawnRight;
		PercyDrone droneNode = _percyDroneScene.Instantiate<PercyDrone>();

		// Add bullet to scene (use current scene root)
		GetTree().CurrentScene.AddChild(droneNode);

		droneNode.Activate(this, spawnMarker.GlobalPosition, _despawnMarker.GlobalPosition, _percyPowerupDuration);
	}

	public bool CanTriggerJumpPad()
	{
		bool canTrigger = true;

		if (_jumpPadTriggerLockoutTimer > 0f)
		{
			canTrigger = false;
		}

		return canTrigger;
	}

	public void LaunchFromJumpPad(Vector3 horizontalDir, float horizontalSpeed, float verticalSpeed)
	{
		if (!CanTriggerJumpPad()) 
		{
			return;
		}

		horizontalDir.Y = 0f;

		if (horizontalDir.LengthSquared() < 0.0001f)
		{
			horizontalDir = ForwardDir;
		}

		horizontalDir = horizontalDir.Normalized();

		Velocity = new Vector3(horizontalDir.X * horizontalSpeed, verticalSpeed, horizontalDir.Z * horizontalSpeed);

		_jumpPadArcActive = true;
		_jumpPadBouncesRemaining = _jumpPadMaxBounces;
		_jumpPadSkidTimer = 0f;
		_jumpPadSkidVelocity = Vector3.Zero;

		if (_disableFloorSnappingDuringJumpPad) 
		{
			 FloorSnapLength = 0f;
		}

		// Short period where the launch is mostly untouched.
		_externalLaunchTimer = _jumpPadControlLockTime;

		// // Longer period where momentum gradually blends back to normal movement.
		// _jumpPadBlendBackTimer = 0f;

		// Prevent neighboring jump pads from triggering.
		_jumpPadTriggerLockoutTimer = _jumpPadTriggerLockoutTime;

		// Cancel knockback state so it doesn't fight the jump pad
		_knockbackTimer = 0f;
		_knockbackHorizontal = Vector3.Zero;
	}

	private void HandleJumpPadLanding(float impactFallSpeed, Vector3 landingHorizontalVelocity)
	{
		Vector3 retainedHorizontal = landingHorizontalVelocity * _jumpPadLandingHorizontalRetention;

		bool shouldBounce = _jumpPadBouncesRemaining > 0 && impactFallSpeed >= _jumpPadBounceMinImpactSpeed;

		if (shouldBounce)
		{
			_jumpPadBouncesRemaining--;

			float bounceVelocity = impactFallSpeed * _jumpPadBounceVelocityMultipler;
			bounceVelocity = Mathf.Clamp(bounceVelocity, 0f, _jumpPadMaxBounceVelocity);

			// Bounce upwards
			Velocity = new Vector3(retainedHorizontal.X, bounceVelocity, retainedHorizontal.Z);
			_jumpPadArcActive = true;

			if (_disableFloorSnappingDuringJumpPad)
			{
				FloorSnapLength = 0f;
			}

			return;
		}

		// Final landing - stop arc
		EndJumpArc();

		_jumpPadSkidVelocity = retainedHorizontal;
		_jumpPadSkidTimer = _jumpPadSkidTime;

		Velocity = new Vector3(retainedHorizontal.X, 0f, retainedHorizontal.Z);
	}

	private void EndJumpArc()
	{
		_jumpPadArcActive = false;

		if (_disableFloorSnappingDuringJumpPad)
		{
			FloorSnapLength = _defaultFloorSnapLength;
		}
	}

	public bool CanTriggerSpeedBoostPad()
	{
		return _speedBoostTriggerLockoutTimer <= 0f;
	}

	public void ActivateSpeedBoost(float multiplier, float duration)
	{
		if (!CanTriggerSpeedBoostPad())
		{
			return;
		}

		multiplier = Mathf.Max(1f, multiplier);
		duration = Mathf.Max(0.1f, duration);

		bool wasAlreadyBoosted = _speedBoostTimer > 0f;

		_speedBoostTargetMultiplier = multiplier;
		_speedBoostTotalDuration = duration;
		_speedBoostTimer = duration;

		// If already boosted, do not restart ramp-up
		// Prevents another boost pad from slowing player
		if (!wasAlreadyBoosted)
		{
			_speedBoostElapsedTimer = 0f;
		}
		else
		{
			_speedBoostElapsedTimer = Mathf.Max(_speedBoostElapsedTimer, _speedBoostAccelerationTime);
		}
		
		_speedBoostTriggerLockoutTimer = _speedBoostTriggerLockoutTime;
	}

	private void UpdateSpeedBoost(float dt)
	{
		if (_speedBoostTimer <= 0f)
		{
			_speedBoostTimer = 0f;
			_speedBoostElapsedTimer = 0f;
			_speedBoostTotalDuration = 0f;
			_speedBoostTargetMultiplier = 1f;
			_currentSpeedBoostMultiplier = 1f;
			return;
		}

		_speedBoostTimer -= dt;
		_speedBoostElapsedTimer += dt;

		if (_speedBoostTimer < 0f)
		{
			_speedBoostTimer = -0f;
		}

		float targetMultiplier = _speedBoostTargetMultiplier;

		// Phase 1: ramp up from normal speed to boosted speed.
		if (_speedBoostElapsedTimer < _speedBoostAccelerationTime)
		{
			float t = _speedBoostElapsedTimer / Mathf.Max(_speedBoostAccelerationTime, 0.001f);
			t = SmoothStep01(t);
			_currentSpeedBoostMultiplier = Mathf.Lerp(1f, targetMultiplier, t);
		}
		// Phase 2: ramp down from boosted speed to normal speed.
		else if (_speedBoostTimer < _speedBoostDecelerationTime)
		{
			float t = _speedBoostTimer / Mathf.Max(_speedBoostDecelerationTime, 0.001f);
			t = SmoothStep01(t);
			_currentSpeedBoostMultiplier = Mathf.Lerp(1f, targetMultiplier, t);
		}
		// Phase 3: hold boosted speed.
		else
		{
			_currentSpeedBoostMultiplier = targetMultiplier;
		}
	}
	private void UpdateSpeedBoostFov()
	{
		if (_camera == null)
		{
			return;
		}

		float boostAmount = 0f;

		if (_speedBoostTargetMultiplier > 1f)
		{
			boostAmount = Mathf.InverseLerp(1f, _speedBoostTargetMultiplier, _currentSpeedBoostMultiplier);
		}

		boostAmount = SmoothStep01(boostAmount);

		_camera.Fov = _baseCameraFov + (_speedBoostFovIncrease * boostAmount);
	}

	private float SmoothStep01(float t)
	{
		t = Mathf.Clamp(t, 0f, 1f);
		return t * t * (3f - 2f * t);
	}

	private void InitialisePathGuide()
	{
		if (!_usePathGuide)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(_pathGuideNodePath.ToString()))
		{
			GD.PushWarning("[PlayerGround] Path guide is enabled, but _pathGuideNodePath is empty.");
			return;
		}

		_pathGuide = GetNodeOrNull<Path3D>(_pathGuideNodePath);

		if (_pathGuide == null)
		{
			GD.PushWarning($"[PlayerGround] Could not find Path3D at path {_pathGuideNodePath}");
			return;	
		}

		_pathCurve = _pathGuide.Curve;

	}

	private void UpdatePathGuideForward()
	{
		if (!_usePathGuide)
		{
			return;
		}

		if (TryGetPathGuideForward(out Vector3 desiredForward))
		{
			SetDesiredForward(desiredForward, _pathTurnDegPerSec);
		}
	
	}

	private bool TryGetPathGuideForward(out Vector3 desiredForward)
	{
		desiredForward = Vector3.Zero;

		if (_pathGuide == null)
		{
			return false;
		}

		// null-coalescing assignment operator in csharp
		// only assign right if left is null
		_pathCurve ??= _pathGuide.Curve;

		if (_pathCurve == null)
		{
			return false;
		}

		float pathLength = _pathCurve.GetBakedLength();

		if (pathLength <= 0.01f)
		{
			return false;
		}

		// Convert player world position into the Path3D's local space.
		Vector3 playerLocalPos = _pathGuide.ToLocal(GlobalPosition);

		// Find nearest offset on the baked curve.
		float closestOffset = _pathCurve.GetClosestOffset(playerLocalPos);

		// Look slightly ahead along the curve
		float lookAhead = Mathf.Max(_pathLookAheadDistance, 0.01f);
		float aheadOffset = Mathf.Clamp(closestOffset + lookAhead, 0f, pathLength);
		Vector3 closestLocal = _pathCurve.SampleBaked(closestOffset, true);
		Vector3 aheadLocal = _pathCurve.SampleBaked(aheadOffset, true);
		Vector3 closestGlobal = _pathGuide.ToGlobal(closestLocal);
		Vector3 aheadGlobal = _pathGuide.ToGlobal(aheadLocal);
		Vector3 direction = aheadGlobal - closestGlobal;

		// If we are near end of curve, ahead and closest may be similar.
		// In this circumstance, sample behind the player instead.
		if (direction.LengthSquared() < 0.0001f)
		{
			float behindOffset = Mathf.Clamp(closestOffset - lookAhead, 0f, pathLength);
			Vector3 behindLocal = _pathCurve.SampleBaked(behindOffset, true);
			Vector3 behindGlobal = _pathGuide.ToGlobal(behindLocal);
			direction = closestGlobal - behindGlobal;
		}

		// Keep the player upright - path controls the yaw not the pitch.
		direction.Y = 0f;

		if (direction.LengthSquared() < 0.0001f)
		{
			return false;
		}

		desiredForward = direction.Normalized();
		return true;
	}

	private void SnapYawToForward(Vector3 forward)
	{
		forward.Y = 0f;

		if (forward.LengthSquared() < 0.0001f)
		{
			return;
		}

		forward = forward.Normalized();

		float targetYaw = Mathf.Atan2(-forward.X, -forward.Z);

		Vector3 rotation = GlobalRotation;
		rotation.Y = targetYaw;
		GlobalRotation = rotation;

		_desiredForward = forward;
		_hasDesiredForward = false;
	}
}
