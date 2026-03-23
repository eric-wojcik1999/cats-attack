using Godot;
using System;

public partial class PlayerAirBullet : Area3D
{
	[Export] private float _speed = 100f;
	[Export] private float _lifeTime = 1.5f;
	[Export] private int _bulletDamage = 1;
	public Vector3 Direction { get; set; } = Vector3.Forward;
	private float _lifeTimer;

	public override void _Ready()
	{
		_lifeTimer = _lifeTime;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		GlobalPosition += Direction * _speed * dt;

		_lifeTimer -= dt;

		if (_lifeTimer <= 0)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is MovingEnemyBasic enemy)
		{
			// Make it make the enemy die!!
			enemy.Die();
		}
		else if (body is TotemEnemyBasic totem)
		{
			// Make it make the enemy lose health!
			totem.Hurt(_bulletDamage);
		}
		else if (body is TurretEnemyBasic turret)
		{
			// Make it make the enemy lose health!
			turret.Hurt(_bulletDamage);
		}
		else if (body is StaticEnemyBasic staticEnemy)
		{
			// Make it make the enemy die!!
			staticEnemy.Die();
		}

		QueueFree();
	}
}
