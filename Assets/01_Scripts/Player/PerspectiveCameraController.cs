using UnityEngine;

public enum CameraPerspective
{
    TopDown_Zomboid,       // Vista táctica cenital estilo Project Zomboid
    OverTheShoulder_RE4    // Vista en 3ra persona de exploración / combate estilo Resident Evil 4
}

public class PerspectiveCameraController : MonoBehaviour
{
    public static PerspectiveCameraController Instance { get; private set; }

    [Header("Target & Mode")]
    public Transform targetPlayer;
    public CameraPerspective currentPerspective = CameraPerspective.TopDown_Zomboid;

    [Header("Aiming System (Resident Evil 4 Stance)")]
    [Tooltip("Sensibilidad de apuntado con el ratón")]
    public float aimSensitivity = 2.2f;
    public float minPitch = -45f;
    public float maxPitch = 55f;
    [Tooltip("Posición de la cámara durante el apuntado cerrado sobre el hombro (RE4)")]
    public Vector3 aimOffset = new Vector3(0.50f, 0.05f, -1.30f);
    public float aimFOV = 48f;
    public float normalFOV = 60f;

    [Header("Resident Evil 4 Exploration (Sin apuntar)")]
    public Vector3 re4ExplorationOffset = new Vector3(0.40f, 0.15f, -2.40f);

    [Header("Project Zomboid Top-Down (Cenital)")]
    public Vector3 topDownOffset = new Vector3(0f, 7.5f, -6.5f);
    public float topDownPitch = 48f;

    [Header("Smooth Transitions")]
    public float transitionSpeed = 10f;
    public float rotationSmoothSpeed = 14f;

    [Header("Controls")]
    public KeyCode togglePerspectiveKey = KeyCode.V;

    private float currentPitch = 0f;
    private float currentYaw = 0f;
    private Camera cam;
    private Vector3 currentLocalOffset;

    public float CurrentPitch => currentPitch;
    public float CurrentYaw => currentYaw;
    public bool IsAiming => Input.GetMouseButton(1) && !IsPointerOverUI();
    public bool IsInOverTheShoulder => currentPerspective == CameraPerspective.OverTheShoulder_RE4 || IsAiming;
    public bool IsFirstPerson => IsInOverTheShoulder; // Compatibilidad con scripts existentes

    void Awake()
    {
        if (Instance == null) Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
        }

        if (targetPlayer != null)
        {
            currentYaw = targetPlayer.eulerAngles.y;
        }

        if (cam != null) cam.fieldOfView = normalFOV;
        currentLocalOffset = (currentPerspective == CameraPerspective.TopDown_Zomboid) ? topDownOffset : re4ExplorationOffset;
    }

    void Update()
    {
        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
            return;
        }

        // Alternar vista base con la tecla 'V'
        if (Input.GetKeyDown(togglePerspectiveKey))
        {
            TogglePerspective();
        }

        // Al presionar Clic Derecho para apuntar, sincronizar el Yaw con la dirección en la que el jugador ya estaba mirando
        if (Input.GetMouseButtonDown(1))
        {
            if (targetPlayer != null)
            {
                currentYaw = targetPlayer.eulerAngles.y;
                currentPitch = 0f;
            }
        }

        // Control del cursor: Bloqueado en el centro al apuntar para mover la cámara (estilo RE4)
        if (IsAiming)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (currentPerspective == CameraPerspective.TopDown_Zomboid)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Manejar rotación del ratón cuando se apunta o en modo RE4
        if (IsInOverTheShoulder && !IsPointerOverUI())
        {
            float mouseX = Input.GetAxis("Mouse X") * aimSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * aimSensitivity;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            // Al apuntar o en modo RE4, el personaje gira sincronizado con la mira
            if (IsAiming || currentPerspective == CameraPerspective.OverTheShoulder_RE4)
            {
                targetPlayer.rotation = Quaternion.Euler(0f, currentYaw, 0f);
            }
        }
    }

    [Header("Cinematic Grapple Camera")]
    public Vector3 grappleCameraOffset = new Vector3(0.55f, 0.15f, -1.15f);
    public float grappleFOV = 42f;

    void LateUpdate()
    {
        if (targetPlayer == null || cam == null) return;

        PlayerController player = targetPlayer.GetComponent<PlayerController>();
        bool isGrappling = player != null && player.IsInGrappleQTE;
        bool aiming = IsAiming;

        if (isGrappling)
        {
            // Acercamiento cinemático dramático estilo Resident Evil cuando el zombie muerde el cuello
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, grappleFOV, Time.deltaTime * (transitionSpeed * 2f));
            HandleGrappleBiteCamera();
        }
        else
        {
            float targetFov = aiming ? aimFOV : normalFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * transitionSpeed);

            if (aiming)
            {
                // Transición inmediata y cinemática al modo de apuntado RE4
                HandleRE4AimCamera();
            }
            else if (currentPerspective == CameraPerspective.OverTheShoulder_RE4)
            {
                // Vista de exploración RE4 en 3ra persona
                HandleRE4ExplorationCamera();
            }
            else
            {
                // Vista cenital / isométrica estilo Project Zomboid
                HandleTopDownCamera();
            }
        }
    }

    private void HandleGrappleBiteCamera()
    {
        Quaternion camRotation = Quaternion.Euler(14f, targetPlayer.eulerAngles.y, 0f);
        Vector3 targetPivot = targetPlayer.position + Vector3.up * 1.35f; // Cuello del soldado
        Vector3 desiredPos = targetPivot + camRotation * grappleCameraOffset;

        desiredPos = ResolveWallCollision(targetPivot, desiredPos);

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * (transitionSpeed * 2.5f));
        transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, Time.deltaTime * (rotationSmoothSpeed * 2.5f));
    }

    private void HandleRE4AimCamera()
    {
        Quaternion camRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 targetPivot = targetPlayer.position + Vector3.up * 1.45f; // Hombro / Ojos
        Vector3 desiredPos = targetPivot + camRotation * aimOffset;

        // Detección de colisión con muros
        desiredPos = ResolveWallCollision(targetPivot, desiredPos);

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * (transitionSpeed * 1.5f));
        transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, Time.deltaTime * (rotationSmoothSpeed * 1.5f));
    }

    private void HandleRE4ExplorationCamera()
    {
        Quaternion camRotation = Quaternion.Euler(currentPitch * 0.5f + 10f, currentYaw, 0f);
        Vector3 targetPivot = targetPlayer.position + Vector3.up * 1.45f;
        Vector3 desiredPos = targetPivot + camRotation * re4ExplorationOffset;

        desiredPos = ResolveWallCollision(targetPivot, desiredPos);

        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * transitionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, camRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    private void HandleTopDownCamera()
    {
        Vector3 desiredPos = targetPlayer.position + topDownOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * transitionSpeed);

        Vector3 lookTarget = targetPlayer.position + Vector3.up * 1.1f;
        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
    }

    private Vector3 ResolveWallCollision(Vector3 pivot, Vector3 desiredCameraPos)
    {
        Vector3 rayDir = desiredCameraPos - pivot;
        float rayDist = rayDir.magnitude;
        RaycastHit[] hits = Physics.RaycastAll(pivot, rayDir.normalized, rayDist, ~0, QueryTriggerInteraction.Ignore);

        float closestDist = rayDist;
        Vector3 safePos = desiredCameraPos;

        foreach (var hit in hits)
        {
            if (hit.collider != null &&
                !hit.transform.IsChildOf(targetPlayer) &&
                hit.transform != targetPlayer &&
                !hit.collider.CompareTag("Enemy") &&
                !hit.collider.isTrigger)
            {
                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    safePos = hit.point + hit.normal * 0.15f;
                }
            }
        }

        return safePos;
    }

    public void TogglePerspective()
    {
        SetPerspective(currentPerspective == CameraPerspective.TopDown_Zomboid
            ? CameraPerspective.OverTheShoulder_RE4
            : CameraPerspective.TopDown_Zomboid);
    }

    public void SetPerspective(CameraPerspective perspective)
    {
        currentPerspective = perspective;

        if (targetPlayer != null)
        {
            currentYaw = targetPlayer.eulerAngles.y;
            currentPitch = 0f;
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification(perspective == CameraPerspective.TopDown_Zomboid
                ? "📷 Cámara: Cenital Táctica (Estilo Project Zomboid) [Mantén Clic Derecho para Apuntar RE4]"
                : "📷 Cámara: Tercera Persona (Estilo Resident Evil 4)");
        }
    }

    private bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}
