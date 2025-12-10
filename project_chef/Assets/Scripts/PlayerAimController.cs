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
    [Tooltip("Optional sprites. If using diagonal sprites set Use Cardinal Sprites = false and order: NW, NE, SW, SE. If using cardinal sprites set Use Cardinal Sprites = true and order: N, E, S, W.")]
    public Sprite[] facingSprites;
    [Tooltip("When true, `facingSprites` are treated as cardinal (N, E, S, W). When false, they are treated as diagonals (NW, NE, SW, SE).")]
    public bool useCardinalSprites = true;

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

        // Determine facing based on dominant axis so we can support cardinal (N/E/S/W)
        bool north = flatDir.z > 0f;
        bool east = flatDir.x > 0f;
        int animatorFacing = 0; // integer param for animator (0..3) maps to either diagonal or cardinal depending on animator setup
        int spriteIndex = 0; // index into facingSprites

        float absX = Mathf.Abs(flatDir.x);
        float absZ = Mathf.Abs(flatDir.z);

        if (absX > absZ)
        {
            // Horizontal dominant -> East or West. Choose diagonal sprite based on the Z sign (north vs south).
            if (east)
            {
                if (useCardinalSprites)
                {
                    animatorFacing = 1; // East
                    spriteIndex = 1;
                }
                else
                {
                    // Diagonal: NE if north, SE if south
                    spriteIndex = north ? 1 : 3;
                    animatorFacing = spriteIndex;
                }
            }
            else
            {
                if (useCardinalSprites)
                {
                    animatorFacing = 3; // West
                    spriteIndex = 3;
                }
                else
                {
                    // Diagonal: NW if north, SW if south
                    spriteIndex = north ? 0 : 2;
                    animatorFacing = spriteIndex;
                }
            }
        }
        else
        {
            // Vertical dominant -> North or South
            if (north)
            {
                animatorFacing = 0;
                spriteIndex = 0; // North (or NW for diagonal ordering)
            }
            else
            {
                animatorFacing = 2;
                spriteIndex = 2; // South (or SW for diagonal ordering)
            }
        }

        // Set animator param if present
        if (animator != null)
        {
            animator.SetInteger(facingParam, animatorFacing);
        }
        else if (spriteRenderer != null && facingSprites != null && facingSprites.Length >= 4)
        {
            spriteRenderer.sprite = facingSprites[spriteIndex];
            if (useCardinalSprites)
            {
                // cardinal ordering N,E,S,W: flip when facing west
                spriteRenderer.flipX = (spriteIndex == 3);
            }
            else
            {
                // diagonal ordering NW,NE,SW,SE: do not flip; sprites should include both left and right variants
                spriteRenderer.flipX = false;
            }
        }
        else if (spriteRenderer != null)
        {
            // Fallback: flip sprite on X when mouse is left of player
            spriteRenderer.flipX = !east;
        }
    }
}
