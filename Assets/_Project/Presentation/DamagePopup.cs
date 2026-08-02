using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 떠오르며 사라지는 데미지 숫자 하나.
    /// <c>Update</c>를 두지 않고 <see cref="Advance"/>를 스포너가 호출하게 한 것은 의도적이다.
    /// 화면에 동시에 수십 개가 존재하는데, 각자 <c>Update</c>를 가지면 엔진이 매 프레임
    /// 개별 호출을 하게 되고 그 비용이 개수만큼 곱해진다.
    /// </summary>
    public sealed class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private float _lifetimeSeconds = 0.8f;
        [SerializeField] private float _riseSpeed = 1.5f;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _criticalColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private float _criticalScale = 1.4f;

        private float _elapsedSeconds;

        public bool IsFinished => _elapsedSeconds >= _lifetimeSeconds;

        public void Show(Vector3 worldPosition, string text, bool isCritical)
        {
            transform.position = worldPosition;
            transform.localScale = Vector3.one * (isCritical ? _criticalScale : 1f);

            _label.text = text;
            _label.color = isCritical ? _criticalColor : _normalColor;

            _elapsedSeconds = 0f;
        }

        /// <summary>스포너가 매 프레임 호출한다.</summary>
        public void Advance(float deltaSeconds)
        {
            _elapsedSeconds += deltaSeconds;
            transform.position += Vector3.up * (_riseSpeed * deltaSeconds);

            Color color = _label.color;
            color.a = 1f - Mathf.Clamp01(_elapsedSeconds / _lifetimeSeconds);
            _label.color = color;
        }
    }
}
