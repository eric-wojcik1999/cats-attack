using Godot;
using System;

public partial class MainMenu : Control
{
	private Button _startButton;
	private Button _exitButton;

	public override void _Ready()
	{
		_startButton = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/StartButton");
		_exitButton = GetNode<Button>("MarginContainer/HBoxContainer/VBoxContainer/ExitButton");

		_startButton.Pressed += OnStartPressed;
		_exitButton.Pressed += OnExitPressed;

		_startButton.MouseEntered += OnMenuButtonHovered;
		_exitButton.MouseEntered += OnMenuButtonHovered;

		// Starts the menu track if it isn't already playing.
		// Returning from another menu will NOT restart it.
		_ = MusicManager.Instance.EnsureMenuMusic();
	}

	private void OnMenuButtonHovered()
	{
		SfxManager.Instance?.PlayMenuHover();
	}

	private void OnStartPressed()
	{
		SfxManager.Instance?.PlayMenuSelect();
		_ = SceneManager.Instance.GoToSelectLevel();
	}


	private void OnExitPressed()
	{
		GetTree().Quit();
	}
}
