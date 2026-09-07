using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Progression Settings")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int baseEnemiesPerWave = 6;
    [SerializeField] private int enemiesIncreasePerWave = 3;
    [SerializeField] private float spawnRadius = 12f;

    [Header("Progressive Horde Flow")]
    [Tooltip("Cantidad de zombies que aparecen de golpe al arrancar la ronda")]
    [SerializeField] private int initialBurstCount = 4;
    [Tooltip("Máximo de zombies activos simultáneamente en pantalla para no saturar el mapa")]
    [SerializeField] private int maxSimultaneousEnemies = 7;
    [Tooltip("Intervalo en segundos entre cada nuevo enemigo que entra como refuerzo")]
    [SerializeField] private float progressiveSpawnInterval = 2.0f;

    [Header("Enemy Catalog & Prefabs")]
    public List<EnemyData> enemyDataList = new List<EnemyData>();
    public GameObject enemyBasePrefab;

    [Header("Runtime State")]
    private int totalEnemiesInWave = 0;
    private int enemiesSpawnedSoFar = 0;
    private int enemiesKilledThisWave = 0;
    private bool isWaveInProgress = false;
    private float waveCountdownTimer = 0f;

    private readonly List<EnemyController> activeEnemiesList = new List<EnemyController>();

    public int CurrentWave => currentWave;
    public int EnemiesAlive => activeEnemiesList.Count;
    public int EnemiesRemainingInWave => Mathf.Max(0, totalEnemiesInWave - enemiesKilledThisWave);
    public bool IsWaveInProgress => isWaveInProgress;

    // Eventos
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<int, int> OnEnemyCountChanged; // (restantes, total)
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
            return;
        }

        CleanupEnemyDataList();
    }

    void Start()
    {
        StartCoroutine(WaveRoutine());
    }

    private void CleanupEnemyDataList()
    {
        if (enemyDataList != null)
        {
            enemyDataList.RemoveAll(e => e == null);
        }
        else
        {
            enemyDataList = new List<EnemyData>();
        }

        if (enemyDataList.Count == 0)
        {
            EnemyData[] found = Resources.FindObjectsOfTypeAll<EnemyData>();
            if (found != null && found.Length > 0)
            {
                foreach (var e in found)
                {
                    if (e != null && !enemyDataList.Contains(e))
                    {
                        enemyDataList.Add(e);
                    }
                }
            }
        }
    }

    private IEnumerator WaveRoutine()
    {
        yield return new WaitForSeconds(1.2f);

        while (true)
        {
            currentWave++;
            totalEnemiesInWave = baseEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave;
            enemiesSpawnedSoFar = 0;
            enemiesKilledThisWave = 0;
            activeEnemiesList.Clear();
            isWaveInProgress = true;

            CleanupEnemyDataList();

            OnWaveStarted?.Invoke(currentWave);
            OnEnemyCountChanged?.Invoke(totalEnemiesInWave, totalEnemiesInWave);

            Debug.Log($"[Oleadas] ¡Comenzando Oleada {currentWave}! Total de enemigos en esta ronda: {totalEnemiesInWave}");

            bool isBossWave = (currentWave % 5 == 0);
            if (isBossWave)
            {
                HUDUI hud = FindAnyObjectByType<HUDUI>();
                if (hud != null) hud.ShowNotification("ALERTA: ¡JEFE MUTANTE EN CAMINO!");
            }

            // Multiplicadores progresivos
            float healthMult = 1f + (currentWave - 1) * 0.25f;
            float damageMult = 1f + (currentWave - 1) * 0.20f;
            float speedMult = Mathf.Min(1.6f, 1f + (currentWave - 1) * 0.05f);

            // 1. RÁFAGA INICIAL: Generar un grupo inicial visible (ej. 3-5 zombies de golpe)
            int burstToSpawn = Mathf.Min(initialBurstCount, totalEnemiesInWave);
            for (int i = 0; i < burstToSpawn; i++)
            {
                bool isBoss = isBossWave && (enemiesSpawnedSoFar == totalEnemiesInWave - 1);
                SpawnSingleEnemy(enemiesSpawnedSoFar, totalEnemiesInWave, healthMult, damageMult, speedMult, isBoss);
                enemiesSpawnedSoFar++;
                yield return new WaitForSeconds(0.15f);
            }

            // 2. REFUERZOS PROGRESIVOS: Ir soltando enemigos poco a poco (1 a 1 cada X segundos)
            while (enemiesSpawnedSoFar < totalEnemiesInWave)
            {
                // Limpiar referencias nulas de la lista de activos
                activeEnemiesList.RemoveAll(e => e == null);

                // Si hay espacio en el tope simultáneo, enviar el siguiente refuerzo
                if (activeEnemiesList.Count < maxSimultaneousEnemies)
                {
                    bool isBoss = isBossWave && (enemiesSpawnedSoFar == totalEnemiesInWave - 1);
                    SpawnSingleEnemy(enemiesSpawnedSoFar, totalEnemiesInWave, healthMult, damageMult, speedMult, isBoss);
                    enemiesSpawnedSoFar++;
                    yield return new WaitForSeconds(progressiveSpawnInterval);
                }
                else
                {
                    // Si llegamos al límite en pantalla, esperar medio segundo a que el jugador reduzca la horda
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // 3. FASE FINAL DE LA RONDA: Esperar a que todos los enemigos generados sean eliminados
            while (enemiesKilledThisWave < totalEnemiesInWave || activeEnemiesList.Count > 0)
            {
                activeEnemiesList.RemoveAll(e => e == null);

                // Failsafe de seguridad: si ya no hay enemigos activos en escena pero no cuadró el contador, cerrar ronda
                if (enemiesSpawnedSoFar >= totalEnemiesInWave && activeEnemiesList.Count == 0)
                {
                    enemiesKilledThisWave = totalEnemiesInWave;
                    break;
                }

                yield return new WaitForSeconds(0.4f);
            }

            isWaveInProgress = false;
            OnWaveCompleted?.Invoke(currentWave);
            Debug.Log($"[Oleadas] ¡Oleada {currentWave} superada con éxito!");

            // Recompensa de Oro
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

            // Descanso entre rondas con cuenta regresiva
            waveCountdownTimer = timeBetweenWaves;
            while (waveCountdownTimer > 0)
            {
                OnWaveCountdownChanged?.Invoke(waveCountdownTimer);
                yield return new WaitForSeconds(1f);
                waveCountdownTimer -= 1f;
            }
        }
    }

    private void SpawnSingleEnemy(int index, int total, float healthMult, float damageMult, float speedMult, bool isBoss = false)
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
                CleanupEnemyDataList();
            }

            if (enemyDataList != null && enemyDataList.Count > 0)
            {
                // Filtrar nulos y elegir
                List<EnemyData> validList = enemyDataList.FindAll(e => e != null);
                if (validList.Count > 0)
                {
                    int enemyIndex = UnityEngine.Random.Range(0, validList.Count);
                    chosenData = validList[enemyIndex];
                }
            }
        }

        // Posición de spawn distribuida en círculo alrededor del jugador
        Vector3 spawnPos = GetSpawnPosition(index, total, chosenData != null ? chosenData.groundYOffset : 0f);

        GameObject enemyObj = null;
        if (chosenData != null && chosenData.enemyPrefab != null)
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

        if (enemyObj == null) return;

        EnemyController controller = enemyObj.GetComponent<EnemyController>();
        if (controller == null) controller = enemyObj.AddComponent<EnemyController>();

        controller.Initialize(chosenData, healthMult, damageMult, speedMult);

        if (!activeEnemiesList.Contains(controller))
        {
            activeEnemiesList.Add(controller);
        }

        OnEnemyCountChanged?.Invoke(EnemiesRemainingInWave, totalEnemiesInWave);
    }

    private Vector3 GetSpawnPosition(int index, int total, float yOffset = 0f)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 center = player != null ? player.transform.position : Vector3.zero;

        // Distribución angular alrededor del jugador
        float baseAngle = (total > 0) ? (index * (360f / total)) : UnityEngine.Random.Range(0f, 360f);
        float angle = (baseAngle + UnityEngine.Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(spawnRadius * 0.85f, spawnRadius * 1.25f);

        float x = center.x + Mathf.Sin(angle) * dist;
        float z = center.z + Mathf.Cos(angle) * dist;

        return new Vector3(x, yOffset, z);
    }

    private GameObject CreateProceduralEnemyObject(Vector3 pos, EnemyData data)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = data != null ? $"Enemy_{data.enemyName}" : "Enemy_Procedural";
        enemy.tag = "Enemy";
        enemy.transform.position = pos;

        Renderer r = enemy.GetComponent<Renderer>();
        if (r != null && data != null)
        {
            r.material.color = data.bodyColor;
        }

        Rigidbody rb = enemy.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        return enemy;
    }

    public void OnEnemyDefeated(EnemyController enemy)
    {
        if (enemy != null && activeEnemiesList.Contains(enemy))
        {
            activeEnemiesList.Remove(enemy);
        }
        else
        {
            activeEnemiesList.RemoveAll(e => e == null);
        }

        enemiesKilledThisWave++;
        OnEnemyCountChanged?.Invoke(EnemiesRemainingInWave, totalEnemiesInWave);
    }

    public void UnregisterActiveEnemy(EnemyController enemy)
    {
        if (enemy != null && activeEnemiesList.Contains(enemy))
        {
            activeEnemiesList.Remove(enemy);
            enemiesKilledThisWave++;
            OnEnemyCountChanged?.Invoke(EnemiesRemainingInWave, totalEnemiesInWave);
        }
    }
}
