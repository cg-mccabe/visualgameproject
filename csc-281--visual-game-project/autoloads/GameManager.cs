using Godot;

/// <summary>
/// GameManager — Autoload singleton. Tracks global game state across scenes.
/// Register this in Project > Project Settings > Autoload as "GameManager".
/// </summary>
public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	public int Score { get; private set; } = 0;
	public int Wave { get; private set; } = 1;

	[Signal] public delegate void ScoreChangedEventHandler(int newScore);
	[Signal] public delegate void WaveChangedEventHandler(int newWave);

	public override void _Ready()
	{
		Instance = this;
		ProcessMode = ProcessModeEnum.Always; // Keep running while paused
	}

	public void AddScore(int amount)
	{
		Score += amount;
		EmitSignal(SignalName.ScoreChanged, Score);
	}

	public void NextWave()
	{
		Wave++;
		EmitSignal(SignalName.WaveChanged, Wave);
		GD.Print($"Wave {Wave} started");
	}

	public void Reset()
	{
		Score = 0;
		Wave = 1;
	}
}
