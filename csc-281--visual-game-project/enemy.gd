extends CharacterBody2D
@onready var player = get_parent().get_node("player")
@onready var enemy = get_parent().get_node("enemy")



# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	enemy.position = Vector2(250,250)
	print(enemy.position)

var speed = 200

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _physics_process(delta: float) -> void:
	print(enemy.position)
	print(player.position)
	enemy.position = position.move_toward(player.position, speed)
