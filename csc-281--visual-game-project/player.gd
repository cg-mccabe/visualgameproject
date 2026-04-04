extends CharacterBody2D

var speed = 200

<<<<<<< Updated upstream
var speed = 5
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.

#test
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta: float) -> void:
=======
func _physics_process(delta: float) -> void:
>>>>>>> Stashed changes
	var move = Vector2.ZERO

	if Input.is_action_pressed("ui_right"):
		move += Vector2(1, 0)
	if Input.is_action_pressed("ui_left"):
		move += Vector2(-1, 0)
	if Input.is_action_pressed("ui_up"):
		move += Vector2(0, -1)
	if Input.is_action_pressed("ui_down"):
		move += Vector2(0, 1)

	velocity = move.normalized() * speed
	move_and_slide()
