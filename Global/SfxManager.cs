using Godot;
using System;
using System.Collections.Generic;

public partial class SfxManager : Node
{

	public static SfxManager Instance { get; private set; }

	[ExportGroup("Projectile")]
	private AudioStream _projectileHitSound;
	private float _projectileHitVolumeDb = -3.5f;

    [ExportGroup("Enemy")]
	private AudioStream _enemyFireSound;
    private float _enemyFireVolumeDb = 6.0f;

	[ExportGroup("Boost Pads")]
	private AudioStream _jumpPadSound;
	private AudioStream _speedBoostSound;
	private float _jumpPadVolumeDb = -4.0f;
	private float _speedBoostVolumeDb = -4.0f;

	[ExportGroup("Menu")]
	private AudioStream _menuHoverSound;
	private AudioStream _menuSelectSound;
	private AudioStream _upgradePurchaseSound;

	private float _menuHoverVolumeDb = -2.0f;
	private float _menuSelectVolumeDb = -2.0f;
	private float _upgradePurchaseVolumeDb = -3.0f;

	[ExportGroup("Hazards")]
	private AudioStream _zapSound;
	private float _zapVolumeDb = -3.0f;

	// Volleyids are ULONG so they are long enough for two projectiles not to collide ids
	private readonly HashSet<ulong> _playedProjectileVolleysIds = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;

        _projectileHitSound = GD.Load<AudioStream>("res://Audio/Sfx/projectile-hit.mp3");

        if (_projectileHitSound == null)
        {
            GD.PushError("[SfxManager] Failed to load projectile-hit sound.");
        }

        _jumpPadSound = GD.Load<AudioStream>("res://Audio/Sfx/jump-pad.mp3");

        if (_jumpPadSound == null)
        {
            GD.PushError("[SfxManager] Failed to load jump-pad sound.");
        }

        _speedBoostSound = GD.Load<AudioStream>("res://Audio/Sfx/speed-boost.mp3");

        if (_speedBoostSound == null)
        {
            GD.PushError("[SfxManager] Failed to load speed-boost sound.");
        }
 
		_menuHoverSound = GD.Load<AudioStream>("res://Audio/Sfx/Menu/menu-hover.mp3");

		if (_menuHoverSound == null)
		{
			GD.PushError("[SfxManager] Failed to load menu-hover sound.");
		}


		_menuSelectSound = GD.Load<AudioStream>("res://Audio/Sfx/Menu/menu-select.mp3");

		if (_menuSelectSound == null)
		{
			GD.PushError("[SfxManager] Failed to load menu-select sound.");
		}


		_upgradePurchaseSound = GD.Load<AudioStream>("res://Audio/Sfx/Menu/upgrade-purchase.mp3");

		if (_upgradePurchaseSound == null)
		{
			GD.PushError("[SfxManager] Failed to load upgrade-purchase sound.");
		}

		_zapSound = GD.Load<AudioStream>("res://Audio/Sfx/zap.mp3");

		if (_zapSound == null)
		{
			GD.PushError("[SfxManager] Failed to load zap sound.");
		}
	}

	public void PlayProjectileImpactOnce(ulong volleyId, Vector3 position)
	{
		if (_projectileHitSound == null)
		{
			return;
		}

		if (_playedProjectileVolleysIds.Contains(volleyId))
		{
			return;
		}

		_playedProjectileVolleysIds.Add(volleyId);

		Play3DOneShot(_projectileHitSound, position, _projectileHitVolumeDb);

		RemoveVolleyIdLater(volleyId);
	}

	public void PlayProjectileImpact(Vector3 position)
	{
		if (_projectileHitSound == null)
		{
			return;
		}

		Play3DOneShot(_projectileHitSound, position, _projectileHitVolumeDb);
	}

    public void PlayEnemyFire(Vector3 position)
    {
        if (_enemyFireSound == null)
        {
            return;
        }

        Play3DOneShot(_enemyFireSound, position, _enemyFireVolumeDb);
    }

	private void Play3DOneShot(AudioStream stream, Vector3 position, float volumeDb)
	{
		AudioStreamPlayer3D player = new AudioStreamPlayer3D();
		player.Stream = stream;
		player.VolumeDb = volumeDb;
		GetTree().CurrentScene.AddChild(player);
		player.GlobalPosition = position;
		player.Finished += player.QueueFree;
		player.Play();
	}

	private async void RemoveVolleyIdLater(ulong volleyId)
	{
		await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);
		_playedProjectileVolleysIds.Remove(volleyId);
	}

	public void PlayJumpPad(Vector3 position)
	{
		if (_jumpPadSound == null)
		{
			return;
		}

		Play3DOneShot(_jumpPadSound, position, _jumpPadVolumeDb);
	}

	public void PlaySpeedBoost(Vector3 position)
	{
		if (_speedBoostSound == null)
		{
			return;
		}

		Play3DOneShot(_speedBoostSound, position, _speedBoostVolumeDb);
	}

	public void PlayMenuHover()
	{
		if (_menuHoverSound == null)
		{
			return;
		}

		PlayUiOneShot(_menuHoverSound, _menuHoverVolumeDb);
	}


	public void PlayMenuSelect()
	{
		if (_menuSelectSound == null)
		{
			return;
		}

		PlayUiOneShot(_menuSelectSound, _menuSelectVolumeDb);
	}


	public void PlayUpgradePurchase()
	{
		if (_upgradePurchaseSound == null)
		{
			return;
		}

		PlayUiOneShot(_upgradePurchaseSound, _upgradePurchaseVolumeDb);
	}

	private void PlayUiOneShot(AudioStream stream, float volumeDb)
	{
		AudioStreamPlayer player = new AudioStreamPlayer();
		player.Stream = stream;
		player.VolumeDb = volumeDb;
		player.Bus = "Sfx";

		// Add to SfxManager itself rather than CurrentScene.
		// This is useful because clicking a menu button may immediately change scene. 
		// Since SfxManager is an Autoload, the sound survives that transition.
		AddChild(player);
		player.Finished += player.QueueFree;
		player.Play();
	}

	public void PlayZap(Vector3 position)
	{
		if (_zapSound == null)
		{
			return;
		}

		Play3DOneShot(_zapSound, position, _zapVolumeDb);
	}
}
