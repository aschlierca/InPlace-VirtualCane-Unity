using UnityEngine;

/// <summary>
/// Single point of collision-aware movement for the User avatar.
///
/// All movement scripts (editor WASD, pedometer StepCount, and the iOS
/// in-place movement modes) route their position changes through this
/// component instead of writing transform.position / Translate directly.
/// CharacterController.Move() sweeps the capsule through the world, so the
/// avatar is stopped by (and slides along) any wall collider in its path.
///
/// IMPORTANT - no gravity / no drift:
/// We never apply gravity or any implicit velocity. The avatar only ever
/// moves by the exact delta a movement script asks for, and the vertical
/// component is zeroed. This is why a CharacterController is used instead of
/// a dynamic Rigidbody: a non-kinematic Rigidbody retained velocity and made
/// the user drift forward with no input. CharacterController has no such
/// velocity of its own, so when no script asks it to move, it stays put.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class UserCharacterMover : MonoBehaviour
{
    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // A CharacterController drives movement; a dynamic Rigidbody alongside
        // it fights the controller and caused the "moving forward without input"
        // drift. Force any Rigidbody on this object to kinematic so it only
        // serves trigger/contact detection and never adds velocity.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
    }

    /// <summary>
    /// Move the avatar by a world-space delta, colliding with walls.
    /// The vertical component is discarded so the avatar stays at a fixed
    /// height and never accumulates gravity/idle drift.
    /// </summary>
    public void MoveHorizontal(Vector3 worldDelta)
    {
        if (controller == null || !controller.enabled)
            return;

        worldDelta.y = 0f;
        if (worldDelta.sqrMagnitude > 0f)
            controller.Move(worldDelta);
    }
}
