using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    private CharacterController characterController;
    private Camera mainCamera;
    private Vector3 velocity;

    public bool CanMove { get; set; } = true;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsDead) return;

        HandleMovement();
        HandleRotationTowardsMouse();
    }

    private void HandleMovement()
    {
        if (!CanMove) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(h, 0f, v).normalized;

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move = direction * (moveSpeed * Time.deltaTime);
        characterController.Move(move);

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleRotationTowardsMouse()
    {
        if (!CanMove || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 hitPoint = ray.GetPoint(enterDistance);
            Vector3 lookDirection = hitPoint - transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
    }
}
