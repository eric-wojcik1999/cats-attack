using Godot;
using System;

public partial class TotemEnemyBasic : CharacterBody3D, IPercyDroneTarget
{
	[Export] public float _degreesPerSecond = 180f;
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 4;
	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;
	private bool _isDead = false;
	private Area3D _sideDetection;
	private int _currentHealth = 12;

	public override void _Ready()
	{
		AddToGroup("Enemies");
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
	}


	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		float radPerSec = Mathf.DegToRad(_degreesPerSecond);

		Vector3 rotation = GlobalRotation;
		rotation.Y += radPerSec * dt;
		GlobalRotation = rotation;
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
}
