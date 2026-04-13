using Godot;

/// <summary>
/// Player controller. Handles 8-directional movement, health, melee attacks, and damage response.
/// Added to the "player" group so enemies can find it via GetNodesInGroup("player").
///
/// No Area2D or hitbox node is required for attack detection.
/// Attack hits any EnemyBase within AttackRange in the swing direction.
///
/// INPUT MAP (Project → Project Settings → Input Map):
///   Add action "attack"  →  Key Z
/// </summary>
public partial class Player : CharacterBody2D
{
	// ── Movement ──────────────────────────────────────────────────────────────
	[Export] public float Speed { get; set; } = 200f;

	// ── Health ────────────────────────────────────────────────────────────────
	[Export] public float MaxHealth { get; set; } = 100f;

	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void PlayerDiedEventHandler();

	private float _currentHealth;
	private bool  _isDead       = false;
	private bool  _isInvincible = false;

	[Export] public float InvincibilityDuration { get; set; } = 0.6f;

	// ── Combat ────────────────────────────────────────────────────────────────
	[Export] public float AttackDamage   { get; set; } = 20f;
	[Export] public float AttackCooldown { get; set; } = 0.45f;
	[Export] public float KnockbackForce { get; set; } = 280f;

	/// <summary>
	/// Max distance (px) from player centre that a melee hit can reach.
	/// Increase this if attacks feel like they need more range.
	/// </summary>
	[Export] public float AttackRange { get; set; } = 40f;

	/// <summary>
	/// Half-angle of the attack cone in degrees (180 = full semicircle in front).
	/// </summary>
	[Export] public float AttackAngleDeg { get; set; } = 90f;

	private bool    _isAttacking      = false;
	private bool    _attackOnCooldown = false;
	private Vector2 _facingDirection  = Vector2.Right;

	// ── Sprites ───────────────────────────────────────────────────────────────
	private bool             _hasBat = false;
	private AnimatedSprite2D _sprite;

	// ─────────────────────────────────────────────────────────────────────────

	public override void _Ready()
	{
		AddToGroup("player");
		_currentHealth = MaxHealth;

		_sprite = _hasBat
			? GetNode<AnimatedSprite2D>("AnimatedSprite2DBat")
			: GetNode<AnimatedSprite2D>("AnimatedSprite2DGloves");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;

		var direction = GetInputDirection();

		if (direction.LengthSquared() > 0.01f)
		{
			_facingDirection = direction;
			FlipSprite(direction);
			if (!_isAttacking) _sprite.Play("walking");
		}
		else
		{
			if (!_isAttacking) _sprite.Play("normal");
		}

		Velocity = direction * Speed;
		MoveAndSlide();

		if (Input.IsActionJustPressed("attack") && !_isAttacking && !_attackOnCooldown)
			StartAttack();
	}

	// ── Input ─────────────────────────────────────────────────────────────────

	private Vector2 GetInputDirection() =>
		new Vector2(
			Input.GetAxis("ui_left", "ui_right"),
			Input.GetAxis("ui_up", "ui_down")
		).Normalized();

	private void FlipSprite(Vector2 dir)
	{
		if (dir.X != 0) _sprite.FlipH = dir.X > 0;
	}

	// ── Melee Attack ──────────────────────────────────────────────────────────

	private async void StartAttack()
	{
		_isAttacking      = true;
		_attackOnCooldown = true;

		_sprite.Play("attack");

		// Scan all enemies in the scene for ones inside the attack cone
		float halfAngleRad = Mathf.DegToRad(AttackAngleDeg / 2f);

		foreach (var node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not EnemyBase enemy) continue;

			Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
			float   dist    = toEnemy.Length();

			if (dist > AttackRange) continue;

			// Check if the enemy is within the swing arc
			float angle = _facingDirection.AngleTo(toEnemy.Normalized());
			if (Mathf.Abs(angle) > halfAngleRad) continue;

			Vector2 knockbackDir = toEnemy.Normalized();
			enemy.TakeDamage(AttackDamage, knockbackDir * KnockbackForce);
			GD.Print($"Hit {enemy.Name} for {AttackDamage} dmg (dist={dist:F0}px)");
		}

		// Hold attack animation, then reset
		await ToSignal(GetTree().CreateTimer(AttackCooldown), SceneTreeTimer.SignalName.Timeout);
		_isAttacking      = false;
		_attackOnCooldown = false;
	}

	// ── Receiving Damage ──────────────────────────────────────────────────────

	/// <summary>Called by enemies. No knockback on the player.</summary>
	public void TakeDamage(float amount)
	{
		if (_isDead || _isInvincible) return;

		_currentHealth = Mathf.Max(0, _currentHealth - amount);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
		GD.Print($"Player took {amount} dmg — {_currentHealth}/{MaxHealth} HP");

		if (_currentHealth <= 0) Die();
		else StartInvincibility();
	}

	public void Heal(float amount)
	{
		if (_isDead) return;
		_currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
	}

	private void Die()
	{
		_isDead = true;
		EmitSignal(SignalName.PlayerDied);
		GD.Print("Player died");
		GetNode<CollisionShape2D>("CollisionShape2D")
			.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
	}

	private async void StartInvincibility()
	{
		_isInvincible = true;
		BlinkSprite();
		await ToSignal(GetTree().CreateTimer(InvincibilityDuration), SceneTreeTimer.SignalName.Timeout);
		_isInvincible    = false;
		_sprite.Modulate = Colors.White;
	}

	private async void BlinkSprite()
	{
		const float interval = 0.1f;
		while (_isInvincible)
		{
			_sprite.Visible = !_sprite.Visible;
			await ToSignal(GetTree().CreateTimer(interval), SceneTreeTimer.SignalName.Timeout);
		}
		_sprite.Visible = true;
	}
}
