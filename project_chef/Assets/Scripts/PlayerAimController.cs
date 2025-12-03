using UnityEngine;

/// <summary>
/// Rotates an aim transform toward the mouse cursor (world) so projectile spawn points
/// can use `transform.forward` as firing direction. Also sets a 4-way facing state
/// on an Animator or swaps sprites via a SpriteRenderer as a fallback.
///
/// Attach to the player root. Assign `aimTransform` (e.g. an empty child that holds
/// the projectile spawn point) and `playerCam` (usually Camera.main). Optionally
/// assign `animator` (with integer parameter `facingParam`) or a `spriteRenderer` and
/// `facingSprites` (order: 0=north-west, 1=north-east, 2=south-west, 3=south-east).
/// </summary>
public class PlayerAimController : MonoBehaviour
{
    public Camera playerCam;
    [Tooltip("Transform used for aiming / projectile spawn rotation. Usually an empty child containing the spawn point.")]
    public Transform aimTransform;

    [Header("Sprite / Animator Facing")]
    public Animator animator;
    public string facingParam = "Facing"; // int param 0..3
    public SpriteRenderer spriteRenderer;
    [Tooltip("Optional sprites in order: NW, NE, SW, SE")]
    public Sprite[] facingSprites;

    // internal state to avoid tiny jitter when mouse hasn't really moved
    Vector3 lastWorldPoint = Vector3.positiveInfinity;
    [Header("Pivot Options")]
    [Tooltip("When true, the aim transform will be positioned on a pivot circle around the player at `pivotDistance`.")]
    public bool pivotAroundPlayer = true;
    [Tooltip("Distance from the player pivot where the aimTransform will be placed when pivoting.")]
    public float pivotDistance = 1.5f;

    // Update is called once per frame
    void Update()
    {
        if (playerCam == null) playerCam = Camera.main;
        if (playerCam == null) return;

        // Ray from camera through mouse into world plane at player's y
        Ray camRay = playerCam.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        if (!groundPlane.Raycast(camRay, out float enter)) return;
        Vector3 worldPoint = camRay.GetPoint(enter);

        Vector3 dir = worldPoint - transform.position;
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        if (flatDir.sqrMagnitude < 0.0001f) return;

        // Rotate aim transform to face the mouse (keeps player root rotation unchanged)
        if (aimTransform != null)
        {
            Quaternion rot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

            // Avoid updating when the computed world point is effectively unchanged (prevents tiny jitter/drift)
            if ((lastWorldPoint - worldPoint).sqrMagnitude > 0.000001f)
            {
                // If pivoting is enabled, position the aim transform on a circle around the player
                if (pivotAroundPlayer)
                {
                    Vector3 pivotPos = transform.position;
                    Vector3 targetWorldPos = pivotPos + flatDir.normalized * pivotDistance;

                    // If the aimTransform is a child of the player root, set localPosition so it orbits correctly
                    if (aimTransform.parent == transform)
                    {
                        // Compute local position relative to player
                        Vector3 local = transform.InverseTransformPoint(targetWorldPos);
                        aimTransform.localPosition = local;
                        // Ensure the aim faces outward (+Z local)
                        aimTransform.localRotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                    }
                    else
                    {
                        // Not parented: set world position directly and orient forward
                        aimTransform.position = targetWorldPos;
                        aimTransform.forward = flatDir.normalized;
                    }
                }
                else
                {
                    // Snap directly to aim direction (instant)
                    // Use forward assignment to ensure the transform's local +Z faces the target (yaw updates correctly)
                    aimTransform.forward = flatDir.normalized;
                }

                lastWorldPoint = worldPoint;
            }
        }

        // Determine quadrant: NW (0), NE (1), SW (2), SE (3)
        int quad = 0;
        bool north = flatDir.z >= 0f;
        bool east = flatDir.x >= 0f;
        if (north && !east) quad = 0; // NW
        else if (north && east) quad = 1; // NE
        else if (!north && !east) quad = 2; // SW
        else quad = 3; // SE

        // Set animator param if present
        if (animator != null)
        {
            animator.SetInteger(facingParam, quad);
        }
        else if (spriteRenderer != null && facingSprites != null && facingSprites.Length >= 4)
        {
            spriteRenderer.sprite = facingSprites[quad];
            // Optionally flip horizontally for left-side sprites if your assets expect it
            spriteRenderer.flipX = (quad == 0 || quad == 2);
        }
        else if (spriteRenderer != null)
        {
            // Fallback: flip sprite on X when mouse is left of player
            spriteRenderer.flipX = !east;
        }
    }
}
