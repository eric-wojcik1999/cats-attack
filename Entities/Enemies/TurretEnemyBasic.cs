using Godot;
using System;
using System.Collections.Generic;

public partial class TurretEnemyBasic : CharacterBody3D
{
	[ExportGroup("NodePaths")]
    [Export] private NodePath _corePath = "TurretBaseMesh/TurrentCoreMesh"; // Yaw rotates here
    [Export] private NodePath _barrelPath = "TurretBaseMesh/TurrentCoreMesh/TurretBarrelMesh"; // Pitch rotates here
    [Export] private NodePath _muzzlePath = "TurretBaseMesh/TurrentCoreMesh/TurretBarrelMesh/MuzzleMarker";
    [Export] private NodePath _losPath = "TurretBaseMesh/TurrentCoreMesh/TurretBarrelMesh/MuzzleMarker/LineOfSightRayCast";
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
    private Node3D _core;
    private Node3D _barrel;
    private Node3D _muzzle;
    private RayCast3D _losRay;
    private Area3D _aggroRange;

    // This is actually overkill since there is only ever one target, but keeping anyway
    private readonly HashSet<Node3D> _possibleTargets = new HashSet<Node3D>(); 
    private Node3D _target;
    private float _fireTimer;

    public override void _Ready()
    {
        _core = GetNodeOrNull<Node3D>(_corePath);
        _barrel = GetNodeOrNull<Node3D>(_barrelPath);
        _muzzle = GetNodeOrNull<Node3D>(_muzzlePath);
        _losRay = GetNodeOrNull<RayCast3D>(_losPath);
        _aggroRange = GetNodeOrNull<Area3D>(_aggroRangePath);

        if (_core == null) GD.PushError("[TurretEnemyBasic] Core path is invalid");
        if (_barrel == null) GD.PushError("[TurretEnemyBasic] Barrel path is invalid");
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
        if (_core == null || _muzzle == null) return;

        Vector3 muzzlePos = _muzzle.GlobalPosition;
        Vector3 toTarget = (target.GlobalPosition - muzzlePos);

        // Is there enough horizontal distance between the turrent and the target to compute yaw
        if (toTarget.LengthSquared() < 0.00001f) return;

        // YAW ROTATION: Rotate core around Y only
        Vector3 flat = toTarget;
        flat.Y = 0f;

        // Is there enough horizontal distance between the turrent and the target to compute yaw
        if (flat.LengthSquared() > 0.00001f)
        {
            float desiredYaw = Mathf.Atan2(-flat.X, -flat.Z);
            // // Convert yaw that makes the -Z point to a target
            // desiredYaw += Mathf.Pi;

            Vector3 coreRot = _core.GlobalRotation;
            float diff = Mathf.Wrap(desiredYaw - coreRot.Y, -Mathf.Pi, Mathf.Pi);
            float step = Mathf.Clamp(_turnSpeedYaw * dt, 0f, 1f);
            coreRot.Y += diff * step;
            _core.GlobalRotation = coreRot;
        }

        // ai wants use pitch to be checked if false here but always true lol
        if (_barrel == null) return;

        // PITCH: Rotate barrel around X only (for aiming up and down)
        Vector3 localTarget = _barrel.ToLocal(target.GlobalPosition);

        float desiredPitch = Mathf.Atan2(localTarget.Y, -localTarget.Z);
        float desiredPitchDeg = Mathf.RadToDeg(desiredPitch);
        desiredPitchDeg = Mathf.Clamp(desiredPitchDeg, _minPitchDeg, _maxPitchDeg);
        desiredPitch = Mathf.DegToRad(desiredPitchDeg);

        Vector3 barrelRot = _barrel.Rotation; // Local rotation is fine for pitch
        float pitchDiff = Mathf.Wrap(desiredPitch - barrelRot.X, -Mathf.Pi, Mathf.Pi);
        float pitchStep = Mathf.Clamp(_turnSpeedPitch * dt, 0f, 1f);
        barrelRot.X += pitchDiff * pitchStep;
        _barrel.Rotation = barrelRot;
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
