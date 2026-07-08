using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : GridContainer
{
	[Export] public PackedScene MummyGhostScene;
	
	private const int TotalColumns = 6;
	private const int TotalRows = 5;
	
	// 0 = Empty, 1 = Player 1 (Red), 2 = Player 2 (Blue), -1 = Reserved by incoming ghost
	private int[,] _gridData = new int[TotalColumns, TotalRows];
	private ColorRect[] _cells = new ColorRect[30];

	private Color _emptyColour = new Color("535252c7"); 
	private Color _player1Colour = new Color("be001ca0");
	private Color _player2Colour = new Color("00419aa0");

	// UI Node References
	private CanvasLayer _endScreenLayer;
	private Label _winnerText;
	private Label _scoreBreakdown;
	private Button _replayButton;


	public override void _Ready()
	{
		// 1. Populate the _cells array and force all cells to the empty colour on launch
		for (int i = 0; i < GetChildCount(); i++)
		{
			if (GetChild(i) is ColorRect cell)
			{
				_cells[i] = cell;
				_cells[i].Color = _emptyColour;
			}
		}

		// 2. Assign the nodes from the scene tree layout
_endScreenLayer = GetNode<CanvasLayer>("/root/Main/EndScreenLayer");		_winnerText = _endScreenLayer.GetNode<Label>("Panel/VBoxContainer/WinnerText");
		_scoreBreakdown = _endScreenLayer.GetNode<Label>("Panel/VBoxContainer/ScoreBreakdown");
		_replayButton = _endScreenLayer.GetNode<Button>("Panel/VBoxContainer/ReplayButton");

		// 3. Wire up the button action
		_replayButton.Pressed += OnReplayButtonPressed;
		
		// 4. CRITICAL: Allow the end screen layer to process mouse inputs while the game is paused
		_endScreenLayer.ProcessMode = ProcessModeEnum.Always;

		// Keep it hidden when the level first loads
		_endScreenLayer.Visible = false;
	}

	public void CheckForGameOver(int columnLandedIn)
	{
		// Check the bottom-most cell instead of the top
		bool isColumnFull = CheckIfBottomCellIsOccupied(columnLandedIn); 

		if (isColumnFull)
		{
			TriggerGameOver();
		}
	}

	private bool CheckIfBottomCellIsOccupied(int columnIndex)
	{
		// Calculate the index for the bottom row (Row 4) of this specific column
		int bottomCellIndex = ((TotalRows - 1) * TotalColumns) + columnIndex;
		
		// If the bottom cell in the given column is no longer the empty colour, the column is full
		if (_cells[bottomCellIndex] != null)
		{
			return _cells[bottomCellIndex].Color != _emptyColour;
		}
		return false;
	}

private void TriggerGameOver()
	{
		// 1. Freeze the background world (stops mummies, movement, and timers)
		GetTree().Paused = true;
		
		// 2. Fetch the live scores directly from the ScoreManager
		int realPlayer1Score = 0;
		int realPlayer2Score = 0;
		
		var scoreManager = GetTree().CurrentScene.GetNodeOrNull<ScoreManager>("HUD/Scoreboard");
		
		if (scoreManager != null)
		{
			// Read from the new public properties we just exposed
			realPlayer1Score = scoreManager.Player1Score; 
			realPlayer2Score = scoreManager.Player2Score;
		}
		else
		{
			GD.PrintErr("CRITICAL: Could not locate HUD/Scoreboard to retrieve final points.");
		}
		
		// 3. Make the overlay screen visible to the players
		_endScreenLayer.Visible = true;

		// 4. Evaluate the live scores and construct the text announcements
		if (realPlayer1Score > realPlayer2Score)
		{
			int margin = realPlayer1Score - realPlayer2Score;
			_winnerText.Text = "PLAYER 1 WINS THE GAME!";
			_winnerText.Modulate = new Color("ff4a4a"); 

			_scoreBreakdown.Text = $"Player 1 Score: {realPlayer1Score} pts\n" +
								   $"Player 2 Score: {realPlayer2Score} pts\n\n" +
								   $"Victory Margin: +{margin} points!";
		}
		else if (realPlayer2Score > realPlayer1Score)
		{
			int margin = realPlayer2Score - realPlayer1Score;
			_winnerText.Text = "PLAYER 2 WINS THE GAME!";
			_winnerText.Modulate = new Color("4a90ff"); 

			_scoreBreakdown.Text = $"Player 2 Score: {realPlayer2Score} pts\n" +
								   $"Player 1 Score: {realPlayer1Score} pts\n\n" +
								   $"Victory Margin: +{margin} points!";
		}
		else
		{
			_winnerText.Text = "IT'S A DRAW!";
			_winnerText.Modulate = new Color("ffffff"); 

			_scoreBreakdown.Text = $"Both players finished with an identical score of {realPlayer1Score} points!\n" +
								   "The grid has overflowed without a decisive victor.";
		}
	}

	private void OnReplayButtonPressed()
	{
		// Release pause engine state first, then reload clean layout
		GetTree().Paused = false;
		GetTree().ReloadCurrentScene();
	}

	public void ProcessMummyDeath(Vector2 deathPosition, int playerId)
	{
		// 1. Calculate target column
		float columnWidth = 800f / TotalColumns;
		int targetColumn = Mathf.Clamp((int)(deathPosition.X / columnWidth), 0, TotalColumns - 1);

		// 2. Find the topmost available row
		int targetRow = -1;
		for (int row = 0; row < TotalRows; row++)
		{
			if (_gridData[targetColumn, row] == 0)
			{
				targetRow = row;
				break; 
			}
		}

		if (targetRow == -1) return;

		// 3. Temporarily reserve the slot so concurrent ghosts don't overlap
		_gridData[targetColumn, targetRow] = -1;

		// 4. Calculate the exact visual center of the target UI cell
		int cellIndex = (targetRow * TotalColumns) + targetColumn;
		Vector2 targetVisualPosition = _cells[cellIndex].GlobalPosition + (_cells[cellIndex].Size / 2f);

		// 5. Spawn and animate the ghost
		if (MummyGhostScene == null)
		{
			GD.PrintErr("CRITICAL: MummyGhostScene is not assigned in the Inspector!");
			FinalizeGridFill(targetColumn, targetRow, playerId, cellIndex); // Fallback
			return;
		}

		Node2D ghostInstance = MummyGhostScene.Instantiate<Node2D>();
		
		var collisionShape = ghostInstance.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
		{
			collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		// Add it directly to the GridManager (the UI) instead of the background world
		AddChild(ghostInstance);

		// Set the position AFTER adding it to the tree to ensure UI coordinates align
		ghostInstance.GlobalPosition = deathPosition;

		// Force the sorting order to be mathematically higher than the grid cells
		ghostInstance.ZIndex = 100;

		Tween tween = GetTree().CreateTween();
		tween.TweenProperty(ghostInstance, "global_position", targetVisualPosition, 0.8f)
			 .SetTrans(Tween.TransitionType.Sine)
			 .SetEase(Tween.EaseType.InOut);

		// 6. Finalise the matrix only when the animation completes
		tween.Finished += () => 
		{
			ghostInstance.QueueFree();
			FinalizeGridFill(targetColumn, targetRow, playerId, cellIndex);
			CheckForGameOver(targetColumn); // Check for game over after placement is finalized
		};
	}

	private void FinalizeGridFill(int targetColumn, int targetRow, int playerId, int cellIndex)
	{
		_gridData[targetColumn, targetRow] = playerId;
		_cells[cellIndex].Color = (playerId == 1) ? _player1Colour : _player2Colour;
		CheckForMatches(playerId);
	}

	private void CheckForMatches(int playerId)
	{
		HashSet<(int col, int row)> cellsToClear = new HashSet<(int col, int row)>();

		// Horizontal Scan (-)
		for (int row = 0; row < TotalRows; row++)
		{
			for (int col = 0; col < TotalColumns - 2; col++)
			{
				int id = _gridData[col, row];
				if (id > 0 && id == _gridData[col + 1, row] && id == _gridData[col + 2, row])
				{
					cellsToClear.Add((col, row));
					cellsToClear.Add((col + 1, row));
					cellsToClear.Add((col + 2, row));
				}
			}
		}

		// Vertical Scan (|)
		for (int col = 0; col < TotalColumns; col++)
		{
			for (int row = 0; row < TotalRows - 2; row++)
			{
				int id = _gridData[col, row];
				if (id > 0 && id == _gridData[col, row + 1] && id == _gridData[col, row + 2])
				{
					cellsToClear.Add((col, row));
					cellsToClear.Add((col, row + 1));
					cellsToClear.Add((col, row + 2));
				}
			}
		}

		// Diagonal Down-Right Scan (\)
		for (int col = 0; col < TotalColumns - 2; col++)
		{
			for (int row = 0; row < TotalRows - 2; row++)
			{
				int id = _gridData[col, row];
				if (id > 0 && id == _gridData[col + 1, row + 1] && id == _gridData[col + 2, row + 2])
				{
					cellsToClear.Add((col, row));
					cellsToClear.Add((col + 1, row + 1));
					cellsToClear.Add((col + 2, row + 2));
				}
			}
		}

		// Diagonal Up-Right Scan (/)
		for (int col = 0; col < TotalColumns - 2; col++)
		{
			for (int row = 2; row < TotalRows; row++)
			{
				int id = _gridData[col, row];
				if (id > 0 && id == _gridData[col + 1, row - 1] && id == _gridData[col + 2, row - 2])
				{
					cellsToClear.Add((col, row));
					cellsToClear.Add((col + 1, row - 1));
					cellsToClear.Add((col + 2, row - 2));
				}
			}
		}

		if (cellsToClear.Count > 0)
		{
			var scoreManager = GetTree().CurrentScene.GetNodeOrNull<ScoreManager>("HUD/Scoreboard");
			
			if (scoreManager != null)
			{
				int pointsToAward = cellsToClear.Count * 100;
				scoreManager.AddScore(playerId, pointsToAward);
			}
			else
			{
				GD.PrintErr($"CRITICAL: Could not find Scoreboard at path 'HUD/Scoreboard' to award points.");
			}

			TriggerMatchClear(cellsToClear);
		}
	}

	private async void TriggerMatchClear(HashSet<(int col, int row)> matchedCells)
	{
		foreach (var cell in matchedCells)
		{
			int cellIndex = (cell.row * TotalColumns) + cell.col;
			_cells[cellIndex].Color = new Color("ffffff"); 
		}

		await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);

		foreach (var cell in matchedCells)
		{
			_gridData[cell.col, cell.row] = 0;
			
			int cellIndex = (cell.row * TotalColumns) + cell.col;
			_cells[cellIndex].Color = _emptyColour;
		}
	}
}
