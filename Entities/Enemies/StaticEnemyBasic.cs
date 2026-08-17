using Godot;
using System;

public partial class StaticEnemyBasic : CharacterBody3D, IPercyDroneTarget
{
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	
	[Export] public NodePath _topDetectionPath = "TopDetection";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 1;
	private Area3D _sideDetection;
	private Area3D _topDetection;
	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;
	private bool _isDead = false;
	[Export] private AnimationPlayer _animationPlayer;
	private static readonly StringName IdleAnimationName = new StringName("idle-static-bug");
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
		InitialiseAnimation();
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
				player.BounceBackFromPosition(GlobalPosition, 3f);
				ExplodeSelf();
				QueueFree();
			}
		}
		else 
		{
			GD.PushError("[StaticEnemyBasic] player body is null.");
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
			GD.PushError("[StaticEnemyBasic] player body is null.");
		}
	}

	public void Die()
	{
		if (_isDead)
		{
			return;
		}

		_isDead = true;

		GD.Print("Killed enemy!");
		Global.Instance.AddCurrency(_currencyAmount);
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
        GetTree().CurrentScene.AddChild(explosionNode);
        explosionNode.GlobalPosition = GlobalPosition;
        _ = explosionNode.Explode();
    }

	public void PercyDroneHit()
	{
		Die();
	}

	public bool IsValidPercyDroneTarget()
	{
		return _isDead == false && IsInsideTree() == true;
	}

	private void InitialiseAnimation()
	{
		if (!GodotObject.IsInstanceValid(_animationPlayer))
		{
			GD.PushWarning("[StaticEnemyBasic] AnimationPlayer has not been assigned.");
			return;
		}

		if (!_animationPlayer.HasAnimation(IdleAnimationName))
		{
			GD.PushWarning($"[StaticEnemyBasic] Animation '{IdleAnimationName}' was not found.");
			return;
		}

		Animation animation = _animationPlayer.GetAnimation(IdleAnimationName);
		animation.LoopMode = Animation.LoopModeEnum.Linear;
		_animationPlayer.Play(IdleAnimationName);
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
