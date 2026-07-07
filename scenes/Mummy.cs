using Godot;
using System;

public partial class Mummy : CharacterBody2D
{
	[Export] public float Speed = 150.0f;
	[Export] public float Gravity = 980.0f;
	
	public int MoveDirection { get; set; } = 1; 
	
	private AnimatedSprite2D _animatedSprite;
	private AnimatedSprite2D _deathSprite;
	private bool _isDead = false;

public override void _Ready()
	{
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_deathSprite = GetNodeOrNull<AnimatedSprite2D>("DeathSprite");

		// Force the death sprite to be invisible the moment the mummy spawns
		if (_deathSprite != null)
		{
			_deathSprite.Hide();
		}

		var notifier = GetNodeOrNull<VisibleOnScreenNotifier2D>("VisibleOnScreenNotifier2D");
		if (notifier != null)
		{
			notifier.ScreenExited += OnScreenExited;
		}
	}
	
	private void OnScreenExited()
	{
		QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		// Stop processing standard movement if the mummy is currently dying
		if (_isDead) return;

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

	public async void Die(int killerId)
	{
		// Prevent the Die function from running twice if hit by two spears at the exact same time
		if (_isDead) return;
		_isDead = true;

		// 1. Immediately trigger the GridManager so the UI feels responsive
		var gridManager = GetTree().CurrentScene.GetNodeOrNull<GridManager>("HUD/GridBackground/SoulGrid");
		if (gridManager != null)
		{
			gridManager.HandleMummyDeath(GlobalPosition, killerId);
		}

		// 2. Disable physical collision (so more spears don't hit it)
		// Assuming your main collision node is named "CollisionShape2D"
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
		{
			collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		// 3. Swap the visual sprites
		if (_animatedSprite != null) _animatedSprite.Hide();
		
		if (_deathSprite != null)
		{
			_deathSprite.Show();
			// Ensure it faces the correct way based on travel direction
			_deathSprite.FlipH = MoveDirection == -1; 
			
			// Replace "default" with your actual death animation name if different
			_deathSprite.Play("default"); 

			// 4. Pause this specific method until the animation finishes
			await ToSignal(_deathSprite, AnimatedSprite2D.SignalName.AnimationFinished);
		}

		// 5. Finally delete the node
		QueueFree(); 
	}
}
