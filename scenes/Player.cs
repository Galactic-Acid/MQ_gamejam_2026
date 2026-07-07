using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public int PlayerId = 1;
	[Export] public float Speed = 300.0f;
	[Export] public PackedScene SpearScene;
	
	// The amount of points to deduct when struck by a spear
	[Export] public int PenaltyAmount = 100; 
	
	// Cooldown variables
	[Export] public float SpearCooldown = 1.0f; // 1 second cooldown
	private ulong _lastThrowTime = 0;

	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public int FacingDirection { get; set; } = 1; 

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		float direction = 0;
		if (PlayerId == 1)
		{
			if (Input.IsPhysicalKeyPressed(Key.A)) direction -= 1;
			if (Input.IsPhysicalKeyPressed(Key.D)) direction += 1;
		}
		else if (PlayerId == 2)
		{
			if (Input.IsPhysicalKeyPressed(Key.Left)) direction -= 1;
			if (Input.IsPhysicalKeyPressed(Key.Right)) direction += 1;
		}

		if (direction != 0)
		{
			velocity.X = direction * Speed;
			FacingDirection = direction > 0 ? 1 : -1;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (SpearScene == null) return;

		bool isShootUp = false;
		bool isShootHorizontal = false;

		if (PlayerId == 1 && @event is InputEventKey keyEventP1 && keyEventP1.Pressed && !keyEventP1.Echo)
		{
			if (keyEventP1.Keycode == Key.W) isShootUp = true;
			if (keyEventP1.Keycode == Key.Space) isShootHorizontal = true;
		}
		else if (PlayerId == 2 && @event is InputEventKey keyEventP2 && keyEventP2.Pressed && !keyEventP2.Echo)
		{
			if (keyEventP2.Keycode == Key.Up) isShootUp = true;
			if (keyEventP2.Keycode == Key.Enter) isShootHorizontal = true;
		}

		// Only process the throw if a valid key was pressed
		if (isShootUp || isShootHorizontal)
		{
			// Check if the cooldown has elapsed
			ulong currentTime = Time.GetTicksMsec();
			if (currentTime - _lastThrowTime < SpearCooldown * 1000)
			{
				return; // Exit early; the player must wait longer
			}

			// Trigger the correct spawn angle and update the cooldown timer
			if (isShootUp)
			{
				SpawnSpear(Vector2.Up);
			}
			else if (isShootHorizontal)
			{
				SpawnSpear(new Vector2(FacingDirection, 0));
			}
			
			// Record the exact millisecond this throw occurred
			_lastThrowTime = Time.GetTicksMsec();
		}
	}

	private void SpawnSpear(Vector2 direction)
	{
		Spear spearInstance = SpearScene.Instantiate<Spear>();
		
		spearInstance.PlayerId = this.PlayerId;
		spearInstance.Direction = direction;
		
		if (direction == Vector2.Up)
		{
			spearInstance.RotationDegrees = -90; 
		}
		else if (direction.X == -1)
		{
			spearInstance.RotationDegrees = 180; 
		}
		else
		{
			spearInstance.RotationDegrees = 0; 
		}
		
		GetTree().CurrentScene.AddChild(spearInstance);
		spearInstance.GlobalPosition = GetNode<Marker2D>("ProjectileSpawn").GlobalPosition;
	}

	public void HandleHit(int scoringPlayerId)
	{
		// 1. Locate the Scoreboard in the current scene
		var scoreManager = GetTree().CurrentScene.GetNodeOrNull<ScoreManager>("HUD/Scoreboard");
		
		if (scoreManager != null)
		{
			// 2. Pass a negative integer to deduct points from THIS player
			scoreManager.AddScore(PlayerId, -PenaltyAmount);
			
			GD.Print($"Impact! Player {PlayerId} was hit by Player {scoringPlayerId}. Deducted {PenaltyAmount} points.");
		}
		else
		{
			GD.PrintErr("CRITICAL: Could not find Scoreboard at path 'HUD/Scoreboard' to deduct points.");
		}
	}
}
