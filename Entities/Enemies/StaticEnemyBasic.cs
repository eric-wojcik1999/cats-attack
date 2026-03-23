using Godot;
using System;

public partial class StaticEnemyBasic : CharacterBody3D
{
	[Export] public NodePath _sideDetectionPath = "SideDetection";
	
	[Export] public NodePath _topDetectionPath = "TopDetection";
	[Export] public int _damageAmount = 1;
	[Export] public int _currencyAmount = 1;
	private Area3D _sideDetection;
	private Area3D _topDetection;
	[ExportGroup("Death")]
	[Export] public PackedScene _explosionScene;


	public override void _Ready()
	{
		_sideDetection = GetNode<Area3D>(_sideDetectionPath);
		_sideDetection.BodyEntered += OnSideDetectionPlayerEntered;
		_topDetection = GetNode<Area3D>(_topDetectionPath);
		_topDetection.BodyEntered += OnTopDetectionPlayerEntered;
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

        ExplosionEffect explosionNode = _explosionScene.Instantiate<ExplosionEffect>();
        GetTree().CurrentScene.AddChild(explosionNode);
        explosionNode.GlobalPosition = GlobalPosition;
        _ = explosionNode.Explode();
    }
}
