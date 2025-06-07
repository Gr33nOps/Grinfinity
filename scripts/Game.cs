using Godot;
using System;

public partial class Game : Node2D
{
	private PackedScene enemyScene;
	private Node2D player;
	private const float MinSpawnDistance = 200.0f;

	public override void _Ready()
	{
		enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn");
		player = GetNode<Node2D>("player");

		GetNode<Timer>("Timer").Timeout += OnTimerTimeout;
	}

	private void OnTimerTimeout()
	{
		var enemy = enemyScene.Instantiate<Node2D>();
		var viewportRect = GetViewportRect();

		Vector2 spawnPos;
		do
		{
			spawnPos = new Vector2(
				GD.RandRange(0, (int)viewportRect.Size.X),
				GD.RandRange(0, (int)viewportRect.Size.Y)
			);
		} while (spawnPos.DistanceTo(player.GlobalPosition) < MinSpawnDistance);

		enemy.GlobalPosition = spawnPos;
		AddChild(enemy);
	}
}
