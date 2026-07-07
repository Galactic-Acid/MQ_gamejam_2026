using Godot;
using System;

public partial class Spear : Area2D
{
	[Export] public float Speed = 600.0f;
	
	// Converted from an integer to a Vector2 to support upward movement
	public Vector2 Direction { get; set; } = Vector2.Right;
	public int PlayerId { get; set; } = 1; 

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Move along the X and Y axes based on the given direction
		Position += Direction * Speed * (float)delta;

		// Expanded cleanup boundaries to include the vertical Y axis
		if (GlobalPosition.X > 3000 || GlobalPosition.X < -1000 || GlobalPosition.Y < -1000 || GlobalPosition.Y > 2000)
		{
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		// 1. Check if the spear hits a Mummy
		if (body is Mummy mummyTarget)
		{
			// Updated to match the new method name in Mummy.cs
			mummyTarget.Die(PlayerId);
			QueueFree(); 
			return;
		}
		
		// 2. Check if the spear hits a Player AND ensure it isn't the player who threw it
		if (body is Player playerTarget && playerTarget.PlayerId != this.PlayerId)
		{
			playerTarget.HandleHit(PlayerId);
			QueueFree();
		}
	}
}
