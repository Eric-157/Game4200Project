using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraBounds : MonoBehaviour
{
    public Transform player;
    public string wallTag = "Wall";

    private float leftLimit, rightLimit, bottomLimit;
    private CinemachineVirtualCamera vcam;
    private Transform followTarget;
    private Camera mainCam;

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        mainCam = Camera.main;

        // Create internal follow target
        followTarget = new GameObject("CameraFollowTarget").transform;
        vcam.Follow = followTarget;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        FindRoomBounds();
    }

    void LateUpdate()
    {
        if (!player || !followTarget) return;

        Vector3 targetPos = player.position;

        float vertExtent = mainCam.orthographicSize;
        float horzExtent = vertExtent * mainCam.aspect;

        // Clamp based on camera edges, not center
        float minX = leftLimit + horzExtent;
        float maxX = rightLimit - horzExtent;
        float minZ = bottomLimit + vertExtent; // top-down 'rear wall'

        // Apply clamps
        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.z = Mathf.Max(targetPos.z, minZ); // Only rear wall clamp

        followTarget.position = targetPos;
    }

    private void FindRoomBounds()
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag(wallTag);
        if (walls.Length == 0) return;

        leftLimit = Mathf.Infinity;
        rightLimit = Mathf.NegativeInfinity;
        bottomLimit = Mathf.Infinity;

        foreach (GameObject wall in walls)
        {
            Vector3 pos = wall.transform.position;
            leftLimit = Mathf.Min(leftLimit, pos.x);
            rightLimit = Mathf.Max(rightLimit, pos.x);
            bottomLimit = Mathf.Min(bottomLimit, pos.z);
        }
    }
}
