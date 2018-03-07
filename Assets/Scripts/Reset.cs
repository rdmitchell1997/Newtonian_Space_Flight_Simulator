using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reset : MonoBehaviour {

   public  GameObject ship;
   public Collider stars;
    // Use this for initialization
   private void OnTriggerExit(Collider stars)
   {
       ship.transform.Translate(0, 0, 0);
        Debug.Log("Left starfield!");
   }
}
