using Godot;
using System;
using System.Threading.Tasks;

public partial class MovingEnemyBasic : CharacterBody3D, IPercyDroneTarget
{
	[Export] private float _baseMovementSpeed = 5.0f;
	// (1, 0, 0) <- the positive 1 is 'left' on the x-axis
	[Export(PropertyHint.Range, "0.05,2.0,0.05")]
	private float _turnDuration = 0.3f;

	[Export(PropertyHint.Range, "0.0,2.0,0.05")]
	private float _turnCooldownDuration = 0.25f;
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	[Export] public NodePath _topDetectionPath = "TopDetection";
	[Export] private NodePath _detectFloorLeftPath = "DetectFloorLeft";
	[Export] private NodePath _detectFloorRightPath = "DetectFloorRight";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 1;
	private bool _isTurning = false;
	private Area3D _sideDetection;
	private Area3D _topDetection;
	private RayCast3D _detectFloorLeft;
	private RayCast3D _detectFloorRight;

	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;
	private bool _isDead = false;
	[ExportGroup("Animation")]
	[Export] private AnimationPlayer _animationPlayer;
	[Export(PropertyHint.Range, "0.1,5.0,0.1")]
	private float _animationSpeed = 1.5f;
	private static readonly StringName MovingAnimationName = new StringName("Bug-Moving");
	private double _turnCooldownTimer;
	private Tween _turnTween;

	[ExportGroup("Distance Activation")]
	[Export(PropertyHint.Range, "5.0,200.0,1.0")]
	private float _activationDistance = 45.0f;
	[Export(PropertyHint.Range, "5.0,250.0,1.0")]
	private float _deactivationDistance = 55.0f;
	[Export(PropertyHint.Range, "0.05,2.0,0.05")]
	private double _distanceCheckInterval = 0.25;
	private PlayerGround _player;
	private double _distanceCheckTimer;
	private bool _simulationActive = true;
	[ExportGroup("Audio")]
	[Export] private AudioStream _deathSfx;
	[Export(PropertyHint.Range, "-40.0, 12.0, 0.5")]
	private float _deathSfxVolumeDb = 12.0f;

	public override void _Ready()
	{
		AddToGroup("Enemies");
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
		_topDetection = GetNode<Area3D>(_topDetectionPath);
		_topDetection.BodyEntered += OnTopDetectionPlayerEntered;
		_detectFloorLeft = GetNode<RayCast3D>(_detectFloorLeftPath);
		_detectFloorRight = GetNode<RayCast3D>(_detectFloorRightPath);

		// Make sure there is a gap between activation and deactivation.
		_deactivationDistance = Mathf.Max(_deactivationDistance, _activationDistance + 1.0f);

		InitialiseAnimation();

		FindPlayer();
		UpdateDistanceActivation(forceUpdate: true);
	}

	public override void _Process(double delta)
	{
		if (_isDead || IsQueuedForDeletion())
		{
			return;
		}

		_distanceCheckTimer -= delta;

		if (_distanceCheckTimer > 0.0)
		{
			return;
		}

		_distanceCheckTimer = _distanceCheckInterval;

		if (_player == null || !GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree())
		{
			FindPlayer();

			if (_player == null)
			{
				return;
			}
		}

		UpdateDistanceActivation();
	}

	public override void _PhysicsProcess(double delta)
	{	

		if (_isDead || !_simulationActive || IsQueuedForDeletion())
		{
			return;
		}

		_turnCooldownTimer = Math.Max(0.0, _turnCooldownTimer - delta);

		if (!AreFloorRaysValid())
		{
			GD.PushError("[MovingEnemyBasic] One or both floor detection rays are invalid. Enemy physics has been disabled."
			);

			SetPhysicsProcess(false);
			return;
		}

		//  Stop horizontal movement while the enemy rotates. Gravity is still applied so the enemy remains grounded naturally,
		//  but it cannot continue walking over the edge during the tween.
		if (_isTurning)
		{
			Vector3 turningVelocity = Velocity;

			turningVelocity.X = 0.0f;
			turningVelocity.Z = 0.0f;

			if (!IsOnFloor())
			{
				turningVelocity += GetGravity() * (float)delta;
			}

			Velocity = turningVelocity;
			MoveAndSlide();
			return;
		}

		//  Update both rays before moving.
		//  This prevents the enemy from moving another full physics frame beyond the edge before deciding to turn.
		bool leftHasFloor = _detectFloorLeft.IsColliding();
		bool rightHasFloor = _detectFloorRight.IsColliding();

		// Turn only when BOTH rays find no floor.
		// If either ray still detects the track, the enemy continues moving.
		bool noFloorAhead = !leftHasFloor && !rightHasFloor;

		if (IsOnFloor() && noFloorAhead && _turnCooldownTimer <= 0.0)
		{
			_ = TurnAroundAsync();
			return;
		}

		Vector3 newVelocity = Velocity;

		// In Godot, local -Z is forward.
		Vector3 forward = -GlobalTransform.Basis.Z;
		forward.Y = 0.0f;

		if (!forward.IsZeroApprox())
		{
			forward = forward.Normalized();
		}

		newVelocity.X = forward.X * _baseMovementSpeed;
		newVelocity.Z = forward.Z * _baseMovementSpeed;

		if (!IsOnFloor())
		{
			newVelocity += GetGravity() * (float)delta;
		}

		Velocity = newVelocity;
		MoveAndSlide();
	}


	private bool AreFloorRaysValid()
	{
		return GodotObject.IsInstanceValid(_detectFloorLeft) && GodotObject.IsInstanceValid(_detectFloorRight) && _detectFloorLeft.IsInsideTree() && _detectFloorRight.IsInsideTree();
	}

    private async Task TurnAroundAsync()
    {

		if (_isTurning || _isDead || IsQueuedForDeletion() || !IsInsideTree())
		{
			return;
		}

        _isTurning = true;

		// Stop immediately so momentum cannot carry the enemy over the edge.
		Vector3 stoppedVelocity = Velocity;
		stoppedVelocity.X = 0.0f;
		stoppedVelocity.Z = 0.0f;
		Velocity = stoppedVelocity;

		_turnTween?.Kill();
		_turnTween = CreateTween();
		_turnTween.TweenProperty(this, "rotation_degrees", new Vector3(0.0f, 180.0f, 0.0f), _turnDuration).AsRelative();
		await ToSignal(_turnTween, Tween.SignalName.Finished);

		if (!GodotObject.IsInstanceValid(this) || _isDead || IsQueuedForDeletion() || !IsInsideTree())
		{
			return;
		}


		// Wait for a physics frame so the rays update using the enemy's
		// new orientation rather than their previous direction.

		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		if (!GodotObject.IsInstanceValid(this) || _isDead || IsQueuedForDeletion() || !IsInsideTree())
		{
			return;
		}

		if (AreFloorRaysValid())
		{
			_detectFloorLeft.ForceRaycastUpdate();
			_detectFloorRight.ForceRaycastUpdate();
		}

		_turnCooldownTimer = _turnCooldownDuration;
		_isTurning = false;
    }

	private void OnSideDetectionPlayerEntered(Node3D body)
	{
		if (_isDead)
		{
			return;
		}

		if (body is not PlayerGround player)
		{
			return;
		}

		player.TakeDamage(_damageAmount);
		player.BounceBackFromPosition(GlobalPosition, 4f);

		BeginDeath(awardCurrency: false);
	}

	private void OnTopDetectionPlayerEntered(Node3D body)
	{
		if (_isDead)
		{
			return;
		}

		if (body is not PlayerGround player)
		{
			return;
		}

		player.BounceUp();

		BeginDeath(awardCurrency: true);
	}

	public void Die()
	{
		BeginDeath(awardCurrency: true);
	}

	private void BeginDeath(bool awardCurrency)
	{
		if (_isDead || IsQueuedForDeletion())
		{
			return;
		}

		_isDead = true;
		_isTurning = false;
		_simulationActive = false;

		SetProcess(false);
		SetPhysicsProcess(false);

		_turnTween?.Kill();
		_turnTween = null;

		// Defer physics-object changes when death may begin in a collision callback.
		if (GodotObject.IsInstanceValid(_sideDetection))
		{
			_sideDetection.SetDeferred(
				Area3D.PropertyName.Monitoring,
				false
			);
		}

		if (GodotObject.IsInstanceValid(_topDetection))
		{
			_topDetection.SetDeferred(
				Area3D.PropertyName.Monitoring,
				false
			);
		}

		if (awardCurrency && Global.Instance != null)
		{
			Global.Instance.AddCurrency(_currencyAmount);
		}

		ExplodeSelf();
		QueueFree();
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
		Node parent = GetTree().CurrentScene ?? GetTree().Root;
		parent.AddChild(explosionNode);
        explosionNode.GlobalPosition = GlobalPosition;
        _ = explosionNode.Explode();
    }

	public void PercyDroneHit()
	{
		BeginDeath(awardCurrency: true);
	}

	public bool IsValidPercyDroneTarget()
	{
		return !_isDead && _simulationActive && !IsQueuedForDeletion() && IsInsideTree();
	}

	private void InitialiseAnimation()
	{
		if (!GodotObject.IsInstanceValid(_animationPlayer))
		{
			GD.PushWarning("[MovingEnemyBasic] AnimationPlayer has not been assigned.");
			return;
		}

		if (!_animationPlayer.HasAnimation(MovingAnimationName))
		{
			GD.PushWarning($"[MovingEnemyBasic] Animation '{MovingAnimationName}' was not found.");
			return;
		}

		Animation animation = _animationPlayer.GetAnimation(MovingAnimationName);
		animation.LoopMode = Animation.LoopModeEnum.Linear;
		_animationPlayer.SpeedScale = _animationSpeed;
		_animationPlayer.Play(MovingAnimationName);
	}

	public override void _ExitTree()
	{
		_isDead = true;
		_isTurning = false;

		_turnTween?.Kill();
		_turnTween = null;
	}

	private void UpdateDistanceActivation(bool forceUpdate = false)
	{
		if (_player == null || !GodotObject.IsInstanceValid(_player) || !_player.IsInsideTree())
		{
			return;
		}

		float distanceSquared = GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
		float relativeDistance = _simulationActive ? _deactivationDistance : _activationDistance;
		bool shouldBeActive = distanceSquared <= relativeDistance * relativeDistance;

		if (forceUpdate || shouldBeActive != _simulationActive)
		{
			SetSimulationActive(shouldBeActive, forceUpdate);
		}
	}

	private void SetSimulationActive(bool active, bool forceUpdate = false)
	{
		if (!forceUpdate && active == _simulationActive)
		{
			return;
		}

		// Avoids deactivating mid turn
		if (!active && _isTurning)
		{
			return;
		}

		_simulationActive = active;

		if (!active)
		{
			Velocity = Vector3.Zero;
		}

		// Enables/disables MovingEnemyBasic._PhysicsProcess().
		SetPhysicsProcess(active);

		// Prevent two rays from performing physics queries while distant
		if (GodotObject.IsInstanceValid(_detectFloorLeft))
		{
			_detectFloorLeft.Enabled = active;
		}

		if (GodotObject.IsInstanceValid(_detectFloorRight))
		{
			_detectFloorRight.Enabled = active;
		}

		// Disable player interaction while enemy inactive
		if (GodotObject.IsInstanceValid(_sideDetection))
		{
			_sideDetection.SetDeferred(Area3D.PropertyName.Monitoring, active);
		}

		if (GodotObject.IsInstanceValid(_topDetection))
		{
			_topDetection.SetDeferred(Area3D.PropertyName.Monitoring, active);
		}

		// Stop skeletal animation processing while distant.
		if (GodotObject.IsInstanceValid(_animationPlayer))
		{
			_animationPlayer.Active = active;

			if (active && !_animationPlayer.IsPlaying() && _animationPlayer.HasAnimation(MovingAnimationName))
			{
				_animationPlayer.Play(MovingAnimationName);
			}
		}
	}

	private void FindPlayer()
	{
		Node playerNode = GetTree().GetFirstNodeInGroup("Player");

		if (playerNode is PlayerGround player)
		{
			_player = player;
			return;
		}

		_player = null;
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
