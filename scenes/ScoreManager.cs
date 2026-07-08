using Godot;
using System;

public partial class ScoreManager : Godot.Control
{
	private Label _p1Label;
	private Label _p2Label;

	private int _p1Score = 0;
	private int _p2Score = 0;

public int Player1Score => _p1Score;
public int Player2Score => _p2Score;


	public override void _Ready()
	{
		// Locate the label nodes inside the UI tree
		_p1Label = GetNode<Label>("P1Score");
		_p2Label = GetNode<Label>("P2Score");

		// Initialise text values
		UpdateScoreUI();
	}

	public void AddScore(int playerNumber, int points)
	{
		if (playerNumber == 1)
		{
			_p1Score += points;
			_p1Score = Mathf.Max(0, _p1Score);
		}
		else if (playerNumber == 2)
		{
			_p2Score += points;
			_p2Score = Mathf.Max(0, _p2Score); //>0
		}

		UpdateScoreUI();
	}

	private void UpdateScoreUI()
	{
		if (_p1Label != null) _p1Label.Text = $"Player 1: {_p1Score}";
		if (_p2Label != null) _p2Label.Text = $"Player 2: {_p2Score}";
	}
}
