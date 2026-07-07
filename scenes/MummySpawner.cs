using Godot;
using System;

public partial class MummySpawner : Node2D
{
	[Export] public PackedScene MummyScene;
	[Export] public int SpawnDirection = 1; // 1 makes them run Right, -1 makes them run Left

	private Timer _timer;

	public override void _Ready()
	{
		_timer = GetNode<Timer>("Timer");
		_timer.Timeout += OnTimerTimeout;
	}

	private void OnTimerTimeout()
	{
		// Stop if no mummy blueprint is linked in the inspector
		if (MummyScene == null) return;

		// Create a fresh instance of the mummy
		Mummy mummyInstance = MummyScene.Instantiate<Mummy>();
		
		// Drop it at the exact location of this spawner node
		mummyInstance.GlobalPosition = this.GlobalPosition;
		mummyInstance.MoveDirection = this.SpawnDirection;

		// Add the mummy to the main game arena
		GetParent().AddChild(mummyInstance);
	}
}
