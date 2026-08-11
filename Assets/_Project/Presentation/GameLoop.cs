using System.IO;
using Game.Core;
using Game.Core.Save;
using Game.Gameplay.Combat;
using Game.Gameplay.Progression;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 게임의 진입점. 세이브를 불러와 도메인 객체를 만들고, 매 프레임 전투를 진행시키며,
    /// 주기적으로 그리고 앱이 내려갈 때 저장한다.
    /// 씬 전체에서 <c>Update</c>를 가지는 유일한 컴포넌트다.
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        private const string SaveFileName = "save.json";
        private const float AutoSaveIntervalSeconds = 30f;

        [SerializeField] private BattleHud _hud;
        [SerializeField] private DamagePopupSpawner _popupSpawner;

        [Header("시작 스탯")]
        [SerializeField] private double _attackPower = 5d;
        [SerializeField] private double _attacksPerSecond = 2d;
        [SerializeField] private double _criticalChance = 0.15d;
        [SerializeField] private double _criticalMultiplier = 2d;

        private BattleRunner _battle;
        private SaveStore _saveStore;
        private float _secondsSinceLastSave;

        private void Awake()
        {
            if (_hud == null || _popupSpawner == null)
            {
                // 인스펙터 연결 누락은 씬 작업에서 가장 흔한 실수라, 조용히 NullReference로
                // 터지는 대신 무엇이 빠졌는지 알려주고 멈춘다.
                Debug.LogError("GameLoop: Hud 또는 PopupSpawner가 인스펙터에 연결되지 않았다.", this);
                enabled = false;
                return;
            }

            var stats = new CharacterStats
            {
                AttackPower = _attackPower,
                AttacksPerSecond = _attacksPerSecond,
                CriticalChance = _criticalChance,
                CriticalMultiplier = _criticalMultiplier
            };

            _saveStore = new SaveStore(
                Path.Combine(Application.persistentDataPath, SaveFileName),
                data => JsonUtility.ToJson(data),
                json => JsonUtility.FromJson<SaveData>(json));

            _battle = new BattleRunner(FloorFormula.Default, stats, new SystemRandomSource(), LoadProgress());

            _hud.Bind(_battle);
            _popupSpawner.Bind(_battle);
        }

        private void Update()
        {
            float deltaSeconds = Time.deltaTime;

            _battle.Tick(deltaSeconds);
            _popupSpawner.Tick(deltaSeconds);
            _hud.Refresh();

            _secondsSinceLastSave += deltaSeconds;
            if (_secondsSinceLastSave >= AutoSaveIntervalSeconds) Save();
        }

        /// <summary>모바일에서 앱이 백그라운드로 내려가는 시점. 여기서 저장하지 않으면 그대로 종료될 수 있다.</summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) Save();
        }

        private void OnApplicationQuit() => Save();

        private BattleProgress LoadProgress()
        {
            if (!_saveStore.TryLoad(out SaveData data)) return BattleProgress.Start;

            return new BattleProgress(
                data.floor,
                data.killsOnFloor,
                new BigNumber(data.goldMantissa, data.goldExponent));
        }

        private void Save()
        {
            // Awake에서 멈춘 경우와, 앱이 내려갈 때 Awake보다 먼저 불리는 경우를 막는다.
            if (_battle == null) return;

            _secondsSinceLastSave = 0f;

            BattleProgress progress = _battle.Progress;
            _saveStore.Save(new SaveData
            {
                floor = progress.Floor,
                killsOnFloor = progress.KillsOnFloor,
                goldMantissa = progress.Gold.Mantissa,
                goldExponent = progress.Gold.Exponent
            });
        }
    }
}
