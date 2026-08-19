using Godot;
using System;
using System.Collections.Generic;

public partial class Global : Node
{
	// Creates a globally accessible reference at runtime
	// get; and set; defines getters and setters respectively
 	public static Global Instance { get; private set; }
	public bool CurrentLevelRewardCommitted { get; private set; } = false;

	// =========================================================
	// PERSISTENT GAME STATE
	// =========================================================
    [Export] public int TotalCurrency = 0;
	public int highestUnlockedLevel {get; private set;} = 1;

	// =========================================================
	// CURRENT LEVEL / RUN STATE
	// =========================================================
	public int CurrentLevel {get; private set;} = 0;
	// Cumulative amount from coins + enemy kills
	public int CurrentLevelCurrency { get; private set; } = 0;

	// Actual number of coin pickup objects collected.
	public int CurrentLevelCoinsCollected { get; private set; } = 0;

	public int CurrentLevelEnemiesKilled {get; private set;} = 0;
	public int CurrentLevelPercyCollected {get; private set;} = 0;

	// =========================================================
	// UPGRADE STATE
	// =========================================================
	private readonly HashSet<string> _purchasedUpgrades = new();

	// =========================================================
	// UPGRADE IDS
	// =========================================================

	public const string UpgradeInnerCatHealth = "inner_cat_health";

	public const string UpgradeSuperPooperBullets = "super_pooper_bullets";

	public const string UpgradeExtraLives = "extra_lives";

	// =========================================================
	// SIGNALS
	// =========================================================
	[Signal]
	public delegate void CurrencyChangedEventHandler(int newValue);

	// Future upgrade-menu wallet signal.
	[Signal]
	public delegate void TotalCurrencyChangedEventHandler(int newValue);

	[Signal]
	public delegate void LevelUnlockedEventHandler(int level);

	[Signal]
	public delegate void GameMessageRequestedEventHandler(string message);

	[Signal]
	public delegate void UpgradePurchasedEventHandler(string upgradeId);

	// =========================================================
	// SINGLETON
	// =========================================================
	// Built in function that can be called when a Node is added to the Node tree
	// Singelton guard to protect against duplicates
	public override void _EnterTree() 
	{
		if (Instance != null && Instance != this)
		{
			GD.PushWarning("Duplicate Global singleton detected. Freeing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
	}

	// Built in function that can be called when a Node is removed to the Node tree
	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	// =========================================================
	// CURRENT LEVEL CURRENCY
	// =========================================================
	// Adds currrency using existing AddCurrency method, not modify the total currency wallet
	public bool AddCurrency(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning("[Global] Currency amount must be greater than 0.");

			return false;
		}

		CurrentLevelCurrency += amount;

		EmitSignal(SignalName.CurrencyChanged, CurrentLevelCurrency);

		GD.Print($"[Global] Level currency: {CurrentLevelCurrency} (+{amount})");

		return true;
	}

	// =========================================================
	// PERMANENT CURRENCY
	// =========================================================

	// Eventually called after the level-end calculation.
	public void AddToTotalCurrency(int amount)
	{
		if (amount <= 0)
		{
			return;
		}

		TotalCurrency += amount;

		EmitSignal(SignalName.TotalCurrencyChanged, TotalCurrency);

		GD.Print($"[Global] Total currency: {TotalCurrency} (+{amount})");
	}


	public bool TrySpendCurrency(int amount)
	{
		if (amount <= 0)
		{
			return false;
		}

		if (TotalCurrency < amount)
		{
			return false;
		}

		TotalCurrency -= amount;

		EmitSignal(SignalName.TotalCurrencyChanged, TotalCurrency);

		return true;
	}

	// =========================================================
	// LEVEL STATE
	// =========================================================
	public bool IsLevelUnlocked(int level)
	{
		return level >= 1 && level <= highestUnlockedLevel;
	}
	
	public void UnlockLevel(int level)
	{
		if (level <= highestUnlockedLevel)
		{
			return;
		}

		highestUnlockedLevel = level;

		EmitSignal(SignalName.LevelUnlocked, level);

		GD.Print($"[Global] Level: {level} unlocked");
	}

	public void BeginLevel(int level)
	{
		CurrentLevel = level;

		ResetCurrentLevelStats();

		GD.Print($"[Global] Beginning level: {level}");
	}

	// =========================================================
	// CURRENT LEVEL STATS
	// =========================================================
	public void ResetCurrentLevelStats()
	{
		CurrentLevelCurrency = 0;
		CurrentLevelCoinsCollected = 0;
		CurrentLevelEnemiesKilled = 0;
		CurrentLevelPercyCollected = 0;

		CurrentLevelRewardCommitted = false;

		EmitSignal(SignalName.CurrencyChanged, CurrentLevelCurrency);
	}

	public void AddCoinCollected(int amount = 1)
	{
		if (amount <= 0)
		{
			return;
		}

		CurrentLevelCoinsCollected += amount;
	}

	public void AddEnemyKill(int amount = 1)
	{
		CurrentLevelEnemiesKilled += amount;
	}

	public void AddPercyCollected(int amount = 1)
	{
		CurrentLevelPercyCollected += amount;
	}

	public bool CommitCurrentLevelReward()
	{
		if (CurrentLevelRewardCommitted)
		{
			return false;
		}

		CurrentLevelRewardCommitted = true;

		if (CurrentLevelCurrency > 0)
		{
			AddToTotalCurrency(CurrentLevelCurrency);
		}

		GD.Print($"[Global] Level reward committed. Total currency: {TotalCurrency}");

		return true;
	}

// =========================================================
// UPGRADES
// =========================================================

	public bool HasUpgrade(string upgradeId)
	{
		return _purchasedUpgrades.Contains(upgradeId);
	}

	public bool TryPurchaseUpgrade(string upgradeId, int cost)
	{
		if (String.IsNullOrWhiteSpace(upgradeId))
		{
			return false;
		}

		// Already purchased.
		if (_purchasedUpgrades.Contains(upgradeId))
		{
			return false;
		}

		if (cost <= 0)
		{
			return false;
		}

		if (TotalCurrency < cost)
		{
			RequestGameMessage("Not enough cash!");

			return false;
		}

		TotalCurrency -= cost;
		_purchasedUpgrades.Add(upgradeId);

		EmitSignal(SignalName.TotalCurrencyChanged, TotalCurrency);
		EmitSignal(SignalName.UpgradePurchased, upgradeId);


		GD.Print($"[Global] Purchased upgrade: {upgradeId}");
		GD.Print($"[Global] Remaining currency: {TotalCurrency}");


		return true;
	}

	public void ApplyPurchasedUpgrades(PlayerGround player)
	{
		if (player == null)
		{
			return;
		}

		if (HasUpgrade(UpgradeInnerCatHealth))
		{
			player.ApplyInnerCatHealthUpgrade();
		}

		if (HasUpgrade(UpgradeSuperPooperBullets))
		{
			player.ApplyBulletUpgrade();
		}

		if (HasUpgrade(UpgradeExtraLives)
		)
		{
			player.ApplyExtraLivesUpgrade();
		}
	}

	// =========================================================
	// GAME MESSAGES
	// =========================================================

	public void RequestGameMessage(string message)
	{
		if (String.IsNullOrWhiteSpace(message))
		{
			return;
		}

		EmitSignal(SignalName.GameMessageRequested, message);
	}
}
