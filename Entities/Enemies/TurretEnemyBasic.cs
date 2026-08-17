using Godot;
using System;
using System.Collections.Generic;

public partial class TurretEnemyBasic : CharacterBody3D, IPercyDroneTarget
{
    [ExportGroup("Node Paths")]
    [Export] private NodePath _yawPivotPath = "YawPivot";
    [Export] private NodePath _pitchPivotPath = "YawPivot/PitchPivot";
    [Export] private NodePath _muzzleMarkerPath = "YawPivot/PitchPivot/MuzzleMarker";
    [Export] private NodePath _aggroRangePath = "AggroRange";
    [Export] private NodePath _sideDetectionPath = "SideDetection";
    [Export] private NodePath _topDetectionPath = "TopDetection";

    [ExportGroup("Targeting")]
    // Total horizontal viewing angle
    [Export(PropertyHint.Range, "1.0, 180.0, 1.0")]
    private float _horizontalFieldOfViewDegrees  = 110.0f;
    [Export(PropertyHint.Range, "0.0, 5.0, 0.05")]
    private float _targetHeightOffset  = 1.0f;
    [Export(PropertyHint.Range, "0.1, 30.0, 0.1")]
    private float _turnSpeedYaw = 6.0f;
    [Export(PropertyHint.Range, "0.1, 30.0, 0.1")]
    private float _turnSpeedPitch = 6.0f;
    [Export] private bool _usePitch = true;
    [Export(PropertyHint.Range, "-89.0, 30.0, 1.0")]
    private float _minPitchDeg = -40f;
    [Export(PropertyHint.Range, "0.0, 89.0, 0.1")]
    private float _maxPitchDeg = 40f;
    // Only enable if vertical aiming is in the wrong direction
    [Export] private bool _invertPitch = false;

    [ExportGroup("Firing")]
    [Export] public PackedScene _plasmaBallScene;
    [Export(PropertyHint.Range, "0.05, 10.0, 0.05")]
    private float _rateOfFire = 1.0f;
    [Export(PropertyHint.Range, "1.0, 350.0, 1.0")]
    private float _maxFireDistance = 90f;
    [Export(PropertyHint.Range, "0.1, 45.0, 0.1")]
    private float _fireAimToleranceDegrees = 5.0f;
    // Include both player collision and world collision layers
    [Export(PropertyHint.Layers3DPhysics)]
    private uint _lineOfSightMask = uint.MaxValue;

	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;
    private bool _isDead = false;

    [ExportGroup("Properties")]
    [Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 2;
	private int _currentHealth = 6;

    private Node3D _yawPivot;
    private Node3D _pitchPivot;
    private Node3D _muzzleMarker;
    private Area3D _aggroRange;
    private Area3D _sideDetection;
    private Area3D _topDetection;
    private Node3D _target;
    private float _fireTimer;
    [ExportGroup("Audio")]
	[Export] private AudioStream _deathSfx;
	[Export(PropertyHint.Range, "-40.0, 12.0, 0.5")]
	private float _deathSfxVolumeDb = 12.0f;

    public override void _Ready()
    {
        AddToGroup("Enemies");

        _yawPivot = GetNodeOrNull<Node3D>(_yawPivotPath);
        _pitchPivot = GetNodeOrNull<Node3D>(_pitchPivotPath);
        _muzzleMarker = GetNodeOrNull<Node3D>(_muzzleMarkerPath);
        _aggroRange = GetNodeOrNull<Area3D>(_aggroRangePath);
        _sideDetection = GetNodeOrNull<Area3D>(_sideDetectionPath);
        _topDetection = GetNodeOrNull<Area3D>(_topDetectionPath);

        bool setupValid = true;

        if (_yawPivot == null)
        {
            GD.PushError("[TurretEnemyBasic] YawPivot was not found.");
            setupValid = false;
        }

        if (_pitchPivot == null)
        {
            GD.PushError("[TurretEnemyBasic] PitchPivot was not found.");
            setupValid = false;
        }

        if (_muzzleMarker == null)
        {
            GD.PushError("[TurretEnemyBasic] MuzzleMarker was not found.");
            setupValid = false;
        }

        if (_aggroRange == null)
        {

            setupValid = false;
        }
        else
        {
            _aggroRange.BodyEntered += OnAggroBodyEntered;
            _aggroRange.BodyExited += OnAggroBodyExited;
        }

        if (_sideDetection == null)
        {
            GD.PushError("[TurretEnemyBasic] SideDetection was not found.");
            setupValid = false;
        }
        else
        {
            _sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
        }

        if (_topDetection == null)
        {
            GD.PushError("[TurretEnemyBasic] TopDetection was not found.");
            setupValid = false;
        }
        else
        {
            _topDetection.BodyEntered += OnTopDetectionPlayerEntered;
        }

        if (!setupValid)
        {
            SetPhysicsProcess(false);
            return;
        }

        // Allows the turret to fire immediately once all targeting conditions are satisfied.
        _fireTimer = 0.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDead)
        {
            return;
        }

        float dt = (float)delta;

        _fireTimer = Mathf.Max(_fireTimer - dt, 0.0f);

        if (!IsTargetValid())
        {
            _target = null;
            return;
        }

        Vector3 targetPoint = GetTargetPoint(_target);

        // Target can still be inside aggro range while still being behind or too far around side of target
        if (!IsInsideForwardCone(targetPoint))
        {
            return;
        }

        AimAtPoint(targetPoint, dt);

        float maxDistance = Mathf.Max(_maxFireDistance, 0.0f);

        float distanceSquared = _muzzleMarker.GlobalPosition.DistanceSquaredTo(targetPoint);

        if (distanceSquared > maxDistance * maxDistance)
        {
            return;
        }

        // Do not fire while barrel is still rotating
        if (!IsAimAligned(targetPoint))
        {
            return;
        }

        // Do not fire through walls and shit
        if(!HasClearLineOfSight(targetPoint))
        {
            return;
        }

        TryFire();
    }

    private bool IsTargetValid()
    {
        return _target != null && GodotObject.IsInstanceValid(_target) && _target.IsInsideTree();
    }

    private Vector3 GetTargetPoint(Node3D target)
    {
        return target.GlobalPosition + Vector3.Up * _targetHeightOffset;
    }

    private void AimAtPoint(Vector3 targetPoint, float delta)
    {
        if (_yawPivot == null || _pitchPivot == null)
        {
            return;
        }

        UpdateYaw(targetPoint, delta);

        if (_usePitch)
        {
            UpdatePitch(targetPoint, delta);
        }
    }

    private void UpdateYaw(Vector3 targetPoint, float delta)
    {
        Node3D yawParent = _yawPivot.GetParentNode3D();

        if (yawParent == null)
        {
            return;
        }

        Vector3 worldDirection = targetPoint - _yawPivot.GlobalPosition;
        Vector3 localDirection = yawParent.GlobalTransform.Basis.Inverse() * worldDirection;
        localDirection.Y = 0.0f;

        if (localDirection.LengthSquared() < 0.0001f)
        {
            return;
        }

        localDirection = localDirection.Normalized();

        // Negative Z is treated as the pivot's neutral forward
        float desiredYaw = Mathf.Atan2(localDirection.X, localDirection.Z);
        float smoothing = 1.0f - Mathf.Exp(-_turnSpeedYaw * delta);
        Vector3 rotation = _yawPivot.Rotation;
        rotation.Y = Mathf.LerpAngle(rotation.Y, desiredYaw, smoothing);
        _yawPivot.Rotation = rotation;
    }

    private void UpdatePitch(Vector3 targetPoint, float delta)
    {
        Node3D pitchParent = _pitchPivot.GetParentNode3D();

        if (pitchParent == null)
        {
            return;
        }

        Vector3 targetInParent = pitchParent.ToLocal(targetPoint);
        Vector3 localDirection = targetInParent - _pitchPivot.Position;

        if (localDirection.LengthSquared() < 0.0001f)
        {
            return;
        }

        float desiredPitch = Mathf.Atan2(localDirection.Y, localDirection.Z);

        if (_invertPitch)
        {
            desiredPitch = -desiredPitch;
        }

        float minimumPitch = Mathf.DegToRad(_minPitchDeg);
        float maximumPitch = Mathf.DegToRad(_maxPitchDeg);
        desiredPitch = Mathf.Clamp(desiredPitch, minimumPitch, maximumPitch);
        float smoothing = 1.0f - Mathf.Exp(-_turnSpeedPitch * delta);
        Vector3 rotation = _pitchPivot.Rotation;
        rotation.X = Mathf.LerpAngle(rotation.X, desiredPitch, smoothing);
        _pitchPivot.Rotation = rotation;
    }

private bool IsInsideForwardCone(Vector3 targetPoint)
{
    if (_yawPivot == null)
    {
        return false;
    }

    // The TurretEnemyBasic root defines the turret's fixed
    // neutral forward direction.
    Vector3 fixedForward = GlobalTransform.Basis.Z;

    Vector3 toTarget = targetPoint - _yawPivot.GlobalPosition;

    // Horizontal cone only.
    fixedForward.Y = 0.0f;
    toTarget.Y = 0.0f;

    if (fixedForward.LengthSquared() < 0.0001f || toTarget.LengthSquared() < 0.0001f)
    {
        return false;
    }

    fixedForward = fixedForward.Normalized();
    toTarget = toTarget.Normalized();

    float halfAngleRadians = Mathf.DegToRad(_horizontalFieldOfViewDegrees * 0.5f);

    float minimumDot = Mathf.Cos(halfAngleRadians);

    return fixedForward.Dot(toTarget) >= minimumDot;
}

    private bool IsAimAligned(Vector3 targetPoint)
    {
        if (_muzzleMarker == null)
        {
            return false;
        }

        Vector3 toTarget = targetPoint - _muzzleMarker.GlobalPosition;

        if (toTarget.LengthSquared() < 0.0001f)
        {
            return false;
        }

        toTarget = toTarget.Normalized();
        Vector3 muzzleForward = _muzzleMarker.GlobalTransform.Basis.Z;
        muzzleForward = muzzleForward.Normalized();
        float minimumDot = Mathf.Cos(Mathf.DegToRad(_fireAimToleranceDegrees));

        return muzzleForward.Dot(toTarget) >= minimumDot;
    }

    private bool HasClearLineOfSight(Vector3 targetPoint)
    {
        if (_muzzleMarker == null || !IsTargetValid())
        {
            return false;
        }

        Vector3 rayStart = _muzzleMarker.GlobalPosition;
        var query = PhysicsRayQueryParameters3D.Create(rayStart, targetPoint);
        query.CollisionMask = _lineOfSightMask;
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;

        // Prevents the turret's own CharacterBody3D from blocking its ray
        query.Exclude = new Godot.Collections.Array<Rid>
        {
            GetRid()
        };


        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);

        // Nothing blocked the ray before its endpoint
        if (result.Count == 0)
        {
            return true;
        }

        GodotObject collider = result["collider"].AsGodotObject();

        return ColliderBelongsToTarget(collider, _target);
    }

    private bool ColliderBelongsToTarget(GodotObject collider, Node3D target)
    {
        if (collider == target)
        {
            return true;
        }

        if (collider is not Node colliderNode)
        {
            return false;
        }

        return target.IsAncestorOf(colliderNode) || colliderNode.IsAncestorOf(target);
    }

    private void TryFire()
    {
        if (_fireTimer > 0.0f)
        {
            return;
        }

        if (_plasmaBallScene == null)
        {
            GD.PushWarning("[TurretEnemyBasic] Plasma ball scene is missing");

            return;
        }

        _fireTimer = Mathf.Max(_rateOfFire, 0.01f);

        SpawnPlasmaBall();
    }

    private void SpawnPlasmaBall() 
    {
        if (_plasmaBallScene == null)
        {
            GD.PrintErr("Bullet scene not assigned to player!");
            return;
        }

		// Find Turret's forward vector to be direction for bullets
		Vector3 shootDirection = _muzzleMarker.GlobalTransform.Basis.Z;
		shootDirection = shootDirection.Normalized();

		TurretEnemyPlasmaBall plasmaBall = _plasmaBallScene.Instantiate<TurretEnemyPlasmaBall>();
		
		// Add plasma ball to scene (use current scene root)
    	GetTree().CurrentScene.AddChild(plasmaBall);

		plasmaBall.GlobalPosition = _muzzleMarker.GlobalPosition;
		plasmaBall.GlobalRotation = _muzzleMarker.GlobalRotation;
		plasmaBall.Direction = shootDirection;

        SfxManager.Instance?.PlayEnemyFire(_muzzleMarker.GlobalPosition);
    }

    private void OnAggroBodyEntered(Node3D body)
    {
        if (body == null) return;

        if (body is PlayerGround player)
        {
            GD.Print("[TurretEnemyBasic] Player added to possible target list");
            _target = player;
        }
    }

    private void OnAggroBodyExited(Node3D body)
    {
        if (body == null) 
        {
            return;
        }

        if (_target == body) 
        {
            _target = null;
        }
    }

	private void OnSideDetectionPlayerEntered(Node3D body) 
	{
		if (body != null)
		{
			// Disable top collision when side ccollision entered to prevent double collision
			_topDetection.SetCollisionMaskValue(1, false);

			if (body is PlayerGround player)
			{
				player.TakeDamage(_damageAmount);
				player.BounceBackFromPosition(GlobalPosition, 4f);
                ExplodeSelf();
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[TurretEnemyBasic] player body is null.");
		}
	}

	private void OnTopDetectionPlayerEntered(Node3D body) 
	{
		if (body != null)
		{
			// Disable side collision when top ccollision entered to prevent double collision
			_sideDetection.SetCollisionMaskValue(1, false);

			if (body is PlayerGround player)
			{
				player.BounceUp();
				Global.Instance.AddCurrency(_currencyAmount);
                ExplodeSelf();
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[TurretEnemyBasic] player body is null.");
		}
	}

	public void Hurt(int bulletDamage)
	{
		_currentHealth = Math.Max(_currentHealth - bulletDamage, 0);

		if (_currentHealth <= 0) {
			_isDead = true;
			GD.Print("Killed enemy!");
			Global.Instance.AddCurrency(_currencyAmount);
            ExplodeSelf();
			QueueFree();
		}
	}
    private void ExplodeSelf()
    {
        if (_explosionScene == null)
        {
            GD.PrintErr("Explosion scene not assigned to enemy!");
            return;
        }

		PlayDetachedSfx(_deathSfx, _deathSfxVolumeDb);

        ExplosionEffect explosionNode = _explosionScene.Instantiate<ExplosionEffect>();
        GetTree().CurrentScene.AddChild(explosionNode);
        explosionNode.GlobalPosition = GlobalPosition;
        _ = explosionNode.Explode();
    }

	public void PercyDroneHit()
	{
	    Hurt(999);
	}

	public bool IsValidPercyDroneTarget()
	{
		return _isDead == false && IsInsideTree() == true;
	}

	private void PlayDetachedSfx(AudioStream stream, float volumeDb)
	{
		if (stream == null)
		{
			return;
		}

    	// Capture death position before the enemy gets freed.
		Vector3 soundPosition = GlobalPosition;

		AudioStreamPlayer3D player = new AudioStreamPlayer3D();

		player.Stream = stream;
		player.VolumeDb = volumeDb;
		player.Bus = "Sfx";

		Node parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(player);
		player.GlobalPosition = soundPosition;
		player.Finished += player.QueueFree;
		player.Play();
	}
}