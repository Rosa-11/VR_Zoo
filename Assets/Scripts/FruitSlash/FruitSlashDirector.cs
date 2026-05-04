using System.Collections;
using System.Collections.Generic;
using Core.Event;
using Manager;
using UnityEngine;

namespace FruitSlash
{
    /// <summary>
    /// 切水果小游戏总控：阶段、波次、动态难度、特殊果实和完成事件。
    /// </summary>
    public class FruitSlashDirector : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private FruitSlashScoreController scoreController;
        [SerializeField] private List<FruitSlashBlade> blades = new();
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform targetCenter;
        [SerializeField] private Animator longNeckAnimator;

        [Header("果实配置")]
        [SerializeField] private List<FruitSlashFruitConfigSO> fruitConfigs = new();
        [SerializeField] private LayerMask fruitLayerMask = ~0;

        [Header("节奏")]
        [SerializeField] private bool autoStart;
        [Tooltip("果实飞行时间倍率。数值越大飞得越慢，推荐过快时调到 1.25-1.6。")]
        [SerializeField] private float flightTimeMultiplier = 1f;
        [SerializeField] private int tutorialEndCutCount = 5;
        [SerializeField] private int advancedEndCutCount = 18;
        [SerializeField] private int rainbowTriggerCutCount = 30;
        [SerializeField] private int rareInterval = 20;

        [Header("范围")]
        [SerializeField] private float tutorialHalfWidth = 0f;
        [SerializeField] private float advancedHalfWidth = 1.2f;
        [SerializeField] private float stableHalfWidth = 1.5f;
        [SerializeField] private float targetHeight = 1.25f;

        [Header("动态难度")]
        [SerializeField] private float missWindow = 10f;
        [SerializeField] private int missesToSlowDown = 3;
        [SerializeField] private int emptyWavesToSlowDown = 2;
        [SerializeField] private int successCutsToRecover = 3;

        [Header("占位果实")]
        [SerializeField] private float placeholderFruitScale = 0.32f;
        [SerializeField] private Material placeholderMaterial;

        [Header("调试")]
        [SerializeField] private bool debugLog;

        public FruitSlashStageType CurrentStage { get; private set; }
        public int CutFruitCount { get; private set; }
        public bool IsRunning { get; private set; }

        private readonly List<FruitSlashFruit> _activeFruits = new();
        private readonly Queue<float> _recentMissTimes = new();
        private Coroutine _spawnRoutine;
        private int _ordinaryCutsSinceRare;
        private int _consecutiveMisses;
        private int _consecutiveEmptyWaves;
        private int _consecutiveSuccessCuts;
        private int _currentWaveCuts;
        private bool _slowDownNextWave;
        private bool _pendingRareFruit;
        private bool _rainbowSpawned;
        private bool _completed;

        private void Awake()
        {
            if (scoreController == null)
                scoreController = GetComponentInChildren<FruitSlashScoreController>();
        }

        private void Start()
        {
            if (autoStart)
                StartGame();
        }

        /// <summary>
        /// 用于 LanTest 自举脚本配置运行时引用。
        /// </summary>
        public void ConfigureLanTest(
            FruitSlashScoreController score,
            IList<FruitSlashBlade> bladeList,
            Transform spawn,
            Transform target,
            Animator animator = null)
        {
            scoreController = score;
            blades.Clear();
            if (bladeList != null)
                blades.AddRange(bladeList);
            spawnPoint = spawn;
            targetCenter = target;
            longNeckAnimator = animator;
        }

        /// <summary>
        /// 开始小游戏。
        /// </summary>
        public void StartGame()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            _completed = false;
            _rainbowSpawned = false;
            _pendingRareFruit = false;
            CutFruitCount = 0;
            _ordinaryCutsSinceRare = 0;
            _consecutiveMisses = 0;
            _consecutiveEmptyWaves = 0;
            _consecutiveSuccessCuts = 0;
            _slowDownNextWave = false;
            _recentMissTimes.Clear();
            CurrentStage = FruitSlashStageType.Tutorial;

            if (scoreController != null)
                scoreController.ResetScore();
            GameManager.Event.Broadcast(FruitSlashEvents.Started, new EventParameter<FruitSlashDirector>(this));
            if (debugLog)
                Debug.Log("[FruitSlashDirector] Started");
            BroadcastStageChanged(CurrentStage);

            if (_spawnRoutine != null)
                StopCoroutine(_spawnRoutine);
            _spawnRoutine = StartCoroutine(SpawnLoop());
        }

        /// <summary>
        /// 停止小游戏并停止继续生成果实。
        /// </summary>
        public void StopGame()
        {
            IsRunning = false;
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }
        }

        /// <summary>
        /// 立即生成下一波，供按钮或调试调用。
        /// </summary>
        public void SpawnNextWave()
        {
            if (!IsRunning || _completed)
                return;

            if (debugLog)
                Debug.Log("[FruitSlashDirector] SpawnNextWave requested");
            SpawnWave();
        }

        /// <summary>
        /// 强制生成七彩巨大果串。
        /// </summary>
        public void ForceSpawnRainbowBunch()
        {
            if (_completed || _rainbowSpawned)
                return;

            _rainbowSpawned = true;
            if (debugLog)
                Debug.Log("[FruitSlashDirector] Spawn rainbow bunch");
            SpawnFruit(FruitSlashFruitType.RainbowBunch, true, false, true, true);
        }

        /// <summary>
        /// 果实切中回调。
        /// </summary>
        public void NotifyFruitCut(FruitSlashFruit fruit, FruitSlashBlade blade, int sameSwingCutCount)
        {
            if (fruit == null || _completed)
                return;

            _activeFruits.Remove(fruit);
            _currentWaveCuts += 1;
            _consecutiveMisses = 0;
            _consecutiveSuccessCuts += 1;

            if (fruit.IsRainbowBunch)
            {
                if (scoreController != null)
                    scoreController.CompleteRainbowBunch(fruit.RainbowReward);
                if (longNeckAnimator != null)
                    longNeckAnimator.SetTrigger("Cheer");
                if (debugLog)
                    Debug.Log($"[FruitSlashDirector] Rainbow completed, totalScore={(scoreController != null ? scoreController.TotalScore : 0)}");
                _completed = true;
                StopGame();
                GameManager.Event.Broadcast(FruitSlashEvents.Completed, new EventParameter<int>(scoreController != null ? scoreController.TotalScore : 0));
                return;
            }

            CutFruitCount += 1;
            if (scoreController != null)
                scoreController.AddFruitScore(fruit, sameSwingCutCount);
            if (debugLog)
                Debug.Log($"[FruitSlashDirector] Fruit cut: type={fruit.FruitType}, count={CutFruitCount}, sameSwing={sameSwingCutCount}, stage={CurrentStage}");

            if (fruit.IsRare)
            {
                EmpowerBlades(5f);
            }
            else
            {
                _ordinaryCutsSinceRare += 1;
                if (_ordinaryCutsSinceRare >= rareInterval)
                {
                    _ordinaryCutsSinceRare = 0;
                    _pendingRareFruit = true;
                }
            }

            if (_slowDownNextWave && _consecutiveSuccessCuts >= successCutsToRecover)
            {
                _slowDownNextWave = false;
                _consecutiveSuccessCuts = 0;
            }

            UpdateStage();
        }

        /// <summary>
        /// 果实完整落地回调。
        /// </summary>
        public void NotifyFruitMissed(FruitSlashFruit fruit)
        {
            if (fruit != null)
                _activeFruits.Remove(fruit);

            float now = Time.time;
            _recentMissTimes.Enqueue(now);
            while (_recentMissTimes.Count > 0 && now - _recentMissTimes.Peek() > missWindow)
                _recentMissTimes.Dequeue();

            _consecutiveMisses += 1;
            _consecutiveSuccessCuts = 0;

            if (_consecutiveMisses >= missesToSlowDown || _recentMissTimes.Count >= missesToSlowDown)
                RequestSlowDown();

            if (debugLog)
                Debug.Log($"[FruitSlashDirector] Fruit missed: consecutiveMisses={_consecutiveMisses}, recentMisses={_recentMissTimes.Count}");
        }

        private IEnumerator SpawnLoop()
        {
            while (IsRunning && !_completed)
            {
                _currentWaveCuts = 0;
                SpawnWave();

                float interval = GetWaveInterval(CurrentStage);
                yield return new WaitForSeconds(interval);

                if (_currentWaveCuts == 0)
                {
                    _consecutiveEmptyWaves += 1;
                    if (_consecutiveEmptyWaves >= emptyWavesToSlowDown)
                        RequestSlowDown();
                }
                else
                {
                    _consecutiveEmptyWaves = 0;
                }
            }
        }

        private void SpawnWave()
        {
            if (_rainbowSpawned || CutFruitCount >= rainbowTriggerCutCount)
            {
                ForceSpawnRainbowBunch();
                return;
            }

            int fruitCount = GetWaveFruitCount(CurrentStage);
            bool slowWave = _slowDownNextWave;
            if (slowWave)
                fruitCount = 1;

            if (debugLog)
                Debug.Log($"[FruitSlashDirector] Spawn wave: stage={CurrentStage}, fruitCount={fruitCount}, slowWave={slowWave}");

            for (int i = 0; i < fruitCount; i++)
            {
                bool useRare = _pendingRareFruit;
                if (useRare)
                    _pendingRareFruit = false;

                FruitSlashFruitType type = PickFruitType(CurrentStage, useRare);
                bool fast = !useRare && CurrentStage == FruitSlashStageType.Stable && Random.value < 0.12f;
                SpawnFruit(type, useRare, fast, false, slowWave);
            }

            if (longNeckAnimator != null)
                longNeckAnimator.SetTrigger("Throw");
        }

        private void SpawnFruit(FruitSlashFruitType type, bool rare, bool fast, bool rainbow, bool slowWave)
        {
            FruitSlashFruitConfigSO config = FindConfig(type);
            GameObject fruitObject = CreateFruitObject(config, type, rare, fast, rainbow);
            Vector3 start = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 2f + Vector3.up * 1.4f;
            Vector3 target = GetTargetPosition(slowWave);
            float flightTime = GetFlightTime(CurrentStage, slowWave, fast, config);
            Vector3 velocity = CalculateBallisticVelocity(start, target, flightTime);

            fruitObject.transform.position = start;
            FruitSlashFruit fruit = fruitObject.GetComponent<FruitSlashFruit>();
            fruit.Initialize(this, config, rare ? FruitSlashFruitType.Rare : type, rare, fast, rainbow, velocity);
            _activeFruits.Add(fruit);
        }

        private GameObject CreateFruitObject(FruitSlashFruitConfigSO config, FruitSlashFruitType type, bool rare, bool fast, bool rainbow)
        {
            GameObject fruitObject = null;
            if (config != null && config.fruitPrefab != null)
                fruitObject = Instantiate(config.fruitPrefab);

            if (fruitObject == null)
                fruitObject = CreatePlaceholderFruit(type, rare, fast, rainbow);

            if (fruitObject.GetComponent<Rigidbody>() == null)
                fruitObject.AddComponent<Rigidbody>();

            if (fruitObject.GetComponent<Collider>() == null)
            {
                SphereCollider sphere = fruitObject.AddComponent<SphereCollider>();
                sphere.radius = rainbow ? 0.55f : 0.28f;
            }

            FruitSlashFruit fruit = fruitObject.GetComponent<FruitSlashFruit>();
            if (fruit == null)
                fruit = fruitObject.AddComponent<FruitSlashFruit>();

            return fruitObject;
        }

        private GameObject CreatePlaceholderFruit(FruitSlashFruitType type, bool rare, bool fast, bool rainbow)
        {
            GameObject root = new GameObject("FruitSlash_" + type);
            root.transform.localScale = Vector3.one * (rainbow ? placeholderFruitScale * 1.8f : placeholderFruitScale);

            Color color = rare ? FruitSlashFruitConfigSO.GetDefaultColor(FruitSlashFruitType.Rare) : FruitSlashFruitConfigSO.GetDefaultColor(type);
            if (fast)
                color = FruitSlashFruitConfigSO.GetDefaultColor(FruitSlashFruitType.Fast);

            if (rainbow)
            {
                for (int i = 0; i < 5; i++)
                {
                    GameObject bead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    bead.name = "RainbowBead_" + i;
                    bead.transform.SetParent(root.transform, false);
                    bead.transform.localPosition = new Vector3((i - 2) * 0.45f, Mathf.Sin(i) * 0.12f, 0f);
                    bead.transform.localScale = Vector3.one * 0.75f;
                    ApplyColor(bead, Color.HSVToRGB(i / 5f, 0.8f, 1f));
                    Destroy(bead.GetComponent<Collider>());
                }
            }
            else
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localScale = new Vector3(1f, 1.15f, 1f);
                ApplyColor(body, color);
                Destroy(body.GetComponent<Collider>());

                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = "Cap";
                cap.transform.SetParent(root.transform, false);
                cap.transform.localPosition = new Vector3(0.12f, 0.45f, 0f);
                cap.transform.localScale = new Vector3(0.55f, 0.35f, 0.55f);
                ApplyColor(cap, Color.Lerp(color, Color.white, 0.25f));
                Destroy(cap.GetComponent<Collider>());
            }

            return root;
        }

        private void ApplyColor(GameObject target, Color color)
        {
            Renderer targetRenderer = target.GetComponent<Renderer>();
            if (targetRenderer == null)
                return;

            if (placeholderMaterial != null)
                targetRenderer.material = placeholderMaterial;
            targetRenderer.material.color = color;
        }

        private Vector3 GetTargetPosition(bool slowWave)
        {
            Vector3 center = targetCenter != null ? targetCenter.position : transform.position + Vector3.forward * 0.8f + Vector3.up * targetHeight;
            float halfWidth = GetStageHalfWidth(CurrentStage);
            if (slowWave)
                halfWidth *= 0.3f;

            center.y = targetHeight;
            center.x += Random.Range(-halfWidth, halfWidth);
            center.z += Random.Range(-0.25f, 0.25f);
            return center;
        }

        private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 target, float flightTime)
        {
            flightTime = Mathf.Max(0.2f, flightTime);
            Vector3 gravity = Physics.gravity;
            return (target - start - 0.5f * gravity * flightTime * flightTime) / flightTime;
        }

        private FruitSlashFruitType PickFruitType(FruitSlashStageType stage, bool rare)
        {
            if (rare)
                return FruitSlashFruitType.Rare;

            if (stage == FruitSlashStageType.Tutorial)
                return FruitSlashFruitType.FlameEgg;

            int index = Random.Range(0, 3);
            switch (index)
            {
                case 1:
                    return FruitSlashFruitType.GoldenFan;
                case 2:
                    return FruitSlashFruitType.ConeFruit;
                case 0:
                default:
                    return FruitSlashFruitType.FlameEgg;
            }
        }

        private int GetWaveFruitCount(FruitSlashStageType stage)
        {
            switch (stage)
            {
                case FruitSlashStageType.Advanced:
                    return Random.value < 0.35f ? 2 : 1;
                case FruitSlashStageType.Stable:
                    return 2;
                case FruitSlashStageType.Tutorial:
                default:
                    return 1;
            }
        }

        private float GetWaveInterval(FruitSlashStageType stage)
        {
            switch (stage)
            {
                case FruitSlashStageType.Advanced:
                    return 1f;
                case FruitSlashStageType.Stable:
                    return 0.8f;
                case FruitSlashStageType.Tutorial:
                default:
                    return 1.25f;
            }
        }

        private float GetFlightTime(FruitSlashStageType stage, bool slowWave, bool fast, FruitSlashFruitConfigSO config)
        {
            float time;
            if (config != null)
            {
                time = Random.Range(config.flightTimeRange.x, config.flightTimeRange.y);
            }
            else
            {
                switch (stage)
                {
                    case FruitSlashStageType.Advanced:
                        time = Random.Range(1.55f, 1.95f);
                        break;
                    case FruitSlashStageType.Stable:
                        time = Random.Range(1.35f, 1.75f);
                        break;
                    case FruitSlashStageType.Tutorial:
                    default:
                        time = Random.Range(2.2f, 2.7f);
                        break;
                }
            }

            if (fast)
                time *= 0.82f;
            if (slowWave)
                time *= 1.35f;

            return time * Mathf.Max(0.2f, flightTimeMultiplier);
        }

        private float GetStageHalfWidth(FruitSlashStageType stage)
        {
            switch (stage)
            {
                case FruitSlashStageType.Advanced:
                    return advancedHalfWidth;
                case FruitSlashStageType.Stable:
                    return stableHalfWidth;
                case FruitSlashStageType.Tutorial:
                default:
                    return tutorialHalfWidth;
            }
        }

        private FruitSlashFruitConfigSO FindConfig(FruitSlashFruitType type)
        {
            for (int i = 0; i < fruitConfigs.Count; i++)
            {
                FruitSlashFruitConfigSO config = fruitConfigs[i];
                if (config != null && config.fruitType == type)
                    return config;
            }

            return null;
        }

        private void UpdateStage()
        {
            FruitSlashStageType nextStage;
            if (CutFruitCount <= tutorialEndCutCount)
                nextStage = FruitSlashStageType.Tutorial;
            else if (CutFruitCount <= advancedEndCutCount)
                nextStage = FruitSlashStageType.Advanced;
            else
                nextStage = FruitSlashStageType.Stable;

            if (nextStage == CurrentStage)
                return;

            CurrentStage = nextStage;
            BroadcastStageChanged(CurrentStage);
        }

        private void BroadcastStageChanged(FruitSlashStageType stage)
        {
            if (debugLog)
                Debug.Log($"[FruitSlashDirector] Stage changed: {stage}");
            GameManager.Event.Broadcast(FruitSlashEvents.StageChanged, new EventParameter<FruitSlashStageType>(stage));
        }

        private void RequestSlowDown()
        {
            _slowDownNextWave = true;
            if (longNeckAnimator != null)
                longNeckAnimator.SetTrigger("ScratchHead");
            if (debugLog)
                Debug.Log("[FruitSlashDirector] Slow down next wave");
        }

        private void EmpowerBlades(float duration)
        {
            for (int i = 0; i < blades.Count; i++)
            {
                if (blades[i] != null)
                    blades[i].SetEmpowered(true, duration);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Vector3 spawn = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.forward * 2f + Vector3.up * 1.4f;
            Gizmos.DrawWireSphere(spawn, 0.15f);

            Gizmos.color = Color.yellow;
            Vector3 target = targetCenter != null ? targetCenter.position : transform.position + Vector3.forward * 0.8f + Vector3.up * targetHeight;
            Gizmos.DrawWireCube(target, new Vector3(stableHalfWidth * 2f, 0.2f, 0.5f));
        }
#endif
    }
}
