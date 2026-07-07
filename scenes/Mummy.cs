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
		_animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
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

	public void HandleHit(int scoringPlayerId)
	{
		GD.Print($"Mummy killed! Player {scoringPlayerId} gets a point.");
		QueueFree(); 
	}
}
