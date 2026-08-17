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
	public float CurrentAutoForwardSpeed => _autoForwardSpeed * _currentSpeedBoostMultiplier;
	public float CurrentForwardVelocity
	{
		get
		{
			Vector3 horizontalVelocity =
				new Vector3(Velocity.X, 0.0f, Velocity.Z);

			return horizontalVelocity.Dot(ForwardDir);
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

	[ExportGroup("Player Visual")]
	[Export] private Node3D _playerVisualRoot;

	[ExportGroup("Visual Lean")]
	[Export(PropertyHint.Range, "0.0, 45.0, 0.5")]
	private float _maximumnLeanDegrees = 12.0f;

	[Export(PropertyHint.Range, "1.0, 30.0, 0.5")]
	private float _leanSpeed = 10.0f;

	private float _visualBaseRoll;

	[ExportGroup("GroundDust")]
	[Export] private GpuParticles3D _groundDust;
	[Export] private GpuParticles3D _landingDustBurst;
	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	private float _groundDustVelocityInheritance = 0.85f;

	[Export(PropertyHint.Range, "0.0,20.0,0.1")]
	private float _landingDustMinimumFallSpeed = 1.5f;

	[Export(PropertyHint.Range, "0.1,30.0,0.1")]
	private float _landingDustFullImpactSpeed = 10.0f;

	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	private float _landingDustMinimumAmountRatio = 0.4f;

	private bool _wasOnFloor;
	private bool _floorStateInitialised;
	[ExportGroup("Audio")]
	[Export] private AudioStream _hurtSfx;
	[Export] private AudioStream _deathSfx;

	[Export(PropertyHint.Range, "-40.0, 6.0, 0.5")]
	private float _hurtSfxVolumeDb = -6.0f;

	[Export(PropertyHint.Range, "-40.0, 6.0, 0.5")]
	private float _deathSfxVolumeDb = -4.0f;
	[ExportGroup("Jump Pad Skid Audio")]

	[Export] private AudioStream _jumpPadSkidSfx;

	[Export(PropertyHint.Range, "-40.0, 6.0, 0.5")]
	private float _jumpPadSkidVolumeDb = -4.0f;

	// How much of the sound is allowed to play before fading.
	[Export(PropertyHint.Range, "0.05, 2.0, 0.05")]
	private float _jumpPadSkidClipTime = 0.35f;

	// Quick fade after the audible portion.
	[Export(PropertyHint.Range, "0.01, 0.5, 0.01")]
	private float _jumpPadSkidFadeTime = 0.10f;

	// Allows stronger landings to make louder skid sounds.
	[Export(PropertyHint.Range, "0.0, 1.0, 0.05")]
	private float _jumpPadSkidMinimumVolumeFactor = 0.45f;

	private AudioStreamPlayer _jumpPadSkidAudio;
	private Tween _jumpPadSkidAudioTween;

	[Export(PropertyHint.Range, "0.0,30.0,0.5")]
	private float _hardLandingMinimumFallSpeed = 7.0f;

		
	public override void _Ready() 
	{
		_currentTurnDegPerSec = _defaultTurnDegPerSec;
		_cameraController = GetNode<Node3D>("CameraController");
		_spawnLeft = GetNode<Node3D>(_droneSpawnLeftPath);
		_spawnRight = GetNode<Node3D>(_droneSpawnRightPath);
		_despawnMarker = GetNode<Node3D>(_droneDespawnPath);
		_camera = GetNodeOrNull<Camera3D>(_cameraNodePath);

		if (_playerVisualRoot == null) 
		{
			GD.PushWarning("[PlayerGround] Player Visual Root has not been assigned.");
		} else 
		{
			_visualBaseRoll = _playerVisualRoot.Rotation.Z;
		}

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

		InitialiseGroundDustContinious();
		InitialiseGroundDustLanding();
		InitialiseJumpPadSkidAudio();
		AddToGroup("Player");
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
		UpdateVisualLean(horizontalInput, dt);

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

		// This is the floor state after this frame's movement.
		bool isOnFloorNow = IsOnFloor();

		// Detect transition from being airborne to grounded
		bool landedThisFrame = _floorStateInitialised && !_wasOnFloor && isOnFloorNow;

		float landingImpactSpeed = Mathf.Max(-preMoveVerticalVelocity, 0.0f);

		bool wasJumpPadLanding = _jumpPadArcActive;

		if (_jumpPadArcActive && isOnFloorNow && preMoveVerticalVelocity <= 0f)
		{
			HandleJumpPadLanding(-preMoveVerticalVelocity, preMoveHorizontalVelocity);
		}

		// Hard NORMAL landing.
		// Jump-pad impacts already play the sound from HandleJumpPadLanding().
		if (landedThisFrame && !wasJumpPadLanding && landingImpactSpeed >= _hardLandingMinimumFallSpeed)
		{
			PlayJumpPadSkidSfx(landingImpactSpeed);
		}


		// Only create burst after landing handler
		// Jump pad bounce should not produce full landing burst until after player reaches final landing
		if (landedThisFrame && landingImpactSpeed >= _landingDustMinimumFallSpeed)
		{
			TriggerLandingDustBurst(landingImpactSpeed);
		}

		// ───── Apply ground dust effect ─────
		UpdateGroundDust(isOnFloorNow);

		// Save floor state for the next physics frame.
		_wasOnFloor = isOnFloorNow;
		_floorStateInitialised = true;

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

		if (_health < prevHealth && _health > 0)
		{
			PlayDetachedSfx(_hurtSfx, _hurtSfxVolumeDb);
			// Emit signal to update UI health value
			EmitSignal(SignalName.HealthChanged, _health);
		}

		// Handling death
		if (_health == 0 && prevHealth > 0)
		{
			PlayDetachedSfx(_deathSfx, _deathSfxVolumeDb);
			_hasDied = true;
			EmitSignal(SignalName.Died);

			if (IsInstanceValid(_playerVisualRoot))
			{
				_playerVisualRoot.Visible = false;
			}

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

    	PlayJumpPadSkidSfx(impactFallSpeed);

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

	private void UpdateVisualLean(float horizontalInput, float delta)
	{
		if (!GodotObject.IsInstanceValid(_playerVisualRoot))
		{
			return;
		}

		float leanDirection = 1.0f;

		float targetLeanRadius = Mathf.DegToRad(_maximumnLeanDegrees * horizontalInput * leanDirection);

		float targetRoll = _visualBaseRoll + targetLeanRadius;

		float smoothing = 1.0f - Mathf.Exp(-_leanSpeed * delta);

		Vector3 visualRotation = _playerVisualRoot.Rotation;

		visualRotation.Z = Mathf.LerpAngle(visualRotation.Z, targetRoll, smoothing);

		_playerVisualRoot.Rotation = visualRotation;
	}

	private void InitialiseGroundDustContinious()
	{
		if (!GodotObject.IsInstanceValid(_groundDust)) 
		{
			GD.PushWarning("[PlayerGround] Ground Dust has not been assigned.");
			return;
		}

		_groundDust.LocalCoords  = true;
		_groundDust.OneShot = false;
		_groundDust.Explosiveness = 0.0f;
		_groundDust.Emitting = false;

		if (_groundDust.ProcessMaterial is ParticleProcessMaterial material)
		{
			material.InheritVelocityRatio = Mathf.Clamp(_groundDustVelocityInheritance, 0.0f, 0.1f);
		}
		else 
		{
			GD.PushWarning("[PlayerGround] Ground Dust does not use a ParticleProcessMaterial.");
		}
	}

	private void InitialiseGroundDustLanding()
	{
		if (!GodotObject.IsInstanceValid(_landingDustBurst)) 
		{
			GD.PushWarning("[PlayerGround] Landing Dust Burst has not been assigned.");
			return;
		}

		_landingDustBurst.LocalCoords  = false;
		_landingDustBurst.OneShot = true;
		_landingDustBurst.Explosiveness = 1.0f;
		_landingDustBurst.Emitting = false;
	}

	private void UpdateGroundDust(bool isOnFloorNow)
	{
		if (!GodotObject.IsInstanceValid(_groundDust))
		{
			GD.PushWarning("[PlayerGround] Ground Dust has not been assigned.");
			return;
		}

		bool shouldEmit = isOnFloorNow && Velocity.Y <= 0.05 && !_jumpPadArcActive && !_hasDied;

		if (_groundDust.Emitting != shouldEmit)
		{
			_groundDust.Emitting = shouldEmit;
		}
	}

	private void TriggerLandingDustBurst(float impactFloorSpeed)
	{
		if (!GodotObject.IsInstanceValid(_landingDustBurst))
		{
			GD.PushWarning("[PlayerGround] Landing Dust Burst has not been assigned.");
			return;
		}

		float fullImpactSpeed = Mathf.Max(_landingDustFullImpactSpeed, _landingDustMinimumFallSpeed + 0.01f);

		float impactFactor = Mathf.InverseLerp(_landingDustMinimumFallSpeed, fullImpactSpeed, impactFloorSpeed);

		_landingDustBurst.AmountRatio = Mathf.Lerp(_landingDustMinimumAmountRatio, 1.0f, impactFactor);

		// Restart immediately, even if particles from a previous
		// landing burst are still alive.
		_landingDustBurst.Restart();
	}

	private void PlayDetachedSfx(AudioStream stream, float volumeDb)
	{
		if (stream == null)
		{
			return;
		}

		AudioStreamPlayer player = new AudioStreamPlayer();

		player.Stream = stream;
		player.VolumeDb = volumeDb;
		player.Bus = "Sfx";

		Node parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	private void InitialiseJumpPadSkidAudio()
	{
		if (_jumpPadSkidSfx == null)
		{
			GD.PushWarning("[PlayerGround] Jump pad SFX has not been assigned.");

			return;
		}

		_jumpPadSkidAudio = new AudioStreamPlayer();
		_jumpPadSkidAudio.Name = "JumpPadSkidAudio";
		_jumpPadSkidAudio.Stream = _jumpPadSkidSfx;
		_jumpPadSkidAudio.VolumeDb = _jumpPadSkidVolumeDb;
		_jumpPadSkidAudio.Bus = "Sfx";
		AddChild(_jumpPadSkidAudio);
	}

	private void PlayJumpPadSkidSfx(float impactSpeed)
	{
		if (!GodotObject.IsInstanceValid(_jumpPadSkidAudio))
		{
			return;
		}

		// Kill the previous fade sequence if another bounce happens
		// before it has finished.
		if (_jumpPadSkidAudioTween != null && _jumpPadSkidAudioTween.IsValid())
		{
			_jumpPadSkidAudioTween.Kill();
		}

		// Stop whatever portion of the sound is currently playing.
		_jumpPadSkidAudio.Stop();

		// Scale volume using landing strength.
		float impactFactor = Mathf.InverseLerp(_jumpPadBounceMinImpactSpeed, Mathf.Max(_landingDustFullImpactSpeed, _jumpPadBounceMinImpactSpeed + 0.01f), impactSpeed);
		impactFactor = Mathf.Clamp(impactFactor, 0f, 1f);
		float volumeFactor = Mathf.Lerp(_jumpPadSkidMinimumVolumeFactor, 1f, impactFactor);

		// Convert a linear intensity factor into dB.
		float impactVolumeDb = _jumpPadSkidVolumeDb + Mathf.LinearToDb(Mathf.Max(volumeFactor, 0.001f));
		_jumpPadSkidAudio.VolumeDb = impactVolumeDb;
		_jumpPadSkidAudio.Play(0f);
		_jumpPadSkidAudioTween = CreateTween();
		// First leave it at full volume for the short skid portion.
		_jumpPadSkidAudioTween.TweenInterval(_jumpPadSkidClipTime);
		// Then fade it quickly.
		_jumpPadSkidAudioTween.TweenProperty(_jumpPadSkidAudio, "volume_db", -40f, _jumpPadSkidFadeTime);
		_jumpPadSkidAudioTween.TweenCallback(
			Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(_jumpPadSkidAudio))
				{
					_jumpPadSkidAudio.Stop();

					// Restore base volume so the next landing doesn't begin at -40 dB.
					_jumpPadSkidAudio.VolumeDb = _jumpPadSkidVolumeDb;
				}
			})
		);
	}
}
