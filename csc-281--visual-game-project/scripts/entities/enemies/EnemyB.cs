using Godot;
 
/// <summary>
/// EnemyB — A slow, tanky bruiser. Hits hard and absorbs punishment.
/// Attach to EnemyB.tscn which inherits from Enemy.tscn.
/// </summary>
public partial class EnemyB : EnemyBase
{
	[Export] public float KnockbackResistance { get; set; } = 0.5f;
 
	private AnimatedSprite2D _sprite;
 
	public override void _Ready()
	{
		Speed = 45f;
		Health = 80f;
		Damage = 25f;
		base._Ready();
 
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
 
	}
 
	protected override void OnDamaged(float amount)
	{
		// Briefly brighten to show a hit
		_sprite.Modulate = Colors.White;
		_ = ResetColorAfterDelay();
	}
 
	protected override void OnDeath()
	{
		_sprite.Play("Dying");
		GD.Print("EnemyB died — award more points here");
		base.OnDeath();
	}
 
	private async System.Threading.Tasks.Task ResetColorAfterDelay()
	{
		await ToSignal(GetTree().CreateTimer(0.08f), SceneTreeTimer.SignalName.Timeout);
		if (IsInsideTree()) // Guard against calling after QueueFree
			_sprite.Modulate = new Color(1f, 0.4f, 0.4f);
	}
}
 
