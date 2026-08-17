using Godot;
using System;

public partial class TotemEnemyBasic : CharacterBody3D, IPercyDroneTarget
{
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 4;
	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;
	private bool _isDead = false;
	private Area3D _sideDetection;
	private int _currentHealth = 12;
	[ExportGroup("Rotation")]
	[Export] private NodePath _armsPivotPath = "ArmsPivot";
	[Export(PropertyHint.Range, "-1080.0,1080.0,1.0")]
	private float _degreesPerSecond = 180.0f;
	private Node3D _armsPivot;
	[ExportGroup("Audio")]
	[Export] private AudioStream _deathSfx;
	[Export(PropertyHint.Range, "-40.0, 12.0, 0.5")]
	private float _deathSfxVolumeDb = 12.0f;

	public override void _Ready()
	{
		AddToGroup("Enemies");
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_armsPivot = GetNode<Node3D>(_armsPivotPath);

		if (_sideDetection == null)
		{
			GD.PushError("[TotemEnemyBasic] SideDetection was not found.");
		}
		else
		{
			_sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
		}

		if (_armsPivot == null)
		{
			GD.PushError("[TotemEnemyBasic] ArmsPivot was not found.");
		}

	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead || !GodotObject.IsInstanceValid(_armsPivot))
		{
			GD.PushError("[TotemEnemyBasic] Something wrong.");
			return;
		}

		float dt = (float)delta;
		float radians = Mathf.DegToRad(_degreesPerSecond * dt);

		_armsPivot.RotateObjectLocal(Vector3.Up, radians);
	}

	private void OnSideDetectionPlayerEntered(Node3D body) 
	{
		if (body != null)
		{
			if (body is PlayerGround player)
			{
				player.TakeDamage(_damageAmount);
				player.BounceBackFromPosition(GlobalPosition, 8f);
			}
		}
		else 
		{
			GD.PushError("[MovingEnemyBasic] player body is null.");
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
		ExplosionEffect explosionNode2 = _explosionScene.Instantiate<ExplosionEffect>();
        GetTree().CurrentScene.AddChild(explosionNode);
		GetTree().CurrentScene.AddChild(explosionNode2);
        explosionNode.GlobalPosition = GlobalPosition;
		explosionNode2.GlobalPosition = explosionNode2.GlobalPosition = GlobalPosition + new Vector3(0f, 2f, 0f);
        _ = explosionNode.Explode();
		_ = explosionNode2.Explode();
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
