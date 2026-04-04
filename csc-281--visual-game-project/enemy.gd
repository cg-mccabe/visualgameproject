extends CharacterBody2D

@onready var player = get_parent().get_node("player")

var speed = 200

func _ready() -> void:
	print("Player ref: ", player)
	print("Player name: ", player.name)
	position = Vector2(250, 250)

func _physics_process(delta: float) -> void:
	position = position.move_toward(player.position, speed * delta)
