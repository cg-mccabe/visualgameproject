using Godot;
 
/// <summary>
/// Player controller. Handles 8-directional movement, health, and basic damage response.
/// Add to the "player" group so enemies can find this node via GetNodesInGroup("player").
/// </summary>
public partial class Player : CharacterBody2D
{
	[Export] public float Speed { get; set; } = 200f;
	[Export] public float MaxHealth { get; set; } = 100f;
 
	// Emitted when health changes — connect this to HUD
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void PlayerDiedEventHandler();
 
	private float _currentHealth;
	private bool _isDead = false;
	private bool _isInvincible = false;
	private bool _hasBat = false;
	private bool _hasGloves = false;
	
 
	// Iframes after taking damage
	[Export] public float InvincibilityDuration { get; set; } = 0.6f;
 
	private AnimatedSprite2D _sprite;
	//private AnimatedSprite2D _spriteBat;
 
	public override void _Ready()
	{
		AddToGroup("player");
		_currentHealth = MaxHealth;
		if (_hasBat == false) {
			_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2DGloves");
		} else {
			_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2DBat");
		}
		
		
		
	}
 
	public override void _PhysicsProcess(double delta)
	{
		if (_isDead) return;
 
		var direction = GetInputDirection();
		Velocity = direction * Speed;
		MoveAndSlide();
		FlipSprite(direction);
		
		 if (direction.LengthSquared() > 0.01f) {
			_sprite.Play("walking"); // swap for AnimatedSprite2D if not using AnimationPlayer
		} else {
			_sprite.Play("normal");
		}
		
	}
 
	private Vector2 GetInputDirection()
	{
		return new Vector2(
			Input.GetAxis("ui_left", "ui_right"),
			Input.GetAxis("ui_up", "ui_down")
		).Normalized();
	}
 
	private void FlipSprite(Vector2 direction)
	{
		if (direction.X != 0)
			_sprite.FlipH = direction.X > 0;
	}
 
	/// <summary>
	/// Called by enemies or hazards to deal damage to the player.
	/// Includes invincibility frames to prevent instant death from overlapping enemies.
	/// </summary>
	public void TakeDamage(float amount)
	{
		if (_isDead || _isInvincible) return;
 
		_currentHealth = Mathf.Max(0, _currentHealth - amount);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
 
		GD.Print($"Player took {amount} damage — {_currentHealth}/{MaxHealth} HP remaining");
 
		if (_currentHealth <= 0)
		{
			Die();
		}
		else
		{
			StartInvincibility();
		}
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
		GD.Print("Player died — handle game over here");
		// Disable collision so enemies walk through the corpse
		GetNode<CollisionShape2D>("CollisionShape2D").SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
	}
 
	private async void StartInvincibility()
	{
		_isInvincible = true;
		BlinkSprite(); // Visual feedback
		await ToSignal(GetTree().CreateTimer(InvincibilityDuration), SceneTreeTimer.SignalName.Timeout);
		_isInvincible = false;
		_sprite.Modulate = Colors.White;
	}
 
	private async void BlinkSprite()
	{
		float elapsed = 0f;
		float blinkInterval = 0.1f;
		while (_isInvincible)
		{
			_sprite.Visible = !_sprite.Visible;
			await ToSignal(GetTree().CreateTimer(blinkInterval), SceneTreeTimer.SignalName.Timeout);
			elapsed += blinkInterval;
		}
		_sprite.Visible = true;
	}
}
