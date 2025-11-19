using UnityEngine;
using Cinemachine;
using System.Collections;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraBounds : MonoBehaviour
{
    public Transform player;
    public string wallTag = "Wall";
    public enum PlaneMode { XZ, XY }
    public PlaneMode plane = PlaneMode.XZ;

    private float leftLimit, rightLimit, bottomLimit, topLimit;
    private CinemachineVirtualCamera vcam;
    private Transform followTarget;
    private Camera mainCam;
    private GameObject roomRootOverride;

    void Awake()
    {
        RoomManager.OnRoomChanged += OnRoomChanged;
    }

    void OnDestroy()
    {
        RoomManager.OnRoomChanged -= OnRoomChanged;
    }

    void Start()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        mainCam = Camera.main;

        followTarget = new GameObject("CameraFollowTarget").transform;
        vcam.Follow = followTarget;

        RefreshPlayerReference();
        FindRoomBounds();
    }

    void LateUpdate()
    {
        if (!player || !followTarget || !mainCam) return;

        Vector3 targetPos = player.position;

        float vertExtent = mainCam.orthographicSize;
        float horzExtent = vertExtent * mainCam.aspect;

        if (plane == PlaneMode.XZ)
        {
            float minX = leftLimit + horzExtent;
            float maxX = rightLimit - horzExtent;
            float minZ = bottomLimit + vertExtent;
            float maxZ = topLimit - vertExtent;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.z = Mathf.Clamp(targetPos.z, minZ, maxZ);
        }
        else // XY plane
        {
            float minX = leftLimit + horzExtent;
            float maxX = rightLimit - horzExtent;
            float minY = bottomLimit + vertExtent;
            float maxY = topLimit - vertExtent;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        followTarget.position = targetPos;
    }

    // Called when a new room is generated
    private void OnRoomChanged()
    {
        StartCoroutine(RefreshCameraAfterRoomLoad());
    }

    private IEnumerator RefreshCameraAfterRoomLoad()
    {
        // Wait 1 frame so the new room and player spawnpoint exist
        yield return null;

        // Refresh camera reference (Camera.main can change between scenes)
        mainCam = Camera.main;

        // Refresh player reference
        RefreshPlayerReference();

        // Recalculate the new room walls
        FindRoomBounds();

        // Snap camera follow target to player immediately
        if (player)
            ForceSnapToPlayer();
    }

    /// <summary>
    /// Let external systems set which GameObject is the current room root. If not set,
    /// the script will try to find a GameObject named "RoomRoot".
    /// </summary>
    public void SetRoomRoot(GameObject root)
    {
        roomRootOverride = root;
        FindRoomBounds();
    }

    /// <summary>
    /// Immediately move the camera follow target to the player's position so the camera snaps.
    /// Call this after teleporting or moving the player.
    /// </summary>
    public void ForceSnapToPlayer()
    {
        if (!player || !followTarget) return;
        followTarget.position = player.position;
    }

    private void RefreshPlayerReference()
    {
        var foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer)
            player = foundPlayer.transform;
    }

    private void FindRoomBounds()
    {
        GameObject roomRoot = GameObject.Find("RoomRoot");
        if (roomRoot == null) return;

        // Optional: rotate camera to match room rotation
        vcam.transform.rotation = roomRoot.transform.rotation;

        // Only consider walls that are children of the RoomRoot to avoid other rooms
        Transform[] children = roomRoot.GetComponentsInChildren<Transform>(true);

        bool foundAny = false;
        leftLimit = Mathf.Infinity;
        rightLimit = Mathf.NegativeInfinity;
        bottomLimit = Mathf.Infinity;
        topLimit = Mathf.NegativeInfinity;

        foreach (Transform t in children)
        {
            if (!t.gameObject.CompareTag(wallTag)) continue;

            // Prefer collider bounds if available, then renderer bounds, else transform position
            Bounds b = new Bounds(t.position, Vector3.zero);
            Collider2D c2d = t.GetComponent<Collider2D>();
            if (c2d != null)
                b = c2d.bounds;
            else
            {
                Collider c3d = t.GetComponent<Collider>();
                if (c3d != null)
                    b = c3d.bounds;
                else
                {
                    Renderer r = t.GetComponent<Renderer>();
                    if (r != null)
                        b = r.bounds;
                }
            }

            foundAny = true;

            // Evaluate bounds according to plane
            leftLimit = Mathf.Min(leftLimit, b.min.x);
            rightLimit = Mathf.Max(rightLimit, b.max.x);

            if (plane == PlaneMode.XZ)
            {
                bottomLimit = Mathf.Min(bottomLimit, b.min.z);
                topLimit = Mathf.Max(topLimit, b.max.z);
            }
            else // XY
            {
                bottomLimit = Mathf.Min(bottomLimit, b.min.y);
                topLimit = Mathf.Max(topLimit, b.max.y);
            }
        }

        if (!foundAny)
        {
            // fallback: try global tagged walls
            GameObject[] walls = GameObject.FindGameObjectsWithTag(wallTag);
            foreach (GameObject wall in walls)
            {
                Bounds b = new Bounds(wall.transform.position, Vector3.zero);
                Collider2D c2d = wall.GetComponent<Collider2D>();
                if (c2d != null) b = c2d.bounds;
                else { Collider c3d = wall.GetComponent<Collider>(); if (c3d != null) b = c3d.bounds; else { Renderer r = wall.GetComponent<Renderer>(); if (r != null) b = r.bounds; } }

                leftLimit = Mathf.Min(leftLimit, b.min.x);
                rightLimit = Mathf.Max(rightLimit, b.max.x);

                if (plane == PlaneMode.XZ)
                {
                    bottomLimit = Mathf.Min(bottomLimit, b.min.z);
                    topLimit = Mathf.Max(topLimit, b.max.z);
                }
                else
                {
                    bottomLimit = Mathf.Min(bottomLimit, b.min.y);
                    topLimit = Mathf.Max(topLimit, b.max.y);
                }
            }
        }
    }



}
