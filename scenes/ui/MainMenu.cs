using Godot;

namespace Game.UI;

public partial class MainMenu : Node
{
    private Button playButton;
    private Button optionsButton;
    private Button quitButton;
    private Control mainMenuContainer;
    private LevelSelectScreen levelSelectScreen;

    public override void _Ready()
    {
        playButton = GetNode<Button>("%PlayButton");
        optionsButton = GetNode<Button>("%OptionsButton");
        quitButton = GetNode<Button>("%QuitButton");
        mainMenuContainer = GetNode<Control>("%MainMenuContainer");
        levelSelectScreen = GetNode<LevelSelectScreen>("%LevelSelectScreen");

        levelSelectScreen.Visible = false;
        mainMenuContainer.Visible = true;

        playButton.Pressed += OnPlayButtonPressed;
        levelSelectScreen.BackPressed += OnLevelSelectBackPressed;
        quitButton.Pressed += OnQuitButtonPressed;
    }

    private void OnPlayButtonPressed()
    {
        mainMenuContainer.Visible = false;
        levelSelectScreen.Visible = true;
        // LevelManager.Instance.ChangeToLevel(0);
    }

    private void OnLevelSelectBackPressed()
    {
        levelSelectScreen.Visible = false;
        mainMenuContainer.Visible = true;
    }

    private void OnQuitButtonPressed()
    {
        GetTree().Quit();
    }
}
