using Godot;
using System;

public partial class PercyDrone : Area3D
{
	private enum PercyState 
	{
		Inactive,
		Entering,
		Hunting,
		Exiting
	}

	[ExportGroup("Behaviour")]
	[Export] private float _targetRadius = 35f;
	[Export] private float _speed = 24f;
	[Export] private float _turnSpeed = 9f;
	[Export] private float _retargetInterval = 0.15f;
	[Export] private float _hitDistance = 1.2f;

	[ExportGroup("Movement")]
	[Export] private float _wobbleStrength = 4f;
	[Export] private float _wobbleSpeed = 12f;
	[Export] private float _entryHeight = 2.5f;

	private PercyState _state = PercyState.Inactive;

	private Node3D _owner;
	private Vector3 _spawnPosition;
	private Vector3 _despawnPosition;

	private Node3D _currentTarget;
	private Vector3 _velocity;
	private float _activeTimer;
	private float _retargetTimer;
	private float _wobbleSeed;
	[ExportGroup("Audio")]

	[Export] private AudioStream _startSfx;

	[Export] private AudioStream _loopSfx;

	[Export(PropertyHint.Range, "-40.0,6.0,0.5")]
	private float _startSfxVolumeDb = -4f;

	[Export(PropertyHint.Range, "-40.0,6.0,0.5")]
	private float _loopSfxVolumeDb = -7f;

	[Export(PropertyHint.Range, "0.0,1.0,0.05")]
	private float _audioCrossfadeTime = 0.2f;

	private AudioStreamPlayer _startAudio;
	private AudioStreamPlayer _loopAudio;
	private Tween _audioTween;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;

		Visible = true;
		Monitoring = true;

		_wobbleSeed = (float)GD.RandRange(0.0, 1000.0);

		InitialiseAudio();
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;

		if (_state == PercyState.Inactive)
		{
			return;
		}

		if (_owner == null || !IsInstanceValid(_owner))
		{
			StartExit();
			return;
		}

		_activeTimer -= dt;

		if (_activeTimer <= 0f && _state != PercyState.Exiting)
		{
			StartExit();
		}

		switch(_state)
		{
			case PercyState.Entering:
				UpdateEntering(dt);
				break;
			case PercyState.Hunting:
				UpdateHunting(dt);
				break;
			case PercyState.Exiting:
				UpdateExiting(dt);
				break;
		}
	}

	public void Activate(Node3D owner, Vector3 spawnPosition, Vector3 despawnPosition, float duration)
	{
		_owner = owner;
		_spawnPosition = spawnPosition;
		_despawnPosition = despawnPosition;

		GlobalPosition = _spawnPosition;

		Visible = true;
		Monitoring = true;

		_activeTimer = duration;
		_retargetTimer = 0f;
		_currentTarget = null;
		_velocity = Vector3.Zero;

		_state = PercyState.Entering;

		StartDroneAudio();
	}

	private void UpdateEntering(float dt)
	{
		Vector3 entryTarget = _owner.GlobalPosition + Vector3.Up * _entryHeight;

		FlyTowards(entryTarget, dt);

		if (GlobalPosition.DistanceTo(entryTarget) < 2f)
		{
			_state = PercyState.Hunting;
		}
	}

	private void Deactivate()
	{
		_state = PercyState.Inactive;
		_currentTarget = null;
		Visible = false;
		Monitoring = false;
		QueueFree();
	}

	// Every physics frame, the drone checks whether it has a valid enemy target.
	// If it does not, it looks for one near the player.
	// If it finds one, it flies toward that enemy.
	// When it gets close enough, it tells the enemy “you were hit by Percy”.
	// The enemy handles its own death.
	// Then Percy forgets that target and immediately looks for another one.
	// If no enemies are available, Percy flies back near the player and waits.
	private void UpdateHunting(float dt)
	{
		_retargetTimer -= dt;

		if (_currentTarget == null || !IsInstanceValid(_currentTarget) || _retargetTimer <= 0f)
		{
			_currentTarget = FindBestTarget();
			_retargetTimer = _retargetInterval;
		}

		if (_currentTarget == null)
		{
			Vector3 idlePoint = _despawnPosition + Vector3.Up * 0.8f;
			FlyTowards(idlePoint, dt);
			return;
		}

		Vector3 targetPosition = _currentTarget.GlobalPosition + Vector3.Up * 0.8f;
		FlyTowards(targetPosition, dt);

		if (GlobalPosition.DistanceTo(targetPosition) <= _hitDistance)
		{
			HitTarget(_currentTarget);
			_currentTarget = null;
			_retargetTimer = 0f;
		}
	}

	private void UpdateExiting(float dt)
	{
		FlyTowards(_despawnPosition, dt);

		if (GlobalPosition.DistanceTo(_despawnPosition) < 1.5f) 
		{
			Deactivate();
		}
	}

	private void StartExit()
	{
		if (_state == PercyState.Exiting)
		{
			return;
		}

		_currentTarget = null;
		_state = PercyState.Exiting;

		FadeOutDroneAudio();
	}

	// Work out the direction from the drone to the target point.
	// Convert that direction into a movement velocity.
	// Smoothly steer the drone’s current velocity toward that desired velocity.
	// Move the drone.
	// Rotate the drone to face the movement direction.
	// TARGET in this case is just a position near the player.
	private void FlyTowards(Vector3 targetPosition, float dt)
	{
		Vector3 toTarget = targetPosition - GlobalPosition;

		if (toTarget.LengthSquared() < 0.001f)
		{
			return;
		}

		Vector3 desiredDirection = toTarget.Normalized();
		Vector3 desiredVelocity = desiredDirection * _speed;

		desiredVelocity += GetWobbleOffset();
		_velocity = _velocity.Lerp(desiredVelocity, Mathf.Clamp(_turnSpeed * dt, 0f, 1f));

		GlobalPosition += _velocity * dt;

		if (_velocity.LengthSquared() > 0.01f)
		{
			LookAt(GlobalPosition + _velocity.Normalized(), Vector3.Up);
		}
	}

	private Node3D FindBestTarget()
	{
		Node3D bestTarget = null;
		float bestDistance = float.MaxValue;

		foreach (Node node in GetTree().GetNodesInGroup("Enemies"))
		{
			if (node is not Node3D enemyNode)
			{
				GD.Print("Node is not an enemy node");
				continue;
			}

			if (node is not IPercyDroneTarget droneTarget)
			{
				GD.Print("Not a valid drone target");
				continue;			
			}

			if (!droneTarget.IsValidPercyDroneTarget())
			{
				GD.Print("Node is not an IPercyDroneTarget");
				continue;			
			}

			float distanceToOwner = enemyNode.GlobalPosition.DistanceTo(_owner.GlobalPosition);

			if (distanceToOwner > _targetRadius)
			{
				continue;
			}

			float distanceToDrone = enemyNode.GlobalPosition.DistanceTo(GlobalPosition);

			if (distanceToDrone < bestDistance)
			{
				bestDistance = distanceToDrone;
				bestTarget = enemyNode;
			}
		}
		return bestTarget;
	}

	private void HitTarget(Node3D target)
	{
		if (target is IPercyDroneTarget droneTarget)
		{
			droneTarget.PercyDroneHit();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (_state == PercyState.Hunting)
		{
			if (body is IPercyDroneTarget target)
			{
				target.PercyDroneHit();
				_currentTarget = null;
				_retargetTimer = 0f;
			}
		}
	}

	private Vector3 GetWobbleOffset()
	{
		float t = Time.GetTicksMsec() / 1000f;
		float x = Mathf.Sin((t + _wobbleSeed) * _wobbleSpeed);
		float y = Mathf.Cos((t + _wobbleSeed) * _wobbleSpeed * 1.37f);
		float z = Mathf.Sin((t + _wobbleSeed) * _wobbleSpeed * 0.71f);

		return new Vector3(x, y, z) * _wobbleStrength;
	}

	private void InitialiseAudio()
	{
		_startAudio = new AudioStreamPlayer
		{
			Name = "PercyStartAudio",
			Stream = _startSfx,
			VolumeDb = _startSfxVolumeDb,
			Bus = "Sfx"
		};

		AddChild(_startAudio);

		_loopAudio = new AudioStreamPlayer
		{
			Name = "PercyLoopAudio",
			Stream = _loopSfx,
			VolumeDb = -40f,
			Bus = "Sfx"
		};

		if (_loopAudio.Stream is AudioStreamMP3 mp3)
		{
			mp3.Loop = true;
		}

		AddChild(_loopAudio);

		if (_startAudio != null)
		{
			_startAudio.Finished += BeginLoopAudio;
		}
	}

	private void StartDroneAudio()
	{
		if (_audioTween != null && _audioTween.IsValid())
		{
			_audioTween.Kill();
		}

		if (GodotObject.IsInstanceValid(_loopAudio))
		{
			_loopAudio.Stop();
			_loopAudio.VolumeDb = -40f;
		}

		if (GodotObject.IsInstanceValid(_startAudio) &&
			_startAudio.Stream != null)
		{
			_startAudio.VolumeDb = _startSfxVolumeDb;
			_startAudio.Play();
		}
		else
		{
			// No start clip assigned, so just start looping immediately.
			BeginLoopAudio();
		}
	}

	private void BeginLoopAudio()
	{
		if (!GodotObject.IsInstanceValid(_loopAudio) ||
			_loopAudio.Stream == null)
		{
			return;
		}

		_loopAudio.VolumeDb = -40f;
		_loopAudio.Play();
		_audioTween = CreateTween();
		_audioTween.TweenProperty(_loopAudio, "volume_db", _loopSfxVolumeDb, _audioCrossfadeTime);
	}

	private void FadeOutDroneAudio()
	{
		if (_audioTween != null && _audioTween.IsValid())
		{
			_audioTween.Kill();
		}

		if (!GodotObject.IsInstanceValid(_loopAudio) || !_loopAudio.Playing)
		{
			return;
		}

		_audioTween = CreateTween();
		_audioTween.TweenProperty(_loopAudio, "volume_db", -40f, 0.35f);
		_audioTween.TweenCallback(
			Callable.From(() =>
			{
				if (GodotObject.IsInstanceValid(_loopAudio))
				{
					_loopAudio.Stop();
				}
			})
		);
	}
}