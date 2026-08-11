using Game.Gameplay.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// 개발용 버튼 모음. 대기 시간 없이 상태를 만들어 확인하기 위한 것이다.
    /// 에디터와 개발 빌드에서만 살아 있고, 배포 빌드에서는 스스로 꺼진다.
    /// 필요한 기능이 생길 때마다 여기에 버튼을 하나씩 추가한다.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        [SerializeField] private Button _clearFloorButton;

        private BattleRunner _battle;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        public void Bind(BattleRunner battle)
        {
            _battle = battle;
            _clearFloorButton.onClick.AddListener(ClearFloor);
        }

        private void ClearFloor() => _battle.ClearFloorImmediately();
    }
}
