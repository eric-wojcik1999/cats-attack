using Godot;
using System;

public partial class Hud : CanvasLayer
{
	// Can swap out what label path it refers to via inspector or keep default
	[Export] public NodePath _scoreValuePath = "ScorePanel/ScoreValue";
	[Export] public NodePath _healthValuePath = "HealthPanel/HealthValue";
	[Export] public NodePath _percyFlashPath = "PercyFlash";
	[Export] public PlayerGround player;

	private ColorRect _percyFlash;
	private Tween _percyFlashTween;
	private Label _scoreValueLabel;
	private Label _healthValueLabel;

	public override void _Ready()
	{
		_scoreValueLabel = GetNode<Label>(_scoreValuePath);
		_scoreValueLabel.Text = "0";
		_healthValueLabel = GetNode<Label>(_healthValuePath);
		_healthValueLabel.Text = $"{player.Health}/{player.MaxHealth}";

		player.HealthChanged += OnHealthChanged;
		player.Died += OnDied;

		_percyFlash = GetNode<ColorRect>(_percyFlashPath);
		_percyFlash.Color = new Color(0.55f, 0.0f, 1.0f, 0.0f);

		player.PercyPowerupCollected += OnPercyPowerupCollected;

		if (Global.Instance != null)
		{
			Global.Instance.CurrencyChanged += OnCurrencyChanged;
		}
		else 
		{
			GD.PushError("[Hud] Global.Instance is null. Is Global autoload configured?");
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

		_percyFlashTween.TweenProperty(
			_percyFlash,
			"color",
			new Color(0.55f, 0.0f, 1.0f, 0.45f),
			0.08f
		);

		_percyFlashTween.TweenProperty(
			_percyFlash,
			"color",
			new Color(0.55f, 0.0f, 1.0f, 0.0f),
			0.25f
		);
	}
}
