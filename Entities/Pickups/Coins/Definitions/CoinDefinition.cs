using Godot;
using System;

[GlobalClass]
public partial class CoinDefinition : Resource
{
    [Export]
    public string DisplayName { get; set; } = "Coin";
    
    [Export(PropertyHint.Range, "1,999999,1")]
    public int Amount {get; set; } = 1;

    [Export(PropertyHint.MultilineText)]
    public string PickupMessage { get; set; } = "Picked up ${name} + ${amount}";

    [Export]
    public PackedScene ModelScene { get; set; }

    [Export]
    public AudioStream PickupSfx { get; set; }
}