using Godot;
using System;

public partial class Hud : CanvasLayer
{
	// Can swap out what label path it refers to via inspector or keep default
	[Export] public NodePath _scoreValuePath = "ScorePanel/ScoreValue";

	private Label _scoreValueLabel;

	public override void _Ready()
	{
		_scoreValueLabel = GetNode<Label>(_scoreValuePath);
		_scoreValueLabel.Text = "0";

		if (Global.Instance != null)
		{
			Global.Instance.CurrencyChanged += OnCurrencyChanged;
		}
		else 
		{
			GD.PushError("[Hud] Global.Instance is null. Is Global autoload configured?");
		}

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
