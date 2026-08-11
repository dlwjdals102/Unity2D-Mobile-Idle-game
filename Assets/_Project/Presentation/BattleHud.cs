using Game.Core;
using Game.Gameplay.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// 전투 화면의 상태 표시.
    /// 값이 실제로 바뀐 항목만 갱신한다. 매 프레임 문자열을 새로 만들면 그대로 GC 부하가 되는데,
    /// 층·골드·처치 수는 초당 몇 번 바뀌지 않으므로 대부분의 프레임에서 할당이 0이 된다.
    /// </summary>
    public sealed class BattleHud : MonoBehaviour
    {
        [SerializeField] private TMP_Text _floorLabel;
        [SerializeField] private TMP_Text _goldLabel;
        [SerializeField] private TMP_Text _diamondLabel;
        [SerializeField] private TMP_Text _killProgressLabel;
        [SerializeField] private TMP_Text _bossTimerLabel;
        [SerializeField] private Image _healthBarFill;

        private BattleRunner _battle;

        // 화면에 지금 찍혀 있는 값. 실제 값과 다를 때만 문자열을 새로 만든다.
        private int _shownFloor = -1;
        private int _shownKills = -1;
        private int _shownRequiredKills = -1;
        private int _shownBossTenths = -1;
        private int _shownDiamonds = -1;
        private BigNumber _shownGold = new BigNumber(-1d, 0);

        public void Bind(BattleRunner battle) => _battle = battle;

        public void Refresh()
        {
            if (_battle.Floor != _shownFloor)
            {
                _shownFloor = _battle.Floor;
                _floorLabel.text = _shownFloor + "층";
            }

            // 필요 처치 수도 함께 본다. 보스 층에 들어서면 처치 수가 0으로 같아도
            // 분모가 10에서 1로 바뀌기 때문이다.
            if (_battle.KillsOnFloor != _shownKills || _battle.RequiredKills != _shownRequiredKills)
            {
                _shownKills = _battle.KillsOnFloor;
                _shownRequiredKills = _battle.RequiredKills;
                _killProgressLabel.text = _shownKills + " / " + _shownRequiredKills;
            }

            if (!_battle.Gold.Equals(_shownGold))
            {
                _shownGold = _battle.Gold;
                _goldLabel.text = _shownGold.ToString();
            }

            if (_battle.Diamonds != _shownDiamonds)
            {
                _shownDiamonds = _battle.Diamonds;
                _diamondLabel.text = _shownDiamonds.ToString();
            }

            _healthBarFill.fillAmount = HealthRatio();
            RefreshBossTimer();
        }

        /// <summary>
        /// 체력 비율. 체력 자체는 double 범위를 넘어설 수 있지만 비율은 항상 0~1이므로,
        /// BigNumber끼리 나눈 뒤에 double로 내린다. 순서를 바꾸면 무한대가 되어 계산이 깨진다.
        /// </summary>
        private float HealthRatio()
        {
            double ratio = (_battle.MonsterHealth / _battle.MonsterMaxHealth).ToDouble();
            return Mathf.Clamp01((float)ratio);
        }

        private void RefreshBossTimer()
        {
            bool isBossFloor = _battle.IsBossFloor;
            if (_bossTimerLabel.gameObject.activeSelf != isBossFloor)
                _bossTimerLabel.gameObject.SetActive(isBossFloor);

            if (!isBossFloor) return;

            // 0.1초 단위까지만 보여주므로, 표시값이 바뀌는 초당 10번만 문자열을 만든다.
            int tenths = Mathf.Max(0, Mathf.CeilToInt((float)_battle.BossSecondsRemaining * 10f));
            if (tenths == _shownBossTenths) return;

            _shownBossTenths = tenths;
            _bossTimerLabel.text = (tenths / 10f).ToString("0.0");
        }
    }
}
