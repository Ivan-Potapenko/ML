using UnityEngine;

namespace HideAndSeek {

    public abstract class AbstractWeapon : MonoBehaviour {

        [SerializeField]
        private int _damage;

        [SerializeField]
        private float _timeBetweenAttack;

        public bool CanAttack => _currentTimeBetweenAttack <= 0;
        public HideAndSeekAgent Owner { get; private set; }

        private float _currentTimeBetweenAttack;

        public void Init(HideAndSeekAgent owner) {
            Owner = owner;
        }

        public void Attack() {
            if (!CanAttack) {
                return;
            }
            _currentTimeBetweenAttack = _timeBetweenAttack;
            AttackInternal();
        }

        protected abstract void AttackInternal();

        protected void DoDamage(Health health) {
            if (health.TeamId == Owner.AgentTeamId) {
                return;
            }
            health.DoDamage(Owner, _damage);
        }

        private void Update() {
            if (_currentTimeBetweenAttack > 0) {
                _currentTimeBetweenAttack -= Time.deltaTime;
            }
        }
    }
}
