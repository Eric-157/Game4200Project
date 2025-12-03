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
    [Tooltip("Enable runtime debug logs for camera follow and bounds (temporary)")]
    public bool debugLogs = false;
    [Tooltip("When enabled the camera will not be clamped on the 'back' side (negative Z for XZ plane).")]
    public bool allowUnlimitedBack = true;

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
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }
        // Create a follow target for Cinemachine so rotation is not inherited
        if (vcam != null)
        {
            // Create a separate follow target object so the camera's smoothing and clamping
            // operates on the follow target instead of directly moving the player transform.
            followTarget = new GameObject("CameraFollowTarget").transform;
            // Determine Cinemachine transposer offset if present (so we can counter it when positioning the follow target)
            Vector3 transposerOffset = Vector3.zero;
            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
                transposerOffset = transposer.m_FollowOffset;

            // Place follow target at player's XZ but keep camera's current Y (height) minus transposer offset.y
            Vector3 startPos = (player != null) ? new Vector3(player.position.x, transform.position.y - transposerOffset.y, player.position.z) : transform.position;
            followTarget.position = startPos;
            followTarget.rotation = Quaternion.identity;
            vcam.Follow = followTarget;

            // IMPORTANT: in the Virtual Camera component in the Inspector set 'Aim' to 'Do Nothing'
            // and 'Body' to 'Transposer' (or 'Framing Transposer' for 2D). That prevents rotation
            // from being driven by the target while allowing position following.
        }

        RefreshBounds();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Defensive: if followTarget wasn't created in Start (race or missing vcam), create it now
        if (vcam != null && followTarget == null)
        {
            followTarget = new GameObject("CameraFollowTarget").transform;
            var transposer2 = vcam.GetCinemachineComponent<CinemachineTransposer>();
            Vector3 transposerOffset2 = (transposer2 != null) ? transposer2.m_FollowOffset : Vector3.zero;
            followTarget.position = new Vector3(player.position.x, transform.position.y - transposerOffset2.y, player.position.z);
            followTarget.rotation = Quaternion.identity;
            vcam.Follow = followTarget;
            if (debugLogs) Debug.Log("CameraController: lazy-created followTarget at " + followTarget.position + " transposerOffset=" + transposerOffset2);
        }

        // Determine current camera extents (orthographic assumed)
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        float vertExtent;
        float horzExtent;
        if (mainCam.orthographic)
        {
            vertExtent = mainCam.orthographicSize;
            horzExtent = vertExtent * mainCam.aspect;
        }
        else
        {
            // Perspective camera: compute world-space extents at the follow target distance
            Vector3 refPoint = (followTarget != null) ? followTarget.position : (player != null ? player.position : transform.position);
            float distance = Mathf.Abs(Vector3.Dot(refPoint - mainCam.transform.position, mainCam.transform.forward));
            // Prevent zero or negative distances
            distance = Mathf.Max(0.01f, distance);
            float frustumHeight = 2f * distance * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            vertExtent = frustumHeight * 0.5f;
            horzExtent = vertExtent * mainCam.aspect;
        }
        if (debugLogs)
        {
            var transposerLog = vcam != null ? vcam.GetCinemachineComponent<CinemachineTransposer>()?.m_FollowOffset ?? Vector3.zero : Vector3.zero;
            Debug.Log($"Camera extents: orthographic={mainCam.orthographic} vert={vertExtent} horz={horzExtent} transposerOffset={transposerLog}");
        }

        // Build target position from player but keep the camera's depth/height axis unchanged
        Vector3 desired = followTarget != null ? followTarget.position : transform.position;

        if (plane == PlaneMode.XZ)
            desired = new Vector3(player.position.x, desired.y, player.position.z);
        else
            desired = new Vector3(player.position.x, player.position.y, desired.z);

        // Clamp based on computed limits and camera extents
        if (plane == PlaneMode.XZ)
        {
            // We'll treat horizontal and depth axes agnostically and map them back to X/Z.
            // Decide whether Z should be considered the horizontal axis by comparing spreads.
            float horizontalSpread = Mathf.Abs(rightLimit - leftLimit);
            float depthSpread = Mathf.Abs(topLimit - bottomLimit);
            bool useZAsHorizontal = (depthSpread > horizontalSpread);

            float hMin = leftLimit;
            float hMax = rightLimit;
            float dMin = bottomLimit;
            float dMax = topLimit;

            float desiredH = useZAsHorizontal ? player.position.z : player.position.x;
            float desiredD = useZAsHorizontal ? player.position.x : player.position.z;

            float minH = hMin + horzExtent;
            float maxH = hMax - horzExtent;
            float minD = dMin + vertExtent;
            float maxD = dMax - vertExtent;

            if (allowUnlimitedBack)
                minD = float.NegativeInfinity;

            if (minH > maxH)
            {
                desiredH = (hMin + hMax) * 0.5f;
            }
            else
            {
                desiredH = Mathf.Clamp(desiredH, minH, maxH);
            }

            if (minD > maxD)
            {
                desiredD = (dMin + dMax) * 0.5f;
            }
            else
            {
                desiredD = Mathf.Clamp(desiredD, minD, maxD);
            }

            if (useZAsHorizontal)
            {
                desired.z = desiredH;
                desired.x = desiredD;
            }
            else
            {
                desired.x = desiredH;
                desired.z = desiredD;
            }
        }
        else // XY
        {
            float minX = leftLimit + horzExtent;
            float maxX = rightLimit - horzExtent;
            float minY = bottomLimit + vertExtent;
            float maxY = topLimit - vertExtent;

            if (minX > maxX)
            {
                desired.x = (leftLimit + rightLimit) * 0.5f;
            }
            else
            {
                desired.x = Mathf.Clamp(desired.x, minX, maxX);
            }

            if (minY > maxY)
            {
                desired.y = (bottomLimit + topLimit) * 0.5f;
            }
            else
            {
                desired.y = Mathf.Clamp(desired.y, minY, maxY);
            }
        }

        // Smoothly move follow target (or directly camera if no vcam)
        if (followTarget != null)
        {
            followTarget.position = Vector3.SmoothDamp(followTarget.position, desired, ref velocity, smoothTime);
            // Make sure follow target doesn't rotate (prevents Cinemachine from inheriting rotation)
            followTarget.rotation = Quaternion.identity;
            if (debugLogs)
            {
                Debug.Log($"CameraController LateUpdate - player={player.position} desired={desired} followTarget={followTarget.position} left={leftLimit} right={rightLimit} bot={bottomLimit} top={topLimit}");
            }
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
        if (roomRoot == null)
            roomRoot = GameObject.Find("Room");
        // Prefer GameManager's current room root if available
        if (roomRoot == null && GameManager.Instance != null)
            roomRoot = GameManager.Instance.GetCurrentRoomRoot();
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

        // If we have a Room root, compute an aggregate bounds from all Renderers/Colliders
        // under that root. This is more robust if walls are not individually tagged.
        Bounds roomBounds = new Bounds();
        bool hasRoomBounds = false;
        if (roomRoot != null)
        {
            // Look for renderers and colliders inside the room root
            var renderers = roomRoot.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (!hasRoomBounds)
                {
                    roomBounds = r.bounds;
                    hasRoomBounds = true;
                }
                else roomBounds.Encapsulate(r.bounds);
            }

            var colliders3d = roomRoot.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders3d)
            {
                if (!hasRoomBounds)
                {
                    roomBounds = c.bounds;
                    hasRoomBounds = true;
                }
                else roomBounds.Encapsulate(c.bounds);
            }

            var coll2 = roomRoot.GetComponentsInChildren<Collider2D>(true);
            foreach (var c in coll2)
            {
                Bounds b = new Bounds(c.bounds.center, c.bounds.size);
                if (!hasRoomBounds)
                {
                    roomBounds = b;
                    hasRoomBounds = true;
                }
                else roomBounds.Encapsulate(b);
            }
        }

        if (debugLogs && hasRoomBounds)
        {
            Debug.Log($"CameraController.RefreshBounds: computed roomBounds min={roomBounds.min} max={roomBounds.max}");
        }

        if (debugLogs)
        {
            Debug.Log($"CameraController.RefreshBounds: found {candidates.Count} wall candidates (wallTag='{wallTag}')");
            foreach (var t in candidates)
            {
                var c2 = t.GetComponent<Collider2D>();
                Bounds b = new Bounds(t.position, Vector3.zero);
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
                Debug.Log($"  candidate: {t.name} pos={t.position} bounds.min={b.min} bounds.max={b.max}");
            }
        }

        if (candidates.Count == 0 && !hasRoomBounds)
        {
            // nothing found — set large bounds so camera is not clamped
            leftLimit = -10000f; rightLimit = 10000f; bottomLimit = -10000f; topLimit = 10000f;
            return;
        }

        // If we have an aggregate roomBounds, prefer that for setting limits
        if (hasRoomBounds)
        {
            // Choose dominant horizontal axis (the axis with larger spread) so rooms rotated or laid out
            // along Z or X work naturally. If Z spread is larger we treat Z as left/right axis.
            float spreadX = roomBounds.size.x;
            float spreadZ = roomBounds.size.z;
            bool useZAsHorizontal = spreadZ > spreadX;

            if (useZAsHorizontal)
            {
                // left/right are along Z, depth/front/back along X
                leftLimit = roomBounds.min.z;
                rightLimit = roomBounds.max.z;
                bottomLimit = roomBounds.min.x;
                topLimit = roomBounds.max.x;
                if (debugLogs) Debug.Log("CameraController: using Z as horizontal axis (left/right).");
            }
            else
            {
                // left/right along X, depth/front/back along Z
                leftLimit = roomBounds.min.x;
                rightLimit = roomBounds.max.x;
                bottomLimit = roomBounds.min.z;
                topLimit = roomBounds.max.z;
                if (debugLogs) Debug.Log("CameraController: using X as horizontal axis (left/right).");
            }
        }
        else
        {
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

                // left = min x, right = max x
                leftLimit = Mathf.Min(leftLimit, b.min.x);
                rightLimit = Mathf.Max(rightLimit, b.max.x);

                if (plane == PlaneMode.XZ)
                {
                    // bottom = min z, top = max z
                    bottomLimit = Mathf.Min(bottomLimit, b.min.z);
                    topLimit = Mathf.Max(topLimit, b.max.z);
                }
                else
                {
                    // bottom = min y, top = max y
                    bottomLimit = Mathf.Min(bottomLimit, b.min.y);
                    topLimit = Mathf.Max(topLimit, b.max.y);
                }
            }

            // If after scanning we somehow ended up with inverted or infinite values, provide a safe fallback
            if (float.IsInfinity(leftLimit) || float.IsInfinity(rightLimit) || leftLimit >= rightLimit)
            {
                leftLimit = -10000f; rightLimit = 10000f;
            }
            if (float.IsInfinity(bottomLimit) || float.IsInfinity(topLimit) || bottomLimit >= topLimit)
            {
                bottomLimit = -10000f; topLimit = 10000f;
            }
        }
    }

    public void SetPlayer(Transform t)
    {
        player = t;
        if (followTarget != null) followTarget.position = new Vector3(player.position.x, followTarget.position.y, player.position.z);
    }
}
