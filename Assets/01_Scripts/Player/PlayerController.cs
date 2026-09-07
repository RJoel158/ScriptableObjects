using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("References")]
    private CharacterController characterController;
    private Camera mainCamera;
    private Vector3 velocity;

    public bool CanMove { get; set; } = true;
    public bool IsStunned { get; private set; } = false;
    public bool IsInGrappleQTE { get; private set; } = false;
    public float GrappleProgress { get; private set; } = 0f; // 0 to 1

    private EnemyController currentGrappler;
    private Coroutine grappleRoutine;

    // Animator Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int StruggleHash = Animator.StringToHash("Struggle");

    public void StartGrappleQTE(EnemyController grabber, float duration = 2.5f)
    {
        if (IsInGrappleQTE || grabber == null) return;
        currentGrappler = grabber;
        grappleRoutine = StartCoroutine(GrappleQTERoutine(duration));
    }

    private System.Collections.IEnumerator GrappleQTERoutine(float duration)
    {
        IsInGrappleQTE = true;
        CanMove = false;
        GrappleProgress = 0.2f;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetBool(StruggleHash, true);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowQTEPrompt(true);

        float timer = duration;
        while (timer > 0 && IsInGrappleQTE && GrappleProgress < 1f)
        {
            timer -= Time.deltaTime;
            // Descenso gradual de la barra si no se presiona
            GrappleProgress = Mathf.Max(0.05f, GrappleProgress - Time.deltaTime * 0.18f);
            yield return null;
        }

        if (GrappleProgress >= 1f)
        {
            // ¡Éxito en el QTE! El jugador se libra y empuja al zombie
            if (hud != null) hud.ShowNotification("¡TE HAS LIBRADO DEL ZOMBIE!");
            if (currentGrappler != null)
            {
                currentGrappler.ApplyKnockback(transform.forward, 2.5f);
                currentGrappler.TakeDamage(15, currentGrappler.transform.position, transform.forward);
            }
        }
        else
        {
            // Fallo en el QTE: El zombie inflige daño severo
            PlayerHealth health = GetComponent<PlayerHealth>();
            if (health != null && currentGrappler != null)
            {
                health.TakeDamage(35, transform.position, transform.forward);
            }
            if (hud != null) hud.ShowNotification("¡MORDIDA SEVERA RECIBIDA!");
        }

        if (animator != null)
        {
            animator.SetBool(StruggleHash, false);
        }

        if (hud != null) hud.ShowQTEPrompt(false);
        IsInGrappleQTE = false;
        CanMove = true;
        currentGrappler = null;
    }

    public void Stun(float duration)
    {
        if (IsStunned || IsInGrappleQTE) return;
        StartCoroutine(StunRoutine(duration));
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        IsStunned = true;
        CanMove = false;
        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowNotification("¡ATURDIDO!");

        yield return new WaitForSeconds(duration);

        IsStunned = false;
        CanMove = true;
    }

    public static PlayerController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null && health.IsDead)
        {
            UpdateAnimation(0f, 0f, 0f);
            return;
        }

        // Si está en forcejeo QTE con el zombie, presionar Espacio o Clic Izquierdo para liberarse
        if (IsInGrappleQTE)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                GrappleProgress += 0.24f;
            }
            return;
        }

        // Selección rápida de objetos / armas con teclas 1..5 (Estilo Hotbar)
        HandleHotbarInput();

        HandleMovement();

        PerspectiveCameraController camCtrl = PerspectiveCameraController.Instance;
        if (camCtrl == null || !camCtrl.IsInOverTheShoulder)
        {
            HandleRotationTowardsMouse();
        }
    }

    private void HandleHotbarInput()
    {
        if (Inventory.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Inventory.Instance.EquipPrimary();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Inventory.Instance.EquipSecondary();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Inventory.Instance.UseQuickConsumable();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (Inventory.Instance.items.Count > 3 && Inventory.Instance.items[3] != null)
                Inventory.Instance.UseItem(Inventory.Instance.items[3].item);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (Inventory.Instance.items.Count > 4 && Inventory.Instance.items[4] != null)
                Inventory.Instance.UseItem(Inventory.Instance.items[4].item);
        }
    }

    private void HandleMovement()
    {
        if (!CanMove)
        {
            UpdateAnimation(0f, 0f, 0f);
            return;
        }

        float h = Input.GetAxisRaw("Horizontal"); // A (-1) / D (+1)
        float v = Input.GetAxisRaw("Vertical");   // S (-1) / W (+1)

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && v > 0;
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 moveInput = new Vector3(h, 0f, v).normalized;
        Vector3 moveDirection = Vector3.zero;

        PerspectiveCameraController camCtrl = PerspectiveCameraController.Instance;
        bool isFirstPerson = camCtrl != null && camCtrl.IsFirstPerson;

        if (mainCamera != null)
        {
            Vector3 camForward = mainCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = mainCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveDirection = (camForward * v + camRight * h).normalized;
        }
        else
        {
            moveDirection = moveInput;
        }

        float currentSpeed = moveDirection.magnitude * targetSpeed;

        if (characterController.isGrounded)
        {
            if (velocity.y < 0f)
            {
                velocity.y = -2f; // Mantener pegado al suelo sin hundirse
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        Vector3 totalMovement = (moveDirection * targetSpeed + velocity) * Time.deltaTime;
        characterController.Move(totalMovement);

        // Vector local: Multiplicar por 2 si está corriendo (Sprint con Shift)
        Vector3 localMove = moveDirection.sqrMagnitude > 0.001f 
            ? transform.InverseTransformDirection(moveDirection) 
            : Vector3.zero;

        float multiplier = isSprinting ? 2f : 1f;
        float targetMoveX = localMove.x * (isSprinting ? 1.5f : 1f);
        float targetMoveZ = localMove.z * multiplier;

        UpdateAnimation(currentSpeed, targetMoveX, targetMoveZ);
    }

    private void UpdateAnimation(float speed, float targetMoveX, float targetMoveZ)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) return;

        float currentX = animator.GetFloat(MoveXHash);
        float currentZ = animator.GetFloat(MoveZHash);
        float currentSpeed = animator.GetFloat(SpeedHash);

        // Suavizado dinámico de blend tree (dampening) para que el movimiento de pies sea orgánico
        float smoothX = Mathf.MoveTowards(currentX, targetMoveX, Time.deltaTime * 8f);
        float smoothZ = Mathf.MoveTowards(currentZ, targetMoveZ, Time.deltaTime * 8f);
        float smoothSpeed = Mathf.MoveTowards(currentSpeed, speed, Time.deltaTime * 10f);

        animator.SetFloat(SpeedHash, smoothSpeed);
        animator.SetFloat(MoveXHash, smoothX);
        animator.SetFloat(MoveZHash, smoothZ);
        animator.SetBool(IsMovingHash, speed > 0.1f);
    }

    private void HandleRotationTowardsMouse()
    {
        if (!CanMove || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPoint = Vector3.zero;
        bool foundHit = false;

        // 1. Raycast contra enemigos, obstáculos y objetos en el mundo
        RaycastHit[] hits = Physics.RaycastAll(ray, 150f, ~0, QueryTriggerInteraction.Ignore);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider != null && h.collider.gameObject != gameObject && !h.transform.IsChildOf(transform))
                {
                    targetPoint = h.point;
                    foundHit = true;
                    break;
                }
            }
        }

        // 2. Si no impactó contra un collider, proyectar sobre el plano horizontal a la altura del arma (pecho/hombro)
        if (!foundHit)
        {
            float aimHeight = transform.position.y + 1.25f;
            Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, aimHeight, 0f));
            if (aimPlane.Raycast(ray, out float enterDist))
            {
                targetPoint = ray.GetPoint(enterDist);
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
                if (groundPlane.Raycast(ray, out float gDist))
                {
                    targetPoint = ray.GetPoint(gDist);
                }
                else
                {
                    targetPoint = ray.GetPoint(30f);
                }
            }
        }

        Vector3 lookDirection = targetPoint - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            // Rotación rápida y suave hacia el punto de mira
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), Time.deltaTime * 28f);
        }
    }
}
