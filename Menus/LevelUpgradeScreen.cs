using Godot;
using System;

public partial class LevelUpgradeScreen : Control
{
	// =========================================================
	// COSTS
	// =========================================================

	private const int InnerCatHealthCost = 50;
	private const int BulletUpgradeCost = 200;
	private const int ExtraLivesCost = 250;


	// =========================================================
	// UI
	// =========================================================

	private TextureButton _innerCatHealthButton;
	private TextureButton _bulletUpgradeButton;
	private TextureButton _extraLivesButton;

	private Label _upgradeNameLabel;
	private Label _costLabel;
	private Label _descriptionLabel;
	private Label _cashValueLabel;

	private Button _continueButton;


	// =========================================================
	// CURRENT SELECTION
	// =========================================================

	private string _selectedUpgradeId = "";
	private int _selectedUpgradeCost = 0;

	public override void _Ready()
	{
		_innerCatHealthButton =
			GetNode<TextureButton>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/UpgradeContainer/MarginContainer/HBoxContainer/InnerCatHealthButton"
			);
		_bulletUpgradeButton =
			GetNode<TextureButton>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/UpgradeContainer/MarginContainer/HBoxContainer/BulletUpgradeButton"
			);
		_extraLivesButton =
			GetNode<TextureButton>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/UpgradeContainer/MarginContainer/HBoxContainer/ExtraLivesButton"
			);
		_upgradeNameLabel =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/DescContainer/MarginContainer/HBoxContainer/VBoxContainerLabels/UpgradeName"
			);

		_costLabel =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/DescContainer/MarginContainer/HBoxContainer/VBoxContainerLabels/Cost"
			);

		_descriptionLabel =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/DescContainer/MarginContainer/HBoxContainer/VBoxContainerLabels/Description"
			);
		_cashValueLabel =
			GetNode<Label>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/HeaderRow/RightArea/CashValue"
			);
		_continueButton =
			GetNode<Button>(
				"MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/ContinueButton"
			);

		_innerCatHealthButton.MouseEntered += ShowInnerCatHealth;
		_bulletUpgradeButton.MouseEntered += ShowBulletUpgrade;
		_extraLivesButton.MouseEntered += ShowExtraLives;
		_innerCatHealthButton.Pressed += () => TryPurchaseUpgrade(Global.UpgradeInnerCatHealth, InnerCatHealthCost);
		_bulletUpgradeButton.Pressed += () => TryPurchaseUpgrade(Global.UpgradeSuperPooperBullets, BulletUpgradeCost);
		_extraLivesButton.Pressed += () => TryPurchaseUpgrade(Global.UpgradeExtraLives, ExtraLivesCost);
		_continueButton.Pressed += OnContinuePressed;

		// Update currency immediately.
		UpdateCashDisplay();
		// Refresh any upgrades purchased previously.
		RefreshUpgradeButtons();
		// Give the panel useful default text.
		ShowInnerCatHealth();

		Global.Instance.TotalCurrencyChanged += OnTotalCurrencyChanged;
	}

	public override void _ExitTree()
	{
		if (Global.Instance != null)
		{
			Global.Instance.TotalCurrencyChanged -= OnTotalCurrencyChanged;
		}
	}

	// =========================================================
	// DESCRIPTIONS
	// =========================================================
	private void ShowInnerCatHealth()
	{
		_selectedUpgradeId = Global.UpgradeInnerCatHealth;
		_selectedUpgradeCost = InnerCatHealthCost;
		_upgradeNameLabel.Text = "INNER CAT HEALTH PLUS";
		_descriptionLabel.Text = "Make your cat twice as durable!";
		UpdateSelectedCostText();
	}

	private void ShowBulletUpgrade()
	{
		_selectedUpgradeId = Global.UpgradeSuperPooperBullets;
		_selectedUpgradeCost = BulletUpgradeCost;
		_upgradeNameLabel.Text = "SUPER POOPER BULLETS";
		_descriptionLabel.Text = "Faster, more powerful and stinkier bullets.";
		UpdateSelectedCostText();
	}

	private void ShowExtraLives()
	{
		_selectedUpgradeId = Global.UpgradeExtraLives;
		_selectedUpgradeCost = ExtraLivesCost;
		_upgradeNameLabel.Text = "9 LIVES X 2";
		_descriptionLabel.Text = "Extra life when you die.";
		UpdateSelectedCostText();
	}

	// =========================================================
	// PURCHASE
	// =========================================================
	private void TryPurchaseUpgrade(string upgradeId, int cost)
	{
		// Already purchased.
		if (Global.Instance.HasUpgrade(upgradeId))
		{
			return;
		}

		bool purchased = Global.Instance.TryPurchaseUpgrade(upgradeId, cost);

		if (!purchased)
		{
			return;
		}

		RefreshUpgradeButtons();
		UpdateCashDisplay();
		UpdateSelectedCostText();
	}

	// =========================================================
	// BUTTON STATE
	// =========================================================
	private void RefreshUpgradeButtons()
	{
		SetUpgradeButtonPurchasedState(_innerCatHealthButton, Global.UpgradeInnerCatHealth);
		SetUpgradeButtonPurchasedState(_bulletUpgradeButton, Global.UpgradeSuperPooperBullets);
		SetUpgradeButtonPurchasedState(_extraLivesButton, Global.UpgradeExtraLives);
	}

	private void SetUpgradeButtonPurchasedState(TextureButton button, string upgradeId)
	{
		bool purchased = Global.Instance.HasUpgrade(upgradeId);
		button.Disabled = false;
		button.Modulate = purchased ? new Color(0.42f, 0.42f, 0.42f, 1.0f) : Colors.White;
	}

	private void UpdateSelectedCostText()
	{
		if (String.IsNullOrWhiteSpace(_selectedUpgradeId))
		{
			_costLabel.Text = "";
			return;
		}

		if (Global.Instance.HasUpgrade(_selectedUpgradeId))
		{
			_costLabel.Text = "Cost: BOUGHT";
		}
		else
		{
			_costLabel.Text = $"Cost: {_selectedUpgradeCost}";
		}
	}

	// =========================================================
	// CASH DISPLAY
	// =========================================================
	private void UpdateCashDisplay()
	{
		_cashValueLabel.Text = Global.Instance.TotalCurrency.ToString();
	}

	private void OnTotalCurrencyChanged(int newValue)
	{
		_cashValueLabel.Text = newValue.ToString();
	}


	// =========================================================
	// CONTINUE
	// =========================================================
	private void OnContinuePressed()
	{
		_ = SceneManager.Instance.GoToSelectLevel();
	}
}
