using Godot;
using System;

public partial class MummySpawner : Godot.Node
{
	[Export] public PackedScene MummyScene;
	[Export] public Godot.Node SpawnPointsParent; 
	[Export] public float SpawnInterval = 2.0f; 

	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Ready()
	{
		// This MUST print the split-second you run the game. 
		// If it doesn't, this script is NOT attached to an active node in the scene.
		GD.Print("DIAGNOSTIC: MummySpawner script has successfully woken up inside the game tree!");

		_rng.Randomize();

		Timer spawnTimer = new Timer();
		AddChild(spawnTimer); 
		
		spawnTimer.WaitTime = SpawnInterval;
		spawnTimer.Timeout += SpawnMummy;
		spawnTimer.Start(); 
	}

	public void SpawnMummy()
	{
		GD.Print("DIAGNOSTIC: Spawner Timer timed out! Attempting to evaluate spawn points...");

		if (MummyScene == null || SpawnPointsParent == null || SpawnPointsParent.GetChildCount() == 0)
		{
			GD.PrintErr("DIAGNOSTIC ERROR: Missing Inspector assignments for scene or folder path.");
			return;
		}

		var spawnPoints = SpawnPointsParent.GetChildren();
		int totalPoints = spawnPoints.Count;

		GD.Print($"DIAGNOSTIC: Found {totalPoints} nodes inside the folder.");

		long systemTicks = (long)Time.GetTicksMsec();
		int randomIndex = (int)(systemTicks % totalPoints);
		
		var chosenSpawn = spawnPoints[randomIndex] as Godot.Node2D;

		if (chosenSpawn == null)
		{
			GD.PrintErr($"DIAGNOSTIC ERROR: Node at index {randomIndex} is not a valid Marker2D/Node2D.");
			return;
		}

		Mummy newMummy = MummyScene.Instantiate<Mummy>();
		newMummy.GlobalPosition = chosenSpawn.GlobalPosition;
		
		GetTree().CurrentScene.AddChild(newMummy);
		GD.Print($"DIAGNOSTIC SUCCESS: Mummy spawned at {chosenSpawn.Name}!");
	}
}
