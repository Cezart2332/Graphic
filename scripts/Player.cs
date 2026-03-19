using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float MouseSensitivity = 0.003f;
	[Export] public float CameraMinAngle = -60f;
	[Export] public float CameraMaxAngle = 30f;

	private float _gravity = ProjectSettings
		.GetSetting("physics/3d/default_gravity").AsSingle();

	private SpringArm3D _springArm;
	private AnimationPlayer _animPlayer;
	private string _currentAnim = "";

	// Store camera rotation separately
	private float _cameraPitch = 0f;
	private float _cameraYaw = 0f;

	public override void _Ready()
	{
		_springArm = GetNode<SpringArm3D>("SpringArm3D");
		_animPlayer = GetNode<AnimationPlayer>("AuxScene/AnimationPlayer");
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

private void PlayAnim(string name, bool loop = true)
{
	if (_currentAnim == name) return;
	_currentAnim = name;
	_animPlayer.Play(name);

	// Set loop mode on the animation
	var anim = _animPlayer.GetAnimation(name);
	if (anim != null)
		anim.LoopMode = loop ? Animation.LoopModeEnum.Linear : Animation.LoopModeEnum.None;
}


	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
			Input.MouseMode = Input.MouseModeEnum.Visible;

		if (@event is InputEventMouseMotion mouseMotion)
		{
			// Accumulate camera angles independently
			_cameraYaw -= mouseMotion.Relative.X * MouseSensitivity;
			_cameraPitch -= mouseMotion.Relative.Y * MouseSensitivity;
			_cameraPitch = Mathf.Clamp(
				_cameraPitch,
				Mathf.DegToRad(CameraMinAngle),
				Mathf.DegToRad(CameraMaxAngle)
			);

			// Apply ONLY to SpringArm, not to the player body
			_springArm.GlobalRotation = new Vector3(
				_cameraPitch,
				_cameraYaw + Mathf.Pi,   // ← adds 180 degrees so it faces the character
				0
			);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		if (!IsOnFloor())
			velocity.Y -= _gravity * (float)delta;

		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
			PlayAnim("Jumping",false);
		}

		// Movement direction based on camera yaw angle
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		// Use camera yaw so movement matches where camera faces
		Vector3 camForward = new Vector3(Mathf.Sin(_cameraYaw), 0, Mathf.Cos(_cameraYaw));
		Vector3 camRight = new Vector3(Mathf.Cos(_cameraYaw), 0, -Mathf.Sin(_cameraYaw));
		Vector3 direction = (-camRight * inputDir.X - camForward * inputDir.Y).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;

			// Rotate player MESH to face movement — no longer conflicts with camera
			float targetAngle = Mathf.Atan2(direction.X, direction.Z);
			Rotation = new Vector3(
				Rotation.X,
				Mathf.LerpAngle(Rotation.Y, targetAngle, 0.15f),
				Rotation.Z
			);

			if (IsOnFloor()) PlayAnim("Walk");
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(velocity.Z, 0, Speed);

			if (IsOnFloor()) PlayAnim("Idle");
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
