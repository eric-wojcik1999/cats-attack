using Godot;

public partial class Level : Node3D, ILevel
{
	// =========================================================
	// LEVEL SETTINGS
	// =========================================================

	[Export] private AudioStream _levelMusic;

	// =========================================================
	// STATE
	// =========================================================

	private bool _levelCompleted = false;
	private bool _restartRequested = false;

	public override void _Ready()
	{
		// MusicManager already avoids restarting the same track if it is currently playing.
		MusicManager.Instance?.PlayMusic(_levelMusic, -4f, 1.25f);
	}

	// =========================================================
	// INPUT
	// =========================================================

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("restart_level"))
		{
			RequestRestart();
		}
	}

	private void RequestRestart()
	{
		// Prevent repeated R presses while the transition is already underway.
		if (_restartRequested)
		{
			return;
		}

		// Once we've actually completed the level, don't let a late R press interfere with the completion flow.
		if (_levelCompleted)
		{
			return;
		}

		_restartRequested = true;

		GD.Print($"[Level] Restart requested: {SceneFilePath}");
		_ = SceneManager.Instance.RestartCurrentLevel(SceneFilePath);
	}


	// =========================================================
	// COMPLETION
	// =========================================================

	public void CompleteLevel()
	{
		if (_levelCompleted)
		{
			return;
		}

		_levelCompleted = true;

		GD.Print("[Level] Level complete!");
		GD.Print($"Currency: {Global.Instance.CurrentLevelCurrency}");
		GD.Print($"Coins: {Global.Instance.CurrentLevelCoinsCollected}");
		GD.Print($"Enemies: {Global.Instance.CurrentLevelEnemiesKilled}");
		GD.Print($"Percy: {Global.Instance.CurrentLevelPercyCollected}");
		_ = SceneManager.Instance.GoToLevelEnd();
	}
}