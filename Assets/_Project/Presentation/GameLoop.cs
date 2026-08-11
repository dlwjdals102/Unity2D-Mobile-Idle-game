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
        [SerializeField] private StatUpgradeButton _attackPowerButton;
        [SerializeField] private StatUpgradeButton _criticalMultiplierButton;

        [Header("개발용 (없어도 된다)")]
        [SerializeField] private DebugPanel _debugPanel;

        [Header("강화로 오르지 않는 스탯")]
        [SerializeField] private double _attacksPerSecond = 2d;
        [SerializeField] private double _criticalChance = 0.15d;

        private CharacterStats _stats;
        private BattleRunner _battle;
        private StatUpgrades _upgrades;
        private SaveStore _saveStore;
        private float _secondsSinceLastSave;

        private void Awake()
        {
            if (_hud == null || _popupSpawner == null ||
                _attackPowerButton == null || _criticalMultiplierButton == null)
            {
                // 인스펙터 연결 누락은 씬 작업에서 가장 흔한 실수라, 조용히 NullReference로
                // 터지는 대신 무엇이 빠졌는지 알려주고 멈춘다.
                Debug.LogError("GameLoop: 인스펙터에 연결되지 않은 참조가 있다.", this);
                enabled = false;
                return;
            }

            // 공격력과 치명타 배율은 StatUpgrades가 단계에서 계산해 채운다.
            _stats = new CharacterStats
            {
                AttacksPerSecond = _attacksPerSecond,
                CriticalChance = _criticalChance
            };

            _saveStore = new SaveStore(
                Path.Combine(Application.persistentDataPath, SaveFileName),
                data => JsonUtility.ToJson(data),
                json => JsonUtility.FromJson<SaveData>(json));

            if (_saveStore.TryLoad(out SaveData saved))
            {
                var progress = new BattleProgress(
                    saved.floor,
                    saved.killsOnFloor,
                    new BigNumber(saved.goldMantissa, saved.goldExponent),
                    saved.diamonds);

                StartSession(progress, saved.attackPowerLevel, saved.criticalMultiplierLevel);
            }
            else
            {
                StartSession(BattleProgress.Start, 0, 0);
            }

            // 개발용이라 연결하지 않아도 정상 동작한다.
            // 세션이 아니라 GameLoop에 묶으므로, 리셋으로 러너가 바뀌어도 다시 연결할 필요가 없다.
            if (_debugPanel != null) _debugPanel.Bind(this);
        }

        private void Update()
        {
            float deltaSeconds = Time.deltaTime;

            _battle.Tick(deltaSeconds);
            _popupSpawner.Tick(deltaSeconds);

            _hud.Refresh();
            _attackPowerButton.Refresh(_battle.Gold);
            _criticalMultiplierButton.Refresh(_battle.Gold);

            _secondsSinceLastSave += deltaSeconds;
            if (_secondsSinceLastSave >= AutoSaveIntervalSeconds) Save();
        }

        /// <summary>모바일에서 앱이 백그라운드로 내려가는 시점. 여기서 저장하지 않으면 그대로 종료될 수 있다.</summary>
        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused) Save();
        }

        private void OnApplicationQuit() => Save();

        // ---------- 개발용 ----------

        /// <summary>현재 층을 즉시 클리어한다.</summary>
        public void ClearCurrentFloor() => _battle.ClearFloorImmediately();

        /// <summary>
        /// 세이브를 지우고 그 자리에서 새 게임을 시작한다.
        /// 파일만 지우면 플레이 모드를 빠져나갈 때 종료 저장이 곧바로 되살리므로 상태까지 함께 되돌린다.
        /// </summary>
        public void ResetToNewGame()
        {
            _saveStore.Delete();
            _secondsSinceLastSave = 0f;

            StartSession(BattleProgress.Start, 0, 0);
        }

        // ---------- 내부 ----------

        /// <summary>도메인 객체를 만들고 화면에 연결한다. 리셋 때 다시 호출된다.</summary>
        private void StartSession(BattleProgress progress, int attackPowerLevel, int criticalMultiplierLevel)
        {
            _battle = new BattleRunner(FloorFormula.Default, _stats, new SystemRandomSource(), progress);
            _upgrades = StatUpgrades.CreateDefault(_stats, _battle);
            _upgrades.Restore(attackPowerLevel, criticalMultiplierLevel);

            _hud.Bind(_battle);
            _popupSpawner.Bind(_battle);
            _attackPowerButton.Bind(_upgrades, _upgrades.AttackPower);
            _criticalMultiplierButton.Bind(_upgrades, _upgrades.CriticalMultiplier);
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
                goldExponent = progress.Gold.Exponent,
                diamonds = progress.Diamonds,
                attackPowerLevel = _upgrades.AttackPower.Level,
                criticalMultiplierLevel = _upgrades.CriticalMultiplier.Level
            });
        }
    }
}
