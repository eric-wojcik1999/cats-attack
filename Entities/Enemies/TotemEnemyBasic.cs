using Godot;
using System;

public partial class TotemEnemyBasic : CharacterBody3D
{
	[Export] public float _degreesPerSecond = 180f;
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 4;
	private Area3D _sideDetection;
	private int _currentHealth = 12;

	public override void _Ready()
	{
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
			GD.Print("Killed enemy!");
			Global.Instance.AddCurrency(_currencyAmount);
			QueueFree();
		}
	}
}
