using Godot;
using System;
using System.Collections.Generic;

public partial class TurretEnemyBasic : CharacterBody3D
{
	[ExportGroup("Node Paths")]
    [Export] private NodePath _yawPivotPath = "YawPivot"; // Yaw rotates here
    [Export] private NodePath _pivotPitchPath = "YawPivot/PitchPivot"; // Pitch rotates here
    [Export] private NodePath _muzzlePath = "yawPivot/PitchPivot/MuzzleMarker";
    [Export] private NodePath _losPath = "YawPivot/PitchPivot/MuzzleMarker/LineOfSightRayCast";
    [Export] private NodePath _aggroRangePath = "AggroRange";

    [ExportGroup("Targeting")]
    [Export] private float _turnSpeedYaw = 6.0f;
    [Export] private float _turnSpeedPitch = 6.0f;
    [Export] private bool _usePitch = true;
    [Export] private float _minPitchDeg = -20f;
    [Export] private float _maxPitchDeg = 20f;

    [ExportGroup("Firing")]
    [Export] private PackedScene _plasmaBallScene;
    [Export] private float _fireInterval = 1.0f;
    [Export] private float _muzzleForwardSpawnOffset = 0.2f; // Probably not needed
    [Export] private float _maxFireDistance = 999f;

    // Don't neeeed to type as MeshInstance3D here (even though they are nearly all MeshInstance3D nodes)
    // because Node3D can be better in this case as it has everything we need for what we want to accomplish
    // If need to change mesh specific things like mesh colour, then yes type it as a MeshInstance3D
    private Node3D _yawPivot;
    private Node3D _pitchPivot;
    private Node3D _muzzle;
    private RayCast3D _losRay;
    private Area3D _aggroRange;

    // This is actually overkill since there is only ever one target, but keeping anyway
    private readonly HashSet<Node3D> _possibleTargets = new HashSet<Node3D>(); 
    private Node3D _target;
    private float _fireTimer;

    public override void _Ready()
    {
        _yawPivot = GetNodeOrNull<Node3D>(_yawPivotPath);
        _pitchPivot = GetNodeOrNull<Node3D>(_pivotPitchPath);
        _muzzle = GetNodeOrNull<Node3D>(_muzzlePath);
        _losRay = GetNodeOrNull<RayCast3D>(_losPath);
        _aggroRange = GetNodeOrNull<Area3D>(_aggroRangePath);

        if (_yawPivot == null) GD.PushError("[TurretEnemyBasic] Core path is invalid");
        if (_pitchPivot == null) GD.PushError("[TurretEnemyBasic] Barrel path is invalid");
        if (_muzzle == null) GD.PushError("[TurretEnemyBasic] Muzzle path is invalid");
        if (_losRay == null) GD.PushError("[TurretEnemyBasic] LineOfSight path is invalid");
        if (_aggroRange == null) GD.PushError("[TurretEnemyBasic] Core path is invalid");

        if (_aggroRange != null)
        {
            _aggroRange.BodyEntered += OnAggroBodyEntered;
            _aggroRange.BodyExited += OnAggroBodyExited;
        }

        _fireTimer = _fireInterval;
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
    }

    private void AimAtTarget(Node3D target, float dt)
    {
        if (_yawPivot == null || _pitchPivot == null || _muzzle == null) return;

        Vector3 muzzlePos = _muzzle.GlobalPosition;
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
        Vector3 localTarget = _pitchPivot.ToLocal(target.GlobalPosition);

        // float desiredPitch = Mathf.Atan2(localTarget.Y, -localTarget.Z);
        float desiredPitch = Mathf.Atan2(localTarget.Y, localTarget.Z);
        float desiredPitchDeg = Mathf.Clamp(Mathf.RadToDeg(desiredPitch), _minPitchDeg, _maxPitchDeg);
        desiredPitch = Mathf.DegToRad(desiredPitchDeg);

        Vector3 pitchRot = _pitchPivot.Rotation; // Local rotation is fine for pitch
        float pitchDiff = Mathf.Wrap(desiredPitch - pitchRot.X, -Mathf.Pi, Mathf.Pi);
        float pitchStep = Mathf.Clamp(_turnSpeedPitch * dt, 0.0f, 1.0f);
        pitchRot.X += pitchDiff * pitchStep;
        _pitchPivot.Rotation = pitchRot;
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

        _possibleTargets.Remove(body);

        if (_target == body) 
        {
            _target = null;
        }
    }
}
