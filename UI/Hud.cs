using Godot;
using System;

public partial class Hud : CanvasLayer
{
	// Can swap out what label path it refers to via inspector or keep default
	[Export] public NodePath _scoreValuePath = "ScorePanel/ScoreValue";
	[Export] public NodePath _healthValuePath = "HealthPanel/HealthValue";
	[Export] public PlayerGround player;

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
    }
}
