using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Rigidbody))]
public class SpacecraftControl : MonoBehaviour 
{
    public Collider stars;
    public Vector3 startPos;
    public float MaxEnginePower = 40f;
	public float RollEffect = 50f;
	public float PitchEffect = 50f;
	public float YawEffect = 0.2f;
	public float BankedTurnEffect = 0.5f;
	public float AutoTurnPitch = 0.5f;
	public float AutoRollLevel = 0.1f;
	public float AutoPitchLevel = 0.1f;
	public float AirBreaksEffect = 3f;
	public float ThrottleChangeSpeed = 0.3f;
	public float DragIncreaseFactor = 0.001f;


	private float Throttle;
	private bool AirBrakes;
	private float ForwardSpeed;
	private float EnginePower;
	private float cur_MaxEnginePower;
	private float RollAngle;
	private float PitchAngle;
	private float RollInput;
	private float PitchInput;
	private float YawInput;
	private float ThrottleInput;

	private float OriginalDrag;
	private float OriginalAngularDrag;
	private float AeroFactor =1;
	private bool Immobilized = false;
	private float BankedTurnAmount;
	private Rigidbody rigidBody;

	public Camera third;
	public Camera first;

	WheelCollider[] cols;

    void Start()
	{
        startPos = transform.position;
        //This is a simple boolean operation to enable 2 diffrent camera angles
        third.enabled = true;
		first.enabled = false;

        //here I am getting a RigidBody component from the object that is used for certain pyhsics based commands such as Drag and AngularDrag
        rigidBody = GetComponent<Rigidbody> ();

		OriginalDrag = rigidBody.drag;
		OriginalAngularDrag = rigidBody.angularDrag;

        //this is a for loop where for each Component in the child of the ship a wheel collider will be placed and its motorTorque will be set to 0.18f
        for (int i = 0; i < transform.childCount; i++)
		{
			foreach(var componentsInChild in transform.GetChild(i).GetComponentsInChildren<WheelCollider>())
			{
				componentsInChild.motorTorque = 0.18f;
			}
		}
	}

    private void OnTriggerExit(Collider stars)
    {
        transform.position = startPos;
        //Debug.Log("Left starfield!");
    }

    public void Move(float rollInput, float pitchInput, float yawInput, float throttleInput, bool airBrakes)
	{
		this.RollInput = rollInput;
		this.PitchInput = pitchInput;
		this.YawInput = yawInput;
		this.ThrottleInput = throttleInput;
		this.AirBrakes = airBrakes;

		ClampInput ();
		CalculateRollAndPitchAngles ();
		//AutoLevel ();
		CalculateForwardSpeed ();
		ControlThrottle ();
		CalculateDrag ();
		CalculateLinearForces ();
		CalculateTorque ();

        //if the throttle is less than 0.1f than a vector3 will be created called CurrentVerlocity and that will equal the verlocity value of the rigidbody object
        //a second vector3 will be created entitled newVerlocity which will equal the currentVerlocity times by the deltatime value
        //Finally we will be setting the rigidBodys verlocity value equal to currentlyVerlocity - newVerlocity

        if (Throttle < 0.1f)
		{
			Vector3 currentVelocity = rigidBody.velocity;
			Vector3 newVelocity = currentVelocity * Time.deltaTime;
			rigidBody.velocity = currentVelocity - newVelocity;
		}
	}

    //the function Mathf.Clamp will restrict a value of choice to a minimum and maximum value
    //for example RollInput = Mathf.Clamp (RollInput, -1, 1) is retricting thevalue RollInput to a minimum value of -1 and a maximum value of 

    void ClampInput()
	{
		RollInput = Mathf.Clamp (RollInput, -1, 1);
		PitchInput = Mathf.Clamp (PitchInput, -1, 1);
		YawInput = Mathf.Clamp (YawInput, -1, 1);
		ThrottleInput = Mathf.Clamp (ThrottleInput, -1, 1);
	}

	void CalculateRollAndPitchAngles()
	{
		Vector3 flatForward = transform.forward;
		flatForward.y = 0;

		if(flatForward.sqrMagnitude > 0)
		{
            //here we ae saying if the squareMagnatude of flatForward is greater than 0 than Normalize (set the value to 1) the value flatForward 
            flatForward.Normalize();

            //here we are using InverseTransformDirection that is converting the worldspace value of flatForward to localspace and saving that value in localFlatForward 
            Vector3 localFlatForward = transform.InverseTransformDirection(flatForward);

            //Here the value PitchAngle is being set to the Tan value of the localFlatForward y and z co-ordinates
            PitchAngle = Mathf.Atan2(localFlatForward.y,localFlatForward.z);

            //here flatRight is equal to the cross product of 2 vectors (in this case Vector3.up and flatForward)
            Vector3 flatRight = Vector3.Cross(Vector3.up, flatForward);

            //here we are using InverseTransformDirection that is converting the worldspace value of flatright to localspace and saving that value in localFlatForward
            Vector3 localFlatRight = transform.InverseTransformDirection(flatRight);

            //Here the value RollAngle is being set to the Tan value of the localFlatForward y and z co-ordinates
            RollAngle = Mathf.Atan2(localFlatRight.y, localFlatRight.x);
		}
	}

	void AutoLevel()
	{
        //here we are setting BankedTurnAmount equal to the sin of RollAngle
        BankedTurnAmount = Mathf.Sin (RollAngle);

        //here we are saying if the value of RollInput is equal to 0 then we will set it to RollAngle times AutoRollLevel
        if (RollInput == 0f) 
		{
			RollInput = -RollAngle*AutoRollLevel;
		}

        //here we are saying if PitchInput is equal to 0 then the PitchInput equal to -PitchAngle * AutoPitchLevel
        // then PitchInput is being set to PitchInput minus the absolute value of BankedTurnAmount times itself times AutoTurnPitch
        if (PitchInput == 0f)
		{
			PitchInput = -PitchAngle*AutoPitchLevel;
			PitchInput -= Mathf.Abs(BankedTurnAmount*BankedTurnAmount*AutoTurnPitch);
		}
	}

	void CalculateForwardSpeed()
	{
        //here we are using InverseTransformDirection that is converting the worldspace value of the rigidbody's verlocity value to localspace and saving that value in localVerlocity
        Vector3 localVelocity = transform.InverseTransformDirection (rigidBody.velocity);

        //here we are returning the larger of the 2 values (either 0 or localVerlocity.z) and saving it in ForwardSpeed
        ForwardSpeed = Mathf.Max (0, localVelocity.z);
	}

	void ControlThrottle()
	{
		if(Immobilized)
		{
			ThrottleInput = -0.5f;
		}

        //clamps the values of throttle + ThrottleInput * deltaTime * ThrottleChangeSpeed between 0 and 1
        Throttle = Mathf.Clamp01 (Throttle + ThrottleInput * Time.deltaTime * ThrottleChangeSpeed);

        //sets EnginePower equal to Throttle times MaxEnginePower
        EnginePower = Throttle * MaxEnginePower;
	}

	void CalculateDrag()
	{
        //here we are returning the length of the vector 'rigidBody.Verlocity' times by DragIncreaseFactor
        float extraDrag = rigidBody.velocity.magnitude * DragIncreaseFactor;

        //Here we are setting drag equal to AirBreaks or 'OrigonallDrag' plus 'extraDrag' times by AirBreaksEffect and OrigionalDrag + extraDrag
        rigidBody.drag = (AirBrakes ? (OriginalDrag + extraDrag) * AirBreaksEffect : OriginalDrag + extraDrag);
        //Here we are taking the angularDrag of the rigidBdy object and setting its value to OrigionalAngularDrag * ForwardSpeed / 1000 + OrigionalAngularDrag
        rigidBody.angularDrag = OriginalAngularDrag * ForwardSpeed / 1000 + OriginalAngularDrag;
	}

	void CalculateLinearForces()
	{
        //Here we are creating a new Vector3 called forces and setting it to zero
        Vector3 forces = Vector3.zero;

        //Here we are setting forces equal to itself + EnginePower * a transformation on the foward (Blue) axis
        forces += EnginePower * transform.forward;

        //Here we are adding the adbove forces to the rigidbody object
        rigidBody.AddForce (forces);
	}

	void CalculateTorque()
	{
        //Here we are creating a new vector free and setting it to zero
        Vector3 torque = Vector3.zero;

        //here we are adding the correct transforms to torque and finally applying them to the rigidbody
        torque += PitchInput * PitchEffect * transform.right;
		torque += YawInput * YawEffect * transform.up;
		torque += -RollInput * RollEffect * transform.forward;
		torque += BankedTurnAmount * BankedTurnEffect * transform.up;

		rigidBody.AddTorque (torque * AeroFactor);
	}

    //Stops the Player Moving
    public void Immobilize()
	{
		Immobilized = true;
	}

	public void Reset()
	{
		Immobilized = false;
	}
    //This update function simply enables the camera to be changed from first to third person on the fly with the press of a button.
    public void Update()
	{
		if (Input.GetKey (KeyCode.Alpha1)) {
			first.enabled = false;
			third.enabled = true;
		}

		if (Input.GetKey (KeyCode.Alpha2)) {
			first.enabled = true;
			third.enabled = false;
		}
	}


}
