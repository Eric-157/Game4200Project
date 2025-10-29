using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script in reference to this Reddit post: https://www.reddit.com/r/Unity3D/comments/nd0y2f/how_to_make_the_player_rotate_based_on_the_mouse/

public class RotatePlayerFollowMouse : MonoBehaviour
{
    //Public variables
    public Camera playerCam;

    //Private variables
    private Ray camRay;
    private Plane groundPlane;
    private float rayLength;
    private Vector3 pointToLook;

    //Update is called once per frame
    private void Update()
    {
        //Sending a raycast
        camRay = playerCam.ScreenPointToRay(Input.mousePosition);

        //Setting groundPlane
        groundPlane = new Plane(Vector3.up, Vector3.zero);

        //Checking if the ray hit something
        if (groundPlane.Raycast(camRay, out rayLength))
        {
            pointToLook = camRay.GetPoint(rayLength);
        }

        //Rotating the player
        transform.LookAt(new Vector3(pointToLook.x, transform.position.y, pointToLook.z));
    }
}
