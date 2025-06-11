using Godot;
using System;

public partial class GameOver : Control
{
	private TextureButton RestartButton;
	private TextureButton MenuButton;
	private Label survivalTimeLabel;
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	private AudioStreamPlayer gameOverSound;
	public static float SurvivalTimeToShow = 0f;
	
	public override void _Ready()
	{
		survivalTimeLabel = GetNode<Label>("SurvivalTimeLabel");
		RestartButton = GetNode<TextureButton>("GameOverMenu/RestartButton");
		MenuButton = GetNode<TextureButton>("GameOverMenu/MenuButton");
		buttonSound = GetNode<AudioStreamPlayer>("GameOverMenu/ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("GameOverMenu/HoverSound");
		gameOverSound = GetNode<AudioStreamPlayer>("GameOverMenu/GameOverSound");
		
		PlayGameOverSound();
		ShowSurvivalTime();
		
		RestartButton.Pressed += ORBP;
		MenuButton.Pressed += OMBP;
		RestartButton.MouseEntered += ORBH;
		MenuButton.MouseEntered += OMBH;

		Input.MouseMode = Input.MouseModeEnum.Visible;
	}

	private void ShowSurvivalTime()
	{
		int minutes = (int)(SurvivalTimeToShow / 60);
		int seconds = (int)(SurvivalTimeToShow % 60);
		survivalTimeLabel.Text = $"You Survived: {minutes:D2}:{seconds:D2}";
	}
		
	private async void ORBP()
	{
		buttonSound.Play();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout"); 
		GetTree().ChangeSceneToFile("res://scenes/game.tscn");
	}

	private async void OMBP()
	{
		buttonSound.Play();
		await ToSignal(GetTree().CreateTimer(0.3f), "timeout"); 
		GetTree().ChangeSceneToFile("res://scenes/Menu.tscn");
	}
	
	private void ORBH()
	{
		hoverSound.Play();
	}
	
	private void OMBH()
	{
		hoverSound.Play();
	}
	
	private void PlayGameOverSound()
	{
		gameOverSound.Play();
	}
}
