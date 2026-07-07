using Godot;
using System;

// 1. Explicitly define Godot.Node to fix the InvalidCastException
public partial class MummySpawner : Godot.Node
{
	[Export] public PackedScene MummyScene;
	
	// 2. Explicitly define Godot.Node here as well
	[Export] public Godot.Node SpawnPointsParent; 
	
	[Export] public float SpawnInterval = 2.0f; 
	
	public override void _Ready()
	{
		// 3. Generate the timer in code to fix the "Node not found" crash
		Timer spawnTimer = new Timer();
		spawnTimer.WaitTime = SpawnInterval;
		spawnTimer.Autostart = true;
		spawnTimer.Timeout += SpawnMummy;
		AddChild(spawnTimer);
	}

	public void SpawnMummy()
	{
		if (SpawnPointsParent == null || SpawnPointsParent.GetChildCount() == 0)
		{
			GD.PrintErr("CRITICAL: Spawn points missing. Assign the SpawnPoints parent node.");
			return;
		}

		var spawnPoints = SpawnPointsParent.GetChildren();
		int randomIndex = GD.RandRange(0, spawnPoints.Count - 1);
		
		// 4. Cast strictly to Godot.Node2D
		var chosenSpawn = spawnPoints[randomIndex] as Godot.Node2D;

		if (chosenSpawn != null)
		{
			Mummy newMummy = MummyScene.Instantiate<Mummy>();
			newMummy.GlobalPosition = chosenSpawn.GlobalPosition;
			
			GetTree().CurrentScene.AddChild(newMummy);
		}
	}
}
