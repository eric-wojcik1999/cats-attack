using Godot;
using System;
using System.Collections.Generic;

public partial class TurretEnemyBasic : CharacterBody3D
{
	[ExportGroup("Node Paths")]
    [Export] private NodePath _yawPivotPath = "YawPivot"; // Yaw rotates here
    [Export] private NodePath _pivotPitchPath = "YawPivot/PitchPivot"; // Pitch rotates here
    [Export] private NodePath _muzzleMarkerPath = "YawPivot/PitchPivot/MuzzleMarker";
    [Export] private NodePath _losPath = "YawPivot/PitchPivot/MuzzleMarker/LineOfSightRayCast";
    [Export] private NodePath _aggroRangePath = "AggroRange";
    [Export] private NodePath _sideDetectionPath = "SideDetection";
    
    [Export] private NodePath _topDetectionPath = "TopDetection";

    [ExportGroup("Targeting")]
    [Export] private float _turnSpeedYaw = 6.0f;
    [Export] private float _turnSpeedPitch = 6.0f;
    [Export] private bool _usePitch = true;
    [Export] private float _minPitchDeg = -40f;
    [Export] private float _maxPitchDeg = 40f;

    [ExportGroup("Firing")]
    [Export] public PackedScene _plasmaBallScene;
    [Export] private float _rateOfFire = 1.0f;
    [Export] private float _maxFireDistance = 999f;

    [ExportGroup("Properties")]
    [Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 1;

    // Don't neeeed to type as MeshInstance3D here (even though they are nearly all MeshInstance3D nodes)
    // because Node3D can be better in this case as it has everything we need for what we want to accomplish
    // If need to change mesh specific things like mesh colour, then yes type it as a MeshInstance3D
    private Node3D _yawPivot;
    private Node3D _pitchPivot;
    private Node3D _muzzleMarker;
    private RayCast3D _losRay;
    private Area3D _aggroRange;
    private Area3D _sideDetection;
    
    private Area3D _topDetection;

    // // This is actually overkill since there is only ever one target, but keeping anyway
    // private readonly HashSet<Node3D> _possibleTargets = new HashSet<Node3D>(); 
    private Node3D _target;
    private float _fireTimer;

    public override void _Ready()
    {
        _yawPivot = GetNodeOrNull<Node3D>(_yawPivotPath);
        _pitchPivot = GetNodeOrNull<Node3D>(_pivotPitchPath);
        _muzzleMarker = GetNodeOrNull<Node3D>(_muzzleMarkerPath);
        _losRay = GetNodeOrNull<RayCast3D>(_losPath);
        _aggroRange = GetNodeOrNull<Area3D>(_aggroRangePath);
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
		_topDetection = GetNode<Area3D>(_topDetectionPath);
		_topDetection.BodyEntered += OnTopDetectionPlayerEntered;

        if (_yawPivot == null) GD.PushError("[TurretEnemyBasic] Core path is invalid");
        if (_pitchPivot == null) GD.PushError("[TurretEnemyBasic] Barrel path is invalid");
        if (_muzzleMarker == null) GD.PushError("[TurretEnemyBasic] Muzzle path is invalid");
        if (_losRay == null) GD.PushError("[TurretEnemyBasic] LineOfSight path is invalid");
        if (_aggroRange == null) GD.PushError("[TurretEnemyBasic] Core path is invalid");

        if (_aggroRange != null)
        {
            _aggroRange.BodyEntered += OnAggroBodyEntered;
            _aggroRange.BodyExited += OnAggroBodyExited;
        }

        _fireTimer = _rateOfFire;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        // Validation to ensure target isn't already freed before use
        if (_target != null && !GodotObject.IsInstanceValid(_target))
        {
            _target = null;
        }

        if (_target == null) return;

        // AIM
        AimAtTarget(_target, dt);

        // CHECK LINE OF SIGHT 

        // FIRE IF IN LOS
        HandleAutoFire(dt);
    }

    private void AimAtTarget(Node3D target, float dt)
    {
        if (_yawPivot == null || _pitchPivot == null || _muzzleMarker == null) return;

        Vector3 muzzlePos = _muzzleMarker.GlobalPosition;
        Vector3 toTarget = (target.GlobalPosition - muzzlePos);

        // Is there enough horizontal distance between the turrent and the target to compute yaw
        if (toTarget.LengthSquared() < 0.0001f) return;

        // YAW ROTATION: Rotate core around Y only
        Vector3 flat = toTarget;
        flat.Y = 0.0f;

        // Is there enough horizontal distance between the turrent and the target to compute yaw
        if (flat.LengthSquared() > 0.0001f)
        {
            float desiredYaw = Mathf.Atan2(-flat.X, -flat.Z);

            Vector3 yawRot = _yawPivot.GlobalRotation;
            float yawDiff = Mathf.Wrap(desiredYaw - yawRot.Y, -Mathf.Pi, Mathf.Pi);
            float yawStep = Mathf.Clamp(_turnSpeedYaw * dt, 0.0f, 1.0f);
            yawRot.Y += yawDiff * yawStep;
            _yawPivot.GlobalRotation = yawRot;
        }


        if (!_usePitch) return;

        // PITCH: Rotate barrel around X only (for aiming up and down)
        // Something is slightly off with the pitch, keeps jittering back upwards. FIX LATER
        Vector3 localTarget = _pitchPivot.ToLocal(target.GlobalPosition);
        float desiredPitch = Mathf.Atan2(localTarget.Y, localTarget.Z);
        float desiredPitchDeg = Mathf.Clamp(Mathf.RadToDeg(desiredPitch), _minPitchDeg, _maxPitchDeg);
        desiredPitch = Mathf.DegToRad(desiredPitchDeg);

        Vector3 pitchRot = _pitchPivot.Rotation; // Local rotation is fine for pitch
        float pitchDiff = Mathf.Wrap(desiredPitch - pitchRot.X, -Mathf.Pi, Mathf.Pi);
        float pitchStep = Mathf.Clamp(_turnSpeedPitch * dt, 0.0f, 1.0f);
        pitchRot.X += pitchDiff * pitchStep;
        _pitchPivot.Rotation = pitchRot;
    }

    private void HandleAutoFire(float dt)
    {
        if (_plasmaBallScene == null || _muzzleMarker == null) return;

    	// Decrease timer towards 0
		_fireTimer -= dt;

		// If timer still has time left, ignore rest of function
		if (_fireTimer > 0f) return;

		// Reset timer if ROF
		_fireTimer = _rateOfFire;
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
    }

    private void OnAggroBodyEntered(Node3D body)
    {
        if (body == null) return;

        if (body is PlayerGround player)
        {
            GD.Print("[TurretEnemyBasic] Player added to possible target list");
            _target = player;
            // _possibleTargets.Add(player);
        }
    }

    private void OnAggroBodyExited(Node3D body)
    {
        if (body == null) return;

        // _possibleTargets.Remove(body);

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
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[TurretEnemyBasic] player body is null.");
		}
	}
}
