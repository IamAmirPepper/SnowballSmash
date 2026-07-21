using SnowballSmash.Events;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Reusable pooled lane spawner for objects that move from a far spawn point toward a near target point.
    /// </summary>
    public class LaneObjectSpawner : MonoBehaviour
    {
        private const float SpawnTickSeconds = 0.05f;

        [Header("Spawning")]
        [Tooltip("Lane definitions used by this spawner. Each lane needs a spawn point and optional target point.")]
        [SerializeField] private SpawnLane[] lanes = new SpawnLane[3];
        [Tooltip("Prefabs this spawner may randomly spawn.")]
        [SerializeField] private List<GameObject> prefabPool = new List<GameObject>();
        [Tooltip("Optional parent transform used for pooled instances.")]
        [SerializeField] private Transform poolParent;
        [Tooltip("How many instances of each prefab to create during preload.")]
        [SerializeField, Min(0)] private int preloadPerPrefab = 2;

        [Header("Travel")]
        [Tooltip("ScriptableObject that defines speed, scale, and despawn tuning for spawned objects.")]
        [SerializeField] private ObjectTravelSettings travelSettings;

        [Header("Fade")]
        [Tooltip("Whether spawned SpriteRenderers should fade in as they travel toward the camera.")]
        [SerializeField] private bool fadeIn;
        [Tooltip("Alpha multiplier applied at the start of travel when fade in is enabled.")]
        [SerializeField, Range(0f, 1f)] private float startAlpha = 0f;
        [Tooltip("Normalized travel progress where fade in reaches full alpha.")]
        [SerializeField, Range(0.01f, 1f)] private float fadeInEndProgress = 0.35f;

        private readonly GameObjectPool pool = new();
        private readonly List<PooledLaneObject> activeObjects = new();

        private Coroutine spawnRoutine;
        private Coroutine travelRoutine;
        private WaitForSeconds spawnTickWait;
        private CancellationTokenSource routineCancellation;

        /// <summary>
        /// Objects currently travelling through this spawner's lanes.
        /// </summary>
        protected IReadOnlyList<PooledLaneObject> ActiveObjects => activeObjects;

        /// <summary>
        /// Initializes cached wait instructions and preloads pooled prefab instances.
        /// </summary>
        private void Awake()
        {
            spawnTickWait = new WaitForSeconds(SpawnTickSeconds);
            PreloadPool();
        }

        /// <summary>
        /// Starts lane spawning and travel processing.
        /// </summary>
        private void OnEnable()
        {
            routineCancellation = new CancellationTokenSource();
            ResetLaneTimers();
            spawnRoutine = StartCoroutine(SpawnRoutine(routineCancellation.Token));
            travelRoutine = StartCoroutine(TravelRoutine(routineCancellation.Token));
        }

        /// <summary>
        /// Cancels active routines and stops any running coroutine handles.
        /// </summary>
        private void OnDisable()
        {
            routineCancellation?.Cancel();

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            if (travelRoutine != null)
            {
                StopCoroutine(travelRoutine);
            }

            routineCancellation?.Dispose();
            routineCancellation = null;
        }

        /// <summary>
        /// Randomizes every lane's first spawn time.
        /// </summary>
        private void ResetLaneTimers()
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                ResetLaneTimer(lanes[i]);
            }
        }

        /// <summary>
        /// Periodically checks lane timers and spawns objects until cancellation is requested.
        /// </summary>
        private IEnumerator SpawnRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TickLanes();
                yield return spawnTickWait;
            }
        }

        /// <summary>
        /// Moves active objects along their lane paths until cancellation is requested.
        /// </summary>
        private IEnumerator TravelRoutine(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MoveActiveObjects();
                yield return null;
            }
        }

        /// <summary>
        /// Builds the initial inactive object queues for every configured prefab.
        /// </summary>
        private void PreloadPool()
        {
            pool.Clear();
            activeObjects.Clear();

            foreach (GameObject prefab in prefabPool)
            {
                if (prefab == null)
                {
                    continue;
                }

                pool.Preload(prefab, poolParent, preloadPerPrefab);
            }
        }

        /// <summary>
        /// Spawns any lane whose timer has elapsed.
        /// </summary>
        private void TickLanes()
        {
            if (prefabPool.Count == 0)
            {
                return;
            }

            for (int i = 0; i < lanes.Length; i++)
            {
                SpawnLane lane = lanes[i];
                if (!lane.HasSpawnPoint || Time.time < lane.nextSpawnTime)
                {
                    continue;
                }

                SpawnInLane(lane);
                ResetLaneTimer(lane);
            }
        }

        /// <summary>
        /// Pulls a pooled instance from the selected prefab and initializes its lane travel state.
        /// </summary>
        private void SpawnInLane(SpawnLane lane)
        {
            GameObject prefab = GetRandomPrefab();
            if (prefab == null)
            {
                return;
            }

            GameObject instance = pool.Get(prefab, poolParent);
            Transform instanceTransform = instance.transform;
            Vector3 targetPosition = lane.GetTargetPosition(travelSettings.DespawnY);
            Vector3 objectBaseScale = prefab.transform.localScale;

            instanceTransform.SetPositionAndRotation(lane.spawnPoint.position, lane.spawnPoint.rotation);
            instanceTransform.localScale = objectBaseScale * travelSettings.GetScale(0f);
            instance.SetActive(true);

            PooledLaneObject pooledObject = new PooledLaneObject
            {
                gameObject = instance,
                transform = instanceTransform,
                baseScale = objectBaseScale,
                startPosition = lane.spawnPoint.position,
                targetPosition = targetPosition,
                travelDistance = Vector3.Distance(lane.spawnPoint.position, targetPosition),
                spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true)
            };
            pooledObject.spriteBaseColors = GetSpriteBaseColors(pooledObject.spriteRenderers);

            ApplyFade(pooledObject, 0f);
            activeObjects.Add(pooledObject);
        }

        /// <summary>
        /// Picks a non-empty prefab from the configured prefab pool.
        /// </summary>
        private GameObject GetRandomPrefab()
        {
            for (int attempts = 0; attempts < prefabPool.Count; attempts++)
            {
                GameObject prefab = prefabPool[Random.Range(0, prefabPool.Count)];
                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

        /// <summary>
        /// Advances all active objects and returns finished objects to their pools.
        /// </summary>
        private void MoveActiveObjects()
        {
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                PooledLaneObject pooledObject = activeObjects[i];
                Transform objectTransform = pooledObject.transform;

                float progress = pooledObject.GetTravelProgress(objectTransform.position);
                float travelSpeed = travelSettings.GetSpeed(progress);

                objectTransform.position = Vector3.MoveTowards(
                    objectTransform.position,
                    pooledObject.targetPosition,
                    travelSpeed * Time.deltaTime);

                progress = pooledObject.GetTravelProgress(objectTransform.position);
                objectTransform.localScale = pooledObject.baseScale * travelSettings.GetScale(progress);
                ApplyFade(pooledObject, progress);

                if (ShouldReturnToPool(pooledObject))
                {
                    activeObjects.RemoveAt(i);
                    pool.Return(pooledObject.gameObject, poolParent);
                }
            }
        }

        /// <summary>
        /// Determines whether an object has completed its travel path or crossed the despawn limit.
        /// </summary>
        private bool ShouldReturnToPool(PooledLaneObject pooledObject)
        {
            return pooledObject.transform.position == pooledObject.targetPosition
                || pooledObject.transform.position.y <= travelSettings.DespawnY;
        }

        /// <summary>
        /// Stores original SpriteRenderer colors so fade can preserve each renderer's authored alpha.
        /// </summary>
        private Color[] GetSpriteBaseColors(SpriteRenderer[] spriteRenderers)
        {
            Color[] colors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                colors[i] = spriteRenderers[i].color;
            }

            return colors;
        }

        /// <summary>
        /// Applies optional alpha fade-in to all SpriteRenderers on a pooled object.
        /// </summary>
        private void ApplyFade(PooledLaneObject pooledObject, float progress)
        {
            if (!fadeIn || pooledObject.spriteRenderers == null)
            {
                return;
            }

            float alphaProgress = Mathf.Clamp01(progress / Mathf.Max(0.01f, fadeInEndProgress));
            for (int i = 0; i < pooledObject.spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = pooledObject.spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                Color color = pooledObject.spriteBaseColors[i];
                color.a *= Mathf.Lerp(startAlpha, 1f, alphaProgress);
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// Schedules the next spawn time for a lane.
        /// </summary>
        private void ResetLaneTimer(SpawnLane lane)
        {
            lane.nextSpawnTime = Time.time + lane.GetRandomInterval();
        }
    }
}
