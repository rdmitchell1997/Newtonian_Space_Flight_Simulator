using UnityEngine;
using System.Collections;

public class UserInput : MonoBehaviour {

    //Creating a Reference to spacecraftControl script
    SpacecraftControl spacecraftControl;

	void Start () 
	{
        //Getting a reference to the spacecraftControl script on the object
        spacecraftControl = GetComponent<SpacecraftControl> ();
	}
	

	void FixedUpdate () 
	{
        //Setting Input axis for all movements
        float roll = Input.GetAxis("Horizontal");
		float pitch = Input.GetAxis("Vertical");
		float yaw = Input.GetAxis("Yaw");
		bool airBrakes = Input.GetButton("Fire1");
		float throttle = Input.GetAxis ("Throttle");

        //Calling upon the Move Function from the SspacecraftControl Script
        spacecraftControl.Move (roll, pitch, yaw, throttle, airBrakes);
	}
}
