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
	private CenterContainer _deathMessageContainer;
	private Label _deathMessageLabel;
	private Control _livesContainer;
	private Label _livesValueLabel;
	private int _messageVersion = 0;

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
		// Use unique name paths for elements of UI you know will be...well..unique
		_livesContainer = GetNode<Control>("%LivesContainer");
		_livesValueLabel = GetNode<Label>("%LivesValue");

		HideGameMessage();
		_percyProgressBar.Visible = false;
		_percyPowerupIcon.Visible = false;


		player.HealthChanged += OnHealthChanged;
		player.Died += OnDied;
		player.DeathResolved += OnDeathResolved;

		_percyFlash = GetNode<ColorRect>(_percyFlashPath);
		_percyFlash.Color = new Color(0.55f, 0.0f, 1.0f, 0.0f);

		player.PercyPowerupCollected += OnPercyPowerupCollected;
		player.PercyPowerupActivated += OnPercyPowerupActivated;

		player.ExtraLivesChanged += OnExtraLivesChanged;
		player.Respawned += OnPlayerRespawned;

		if (Global.Instance != null)
		{
			Global.Instance.CurrencyChanged += OnCurrencyChanged;
			Global.Instance.GameMessageRequested += OnGameMessageRequested;
		}
		else 
		{
			GD.PushError("[Hud] Global.Instance is null. Is Global autoload configured?");
		}
		CreateDeathMessageUi();

		_livesContainer.Visible = true;
		UpdateLivesDisplay(player.ExtraLives);
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

	private void OnDeathResolved(bool hasExtraLife)
	{
		if (_deathMessageContainer == null || _deathMessageLabel == null)
		{
			return;
		}


		if (hasExtraLife)
		{
			_deathMessageLabel.Text = "YOU HAVE DIED...SIKE";
		}
		else
		{
			_deathMessageLabel.Text = "YOU HAVE DIED";
		}

		_deathMessageContainer.Visible = true;
	}

    public override void _ExitTree()
    {
        // Unsubscribe to avoid stale handlers
        if (Global.Instance != null) 
		{
            Global.Instance.CurrencyChanged -= OnCurrencyChanged;
			Global.Instance.GameMessageRequested -= OnGameMessageRequested;
		}
		player.HealthChanged -= OnHealthChanged;
		player.Died -= OnDied;
		player.PercyPowerupCollected -= OnPercyPowerupCollected;
		player.DeathResolved -= OnDeathResolved;
		player.ExtraLivesChanged -= OnExtraLivesChanged;
		player.Respawned -= OnPlayerRespawned;
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
		if (_gameMessageLabel == null || _gameMessageContainer == null)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(message))
		{
			HideGameMessage();
			return;
		}

		_messageVersion++;
		int thisMessageVersion = _messageVersion;

		if (_messageTween != null && _messageTween.IsValid())
		{
			_messageTween.Kill();
		}

		_gameMessageLabel.Text = message;
		_gameMessageLabel.Modulate = new Color(1f, 1f, 1f, 1f);
		_gameMessageContainer.Visible = true;

		_messageTween = CreateTween();
		_messageTween.TweenInterval(2.0f);
		_messageTween.TweenProperty(_gameMessageLabel, "modulate:a", 0.0f, 0.5f);
		_messageTween.TweenCallback(Callable.From(() =>
		{
			// Don't let an old message tween hide a newer message.
			if (thisMessageVersion != _messageVersion)
			{
				return;
			}

			HideGameMessage();
		}));
	}

	private void CreateDeathMessageUi()
	{
		_deathMessageContainer = new CenterContainer();
		_deathMessageContainer.Name = "DeathMessageContainer";
		AddChild(_deathMessageContainer);

		_deathMessageContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_deathMessageContainer.MouseFilter = Control.MouseFilterEnum.Ignore;

		PanelContainer deathPanel = new PanelContainer();
		deathPanel.Name = "DeathPanel";
		deathPanel.MouseFilter = Control.MouseFilterEnum.Ignore;

		StyleBoxFlat panelStyle = new StyleBoxFlat();
		panelStyle.BgColor = new Color(0.05f, 0.05f, 0.05f, 0.82f);
		panelStyle.CornerRadiusTopLeft = 18;
		panelStyle.CornerRadiusTopRight = 18;
		panelStyle.CornerRadiusBottomLeft = 18;
		panelStyle.CornerRadiusBottomRight = 18;
		panelStyle.BorderWidthLeft = 2;
		panelStyle.BorderWidthTop = 2;
		panelStyle.BorderWidthRight = 2;
		panelStyle.BorderWidthBottom = 2;
		panelStyle.BorderColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);

		deathPanel.AddThemeStyleboxOverride("panel", panelStyle);
		_deathMessageContainer.AddChild(deathPanel);

		MarginContainer margin = new MarginContainer();
		margin.Name = "DeathMargin";
		margin.AddThemeConstantOverride("margin_left", 40);
		margin.AddThemeConstantOverride("margin_right", 40);
		margin.AddThemeConstantOverride("margin_top", 22);
		margin.AddThemeConstantOverride("margin_bottom", 22);

		deathPanel.AddChild(margin);

		_deathMessageLabel = new Label();
		_deathMessageLabel.Name = "DeathMessage";
		_deathMessageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_deathMessageLabel.VerticalAlignment = VerticalAlignment.Center;
		_deathMessageLabel.AddThemeFontSizeOverride("font_size", 56);
		_deathMessageLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.1f, 0.65f));
		_deathMessageLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_deathMessageLabel.AddThemeConstantOverride("outline_size", 8);

		margin.AddChild(_deathMessageLabel);

		_deathMessageContainer.Visible = false;
	}

	private void OnPlayerRespawned()
	{
		if (_deathMessageContainer != null)
		{
			_deathMessageContainer.Visible = false;
		}


		if (_deathMessageLabel != null)
		{
			_deathMessageLabel.Text = "";
		}
	}

	private void OnExtraLivesChanged(int remainingLives)
	{
		UpdateLivesDisplay(remainingLives);
	}

	private void UpdateLivesDisplay(int remainingLives)
	{
		if (_livesValueLabel == null)
		{
			return;
		}

		_livesValueLabel.Text = remainingLives.ToString();


		// Lives UI should ALWAYS exist, even when the count is zero.
		if (_livesContainer != null)
		{
			_livesContainer.Visible = true;
		}
	}

	private void OnGameMessageRequested(string message)
	{
		ShowGameMessage(message);
	}

	private void HideGameMessage()
	{
		if (_gameMessageContainer != null)
		{
			_gameMessageContainer.Visible = false;
		}

		if (_gameMessageLabel != null)
		{
			_gameMessageLabel.Text = "";
			_gameMessageLabel.Modulate = new Color(1f, 1f, 1f, 1f);
		}
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
	}
}
