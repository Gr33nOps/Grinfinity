// Menu.cs
using Godot;
using System;

public partial class Menu : Node
{
	public override void _Ready()
	{
		GetNode<Button>("PlayButton").Pressed += OnPlayButtonPressed;
		GetNode<Button>("QuitButton").Pressed += OnQuitButtonPressed;
		Input.MouseMode = Input.MouseModeEnum.Visible;
	}
	
	private void OnPlayButtonPressed()
	{
		GetTree().ChangeSceneToFile("res://scenes/game.tscn");
	}
	
	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}
}
