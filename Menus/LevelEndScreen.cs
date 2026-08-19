using Godot;
using System;

public partial class LevelEndScreen : Control
{
	private Label _coinsValue;
	private Label _enemiesValue;
	private Label _percyValue;
	private Label _totalValue;
	private Button _continueButton;


	public override void _Ready()
	{
		_coinsValue = GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/PanelContainer/MarginContainer/HBoxContainer/VBoxContainerValues/Coins"
			);
		_enemiesValue =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/PanelContainer/MarginContainer/HBoxContainer/VBoxContainerValues/Enemies"
			);
		_percyValue =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/PanelContainer/MarginContainer/HBoxContainer/VBoxContainerValues/Percy"
			);
		_totalValue =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/PanelContainer/MarginContainer/HBoxContainer/VBoxContainerValues/Total"
			);
		_continueButton =
			GetNode<Button>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/ContinueButton"
			);
		_continueButton.Pressed += OnContinuePressed;

		UpdateStats();
	}

	private void UpdateStats()
	{
		_coinsValue.Text = Global.Instance.CurrentLevelCoinsCollected.ToString();
		_enemiesValue.Text = Global.Instance.CurrentLevelEnemiesKilled.ToString();
		_percyValue.Text = Global.Instance.CurrentLevelPercyCollected.ToString();

		int finalScore = Global.Instance.CurrentLevelCurrency;

		_totalValue.Text = finalScore.ToString();
	}

	private void OnContinuePressed()
	{
		Global.Instance.CommitCurrentLevelReward();

		int nextLevel = Global.Instance.CurrentLevel + 1;
		Global.Instance.UnlockLevel(nextLevel);

		if (nextLevel <= 3)
		{
			Global.Instance.UnlockLevel(nextLevel);
		}

		_ = SceneManager.Instance.GoToUpgradeScreen();
	}
}
