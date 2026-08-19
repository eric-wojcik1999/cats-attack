using Godot;
using System;
using System.Threading.Tasks;

public partial class MusicManager : Node
{
	public static MusicManager Instance { get; private set; }
	private AudioStreamPlayer _player;
	private AudioStream _menuMusic;
	private AudioStream _levelMusic;
	private const float NormalVolumeDb = -10.0f;
	private const float SilentVolumeDb = -40.0f;
	private Tween _fadeTween;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Instance = this;

		_player = new AudioStreamPlayer
		{
			Name = "MusicPlayer",
			Bus = "Music"
		};

		AddChild(_player);

		_menuMusic = GD.Load<AudioStream>("res://Audio/Music/menu-theme.mp3");
		_levelMusic = GD.Load<AudioStream>("res://Audio/Music/level-theme.mp3");
	}

	// =========================================================
	// MENU MUSIC
	// =========================================================

	public async Task EnsureMenuMusic()
	{
		// Already playing menu music.
		if (_player.Playing && _player.Stream == _menuMusic)
		{
			return;
		}

		_player.Stream = _menuMusic;
		_player.VolumeDb = SilentVolumeDb;
		_player.Play();

		Tween tween = CreateTween();
		tween.TweenProperty(_player, "volume_db", NormalVolumeDb, 0.75f);

		await ToSignal(tween, Tween.SignalName.Finished);
	}


	public async Task FadeOutMenuMusic()
	{
		if (!_player.Playing)
		{
			return;
		}

		Tween tween = CreateTween();
		tween.TweenProperty(_player, "volume_db", SilentVolumeDb, 0.6f);

		await ToSignal(tween, Tween.SignalName.Finished);
		_player.Stop();
	}

	// =========================================================
	// LEVEL MUSIC - FOR LATER
	// =========================================================

	// public async Task PlayLevelMusic()
	// {
	// 	_musicPlayer.Stream = _levelMusic;
	// 	_musicPlayer.VolumeDb = SilentVolumeDb;
	// 	_musicPlayer.Play();

	// 	Tween tween = CreateTween();

	// 	tween.TweenProperty(_musicPlayer, "volume_db", NormalVolumeDb, 0.75f);


	// 	await ToSignal(tween, Tween.SignalName.Finished);
	// }

	public void PlayMusic(AudioStream stream, float volumeDb = -8f, float fadeTime = 1.0f)
	{
		if (stream == null)
		{
			 return;
		}

		// Don't restart same song if already playing
		if (_player.Stream == stream && _player.Playing)
		{
			return;
		}

		if (_fadeTween != null && _fadeTween.IsValid())
		{
			_fadeTween.Kill();
		}

		_player.Stop();
		_player.Stream = stream;
		_player.VolumeDb = -40f;

		if (_player.Stream is AudioStreamMP3 mp3)
		{
			mp3.Loop = true;
		}

		_player.Play();

		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_player, "volume_db", volumeDb, fadeTime);
	}

    public void StopMusic(float fadeTime = 1.0f)
	{
		if (_player.Playing)
		{
			return;
		}

		if (_fadeTween != null && _fadeTween.IsValid())
		{
			_fadeTween.Kill();
		}

        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(_player, "volume_db", -40f, fadeTime);
        _fadeTween.TweenCallback(
            Callable.From(() =>
            {
                _player.Stop();
                _player.Stream = null;
            })
        );
	}

	public async Task FadeOutMusic(float fadeTime = 0.6f)
	{
		if (_player == null || !_player.Playing)
		{
			return;
		}

		if (_fadeTween != null && _fadeTween.IsValid())
		{
			_fadeTween.Kill();
		}

		_fadeTween = CreateTween();
		_fadeTween.TweenProperty(_player, "volume_db", -40.0f, fadeTime);

		await ToSignal(_fadeTween, Tween.SignalName.Finished);

		_player.Stop();
	}
}
