using Godot;
using System;

// Using Godot.Node explicitly prevents the class name from clashing with the namespace
public partial class Node : Godot.Node 
{
	public override void _Ready()
	{
		Godot.GD.Print("C# Pipeline Functional!");
	}
}
