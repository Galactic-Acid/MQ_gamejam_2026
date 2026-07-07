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

	private Color _emptyColour = new Color("000000"); 
	private Color _player1Colour = new Color("ff4a4a");
	private Color _player2Colour = new Color("4a90ff");

	public override void _Ready()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			_cells[i] = GetChild<ColorRect>(i);
			_cells[i].Color = _emptyColour; 
		}
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

		// 1. Add it directly to the GridManager (the UI) instead of the background world
		AddChild(ghostInstance);

		// 2. Set the position AFTER adding it to the tree to ensure UI coordinates align
		ghostInstance.GlobalPosition = deathPosition;

		// 3. Force the sorting order to be mathematically higher than the grid cells
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
