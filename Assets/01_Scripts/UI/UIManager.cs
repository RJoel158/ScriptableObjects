using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private StoreUI storeUI;
    [SerializeField] private HUDUI hudUI;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Keys")]
    [SerializeField] private KeyCode inventoryKey = KeyCode.I;
    [SerializeField] private KeyCode storeKey = KeyCode.T;
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnPlayerDied += ShowGameOver;
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }

        if (Input.GetKeyDown(storeKey))
        {
            ToggleStore();
        }

        if (Input.GetKeyDown(pauseKey))
        {
            CloseAllPanels();
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUI != null)
        {
            inventoryUI.ToggleInventory();
        }
    }

    public void ToggleStore()
    {
        if (storeUI != null)
        {
            storeUI.ToggleStore();
        }
    }

    public void CloseAllPanels()
    {
        if (inventoryUI != null) inventoryUI.gameObject.SetActive(false);
        if (storeUI != null) storeUI.CloseStore();
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }
}
