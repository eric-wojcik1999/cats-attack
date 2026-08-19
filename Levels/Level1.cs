using Godot;
using System;

public partial class Level1 : Node3D, ILevel
{
	[Export] private AudioStream _levelMusic;
    private bool _levelCompleted = false;

    public override void _Ready()
    {
        MusicManager.Instance?.PlayMusic(_levelMusic, -4f, 1.25f);
    }

	public void CompleteLevel()
	{
		if (_levelCompleted)
		{
			return;
		}

		_levelCompleted = true;

		GD.Print("[Level1] Level complete!");
		GD.Print($"Currency: {Global.Instance.CurrentLevelCurrency}");
		GD.Print($"Coins: {Global.Instance.CurrentLevelCoinsCollected}");
		GD.Print($"Enemies: {Global.Instance.CurrentLevelEnemiesKilled}");
		GD.Print($"Percy: {Global.Instance.CurrentLevelPercyCollected}");
		_ = SceneManager.Instance.GoToLevelEnd();
	}
}
