using Godot;

public partial class LevelSelect : Control
{
	private Button _level1Button;
	private Button _level2Button;
	private Button _level3Button;
	private Button _backButton;


	public override void _Ready()
	{
		_level1Button = GetNode<Button>("MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/GridContainer/Level 1");
		_level2Button = GetNode<Button>("MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/GridContainer/Level 2");
		_level3Button = GetNode<Button>("MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/GridContainer/Level 3");
		_backButton = GetNode<Button>("MarginContainer/CenterContainer/PanelContainer/VBoxContainer/HBoxContainer/VBoxContainer/BackButton");
		_level1Button.Pressed += () => StartLevel(1);
		_level2Button.Pressed += () => StartLevel(2);
		_level3Button.Pressed += () => StartLevel(3);
		_backButton.Pressed += OnBackPressed;
		UpdateLevelButtons();

		// Does nothing if menu music is already playing.
		_ = MusicManager.Instance.EnsureMenuMusic();
	}


	private void UpdateLevelButtons()
	{
		SetLevelButtonState(_level1Button, 1);
		SetLevelButtonState(_level2Button, 2);
		SetLevelButtonState(_level3Button, 3);
	}


	private void SetLevelButtonState(Button button, int level)
	{
		bool unlocked = Global.Instance.IsLevelUnlocked(level);

		button.Disabled = !unlocked;

		if (unlocked)
		{
			button.Text = level.ToString("00");
		}
		else
		{
			button.Text = "LOCKED";
		}
	}


	private void StartLevel(int level)
	{
		if (!Global.Instance.IsLevelUnlocked(level))
		{
			return;
		}

		_ = SceneManager.Instance.StartLevel(level);
	}


	private void OnBackPressed()
	{
		_ = SceneManager.Instance.GoToMainMenu();
	}
}