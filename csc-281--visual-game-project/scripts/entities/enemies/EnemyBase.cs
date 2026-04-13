using Godot;

/// <summary>
/// Base class for all enemies. Handles navigation, health, knockback,
/// and contact damage to the player.
///
/// Registers itself in the "enemies" group so Player.cs can find it
/// via GetTree().GetNodesInGroup("enemies") during a melee attack scan.
/// </summary>
public partial class EnemyBase : CharacterBody2D
{
	[Export] public float Speed   { get; set; } = 80f;
	[Export] public float Health  { get; set; } = 30f;
	[Export] public float Damage  { get; set; } = 10f;

	/// <summary>Radius (px) within which the enemy deals contact damage to the player.</summary>
	[Export] public float ContactRadius  { get; set; } = 22f;

	/// <summary>Seconds between contact-damage ticks.</summary>
	[Export] public float DamageInterval { get; set; } = 0.8f;

	/// <summary>Fraction of knockback bled off per physics frame (higher = faster stop).</summary>
	[Export] public float KnockbackDecay { get; set; } = 0.15f;

	protected CharacterBody2D Player;

	private NavigationAgent2D _navAgent;
	private float   _currentHealth;
	private Vector2 _knockbackVelocity = Vector2.Zero;
	private float   _damageCooldown    = 0f;

	// ── Ready ─────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		AddToGroup("enemies");   // lets Player.cs find this node during attack scans

		_navAgent      = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_currentHealth = Health;

		CallDeferred(MethodName.FindPlayer);
		_navAgent.VelocityComputed += OnVelocityComputed;
	}

	private void FindPlayer()
	{
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count > 0)
			Player = players[0] as CharacterBody2D;
	}

	// ── Physics ───────────────────────────────────────────────────────────────

	public override void _PhysicsProcess(double delta)
	{
		if (_damageCooldown > 0f)
			_damageCooldown -= (float)delta;

		// ── Knockback takes priority over navigation ───────────────────────────
		if (_knockbackVelocity.LengthSquared() > 4f)
		{
			_knockbackVelocity = _knockbackVelocity.Lerp(Vector2.Zero, KnockbackDecay);
			Velocity = _knockbackVelocity;
			MoveAndSlide();
			return;
		}
		_knockbackVelocity = Vector2.Zero;

		// ── Normal navigation ─────────────────────────────────────────────────
		if (Player == null) return;

		_navAgent.TargetPosition = Player.GlobalPosition;
		var dir = (_navAgent.GetNextPathPosition() - GlobalPosition).Normalized();

		if (_navAgent.AvoidanceEnabled)
			_navAgent.Velocity = dir * Speed;
		else
		{
			Velocity = dir * Speed;
			MoveAndSlide();
		}

		// ── Contact damage: distance check, no Area2D needed ──────────────────
		if (_damageCooldown <= 0f &&
		    GlobalPosition.DistanceTo(Player.GlobalPosition) <= ContactRadius)
		{
			if (Player is Player p)
			{
				p.TakeDamage(Damage);
				_damageCooldown = DamageInterval;
				GD.Print($"{Name} dealt {Damage} dmg to player");
			}
		}
	}

	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		Velocity = safeVelocity + _knockbackVelocity;
		MoveAndSlide();
	}

	// ── Public API ────────────────────────────────────────────────────────────

	/// <summary>Apply damage + optional knockback impulse. Returns true if the enemy died.</summary>
	public virtual bool TakeDamage(float amount, Vector2 knockback = default)
	{
		_currentHealth    -= amount;
		_knockbackVelocity = knockback;
		OnDamaged(amount);

		if (_currentHealth <= 0)
		{
			OnDeath();
			return true;
		}
		return false;
	}

	// ── Overridable hooks ─────────────────────────────────────────────────────

	protected virtual void OnDamaged(float amount) { }

	protected virtual void OnDeath() => QueueFree();
}
