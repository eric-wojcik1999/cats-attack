using Godot;
using System;

public partial class LaserBeam : Area3D
{
	[Export] private int _damage = 1;

	[Export(PropertyHint.Range, "0.0,5.0,0.05")]
	private float _damageCooldown = 0.75f;

	private bool _canDamage = true;


	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is not PlayerGround player)
		{
			return;
		}

		TryDamagePlayer(player);
	}


	private void TryDamagePlayer(PlayerGround player)
	{
		if (!_canDamage)
		{
			return;
		}

		_canDamage = false;
		player.TakeDamage(_damage);
		SfxManager.Instance?.PlayZap(player.GlobalPosition);
		ResetDamageCooldown();
	}


	private async void ResetDamageCooldown()
	{
		await ToSignal(GetTree().CreateTimer(_damageCooldown), SceneTreeTimer.SignalName.Timeout);
		_canDamage = true;
	}
}
