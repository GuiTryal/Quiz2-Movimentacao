using UnityEngine;

/// <summary>
/// Passa a velocidade e o estado de chão do PlayerController para o Animator
/// (parado / correndo / pulando).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");

    private PlayerController controller;
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (animator == null || controller.Body == null) return;

        Vector3 v = controller.Body.velocity;
        animator.SetFloat(SpeedHash, new Vector2(v.x, v.z).magnitude);
        animator.SetBool(GroundedHash, controller.IsGrounded);
    }
}
