using Godot;
using System;

public partial class Game : Node2D
{
	private PackedScene enemyScene;
	private Node2D player;
	
	public override void _Ready(){
		enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		player = GetNode<Node2D>("player");
	
		GetNode<Timer>("Timer").Timeout += OnTimerTimeout; 
	}
	
	private void OnTimerTimeout(){
		var enemy = enemyScene.Instantiate<Node2D>();
		
		enemy.GlobalPosition = player.GlobalPosition;
		
		while(enemy.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) < 10000){
			var viewportRect = GetViewportRect();
			enemy.GlobalPosition = new Vector2(
				GD.RandRange(0, (int)viewportRect.Size.X),
				GD.RandRange(0, (int)viewportRect.Size.Y)
			);
		}
		AddChild(enemy);
	}
}
