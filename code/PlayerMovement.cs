using Sandbox;
using Sandbox.Citizen;

public sealed class PlayerMovement : Component
{
	// Movement Properties
	[Property] public float GroundControl { get; set; } = 4.0f;
	[Property] public float AirControl { get; set; } = 0.1f;
	[Property] public float MaxForce { get; set; } = 50f;
	[Property] public float Speed { get; set; } = 160f;
	[Property] public float RunSpeed { get; set; } = 290f;
	[Property] public float CrouchSpeed { get; set; } = 90f;
	[Property] public float JumpForce { get; set; } = 400f;
	
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
		IsCrouching = Input.Down( "Duck" );
		IsSprinting = Input.Down( "Run" );
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
}
