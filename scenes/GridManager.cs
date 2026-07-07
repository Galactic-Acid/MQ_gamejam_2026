using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : GridContainer
{
	private const int TotalColumns = 6;
	private const int TotalRows = 5;
	
	// 0 = Empty, 1 = Player 1 (Red), 2 = Player 2 (Blue)
	private int[,] _gridData = new int[TotalColumns, TotalRows];
	private ColorRect[] _cells = new ColorRect[30];

	// The base colour of your empty grid squares (adjust hex to match your UI)
	private Color _emptyColour = new Color("000000"); 
	private Color _player1Colour = new Color("ff4a4a");
	private Color _player2Colour = new Color("4a90ff");

	public override void _Ready()
	{
		// Cache all 30 ColorRect UI children on startup
		for (int i = 0; i < GetChildCount(); i++)
		{
			_cells[i] = GetChild<ColorRect>(i);
			_cells[i].Color = _emptyColour; 
		}
	}

	public void HandleMummyDeath(Vector2 deathPosition, int playerId)
	{
		// 1. Calculate which of the 6 columns the mummy died beneath
		float columnWidth = 800f / TotalColumns;
		int targetColumn = Mathf.Clamp((int)(deathPosition.X / columnWidth), 0, TotalColumns - 1);

		// 2. Find the topmost available row (Index 0 is the top of the screen)
		int targetRow = -1;
		for (int row = 0; row < TotalRows; row++)
		{
			if (_gridData[targetColumn, row] == 0)
			{
				targetRow = row;
				break; // Found the highest empty slot
			}
		}

		// Exit if the column is already completely full
		if (targetRow == -1) return;

		// 3. Register the placement in the matrix
		_gridData[targetColumn, targetRow] = playerId;

		// 4. Update the visual UI cell
		int cellIndex = (targetRow * TotalColumns) + targetColumn;
		_cells[cellIndex].Color = (playerId == 1) ? _player1Colour : _player2Colour;

		// 5. Verify if this new placement created a line of 3 and pass the player ID
		CheckForMatches(playerId);
	}

	// Added playerId parameter to the signature
	private void CheckForMatches(int playerId)
	{
		// We use a HashSet to store matched cells to handle overlapping lines cleanly
		HashSet<(int col, int row)> cellsToClear = new HashSet<(int col, int row)>();

		// Horizontal Scan (-)
		for (int row = 0; row < TotalRows; row++)
		{
			for (int col = 0; col < TotalColumns - 2; col++)
			{
				int id = _gridData[col, row];
				if (id != 0 && id == _gridData[col + 1, row] && id == _gridData[col + 2, row])
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
				if (id != 0 && id == _gridData[col, row + 1] && id == _gridData[col, row + 2])
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
				if (id != 0 && id == _gridData[col + 1, row + 1] && id == _gridData[col + 2, row + 2])
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
				if (id != 0 && id == _gridData[col + 1, row - 1] && id == _gridData[col + 2, row - 2])
				{
					cellsToClear.Add((col, row));
					cellsToClear.Add((col + 1, row - 1));
					cellsToClear.Add((col + 2, row - 2));
				}
			}
		}

		// Process clears and award points
		if (cellsToClear.Count > 0)
		{
			// Bypass the root viewport tree and search the active CurrentScene directly
			var playerNode = GetTree().CurrentScene.GetNodeOrNull<Player>($"Player{playerId}");
			
			if (playerNode != null)
			{
				// Dynamically calculate points: 100 points per cleared block
				// 3 blocks = 300, 4 blocks = 400, 5 block T-shape = 500
				int pointsToAward = cellsToClear.Count * 100;
				playerNode.AddScore(pointsToAward);
			}
			else
			{
				GD.PrintErr($"CRITICAL: Could not find Player {playerId} at path 'Player{playerId}' to award points.");
			}

			// Clear the visual grid and matrix
			TriggerMatchClear(cellsToClear);
		}
	}

	private async void TriggerMatchClear(HashSet<(int col, int row)> matchedCells)
	{
		// 1. Instantly change the visual colour of the matched cells to pure white
		foreach (var cell in matchedCells)
		{
			int cellIndex = (cell.row * TotalColumns) + cell.col;
			_cells[cellIndex].Color = new Color("ffffff"); 
		}

		// 2. Tell this specific method to pause for 0.25 seconds
		await ToSignal(GetTree().CreateTimer(0.25f), SceneTreeTimer.SignalName.Timeout);

		// 3. Once the timer finishes, clear the backend data and reset the visual colour
		foreach (var cell in matchedCells)
		{
			_gridData[cell.col, cell.row] = 0;
			
			int cellIndex = (cell.row * TotalColumns) + cell.col;
			_cells[cellIndex].Color = _emptyColour;
		}
	}
}
