using Godot;

/// <summary>
/// Spawns EnemyA and EnemyB at random off-screen edges at a fixed interval.
/// In the Inspector, assign EnemyAScene, EnemyBScene, and EnemyContainer.
/// </summary>
public partial class Spawner : Node2D
{
	[Export] public PackedScene EnemyAScene { get; set; }
	[Export] public PackedScene EnemyBScene { get; set; }
	[Export] public Node2D EnemyContainer { get; set; }
	[Export] public float SpawnInterval { get; set; } = 2.5f;
	[Export] public float SpawnMargin { get; set; } = 32f;

	private Timer _timer;
	private RandomNumberGenerator _rng = new();

	public override void _Ready()
{
	if (EnemyAScene == null && EnemyBScene == null)
	{
		GD.PrintErr("Spawner: No enemy scenes assigned!");
		return;
	}

	if (EnemyContainer == null)
	{
		GD.PrintErr("Spawner: EnemyContainer not assigned! Drag the Enemies node into the Inspector slot.");
		return;
	}

	_timer = new Timer();
	_timer.WaitTime = SpawnInterval;
	_timer.OneShot = false;
	AddChild(_timer);              // Add to tree FIRST
	_timer.Timeout += SpawnEnemy; // Connect AFTER it's in the tree
	_timer.Start();               // Start explicitly instead of Autostart

	GD.Print("Spawner ready.");
}

	private void SpawnEnemy()
	{
		// Pick a random scene, fall back if one is missing
		PackedScene scene;
		if (EnemyAScene != null && EnemyBScene != null)
			scene = _rng.RandiRange(0, 1) == 0 ? EnemyAScene : EnemyBScene;
		else
			scene = EnemyAScene ?? EnemyBScene;

		var enemy = scene.Instantiate<Node2D>();
		EnemyContainer.AddChild(enemy);
		enemy.GlobalPosition = GetSpawnPosition();

		GD.Print($"Spawned {enemy.Name} at {enemy.GlobalPosition} | Total enemies: {EnemyContainer.GetChildCount()}");
	}

	private Vector2 GetSpawnPosition()
	{
		var viewportSize = GetViewportRect().Size;

		// Find the camera to get the visible center of the screen
		var camera = GetViewport().GetCamera2D();
		var center = camera != null ? camera.GlobalPosition : viewportSize / 2f;

		float halfW = viewportSize.X / 2f + SpawnMargin;
		float halfH = viewportSize.Y / 2f + SpawnMargin;

		int edge = _rng.RandiRange(0, 3);
		return edge switch
		{
			0 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y - halfH), // Top
			1 => new Vector2(center.X + halfW, center.Y + _rng.RandfRange(-halfH, halfH)), // Right
			2 => new Vector2(center.X + _rng.RandfRange(-halfW, halfW), center.Y + halfH), // Bottom
			_ => new Vector2(center.X - halfW, center.Y + _rng.RandfRange(-halfH, halfH)), // Left
		};
	}
}
