using Godot;
 
/// <summary>
/// EnemyA — A fast, fragile chaser. Rushes the player quickly but dies easily.
/// Attach to EnemyA.tscn which inherits from Enemy.tscn.
/// </summary>
public partial class EnemyA : EnemyBase
{
	// Stats are set via [Export] in the Inspector or overridden here.
	// EnemyA is fast and weak — good for swarming the player.
	[Export] public float FlashDuration { get; set; } = 0.1f;
 
	private AnimatedSprite2D _sprite;
	private bool _isFlashing = false;
 
	public override void _Ready()
	{
		Speed = 130f;
		Health = 15f;
		Damage = 5f;
		base._Ready();
 
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Walking");
	}
 
	protected override void OnDamaged(float amount)
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Getting Hit");
		if (!_isFlashing)
			FlashWhite();
	}
 
	protected override void OnDeath()
	{
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.Play("Dying");
		// Emit a signal or call GameManager here to award score
		GD.Print("EnemyA died — award points here");
		base.OnDeath();
	}
 
	private async void FlashWhite()
	{
		_isFlashing = true;
		_sprite.Modulate = Colors.White * 3f; // Overbright flash
		await ToSignal(GetTree().CreateTimer(FlashDuration), SceneTreeTimer.SignalName.Timeout);
		_sprite.Modulate = Colors.White;
		_isFlashing = false;
	}
}
