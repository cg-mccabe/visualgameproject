using Godot;

/// <summary>
/// Root script for the main level. Handles game state transitions (pause, game over).
/// Listens to the Player's signals to react to death.
/// </summary>
public partial class Level : Node2D
{
	private Player _player;
	private CanvasLayer _hud;

	public override void _Ready()
	{
		_player = GetNode<Player>("Player");
		_hud = GetNode<CanvasLayer>("HUD");

		// Wire up player signals
		_player.HealthChanged += OnPlayerHealthChanged;
		_player.PlayerDied += OnPlayerDied;
	}

	private void OnPlayerHealthChanged(float current, float max)
	{
		// Forward to HUD — e.g., _hud.GetNode<ProgressBar>("HealthBar").Value = current / max * 100;
		GD.Print($"HP: {current}/{max}");
	}

	private void OnPlayerDied()
	{
		GD.Print("Game Over — load game over screen here");
		// Example: GetTree().ChangeSceneToFile("res://scenes/ui/GameOver.tscn");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
			GetTree().Paused = !GetTree().Paused;
	}
}
