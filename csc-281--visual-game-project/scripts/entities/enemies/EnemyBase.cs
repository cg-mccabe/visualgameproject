using Godot;
 
/// <summary>
/// Base class for all enemies. Handles navigation toward the player using NavigationAgent2D.
/// Extend this class to create specific enemy types with unique stats and behavior.
/// </summary>
public partial class EnemyBase : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 80f;
	[Export] public float Health { get; set; } = 30f;
	[Export] public float Damage { get; set; } = 10f;
 
	protected CharacterBody2D Player;
 
	private NavigationAgent2D _navAgent;
	private float _currentHealth;
 
	public override void _Ready()
	{
		_navAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_currentHealth = Health;
 
		// Wait one physics frame before pathfinding to let the nav mesh bake
		CallDeferred(MethodName.FindPlayer);
 
		_navAgent.VelocityComputed += OnVelocityComputed;
	}
 
	private void FindPlayer()
	{
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count > 0)
			Player = players[0] as CharacterBody2D;
	}
 
	public override void _PhysicsProcess(double delta)
	{
		if (Player == null) return;
 
		_navAgent.TargetPosition = Player.GlobalPosition;
 
		var nextPos = _navAgent.GetNextPathPosition();
		var direction = (nextPos - GlobalPosition).Normalized();
 
		// Use avoidance if enabled; otherwise set velocity directly
		if (_navAgent.AvoidanceEnabled)
			_navAgent.Velocity = direction * Speed;
		else
		{
			Velocity = direction * Speed;
			MoveAndSlide();
		}
	}
 
	// Called by NavigationAgent2D when avoidance is enabled
	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		Velocity = safeVelocity;
		MoveAndSlide();
	}
 
	/// <summary>Apply damage to this enemy. Returns true if the enemy died.</summary>
	public virtual bool TakeDamage(float amount)
	{
		_currentHealth -= amount;
		OnDamaged(amount);
		if (_currentHealth <= 0)
		{
			OnDeath();
			return true;
		}
		return false;
	}
 
	/// <summary>Override to play a hit animation, flash sprite, etc.</summary>
	protected virtual void OnDamaged(float amount) { }
 
	/// <summary>Override to drop loot, play death VFX, emit signals, etc.</summary>
	protected virtual void OnDeath()
	{
		QueueFree();
	}
}
