using Godot;
using System.Collections.Generic;

/// <summary>
/// Player controller. Handles 8-directional movement, health, melee attacks, and damage response.
/// Added to the "player" group so enemies can find it via GetNodesInGroup("player").
///
/// The player is constrained to stay inside the navigation polygon each physics frame.
/// BoundaryPolygon matches the NavigationPolygon outline in Level.tscn — update it there
/// if the nav region changes again.
///
/// </summary>
public partial class Player : CharacterBody2D
{
	// ── Movement ──────────────────────────────────────────────────────────────
	[Export] public float Speed { get; set; } = 200f;

	// ── Boundary polygon (matches NavigationPolygon outline in Level.tscn) ────
	// Outline from Level.tscn:
	//   PackedVector2Array(-1538,10, -1572,2082, 173,2063, 1311,2068, 1331,958, 1306,7)
	// BoundsInset shrinks the polygon inward so the sprite doesn't clip the edge.
	[Export] public float BoundsInset { get; set; } = 14f;

	private static readonly Vector2[] BoundaryPolygon =
	{
		new(-1538f,   10f),
		new(-1572f, 2082f),
		new(  173f, 2063f),
		new( 1311f, 2068f),
		new( 1331f,  958f),
		new( 1306f,    7f),
	};

	// Inset (shrunk) version built once in _Ready
	private Vector2[] _insetPolygon;

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
	[Export] public float AttackRange    { get; set; } = 40f;
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

		_insetPolygon = BuildInsetPolygon(BoundaryPolygon, BoundsInset);
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

		// ── Constrain to navigation polygon ───────────────────────────────────
		GlobalPosition = ClampToPolygon(GlobalPosition, _insetPolygon);

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

	// ── Polygon boundary helpers ──────────────────────────────────────────────

	/// <summary>
	/// Returns the centroid of a polygon.
	/// </summary>
	private static Vector2 Centroid(Vector2[] poly)
	{
		var c = Vector2.Zero;
		foreach (var v in poly) c += v;
		return c / poly.Length;
	}

	/// <summary>
	/// Builds an inset (shrunk) copy of a convex polygon by moving each vertex
	/// toward the centroid by <paramref name="amount"/> pixels.
	/// Works correctly for the roughly-convex nav outline in this level.
	/// </summary>
	private static Vector2[] BuildInsetPolygon(Vector2[] poly, float amount)
	{
		var center = Centroid(poly);
		var result = new Vector2[poly.Length];
		for (int i = 0; i < poly.Length; i++)
		{
			var dir = (center - poly[i]).Normalized();
			result[i] = poly[i] + dir * amount;
		}
		return result;
	}

	/// <summary>
	/// Ray-casting point-in-polygon test.
	/// Returns true if <paramref name="point"/> is inside <paramref name="poly"/>.
	/// </summary>
	private static bool PointInPolygon(Vector2 point, Vector2[] poly)
	{
		bool inside = false;
		int n = poly.Length;
		for (int i = 0, j = n - 1; i < n; j = i++)
		{
			var vi = poly[i];
			var vj = poly[j];
			if ((vi.Y > point.Y) != (vj.Y > point.Y) &&
				point.X < (vj.X - vi.X) * (point.Y - vi.Y) / (vj.Y - vi.Y) + vi.X)
			{
				inside = !inside;
			}
		}
		return inside;
	}

	/// <summary>
	/// If <paramref name="point"/> is outside <paramref name="poly"/>, returns the
	/// closest point on the polygon boundary. Otherwise returns the point unchanged.
	/// </summary>
	private static Vector2 ClampToPolygon(Vector2 point, Vector2[] poly)
	{
		if (PointInPolygon(point, poly))
			return point;

		// Find the closest point on any edge of the polygon
		float   bestDist  = float.MaxValue;
		Vector2 bestPoint = point;
		int     n         = poly.Length;

		for (int i = 0, j = n - 1; i < n; j = i++)
		{
			Vector2 closest = ClosestPointOnSegment(point, poly[j], poly[i]);
			float   dist    = point.DistanceSquaredTo(closest);
			if (dist < bestDist)
			{
				bestDist  = dist;
				bestPoint = closest;
			}
		}
		return bestPoint;
	}

	/// <summary>
	/// Returns the closest point on segment [a, b] to <paramref name="p"/>.
	/// </summary>
	private static Vector2 ClosestPointOnSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float   t  = ab.LengthSquared();
		if (t == 0f) return a;
		t = Mathf.Clamp((p - a).Dot(ab) / t, 0f, 1f);
		return a + ab * t;
	}

	// ── Melee Attack ──────────────────────────────────────────────────────────

	private async void StartAttack()
	{
		_isAttacking      = true;
		_attackOnCooldown = true;

		_sprite.Play("attack");

		float halfAngleRad = Mathf.DegToRad(AttackAngleDeg / 2f);

		foreach (var node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not EnemyBase enemy) continue;

			Vector2 toEnemy = enemy.GlobalPosition - GlobalPosition;
			float   dist    = toEnemy.Length();

			if (dist > AttackRange) continue;

			float angle = _facingDirection.AngleTo(toEnemy.Normalized());
			if (Mathf.Abs(angle) > halfAngleRad) continue;

			enemy.TakeDamage(AttackDamage, toEnemy.Normalized() * KnockbackForce);
			GD.Print($"Hit {enemy.Name} for {AttackDamage} dmg (dist={dist:F0}px)");
		}

		await ToSignal(GetTree().CreateTimer(AttackCooldown), SceneTreeTimer.SignalName.Timeout);
		_isAttacking      = false;
		_attackOnCooldown = false;
	}

	// ── Receiving Damage ──────────────────────────────────────────────────────

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
