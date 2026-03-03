using Godot;
using System;

public partial class Global : Node
{
	// Creates a globally accessible reference at runtime
	// get; and set; defines getters and setters respectively
 	public static Global Instance { get; private set; }
    [Export] public int currency = 0;

	// Signal used by UI later
	[Signal]
	public delegate void CurrencyChangedEventHandler(int newValue);

	// Built in function that can be called when a Node is added to the Node tree
	// Singelton guard to protect against duplicates
	public override void _EnterTree() 
	{
		if (Instance != null && Instance != this)
		{
			GD.PushWarning("Duplicate Global singleton detected. Freeing duplicate.");
			QueueFree();
			return;
		}

		Instance = this;
	}

	// Built in function that can be called when a Node is removed to the Node tree
	public override void _ExitTree()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void AddCurrency(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning("Amount < 0.");
		} 
		else 
		{
			currency += amount;
			// Emit signal once amount in global instance has changed
			EmitSignal(SignalName.CurrencyChanged, currency);
			GD.Print($"[Global] Currency is now: {currency} (+{amount})");
		}
	}
}
