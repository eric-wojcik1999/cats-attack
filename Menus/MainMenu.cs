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

		// Starts the menu track if it isn't already playing.
		// Returning from another menu will NOT restart it.
		_ = MusicManager.Instance.EnsureMenuMusic();
	}


	private void OnStartPressed()
	{
		_ = SceneManager.Instance.GoToSelectLevel();
	}


	private void OnExitPressed()
	{
		GetTree().Quit();
	}
}
