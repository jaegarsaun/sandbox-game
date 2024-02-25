using Sandbox;

public sealed class CameraMovement : Component
{
	//Properties
	[Property] public PlayerMovement Player { get; set; }
	[Property] public GameObject Body { get; set; }
	[Property] public GameObject Head { get; set; }
	[Property] public float Distance { get; set; } = 0f;
	
	//Variables
	public bool isFirstPerson => Distance == 0f;
	private CameraComponent Camera;
	private ModelRenderer BodyRenderer;

	protected override void OnAwake()
	{
		Camera = Components.Get<CameraComponent>();
		BodyRenderer = Body.Components.Get<ModelRenderer>();
	}


	protected override void OnUpdate()
	{
		// Rotate the head based on mouse movement
		var eyeAngles = Head.Transform.Rotation.Angles();
		eyeAngles.pitch += Input.MouseDelta.y * 0.1f;
		eyeAngles.yaw -= Input.MouseDelta.x * 0.1f;
		eyeAngles.roll = 0f;
		eyeAngles.pitch = eyeAngles.pitch.Clamp( -89.9f, 89.9f );
		Head.Transform.Rotation = eyeAngles.ToRotation();
		
		
		// Set camera pos
		if ( Camera != null )
		{
			var camPos = Head.Transform.Position;

			if ( !isFirstPerson )
			{
				// trace backwards to see where we can put the camera safely.
				// Basically make it so people can't go third person to see through walls
				var camForward = eyeAngles.ToRotation().Forward;
				var camTrace = Scene.Trace.Ray( camPos, camPos - (camForward * Distance) )
					.WithoutTags( "player", "trigger" )
					.Run();

				if ( camTrace.Hit )
				{
					camPos = camTrace.HitPosition + camTrace.Normal;
				}
				else
				{
					camPos = camTrace.EndPosition;
				}
				
				//Show body if not in first person
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
			}
			else
			{
				// Hide body
				BodyRenderer.RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
			}
			
			//Set cam pos to calculated pos
			Camera.Transform.Position = camPos;
			Camera.Transform.Rotation = eyeAngles.ToRotation();
		}
	}
}
