using Godot;
using System;

public partial class Mummy : CharacterBody2D
{
	[Export] public float Speed = 150.0f;
	[Export] public float Gravity = 980.0f;
	
	public int MoveDirection { get; set; } = 1; // 1 = Right, -1 = Left

	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		// Fetch animated sprite reference if available
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Automatically link the screen exit event to our despawn logic
		var notifier = GetNode<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		if (notifier != null)
		{
			notifier.ScreenExited += OnScreenExited;
		}
	}
	
	private void OnScreenExited()
	{
		// Destroy the mummy as soon as it is completely off the 800x800 screen
		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		velocity.X = MoveDirection * Speed;
		
		Velocity = velocity;
		MoveAndSlide();

		if (_animatedSprite != null && MoveDirection != 0)
		{
			_animatedSprite.FlipH = MoveDirection == -1;
		}

		if (GlobalPosition.Y > 3000)
		{
			QueueFree();
		}
	}

	public void Die(int killerId)
	{
		// 1. Locate the GridManager using the relative path from the root
		// Adjust this path if your scene hierarchy structure changes!
		var gridManager = GetTree().Root.GetNodeOrNull<GridManager>("Main/HUD/GridBackground/SoulGrid");
		
		if (gridManager != null)
		{
			// 2. Send the physical position and killer ID straight to the grid system
			gridManager.HandleMummyDeath(GlobalPosition, killerId);
		}
		else
		{
			GD.PrintErr("CRITICAL: GridManager node not found! Verify node path in Mummy.cs.");
		}

		// 3. Clean up the mummy node from the scene tree
		QueueFree(); 
	}
}
