using UnityEngine;

/// <summary>
/// Movimentação básica de um personagem 3D usando Rigidbody (física).
/// WASD / setas: andar | Espaço: pular | Shift esquerdo: correr
/// O movimento é relativo à câmera.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float airControl = 0.4f;
    [SerializeField] private float rotationSpeed = 12f;

    [Header("Pulo")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float fallMultiplier = 2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Chão")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 0.15f;

    [Header("Referências")]
    [SerializeField] private Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsule;

    private Vector2 moveInput;
    private bool sprintHeld;
    private float lastGroundedTime = -10f;
    private float lastJumpPressedTime = -10f;

    // Permite controlar o personagem via código (usado nos testes automatizados)
    private bool externalInput;

    public bool IsGrounded { get; private set; }
    public Rigidbody Body => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (externalInput) return;

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;
    }

    private void FixedUpdate()
    {
        CheckGround();
        Move();
        Jump();
        ApplyExtraGravity();
    }

    private void CheckGround()
    {
        float radius = capsule.radius * 0.9f;
        float halfHeight = capsule.height * 0.5f;
        Vector3 origin = transform.position + capsule.center;
        float distance = halfHeight - radius + groundCheckDistance;

        IsGrounded = Physics.SphereCast(origin, radius, Vector3.down, out _, distance,
                                        groundMask, QueryTriggerInteraction.Ignore);
        if (IsGrounded)
            lastGroundedTime = Time.time;
    }

    private void Move()
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        Vector3 direction = forward * moveInput.y + right * moveInput.x;
        float speed = moveSpeed * (sprintHeld ? sprintMultiplier : 1f);
        Vector3 targetVelocity = direction * speed;

        Vector3 velocity = rb.velocity;
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float control = IsGrounded ? 1f : airControl;
        horizontal = Vector3.MoveTowards(horizontal, targetVelocity,
                                         acceleration * speed * control * Time.fixedDeltaTime);

        rb.velocity = new Vector3(horizontal.x, velocity.y, horizontal.z);

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                                             rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private void Jump()
    {
        bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;

        if (jumpBuffered && canJump)
        {
            // v = sqrt(2 * g * h) -> altura do pulo independente da massa
            float jumpVelocity = Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);

            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
            IsGrounded = false;
        }
    }

    private void ApplyExtraGravity()
    {
        // Queda mais rápida deixa o pulo menos "flutuante"
        if (!IsGrounded && rb.velocity.y < 0f)
            rb.AddForce(Physics.gravity * (fallMultiplier - 1f), ForceMode.Acceleration);
    }

    /// <summary>Controle externo (testes). Desativa a leitura do teclado.</summary>
    public void SetExternalInput(Vector2 move, bool jumpPressed, bool sprint = false)
    {
        externalInput = true;
        moveInput = Vector2.ClampMagnitude(move, 1f);
        sprintHeld = sprint;
        if (jumpPressed)
            lastJumpPressedTime = Time.time;
    }

    public void SetCamera(Transform cam) => cameraTransform = cam;
}
