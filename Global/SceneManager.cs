using Godot;
using System.Threading.Tasks;
using System;

public partial class SceneManager : CanvasLayer
{
    public static SceneManager Instance {get; private set;}
    private ColorRect _fadeRect;
    private bool _transitioning = false;
    private const string MainMenuPath = "res://Menus/main_menu.tscn";
    private const string LevelSelectPath = "res://Menus/level_select.tscn";
    private const string Level1Path = "res://Levels/level-1.tscn";
    private const string LevelEndPath = "res://Menus/level-end-screen.tscn";
    private const string UpgradeScreenPath = "res://Menus/level_upgrade_screen.tscn";

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Layer = 100;
        CreateFadeOverlay();
    }

    private void CreateFadeOverlay()
    {
        _fadeRect = new ColorRect();
        _fadeRect.Color = new Color(0f, 0f, 0f, 0f);
        _fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _fadeRect.MouseFilter = Control.MouseFilterEnum.Ignore;

        AddChild(_fadeRect);
    }

	// =========================================================
	// PUBLIC NAVIGATION
	// =========================================================
    public async Task GoToMainMenu()
    {
        await ChangeScene(MainMenuPath, false);
    }

    public async Task GoToSelectLevel()
    {
        await ChangeScene(LevelSelectPath, false);
    }

    public async Task GoToLevelEnd()
    {
        if (_transitioning)
        {
            return;
        }

        _transitioning = true;
        await FadeToBlack();
        await MusicManager.Instance.FadeOutMusic();

        Error result = GetTree().ChangeSceneToFile(LevelEndPath);

        if (result != Error.Ok)
        {
            GD.PushError($"Failed to load level end scene: {LevelEndPath}");

            _transitioning = false;
            return;
        }

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await MusicManager.Instance.EnsureMenuMusic();
        await FadeFromBlack();
        _transitioning = false;
    }

    public async Task GoToUpgradeScreen()
    {
        await ChangeScene(UpgradeScreenPath, false);
    }

    public async Task StartLevel(int level)
    {
        if (!Global.Instance.IsLevelUnlocked(level))
        {
            return;
        }

        Global.Instance.BeginLevel(level);
        string path = GetLevelPath(level);

        if (String.IsNullOrEmpty(path))
        {
			GD.PushError($"No scene path configured for Level {level}.");

			return;
        }

        await ChangeScene(path, true);
    }

    private string GetLevelPath(int level)
    {
        return level switch
        {
            1 => Level1Path,
			// Future:
			// 2 => "res://Levels/level-2.tscn",
			// 3 => "res://Levels/level-3.tscn",
            _ => ""
        };
    }

	// =========================================================
	// TRANSITION
	// =========================================================
    private async Task ChangeScene(string scenePath, bool enteringGameplay)
    {
        if (_transitioning)
        {
            return;
        }

        _transitioning = true;

        // Fade screen to black
        await FadeToBlack();

        if (enteringGameplay)
        {
			// Let the black screen sit briefly before opening the level.
			// await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
			await MusicManager.Instance.FadeOutMusic();
        }

        Error result = GetTree().ChangeSceneToFile(scenePath);

        if (result != Error.Ok)
        {
            GD.PushError($"Failed to change scene to: {scenePath}");

			_transitioning = false;
			return;
        }

        // Give the new scene a frame to enter the tree.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        await FadeFromBlack();

        _transitioning = false;
    }

    private async Task FadeToBlack()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_fadeRect, "color:a", 1.0f, 0.35f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task FadeFromBlack()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_fadeRect, "color:a", 0.0f, 0.35f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

}