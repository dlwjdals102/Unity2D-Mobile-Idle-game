using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    /// <summary>
    /// 개발용 버튼 모음. 대기 시간 없이 상태를 만들어 확인하기 위한 것이다.
    /// 에디터와 개발 빌드에서만 살아 있고, 배포 빌드에서는 스스로 꺼진다.
    /// 필요한 기능이 생길 때마다 여기에 버튼을 하나씩 추가한다.
    /// <para>
    /// 전투 객체가 아니라 <see cref="GameLoop"/>을 들고 있다.
    /// 세이브 리셋으로 전투가 새로 만들어져도 버려진 객체를 가리키지 않기 위해서다.
    /// </para>
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        [SerializeField] private Button _clearFloorButton;
        [SerializeField] private Button _deleteSaveButton;

        private GameLoop _game;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            gameObject.SetActive(false);
#endif
        }

        public void Bind(GameLoop game)
        {
            _game = game;

            _clearFloorButton.onClick.AddListener(ClearFloor);
            _deleteSaveButton.onClick.AddListener(DeleteSave);
        }

        private void ClearFloor() => _game.ClearCurrentFloor();

        private void DeleteSave() => _game.ResetToNewGame();
    }
}
