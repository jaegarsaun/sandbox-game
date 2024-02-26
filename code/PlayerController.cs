using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;

public sealed class PlayerController : Component
{
	// Variables
	[Property] public float Health { get; set; } = 100f;

	[Property] public float MaxHealth { get; set; } = 500f;

	[Property] public float Armor { get; set; } = 0f;

	[Property] public float MaxArmor { get; set; } = 100f;

	[Property] public List<string> Inventory { get; set; } = new List<string>
	{
		"weapon_pistol"
	};

	public int ActiveSlot = 0;

	public int Slots => 9;
	// Movement Properties
	[Property, Group("Movement")] public float GroundControl { get; set; } = 4.0f;
	[Property, Group("Movement")] public float AirControl { get; set; } = 0.1f;
	[Property, Group("Movement")] public float MaxForce { get; set; } = 50f;
	[Property, Group("Movement")] public float Speed { get; set; } = 160f;
	[Property, Group("Movement")] public float RunSpeed { get; set; } = 290f;
	[Property, Group("Movement")] public float CrouchSpeed { get; set; } = 90f;
	[Property, Group("Movement")] public float JumpForce { get; set; } = 400f;
	
	// Object References
	[Property] public GameObject Head { get; set; }
	[Property] public GameObject Body { get; set; }
	
	// Member Variables
	public Vector3 WishVelocity = Vector3.Zero;
	public bool IsCrouching = false;
	public bool IsSprinting = false;
	private CharacterController characterController;
	private CitizenAnimationHelper animationHelper;


	protected override void OnAwake()
	{
		characterController = Components.Get<CharacterController>();
		animationHelper = Components.Get<CitizenAnimationHelper>();
	}

	protected override void OnUpdate()
	{
		// Set sprint and crouching states
		updateCrouch();
		IsSprinting = Input.Down( "Run" );
		if(Input.Pressed("Jump")) Jump();
		RotateBody();
		UpdateAnimations();
		
	}
	
	protected override void OnFixedUpdate()
	{
		BuildWishVelocity();
		Move();
	}

	void BuildWishVelocity()
	{
		WishVelocity = 0;
		var rotation = Head.Transform.Rotation;
		
		// Look at inputs
		if ( Input.Down( "Forward" ) ) WishVelocity += rotation.Forward;
		if ( Input.Down( "Backward" ) ) WishVelocity += rotation.Backward;
		if ( Input.Down( "Left" ) ) WishVelocity += rotation.Left;
		if ( Input.Down( "Right" ) ) WishVelocity += rotation.Right;
		
		// Make sure you don't move up/down if you're looking up/down
		WishVelocity = WishVelocity.WithZ( 0 );
		if ( WishVelocity.IsNearZeroLength ) WishVelocity = WishVelocity.Normal;
		
		// Order matters... Set the player's wish velocity based on if the player is crouching or not
		if ( IsCrouching ) WishVelocity *= CrouchSpeed;
		else if ( IsSprinting ) WishVelocity *= RunSpeed;
		else WishVelocity *= Speed;
	}

	void Move()
	{
		// Get gravity from scene
		var gravity = Scene.PhysicsWorld.Gravity;

		if ( characterController.IsOnGround )
		{
			// Apply friction and acceleration
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
			characterController.Accelerate(WishVelocity);
			characterController.ApplyFriction(GroundControl);
		}
		else
		{
			// Apply air control / Gravity
			characterController.Velocity += gravity * Time.Delta * 0.5f;
			characterController.Accelerate(WishVelocity.ClampLength(MaxForce));
			characterController.ApplyFriction(AirControl);
			
		}
		
		// Move
		characterController.Move();
		// Apply other gravity
		if ( !characterController.IsOnGround )
		{
			characterController.Velocity += gravity * Time.Delta * 0.5f;
		}
		else
		{
			characterController.Velocity = characterController.Velocity.WithZ( 0 );
		}
	}

	void RotateBody()
	{
		if(Body is null) return;

		var targetAngle = new Angles( 0, Head.Transform.Rotation.Yaw(), 0 ).ToRotation();

		float rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

		if ( rotateDifference > 50f || characterController.Velocity.Length > 10f )
		{
			Body.Transform.Rotation = Rotation.Lerp(Body.Transform.Rotation, targetAngle, Time.Delta * 2f);
			
			
		}
		
	}

	void Jump()
	{
		if ( !characterController.IsOnGround ) return;
		characterController.Punch((Vector3.Up * JumpForce));
		animationHelper?.TriggerJump();
	}

	void UpdateAnimations()
	{
		if(animationHelper is null) return;
		
		animationHelper.WithWishVelocity(WishVelocity);
		animationHelper.WithVelocity(characterController.Velocity);
		animationHelper.AimAngle = Head.Transform.Rotation;
		animationHelper.IsGrounded = characterController.IsOnGround;
		animationHelper.WithLook(Head.Transform.Rotation.Forward, 1f, 0.75f, 0.5f);
		animationHelper.MoveStyle = CitizenAnimationHelper.MoveStyles.Run;
		animationHelper.DuckLevel = IsCrouching ? 1f : 0f;
	}

	void updateCrouch()
	{
		if ( characterController is null ) return;

		if ( Input.Pressed( "Duck" ) && !IsCrouching )
		{
			IsCrouching = true;
			characterController.Height /= 2f;
		}
		
		if ( Input.Released( "Duck" ) && IsCrouching )
		{
			// TODO: do an upwards trace to see if there is a roof above us
			IsCrouching = false;
			characterController.Height *= 2f;
		}
	}
}
