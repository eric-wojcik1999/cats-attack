using Godot;
using System;

public partial class Hud : CanvasLayer
{
	// Can swap out what label path it refers to via inspector or keep default
	[Export] public NodePath _scoreValuePath = "ScorePanel/ScoreValue";
	[Export] public NodePath _healthValuePath = "HealthPanel/HealthValue";
	[Export] public NodePath _gameMessageContainerPath = "GameMessageContainer";
	[Export] public NodePath _gameMessagePath = "GameMessageContainer/GameMessage";
	[Export] public NodePath _percyProgressBarPath = "PercyProgressBar";
	[Export] public NodePath _percyPowerupIconPath = "PercyPowerupIcon";
	[Export] public NodePath _percyFlashPath = "PercyFlash";
	[Export] public PlayerGround player;

	private ColorRect _percyFlash;
	private Label _scoreValueLabel;
	private Label _healthValueLabel;
	private Label _gameMessageLabel;
	private Godot.Range _percyProgressBar;
	private PanelContainer _gameMessageContainer;
	private Control _percyPowerupIcon;
	private Tween _percyFlashTween;
	private bool _isPercyTimerActive = false;
	private float _percyDuration = 0f;
	private float _percyTimeLeft = 0f;
	private Tween _messageTween;

	public override void _Ready()
	{
		_scoreValueLabel = GetNode<Label>(_scoreValuePath);
		_scoreValueLabel.Text = "0";
		_healthValueLabel = GetNode<Label>(_healthValuePath);
		_healthValueLabel.Text = $"{player.Health}/{player.MaxHealth}";
		_gameMessageLabel = GetNode<Label>(_gameMessagePath);
		_percyProgressBar = GetNode<Godot.Range>(_percyProgressBarPath);
		_percyPowerupIcon = GetNode<Control>(_percyPowerupIconPath);
		_gameMessageContainer = GetNode<PanelContainer>(_gameMessageContainerPath);

		_gameMessageLabel.Text = "";
		_percyProgressBar.Visible = false;
		_percyPowerupIcon.Visible = false;
		_gameMessageContainer.Visible = false;

		player.HealthChanged += OnHealthChanged;
		player.Died += OnDied;

		_percyFlash = GetNode<ColorRect>(_percyFlashPath);
		_percyFlash.Color = new Color(0.55f, 0.0f, 1.0f, 0.0f);

		player.PercyPowerupCollected += OnPercyPowerupCollected;
		player.PercyPowerupActivated += OnPercyPowerupActivated;

		if (Global.Instance != null)
		{
			Global.Instance.CurrencyChanged += OnCurrencyChanged;
		}
		else 
		{
			GD.PushError("[Hud] Global.Instance is null. Is Global autoload configured?");
		}

	}

	public override void _Process(double delta)
	{
		if (!_isPercyTimerActive)
		{
			return;
		}

		float dt = (float)delta;

		_percyTimeLeft -= dt;
		_percyTimeLeft = Mathf.Max(_percyTimeLeft, 0f);

		_percyProgressBar.Value = _percyTimeLeft;

		if (_percyTimeLeft <= 0f)
		{
			_isPercyTimerActive = false;
			_percyProgressBar.Visible = false;
			_percyPowerupIcon.Visible = false;
			_gameMessageContainer.Visible = false;
		}
	}

	private void OnDied()
	{
		GD.Print("[Hud] Player has died.");
		// Show a UI that says you have died
	}

	private void OnHealthChanged(int newValue)
	{
		_healthValueLabel.Text = $"{newValue}/{player.MaxHealth}";
	}

    private void OnCurrencyChanged(int newValue)
    {
        _scoreValueLabel.Text = newValue.ToString();
    }

    public override void _ExitTree()
    {
        // Unsubscribe to avoid stale handlers
        if (Global.Instance != null) 
		{
            Global.Instance.CurrencyChanged -= OnCurrencyChanged;
		}
		player.HealthChanged -= OnHealthChanged;
		player.Died -= OnDied;
		player.PercyPowerupCollected -= OnPercyPowerupCollected;
    }

	private void OnPercyPowerupCollected()
	{
		ShowGameMessage("Percy Power has been picked up!");
		FlashPercyPickup();
	}

	private void OnPercyPowerupActivated(float duration)
	{
		StartPercyProgressBar(duration);
	}

	private void FlashPercyPickup() 
	{
		if (_percyFlash == null)
		{
			return;
		}

		if (_percyFlashTween != null && _percyFlashTween.IsValid())
		{
			_percyFlashTween.Kill();
		}

		_percyFlash.Color = new Color(0.55f, 0.0f, 1.0f, 0.0f);
		_percyFlashTween = CreateTween();
		_percyFlashTween.TweenProperty(_percyFlash, "color", new Color(0.55f, 0.0f, 1.0f, 0.45f), 0.08f);
		_percyFlashTween.TweenProperty(_percyFlash, "color", new Color(0.55f, 0.0f, 1.0f, 0.0f), 0.25f);
	}

	private void ShowGameMessage(string message)
	{
		if (_gameMessageLabel == null)
		{
			return;
		}

		if (_messageTween != null && _messageTween.IsValid())
		{
			_messageTween.Kill();
		}

		_gameMessageLabel.Text = message;
		_gameMessageLabel.Modulate = new Color(1f, 1f, 1f, 1f);
		_messageTween = CreateTween();
		_messageTween.TweenInterval(2.0f);
		_messageTween.TweenProperty(_gameMessageLabel, "modulate:a", 0.0f, 0.5f);
		_messageTween.TweenCallback(Callable.From(() =>
		{
			_gameMessageLabel.Text = "";
			_gameMessageLabel.Modulate = new Color(1f, 1f, 1f, 1f);
			_gameMessageContainer.Visible = false;
		}));
	}

	private void StartPercyProgressBar(float duration)
	{
		_percyDuration = duration;
		_percyTimeLeft = duration;
		_isPercyTimerActive = true;
		_percyProgressBar.MinValue = 0;
		_percyProgressBar.MaxValue = duration;
		_percyProgressBar.Value = duration;

		_percyProgressBar.Visible = true;
		_percyPowerupIcon.Visible = true;
		_gameMessageContainer.Visible = true;

		GD.Print($"[HUD] Percy bar started. duration={duration}, min={_percyProgressBar.MinValue}, max={_percyProgressBar.MaxValue}, value={_percyProgressBar.Value}");
	}
}
