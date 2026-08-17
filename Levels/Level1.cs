using Godot;
using System;

public partial class Level1 : Node3D
{
	[Export] private AudioStream _levelMusic;

    public override void _Ready()
    {
        MusicManager.Instance?.PlayMusic(_levelMusic, -4f, 1.25f);
    }
}
