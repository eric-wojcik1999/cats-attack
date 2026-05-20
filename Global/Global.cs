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
	[Signal]
	public delegate void GameMessageRequestedEventHandler(string message);

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

	public bool AddCurrency(int amount)
	{
		if (amount <= 0)
		{
			GD.PushWarning("Amount < 0.");
			return false;
		} 
		else 
		{
			currency += amount;
			// Emit signal once amount in global instance has changed
			EmitSignal(SignalName.CurrencyChanged, currency);
			GD.Print($"[Global] Currency is now: {currency} (+{amount})");
			return true;
		}
	}

	public void RequestGameMessage(string message)
	{
		if (String.IsNullOrWhiteSpace(message))
		{
			return;
		}

		EmitSignal(SignalName.GameMessageRequested, message);
	}
}
