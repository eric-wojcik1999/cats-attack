using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerAir : CharacterBody3D
{
	[Export] private PlayerGround _playerGround;
	[ExportGroup("Follow")]
	[Export] private float _followBackDistance = 0.0f;
	[Export] private float _positionSmoothSpeed = 14.0f;
	[Export] private float _yawFollowSpeed = 16.0f;
	[ExportGroup("Screen space lateral control")]
	[Export] private float _maxLateralOffsetLeft = 8.0f;   // Need to be larger due do asymmetrical player placement skewing towards the right
	[Export] private float _maxLateralOffsetRight = 4.5f;
	[Export] private float _lateralSmoothSpeed = 15.0f;
	[Export] private float _mouseSensitivity = 0.015f;
	[Export(PropertyHint.Range, "0.0,0.49,0.01")] private float _screenEdgePadding = 0.08f;
	[Export] private bool _invertMouseX = false;
	private float _currentLateralOffset = 0f;
	private float _targetLateralOffset = 0f;
	[ExportGroup("Auto vertical avoidance")]

	[Export] private float _maxExtraHeight = 6.0f; 
    [Export] private float _riseSpeed = 8.0f;
    [Export] private float _fallSpeed = 10.0f;
	private float _baseOffsetYFromAnchor = 0f;  
	private float _currentExtraHeight = 0f;
	private float _hangTime = 0.25f;
	private float _hangTimer = 0f;
	private readonly List<RayCast3D> _wallRays = new();

	[ExportGroup("Shooting")]
	[Export] public NodePath _muzzleMarkerPath;
	[Export] public PackedScene _bulletScene;
	[Export] public float _rateOfFire = 0.1f;
	private float _fireTimer = 0f;
	private Node3D _muzzleMarker;

	[ExportGroup("Pitch Aim")]
	[Export] private float _pitchMouseSensitivity = 0.0025f;
	[Export] private float _minPitchDeg = -35f;
	[Export] private float _maxPitchDeg = 45f;
	[Export] private bool _invertMouseY = false;
	[Export] private float _pitchSmoothSpeed = 18f;
	private float _targetPitch = 0f;
	private float _currentPitch = 0f;

	public override void _Ready()
	{
		_muzzleMarker = GetNode<Node3D>(_muzzleMarkerPath);

		if (_muzzleMarker == null )
		{
			GD.Print("[PlayerAir] Muzzle marker not found");
		}
		else
		{
			GD.Print("[PlayerAir] Muzzle marker initialised");
		}

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

		initialiseWallRays();
		_baseOffsetYFromAnchor = GlobalPosition.Y - _playerGround.AirAnchorPosition.Y;
	}

	private void initialiseWallRays()
	{
		Node root = GetNodeOrNull(".") ?? this;

		foreach (Node child in root.GetChildren()) 
		{
			if (child is RayCast3D ray)
			{
				_wallRays.Add(ray);
				
			}
		}

		if (_wallRays.Count == 0)
		{
			GD.Print($"[PlayerAir] No RayCast3D nodes found.");
		} 
		else 
		{
			GD.Print($"[PlayerAir] {_wallRays.Count} RayCast3D nodes have been initialised.");
			foreach (var ray in _wallRays)
			{
				ray.Enabled = true;
			}
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion mouseMotion)
		{
			float direction = _invertMouseY ? 1f : -1f;

			_targetPitch += mouseMotion.Relative.Y * _pitchMouseSensitivity * direction;

			float minPitch = Mathf.DegToRad(_minPitchDeg);
			float maxPitch = Mathf.DegToRad(_maxPitchDeg);

			_targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
		}

	}

	public override void _PhysicsProcess(double delta)
	{
		if (_playerGround != null)
		{
			float dt = (float)delta;

			UpdateTargetLateralFromMouseScreenX();

			// ───── Calculate independant lateral movement ─────
			float lateralBlend = 1f - Mathf.Exp(-_lateralSmoothSpeed * dt);
			_currentLateralOffset = Mathf.Lerp(_currentLateralOffset, _targetLateralOffset, lateralBlend);

			// // ───── Auto-forward movement, shadowing Player Ground + auto vertical adjustment ─────
			// Vector3 targetPos = GetTargetFollowPosition();
			// float posBlend = 1f - Mathf.Exp(-_positionSmoothSpeed * dt);
			// GlobalPosition = GlobalPosition.Lerp(targetPos, posBlend);

			// Compute X/Z follow target
			Vector3 targetPos = GetTargetFollowPosition();

			// Apply vertical avoidance (edits targetPos.Y)
			ApplyVerticalAvoidance(ref targetPos, dt);

			// Smooth position
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
			// Calculate pitch
			float pitchStep = Mathf.Clamp(_pitchSmoothSpeed * dt, 0f, 1f);
			_currentPitch = Mathf.Lerp(_currentPitch, _targetPitch, pitchStep);
			rotation.X = _currentPitch;
			rotation.Z = 0f;

			GlobalRotation = rotation;

			// Fire bullets
			AutoShoot(dt);
		}
		else 
		{
			GD.Print("[Player Air] Player Ground not found");
		}
	}

	private void UpdateTargetLateralFromMouseScreenX()
	{
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;

		if (viewportSize.X > 1f)
		{
			float mouseX = GetViewport().GetMousePosition().X;

			// Normalise mouse X to between 0 and 1
			float normalisedMouseX = mouseX / viewportSize.X; 

			// Apply screen edge padding
			float minX = _screenEdgePadding;
			float maxX = 1f - _screenEdgePadding;

			// Clamp and remap from [minX..maxX] -> [0..1]
			float t = Mathf.InverseLerp(minX, maxX, Mathf.Clamp(normalisedMouseX, minX, maxX));

			if (_invertMouseX)
			{
				t = 1f - t;
			}

			// Convert [0..1] to [-1..1]
			float signed = (t * 2f) - 1f;

			// Asymmetric world-space range:
			// left side uses _maxLateralOffsetLeft
			// right side uses _maxLateralOffsetRight
			if (signed < 0f)
			{
				_targetLateralOffset = signed * _maxLateralOffsetLeft;   // signed is negative
			}
			else
			{
				_targetLateralOffset = signed * _maxLateralOffsetRight;
			}
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

		// Safety clamp to asymmetric limits
		float clampedOffset = _currentLateralOffset;
		clampedOffset = Mathf.Clamp(clampedOffset, -_maxLateralOffsetLeft, _maxLateralOffsetRight);

		// Apply independent lateral movement
		basePos += right * clampedOffset;

		return basePos;
	}

	// Passes in targetPos by reference
	private void ApplyVerticalAvoidance(ref Vector3 targetPos, float dt) 
	{
		float baseY = _playerGround.AirAnchorPosition.Y + _baseOffsetYFromAnchor;

		if (_wallRays.Count == 0) 
		{
			// If no raycasts detected, use OG height
			targetPos.Y = baseY;
		}
		else 
		{
			bool anyBlockedThisFrame = CheckAnyRaysBlocked();

			if (anyBlockedThisFrame)
			{
				_hangTimer = _hangTime;
			}
			else 
			{
				_hangTimer = Mathf.Max(0f, _hangTimer - dt);
			}

			bool blockedWithHang = anyBlockedThisFrame || _hangTimer > 0f;

			if (blockedWithHang)
			{
				// Climb towards max
				_currentExtraHeight = Mathf.MoveToward(_currentExtraHeight, _maxExtraHeight, _riseSpeed * dt);
			}
			else 
			{
				// Descend towards base
				_currentExtraHeight = Mathf.MoveToward(_currentExtraHeight, 0f, _fallSpeed * dt);
			}

			targetPos.Y = baseY + _currentExtraHeight;

		}
	}

	private bool CheckAnyRaysBlocked()
	{
		bool result = false;

		for (int i = 0; i < _wallRays.Count; i++)
		{
			RayCast3D ray = _wallRays[i];
			if (ray.IsColliding())
			{
				result = true;
			}
		}

		return result;
	}

	private void AutoShoot(float dt)
	{
		// Decrease timer towards 0
		_fireTimer -= dt;

		// If timer still has time left, ignore rest of function
		if (_fireTimer > 0f) 
		{
			return;
		}

		// Reset timer if ROF
		_fireTimer = _rateOfFire;
		SpawnBullet();
	}

	private void SpawnBullet()
	{
        if (_bulletScene == null)
        {
            GD.PrintErr("Bullet scene not assigned to player!");
            return;
        }

		// Find Player's forward vector to be direction for bullets
		Vector3 shootDirection = -_muzzleMarker.GlobalTransform.Basis.Z;
		shootDirection = shootDirection.Normalized();

		PlayerAirBullet bulletNode = _bulletScene.Instantiate<PlayerAirBullet>();
		
		// Add bullet to scene (use current scene root)
    	GetTree().CurrentScene.AddChild(bulletNode);

		bulletNode.GlobalPosition = _muzzleMarker.GlobalPosition;
		bulletNode.Direction = shootDirection;
		// Make the bullet visually face the direction it is moving.
		bulletNode.LookAt(bulletNode.GlobalPosition + shootDirection, Vector3.Up);
	}
}
