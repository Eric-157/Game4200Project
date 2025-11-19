using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public enum PlaneMode { XZ, XY }

    [Header("References")]
    [Tooltip("Player transform. If empty the script will try to find an object tagged 'Player'.")]
    public Transform player;

    [Tooltip("Tag used for walls to compute camera bounds.")]
    public string wallTag = "Wall";

    [Header("Behavior")]
    public PlaneMode plane = PlaneMode.XZ;
    [Tooltip("Smoothing time for camera following the player.")]
    public float smoothTime = 0.08f;

    // Cinemachine virtual camera (optional). If present, the script will create a follow target
    // and assign it to this virtual camera so Cinemachine handles the final camera transform.
    private CinemachineVirtualCamera vcam;
    private Transform followTarget;
    private Camera mainCam;

    // bounds
    private float leftLimit = float.NegativeInfinity;
    private float rightLimit = float.PositiveInfinity;
    private float bottomLimit = float.NegativeInfinity;
    private float topLimit = float.PositiveInfinity;

    // smoothing
    private Vector3 velocity = Vector3.zero;

    void Awake()
    {
        // Try to find a Cinemachine virtual camera on the same GameObject first,
        // otherwise find any CinemachineVirtualCamera in the scene.
        vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam == null)
            vcam = FindObjectOfType<CinemachineVirtualCamera>();

        mainCam = Camera.main;
    }

    void Start()
    {
        // Create a follow target for Cinemachine so rotation is not inherited
        if (vcam != null)
        {
            // followTarget = new GameObject("CameraFollowTarget").transform;
            // followTarget.position = (player != null) ? player.position : transform.position;
            // vcam.Follow = followTarget;
            followTarget = player;
            vcam.Follow = followTarget;

            // IMPORTANT: in the Virtual Camera component in the Inspector set 'Aim' to 'Do Nothing'
            // and 'Body' to 'Transposer' (or 'Framing Transposer' for 2D). That prevents rotation
            // from being driven by the target while allowing position following.
        }

        // Ensure we have a player reference
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }

        RefreshBounds();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Determine current camera extents (orthographic assumed)
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float vertExtent = mainCam.orthographicSize;
        float horzExtent = vertExtent * mainCam.aspect;

        // Build target position from player but keep the camera's depth/height axis unchanged
        Vector3 desired = followTarget != null ? followTarget.position : transform.position;

        if (plane == PlaneMode.XZ)
            desired = new Vector3(player.position.x, desired.y, player.position.z);
        else
            desired = new Vector3(player.position.x, player.position.y, desired.z);

        // Clamp based on computed limits and camera extents
        if (plane == PlaneMode.XZ)
        {
            float minX = leftLimit + horzExtent;
            float maxX = rightLimit - horzExtent;
            float minZ = bottomLimit + vertExtent;
            float maxZ = topLimit - vertExtent;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.z = Mathf.Clamp(desired.z, minZ, maxZ);
        }
        else // XY
        {
            float minX = leftLimit + horzExtent;
            float maxX = rightLimit - horzExtent;
            float minY = bottomLimit + vertExtent;
            float maxY = topLimit - vertExtent;

            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        // Smoothly move follow target (or directly camera if no vcam)
        if (followTarget != null)
        {
            followTarget.position = Vector3.SmoothDamp(followTarget.position, desired, ref velocity, smoothTime);
            // Make sure follow target doesn't rotate (prevents Cinemachine from inheriting rotation)
            followTarget.rotation = Quaternion.identity;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            // Keep camera rotation locked
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, 0f);
        }
    }

    /// <summary>
    /// Recalculate rectangular bounds from objects in the scene tagged with `wallTag`.
    /// It will prefer walls that are children of a GameObject named "RoomRoot" if present.
    /// </summary>
    public void RefreshBounds()
    {
        leftLimit = float.PositiveInfinity;
        rightLimit = float.NegativeInfinity;
        bottomLimit = float.PositiveInfinity;
        topLimit = float.NegativeInfinity;

        GameObject roomRoot = GameObject.Find("RoomRoot");
        List<Transform> candidates = new List<Transform>();

        if (roomRoot != null)
        {
            foreach (Transform t in roomRoot.GetComponentsInChildren<Transform>(true))
                if (t.CompareTag(wallTag)) candidates.Add(t);
        }

        if (candidates.Count == 0)
        {
            var walls = GameObject.FindGameObjectsWithTag(wallTag);
            foreach (var w in walls) candidates.Add(w.transform);
        }

        if (candidates.Count == 0)
        {
            // nothing found — set large bounds so camera is not clamped
            leftLimit = -10000f; rightLimit = 10000f; bottomLimit = -10000f; topLimit = 10000f;
            return;
        }

        foreach (var t in candidates)
        {
            Bounds b = new Bounds(t.position, Vector3.zero);
            var c2 = t.GetComponent<Collider2D>();
            if (c2 != null) b = c2.bounds;
            else
            {
                var c3 = t.GetComponent<Collider>();
                if (c3 != null) b = c3.bounds;
                else
                {
                    var r = t.GetComponent<Renderer>();
                    if (r != null) b = r.bounds;
                }
            }

            leftLimit = Mathf.Max(leftLimit, b.min.x);
            rightLimit = Mathf.Min(rightLimit, b.max.x);

            if (plane == PlaneMode.XZ)
            {
                bottomLimit = Mathf.Max(bottomLimit, b.min.z);
                topLimit = Mathf.Min(topLimit, b.max.z);
            }
            else
            {
                bottomLimit = Mathf.Min(bottomLimit, b.min.y);
                topLimit = Mathf.Max(topLimit, b.max.y);
            }
        }
    }

    public void SetPlayer(Transform t)
    {
        player = t;
        if (followTarget != null) followTarget.position = player.position;
    }
}
