// Menu.cs
using Godot;
using System;

public partial class Menu : Node
{
	private AudioStreamPlayer buttonSound;
	private AudioStreamPlayer hoverSound;
	
	public override void _Ready()
	{
		buttonSound = GetNode<AudioStreamPlayer>("ButtonSound");
		hoverSound = GetNode<AudioStreamPlayer>("HoverSound");
		GetNode<Button>("PlayButton").Pressed += OnPlayButtonPressed;
		GetNode<Button>("QuitButton").Pressed += OnQuitButtonPressed;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
	
	private void OnPlayButtonHover()
	{
		hoverSound.Play();
	}
	
	private void OnQuitButtonHover()
	{
		hoverSound.Play();
	}
	
	private async void OnPlayButtonPressed()
	{
		buttonSound.Play();
		await ToSignal(GetTree().CreateTimer(0.25f), "timeout"); 
		GetTree().ChangeSceneToFile("res://scenes/game.tscn");
	}
	
	private async void OnQuitButtonPressed()
	{
		buttonSound.Play();
		await ToSignal(GetTree().CreateTimer(0.25f), "timeout"); 
		GetTree().Quit();
	}
}
