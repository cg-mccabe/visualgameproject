extends Node2D
@onready var player = get_parent().get_node("player")
@onready var enemy = get_parent().get_node("enemy")

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

var to_player = Vector2.ZERO
var speed = 200

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	print("process")
	to_player = player.global_position - enemy.global_position
	enemy.position += to_player.normalized()*speed*delta
