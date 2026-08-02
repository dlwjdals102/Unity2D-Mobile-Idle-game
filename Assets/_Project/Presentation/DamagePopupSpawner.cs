using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Combat;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// 데미지 팝업을 풀에서 꺼내 띄우고, 수명이 끝난 것을 되돌려받는다.
    /// 살아 있는 팝업을 직접 순회하므로 팝업 각자가 <c>Update</c>를 가질 필요가 없다.
    /// </summary>
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [SerializeField] private DamagePopup _prefab;
        [SerializeField] private Transform _spawnAnchor;
        [SerializeField] private int _prewarmCount = 16;
        [SerializeField] private float _spawnRadius = 0.35f;

        private readonly List<DamagePopup> _activePopups = new List<DamagePopup>();
        private ObjectPool<DamagePopup> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<DamagePopup>(
                factory: CreatePopup,
                onRent: popup => popup.gameObject.SetActive(true),
                onReturn: popup => popup.gameObject.SetActive(false));

            _pool.Prewarm(_prewarmCount);
        }

        public void Bind(BattleRunner battle) => battle.DamageDealt += OnDamageDealt;

        public void Tick(float deltaSeconds)
        {
            // 뒤에서부터 도는 이유는, 수명이 끝난 팝업을 순회 중에 안전하게 제거하기 위해서다.
            for (int i = _activePopups.Count - 1; i >= 0; i--)
            {
                DamagePopup popup = _activePopups[i];
                popup.Advance(deltaSeconds);

                if (!popup.IsFinished) continue;

                _activePopups.RemoveAt(i);
                _pool.Return(popup);
            }
        }

        private DamagePopup CreatePopup()
        {
            DamagePopup popup = Instantiate(_prefab, transform);

            // 미리 만들어둔 팝업이 화면에 보이면 안 되므로, 생성 직후 꺼둔다.
            popup.gameObject.SetActive(false);
            return popup;
        }

        private void OnDamageDealt(DamageResult damage)
        {
            DamagePopup popup = _pool.Rent();
            popup.Show(RandomSpawnPosition(), damage.Amount.ToString(), damage.IsCritical);
            _activePopups.Add(popup);
        }

        /// <summary>같은 자리에 겹쳐 찍히지 않도록 몬스터 주변에 흩뿌린다.</summary>
        private Vector3 RandomSpawnPosition()
            => _spawnAnchor.position + (Vector3)(Random.insideUnitCircle * _spawnRadius);
    }
}
