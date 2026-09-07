using UnityEngine;

public class MerchantNPC : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 3.5f;
    [SerializeField] private string merchantName = "Buhonero Táctico";
    [SerializeField] private string welcomeMessage = "¡Bienvenido, soldado! ¿Qué buscas comprar o vender hoy?";

    [Header("Visuals & Atmosphere")]
    [SerializeField] private bool lookAtPlayer = true;
    [SerializeField] private float rotationSpeed = 3f;

    private Transform playerTransform;
    private bool isPlayerNearby = false;
    private bool hasGreeted = false;

    void Start()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = distance <= interactionRadius;

        // Saludo al entrar en rango
        if (isPlayerNearby && !wasNearby && !hasGreeted)
        {
            hasGreeted = true;
            HUDUI hud = FindAnyObjectByType<HUDUI>();
            if (hud != null)
            {
                hud.ShowNotification($"[{merchantName}]: \"{welcomeMessage}\"");
            }
        }
        else if (!isPlayerNearby && wasNearby)
        {
            hasGreeted = false;
        }

        // Rotar suavemente hacia el jugador si está cerca
        if (lookAtPlayer && isPlayerNearby)
        {
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * rotationSpeed);
            }
        }

        // Tecla E para abrir la tienda si está cerca
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
        {
            UIManager uiMgr = FindAnyObjectByType<UIManager>();
            if (uiMgr != null)
            {
                uiMgr.ToggleStore();
            }
        }
    }

    void OnGUI()
    {
        if (!isPlayerNearby) return;

        // Indicador flotante en pantalla
        float boxW = 260f;
        float boxH = 44f;
        float posX = (Screen.width - boxW) / 2f;
        float posY = Screen.height - 130f;

        // Fondo oscuro estilizado
        GUI.color = new Color(0.08f, 0.12f, 0.18f, 0.92f);
        GUI.DrawTexture(new Rect(posX, posY, boxW, boxH), Texture2D.whiteTexture);

        // Borde dorado
        GUI.color = new Color(1f, 0.85f, 0.2f, 1f);
        GUI.DrawTexture(new Rect(posX, posY, boxW, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX, posY + boxH - 2, boxW, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX, posY, 2, boxH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX + boxW - 2, posY, 2, boxH), Texture2D.whiteTexture);

        // Texto
        GUIStyle style = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState() { textColor = Color.white }
        };

        GUI.Label(new Rect(posX, posY, boxW, boxH), "Presiona [E] o [T] para la Tienda", style);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}
