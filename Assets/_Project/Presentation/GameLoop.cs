using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Progression;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 게임의 진입점. 도메인 객체를 만들어 표현 계층에 연결하고, 매 프레임 전투를 진행시킨다.
    /// 씬 전체에서 <c>Update</c>를 가지는 유일한 컴포넌트다.
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        [SerializeField] private BattleHud _hud;
        [SerializeField] private DamagePopupSpawner _popupSpawner;

        [Header("시작 스탯")]
        [SerializeField] private double _attackPower = 5d;
        [SerializeField] private double _attacksPerSecond = 2d;
        [SerializeField] private double _criticalChance = 0.15d;
        [SerializeField] private double _criticalMultiplier = 2d;

        private BattleRunner _battle;

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

            _battle = new BattleRunner(FloorFormula.Default, stats, new SystemRandomSource());

            _hud.Bind(_battle);
            _popupSpawner.Bind(_battle);
        }

        private void Update()
        {
            float deltaSeconds = Time.deltaTime;

            _battle.Tick(deltaSeconds);
            _popupSpawner.Tick(deltaSeconds);
            _hud.Refresh();
        }
    }
}
