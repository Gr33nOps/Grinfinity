using Godot;
using System;
public partial class GameOver : Control
{
	private TextureButton RestartButton;
	private TextureButton MenuButton;
	private Label survivalTimeLabel;
	private Label highScoreLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;
	public static float SurvivalTimeToShow = 0f;
	
	public override void _Ready()
	{
		survivalTimeLabel = GetNode<Label>("SurvivalTimeLabel");
		highScoreLabel = GetNode<Label>("HighScoreLabel");
		RestartButton = GetNode<TextureButton>("GameOverMenu/RestartButton");
		MenuButton = GetNode<TextureButton>("GameOverMenu/MenuButton");
		buttonSound = GetNode<AudioStreamPlayer>("GameOverMenu/ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("GameOverMenu/HoverSound");
		gameOverSound = GetNode<AudioStreamPlayer>("GameOverMenu/GameOverSound");
		
		PlayGameOverSound();
		ShowSurvivalTime();
		ShowHighScore();
		
		RestartButton.Pressed += OnRestartButtonPressed;
		MenuButton.Pressed += OnMenuButtonPressed;
		RestartButton.MouseEntered += OnRestartButtonHover;
		MenuButton.MouseEntered += OnMenuButtonHover;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
	
	private void ShowSurvivalTime()
	{
		int minutes = (int)(SurvivalTimeToShow / 60);
		int seconds = (int)(SurvivalTimeToShow % 60);
		survivalTimeLabel.Text = $"You Survived: {minutes:D2}:{seconds:D2}";
	}
	
	private void ShowHighScore()
	{
		if (highScoreLabel != null)
		{
			// Create temporary ScoreManager to get the high score
			var tempScoreManager = new ScoreManager();
			AddChild(tempScoreManager);
			highScoreLabel.Text = tempScoreManager.GetFormattedHighScore();
		}
	}
		
	private async void OnRestartButtonPressed()
	{
		buttonSound.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/game.tscn");
	}
	private async void OnMenuButtonPressed()
	{
		buttonSound.Play();
		SceneTransition.Instance.ChangeScene("res://scenes/menu.tscn");
	}
	
	private void OnRestartButtonHover()
	{
		hoverSound.Play();
	}
	
	private void OnMenuButtonHover()
	{
		hoverSound.Play();
	}
	
	private void PlayGameOverSound()
	{
		gameOverSound.Play();
	}
}
