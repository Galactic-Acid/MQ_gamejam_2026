using Godot;
using System;

public partial class Player : CharacterBody2D
{
	[Export] public int PlayerId = 1;
	[Export] public float Speed = 300.0f;
	[Export] public PackedScene SpearScene;
	
	[Export] public int PenaltyAmount = 100; 
	
	[Export] public float SpearCooldown = 1.0f;
	private ulong _lastThrowTime = 0;

	public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	public int FacingDirection { get; set; } = 1; 

	public bool IsBlocking { get; private set; } = false;
	
	// State flags to prevent running/idle from interrupting active animations
	private bool _isShooting = false;
	private bool _isHit = false;

	private AnimatedSprite2D _animatedSprite;

public override void _Ready()
	{
		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");
		
		if (_animatedSprite == null)
		{
			GD.PrintErr("CRITICAL: AnimatedSprite2D not found. Check that the node is named 'Sprite2D' in the Scene Tree.");
		}

		// Cache the indicator and apply the correct team colour
		Polygon2D indicator = GetNodeOrNull<Polygon2D>("PlayerIndicator");
		if (indicator != null)
		{
			// Hex codes matched to your GridManager colours
			indicator.Color = (PlayerId == 1) ? new Color("ff4a4a") : new Color("4a90ff");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// 1. Check Block Input (Players cannot block while they are reeling from a hit)
		if (!_isHit)
		{
			if (PlayerId == 1)
			{
				IsBlocking = Input.IsActionPressed("p1_block");
			}
			else if (PlayerId == 2)
			{
				IsBlocking = Input.IsActionPressed("p2_block");
			}
		}
		else
		{
			IsBlocking = false;
		}

		float direction = 0;
		
		// 2. Lock movement if blocking OR currently playing the hit animation
		if (!IsBlocking && !_isHit)
		{
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
		}

		// 3. Apply Velocity
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

		// 4. Handle Animation States (Highest priority to lowest)
		if (_animatedSprite != null)
		{
			// Flip the sprite based on the facing direction
			_animatedSprite.FlipH = FacingDirection == -1;

			if (_isHit)
			{
				// HandleHit is managing this sequence.
			}
			else if (IsBlocking)
			{
				if (_animatedSprite.Animation != "block")
				{
					_animatedSprite.Play("block");
				}
			}
			else if (_isShooting)
			{
				// TriggerShootAnimation is managing this sequence.
			}
			else if (Mathf.Abs(Velocity.X) > 10.0f) // Checks for actual physical movement
			{
				if (_animatedSprite.Animation != "running")
				{
					_animatedSprite.Play("running");
				}
			}
			else
			{
				if (_animatedSprite.Animation != "idle")
				{
					_animatedSprite.Play("idle");
				}
			}
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (SpearScene == null) return;
		
		// Prevent shooting if blocking OR currently reeling from a hit
		if (IsBlocking || _isHit) return;

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

		if (isShootUp || isShootHorizontal)
		{
			ulong currentTime = Time.GetTicksMsec();
			if (currentTime - _lastThrowTime < SpearCooldown * 1000)
			{
				return; 
			}

			if (isShootUp)
			{
				SpawnSpear(Vector2.Up);
				TriggerShootAnimation("shoot_up");
			}
			else if (isShootHorizontal)
			{
				SpawnSpear(new Vector2(FacingDirection, 0));
				TriggerShootAnimation("shoot_straight");
			}
			
			_lastThrowTime = Time.GetTicksMsec();
		}
	}

	private async void TriggerShootAnimation(string animationName)
	{
		if (_animatedSprite == null) return;

		_isShooting = true;
		_animatedSprite.Play(animationName);

		await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
		
		_isShooting = false;
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

	// Updated to run asynchronously so we can wait for the hit animation to finish
	public async void HandleHit(int scoringPlayerId)
	{
		if (IsBlocking)
		{
			GD.Print($"Deflected! Player {PlayerId} blocked the spear from Player {scoringPlayerId}.");
			return; 
		}

		var scoreManager = GetTree().CurrentScene.GetNodeOrNull<ScoreManager>("HUD/Scoreboard");
		
		if (scoreManager != null)
		{
			scoreManager.AddScore(PlayerId, -PenaltyAmount);
			GD.Print($"Impact! Player {PlayerId} was hit by Player {scoringPlayerId}. Deducted {PenaltyAmount} points.");
		}
		else
		{
			GD.PrintErr("CRITICAL: Could not find Scoreboard at path 'HUD/Scoreboard' to deduct points.");
		}

		// 5. Trigger the hit animation and briefly lock inputs
		if (_animatedSprite != null)
		{
			_isHit = true;
			
			// Change "hit" to whatever you named this animation in the SpriteFrames panel
			_animatedSprite.Play("hit"); 
			
			await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
			
			_isHit = false;
		}
	}
}
