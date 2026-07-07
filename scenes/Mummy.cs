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
		if (_isDead) return;
		_isDead = true;
		
		var collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collision != null)
		{
			collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		if (_animatedSprite != null) _animatedSprite.Hide();
		
		// 1. Lock in the exact pixel coordinates of the visual body, not the feet.
		// If the death sprite doesn't exist, we fall back to the main GlobalPosition.
// We take the feet's position and push it UP by a set amount of pixels.
// Adjust the '-50f' up or down until it perfectly matches your mummy's chest height!
Vector2 exactBodyPosition = GlobalPosition + new Vector2(0, 5f); //ghost pos offset to hit body mummy
		if (_deathSprite != null)
		{
			_deathSprite.Show();
			_deathSprite.FlipH = MoveDirection == -1; 
			_deathSprite.Play("default"); 
			
			await ToSignal(_deathSprite, AnimatedSprite2D.SignalName.AnimationFinished);
		}

		var gridManager = GetTree().CurrentScene.GetNodeOrNull<GridManager>("HUD/GridBackground/SoulGrid");
		if (gridManager != null)
		{
			// 2. Pass the exact visual body position we captured earlier
			gridManager.ProcessMummyDeath(exactBodyPosition, killerId);
		}
		else
		{
			GD.PrintErr("CRITICAL FAILURE: GridManager could not be found at path 'HUD/GridBackground/SoulGrid'.");
		}

		QueueFree(); 
	}
}
