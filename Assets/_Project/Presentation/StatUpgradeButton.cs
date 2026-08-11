using Game.Core;
using Game.Gameplay.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// 스탯 강화 하나를 사는 버튼. 단계와 비용을 보여주고, 골드가 모자라면 눌리지 않는다.
    /// </summary>
    public sealed class StatUpgradeButton : MonoBehaviour
    {
        [SerializeField] private string _displayName = "공격력";
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _costLabel;
        [SerializeField] private Button _button;

        private StatUpgrades _upgrades;
        private StatUpgrade _upgrade;

        // 화면에 지금 찍혀 있는 상태. 바뀔 때만 갱신해 매 프레임 문자열을 만들지 않는다.
        private int _shownLevel = -1;
        private bool _shownAffordable;

        // 비용은 단계에서 거듭제곱으로 계산되므로 매 프레임 다시 구하지 않고 단계가 바뀔 때만 갱신한다.
        private BigNumber _cachedCost;

        public void Bind(StatUpgrades upgrades, StatUpgrade upgrade)
        {
            _upgrades = upgrades;
            _upgrade = upgrade;

            _nameLabel.text = _displayName;

            // 첫 Refresh가 반드시 상태를 다시 쓰도록 시작값을 맞춰둔다.
            _shownAffordable = false;
            _button.interactable = false;

            _button.onClick.AddListener(Purchase);
        }

        public void Refresh(BigNumber gold)
        {
            if (_upgrade.Level != _shownLevel)
            {
                _shownLevel = _upgrade.Level;
                _cachedCost = _upgrade.Cost;

                _levelLabel.text = "Lv." + _shownLevel;
                _costLabel.text = _cachedCost.ToString();
            }

            bool isAffordable = gold >= _cachedCost;
            if (isAffordable == _shownAffordable) return;

            _shownAffordable = isAffordable;
            _button.interactable = isAffordable;
        }

        private void Purchase() => _upgrades.TryPurchase(_upgrade);
    }
}
