using Godot;

/// <summary>
/// Root script for the main level. Handles game state transitions (pause, game over).
///
/// NOTE: The HUD node in Level.tscn is named "Hud" (not "HUD") — matched here.
///
/// On player death the game waits briefly, stops the spawner, then loads GameOver.tscn.
/// Create res://scenes/ui/GameOver.tscn (a simple Node2D/Control with a "Game Over" label
/// and a restart button that calls GetTree().ChangeSceneToFile("res://scenes/world/Level.tscn")).
/// </summary>
public partial class Level : Node2D
{
	/// <summary>
	/// Path to your Game Over scene. Adjust if your scene lives elsewhere.
	/// </summary>
	[Export] public string GameOverScenePath { get; set; } = "res://scenes/ui/GameOver.tscn";

	/// <summary>Seconds to wait after the player dies before switching scenes.</summary>
	[Export] public float GameOverDelay { get; set; } = 1.5f;

	private Player      _player;
	private Node        _hud;
	private Node2D      _spawner;
	private bool        _gameOverTriggered = false;

	public override void _Ready()
	{
		_player  = GetNode<Player>("Player");
		_hud     = GetNode<Node>("Hud");        // matches the node name in Level.tscn
		_spawner = GetNodeOrNull<Node2D>("Spawner");

		_player.HealthChanged += OnPlayerHealthChanged;
		_player.PlayerDied    += OnPlayerDied;
	}

	private void OnPlayerHealthChanged(float current, float max)
	{
		// Forward to HUD health bar when you wire it up, e.g.:
		// _hud.GetNode<ProgressBar>("HealthBar").Value = current / max * 100f;
		GD.Print($"HP: {current}/{max}");
	}

	private async void OnPlayerDied()
	{
		if (_gameOverTriggered) return;
		_gameOverTriggered = true;

		GD.Print("Player died — game over");

		// Stop new enemies from spawning
		if (_spawner != null)
			_spawner.ProcessMode = ProcessModeEnum.Disabled;

		// Brief pause so the death animation can play
		await ToSignal(GetTree().CreateTimer(GameOverDelay), SceneTreeTimer.SignalName.Timeout);

		// Switch to the Game Over screen
		GetTree().ChangeSceneToFile(GameOverScenePath);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
			GetTree().Paused = !GetTree().Paused;
	}
}
