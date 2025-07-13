using System;
using UnityEngine;

namespace HideAndSeek {

    public class Health : MonoBehaviour {

        public struct DamageInfo {
            public HideAndSeekAgent damageOwner;
        }

        [SerializeField]
        private int _value;

        [SerializeField]
        private float _killReward;

        [SerializeField]
        private GameObject _onDeathEffect;

        private int _currentHealthValue;
        public bool IsDead => _currentHealthValue <= 0;
        public int CurrentHealthValue => _currentHealthValue;

        public HideAndSeekAgent.TeamId TeamId { get; private set; }

        public event Action<DamageInfo> onDamaged = delegate { };
        public event Action onDead = delegate { };

        public void Init(HideAndSeekAgent.TeamId teamId) {
            TeamId = teamId;
            _currentHealthValue = _value;
        }

        public void DoDamage(HideAndSeekAgent damageOwner, int damage) {
            _currentHealthValue -= damage;
            onDamaged?.Invoke(new DamageInfo() {
                damageOwner = damageOwner,
            });
            if (_currentHealthValue <= 0) {
                if (_onDeathEffect != null) {
                    Instantiate(_onDeathEffect, transform.position, Quaternion.identity);
                }
                damageOwner.AddReward(_killReward);
                onDead?.Invoke();
            }
        }
    }
}