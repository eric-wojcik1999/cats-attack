using Godot;
using System;

public partial class MusicManager : Node
{

	public static MusicManager Instance { get; private set; }
	private AudioStreamPlayer _player;
	private Tween _fadeTween;

	public override void _Ready()
	{
		Instance = this;

		_player = new AudioStreamPlayer
		{
			Name = "MusicPlayer",
			Bus = "Music"
		};

		AddChild(_player);
	}

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
}
