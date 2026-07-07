using Godot;
using System;

public partial class InfoManager : CanvasLayer
{
	private TextureButton _infoButton;
	private Panel _infoPopup;
	private Button _closeButton;

	public override void _Ready()
	{
		_infoButton = GetNode<TextureButton>("InfoButton");
		_infoPopup = GetNode<Panel>("InfoPopup");
		_closeButton = GetNode<Button>("InfoPopup/MarginContainer/VBoxContainer/CloseButton");

		_infoButton.Pressed += OnInfoButtonPressed;
		_closeButton.Pressed += OnCloseButtonPressed;

		// Force the instructions menu to display immediately every time the game runs
		ShowInstructionsMenu();
	}

	private void ShowInstructionsMenu()
	{
		_infoPopup.Visible = true;
		
		// Optional: Pauses the background action (mummies spawning/moving) 
		// so the player can read the layout safely
		GetTree().Paused = true; 
	}

	private void OnInfoButtonPressed()
	{
		ShowInstructionsMenu();
	}

	private void OnCloseButtonPressed()
	{
		_closeButton.ReleaseFocus();
		_infoPopup.Visible = false;
		GetTree().Paused = false; 
	}
}
