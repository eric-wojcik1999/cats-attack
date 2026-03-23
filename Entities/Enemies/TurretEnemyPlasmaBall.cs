using Godot;
using System;

public partial class TurretEnemyPlasmaBall : Area3D
{
	[Export] private float _speed = 40f;
	[Export] private float _lifeTime = 3.0f;
	[Export] private int _turretDamage = 1;
	[Export] private float _growTime = 0.12f;
	public Vector3 Direction { get; set; } = Vector3.Forward;

	private float _lifeTimer;
	private float _growTimer;
	private float _test = 0.1f;

	public override void _Ready()
	{
		_lifeTimer = _lifeTime;
		_growTimer = 0f;
		Scale = Vector3.One * 0.1f;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

        if (_growTimer < _growTime)
        {
            _growTimer += dt;
            float t = Mathf.Clamp(_growTimer / _growTime, 0.0f, 0.65f);
            Scale = Vector3.One * Mathf.Max(t, 0.001f);
        }


		GlobalPosition += Direction * _speed * dt;

		_lifeTimer -= dt;

		if (_lifeTimer < 0.0f) QueueFree();
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is PlayerGround player)
		{
			player.TakeDamage(_turretDamage);
		}

		QueueFree();
	}
}
