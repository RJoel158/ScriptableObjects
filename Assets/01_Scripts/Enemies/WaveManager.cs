using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int baseEnemiesPerWave = 4;
    [SerializeField] private int enemiesIncreasePerWave = 3;
    [SerializeField] private float spawnRadius = 15f;

    [Header("Enemy Catalog & Prefabs")]
    public List<EnemyData> enemyDataList = new List<EnemyData>();
    public GameObject enemyBasePrefab;

    [Header("State")]
    private int totalEnemiesToSpawn = 0;
    private int enemiesSpawnedSoFar = 0;
    private int enemiesAlive = 0;
    private bool isWaveInProgress = false;
    private float waveCountdownTimer = 0f;

    public int CurrentWave => currentWave;
    public int EnemiesAlive => enemiesAlive;
    public bool IsWaveInProgress => isWaveInProgress;

    // Eventos
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int, int> OnEnemyCountChanged; // (vivos, total)
    public event Action<float> OnWaveCountdownChanged;

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
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            currentWave++;
            totalEnemiesToSpawn = baseEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;
            enemiesSpawnedSoFar = 0;
            enemiesAlive = 0;
            isWaveInProgress = true;

            OnWaveStarted?.Invoke(currentWave);
            OnEnemyCountChanged?.Invoke(enemiesAlive, totalEnemiesToSpawn);

            Debug.Log($"[Oleadas] ¡Comenzando Oleada {currentWave}! Total de enemigos: {totalEnemiesToSpawn}");

            // Multiplicadores escalados con la oleada
            float healthMult = 1f + (currentWave - 1) * 0.35f;
            float damageMult = 1f + (currentWave - 1) * 0.25f;
            float speedMult = Mathf.Min(1.6f, 1f + (currentWave - 1) * 0.05f);

            // Generar enemigos en intervalos
            while (enemiesSpawnedSoFar < totalEnemiesToSpawn)
            {
                SpawnEnemy(healthMult, damageMult, speedMult);
                enemiesSpawnedSoFar++;
                enemiesAlive++;
                OnEnemyCountChanged?.Invoke(enemiesAlive, totalEnemiesToSpawn);

                yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 1.8f));
            }

            // Esperar hasta que todos los enemigos sean derrotados
            while (enemiesAlive > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            isWaveInProgress = false;
            OnWaveCompleted?.Invoke(currentWave);
            Debug.Log($"[Oleadas] ¡Oleada {currentWave} completada con éxito!");

            // Recompensa de oleada
            int waveBonusGold = 20 + currentWave * 10;
            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddGold(waveBonusGold);
            }

            // Descanso entre oleadas para ir a la tienda
            waveCountdownTimer = timeBetweenWaves;
            while (waveCountdownTimer > 0)
            {
                OnWaveCountdownChanged?.Invoke(waveCountdownTimer);
                yield return new WaitForSeconds(1f);
                waveCountdownTimer -= 1f;
            }
        }
    }

    private void SpawnEnemy(float healthMult, float damageMult, float speedMult)
    {
        if (enemyDataList == null || enemyDataList.Count == 0) return;

        // Seleccionar tipo de enemigo según la oleada
        int enemyIndex = 0;
        if (currentWave >= 3 && enemyDataList.Count > 1)
        {
            enemyIndex = UnityEngine.Random.Range(0, enemyDataList.Count);
        }
        EnemyData chosenData = enemyDataList[enemyIndex];

        // Posición de spawn alrededor del jugador
        Vector3 spawnPos = GetRandomSpawnPosition();

        GameObject enemyObj;
        if (chosenData.enemyPrefab != null)
        {
            enemyObj = Instantiate(chosenData.enemyPrefab, spawnPos, Quaternion.identity);
        }
        else if (enemyBasePrefab != null)
        {
            enemyObj = Instantiate(enemyBasePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            enemyObj = CreateProceduralEnemyObject(spawnPos, chosenData);
        }

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller == null) controller = enemyObj.AddComponent<EnemyController>();

        controller.Initialize(chosenData, healthMult, damageMult, speedMult);
    }

    private Vector3 GetRandomSpawnPosition()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * spawnRadius;
        return new Vector3(center.x + randomCircle.x, 0.5f, center.z + randomCircle.y);
    }

    private GameObject CreateProceduralEnemyObject(Vector3 pos, EnemyData data)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = $"Enemy_{data.enemyName}";
        enemy.tag = "Enemy";
        enemy.transform.position = pos;

        // Visual
        Renderer r = enemy.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = data.bodyColor;
        }

        // Rigidbody opcional cinemático para física
        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        return enemy;
    }

    public void OnEnemyDefeated(EnemyController enemy)
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyCountChanged?.Invoke(enemiesAlive, totalEnemiesToSpawn);
    }
}
