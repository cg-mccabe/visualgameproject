using Godot;

/// <summary>
/// EnemyB — A slow, tanky bruiser. Hits hard and absorbs punishment.
/// KnockbackResistance (0–1) scales down incoming knockback; 0 = full force, 1 = no knockback.
/// Attach to EnemyB.tscn which inherits from Enemy.tscn.
/// </summary>
public partial class EnemyB : EnemyBase
{
	/// <summary>
	/// Default 0.6 means EnemyB only receives 40 % of the normal knockback force.
	/// </summary>
	[Export] public float KnockbackResistance { get; set; } = 0.6f;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		Speed  = 45f;
		Health = 50f;
		Damage = 25f;
		base._Ready();

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
	}

	/// <summary>Override to reduce incoming knockback by KnockbackResistance before passing to base.</summary>
	public override bool TakeDamage(float amount, Vector2 knockback = default)
	{

		return base.TakeDamage(amount, knockback);
	}

	protected override void OnDamaged(float amount)
	{
		_sprite.Modulate = Colors.White;
		_ = ResetColorAfterDelay();
	}

	protected override void OnDeath()
	{
		_sprite.Play("Dying");
		GameManager.Instance?.AddScore(30);
		GD.Print("EnemyB died — awarded 30 points");
		base.OnDeath();
	}

	private async System.Threading.Tasks.Task ResetColorAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree())
			_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
	}
}
