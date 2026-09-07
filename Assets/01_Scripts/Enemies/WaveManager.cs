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
    [SerializeField] private int baseEnemiesPerWave = 6;
    [SerializeField] private int enemiesIncreasePerWave = 3;
    [SerializeField] private float spawnRadius = 11f;

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
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            currentWave++;
            totalEnemiesToSpawn = baseEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;
            enemiesSpawnedSoFar = 0;
            enemiesAlive = totalEnemiesToSpawn;
            isWaveInProgress = true;

            OnWaveStarted?.Invoke(currentWave);
            OnEnemyCountChanged?.Invoke(enemiesAlive, totalEnemiesToSpawn);

            Debug.Log($"[Oleadas] ¡Comenzando Oleada {currentWave}! Total de enemigos: {totalEnemiesToSpawn}");

            bool isBossWave = (currentWave % 5 == 0);
            if (isBossWave)
            {
                HUDUI hud = FindAnyObjectByType<HUDUI>();
                if (hud != null) hud.ShowNotification("ALERTA: ¡JEFE MUTANTE EN CAMINO!");
            }

            // Multiplicadores escalados progresivamente con la oleada
            float healthMult = 1f + (currentWave - 1) * 0.25f;
            float damageMult = 1f + (currentWave - 1) * 0.20f;
            float speedMult = Mathf.Min(1.6f, 1f + (currentWave - 1) * 0.05f);

            // Generar los enemigos de la oleada de manera visible y continua
            for (int i = 0; i < totalEnemiesToSpawn; i++)
            {
                bool isBoss = isBossWave && (i == totalEnemiesToSpawn - 1);
                SpawnEnemy(i, totalEnemiesToSpawn, healthMult, damageMult, speedMult, isBoss);
                enemiesSpawnedSoFar++;
                yield return new WaitForSeconds(0.12f);
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
            int waveBonusGold = isBossWave ? (100 + currentWave * 20) : (25 + currentWave * 10);
            if (Inventory.Instance != null)
            {
                Inventory.Instance.AddGold(waveBonusGold);
                HUDUI hud = FindAnyObjectByType<HUDUI>();
                if (hud != null)
                {
                    hud.ShowNotification(isBossWave 
                        ? $"¡JEFE DERROTADO! +{waveBonusGold} Oro" 
                        : $"¡Ronda {currentWave} superada! +{waveBonusGold} Oro");
                }
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

    private void SpawnEnemy(int index, int total, float healthMult, float damageMult, float speedMult, bool isBoss = false)
    {
        EnemyData chosenData = null;

        if (isBoss)
        {
            EnemyData[] all = Resources.FindObjectsOfTypeAll<EnemyData>();
            foreach (var e in all)
            {
                if (e != null && (e.name.ToLower().Contains("mutant") || e.enemyName.ToLower().Contains("mutant")))
                {
                    chosenData = e;
                    break;
                }
            }
        }

        if (chosenData == null)
        {
            if (enemyDataList == null || enemyDataList.Count == 0)
            {
                EnemyData[] foundEnemies = Resources.FindObjectsOfTypeAll<EnemyData>();
                if (foundEnemies != null && foundEnemies.Length > 0)
                {
                    enemyDataList = new List<EnemyData>();
                    foreach (var e in foundEnemies)
                    {
                        if (e != null && (e.name.ToLower().Contains("zombie") || e.enemyName.ToLower().Contains("zombie")))
                        {
                            enemyDataList.Add(e);
                        }
                    }

                    if (enemyDataList.Count == 0)
                    {
                        foreach (var e in foundEnemies)
                        {
                            if (e != null && !e.name.ToLower().Contains("mutant"))
                            {
                                enemyDataList.Add(e);
                                break;
                            }
                        }
                    }
                }
            }

            if (enemyDataList != null && enemyDataList.Count > 0)
            {
                int enemyIndex = UnityEngine.Random.Range(0, enemyDataList.Count);
                chosenData = enemyDataList[enemyIndex];
            }
        }

        if (chosenData == null) return;

        // Posición de spawn distribuida en abanico/círculo alrededor del jugador
        Vector3 spawnPos = GetSpawnPosition(index, total, chosenData.groundYOffset);

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

    private Vector3 GetSpawnPosition(int index, int total, float yOffset = 0f)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        // Distribución angular alrededor del jugador (360 grados repartidos con dispersión aleatoria)
        float baseAngle = (total > 0) ? (index * (360f / total)) : UnityEngine.Random.Range(0f, 360f);
        float angle = (baseAngle + UnityEngine.Random.Range(-18f, 18f)) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(spawnRadius * 0.85f, spawnRadius * 1.25f);

        float x = center.x + Mathf.Sin(angle) * dist;
        float z = center.z + Mathf.Cos(angle) * dist;

        return new Vector3(x, yOffset, z);
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
