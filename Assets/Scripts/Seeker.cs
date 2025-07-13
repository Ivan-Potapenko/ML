using UnityEngine;

namespace HideAndSeek {

    public class Seeker : HideAndSeekAgent {

        [SerializeField]
        private float _distanceReward;

        [SerializeField]
        private float _distanceRewardStep;

        [SerializeField]
        private float _distanceChangeReward;

        [SerializeField]
        private float _distanceChangeStep;

        private float _lastDistance = -1;
        private Vector3 _lastPosition;

        public override void ResetAgent() {
            base.ResetAgent();
            _lastPosition = gameObject.transform.position;
        }

        protected override void UpdateStepReward() {
            base.UpdateStepReward();
            if ((_lastPosition - gameObject.transform.position).sqrMagnitude < _distanceChangeStep * _distanceChangeStep) {
                AddReward(-_distanceChangeReward);
            } else {
                AddReward(_distanceChangeReward);
            }
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();
            if (ClosestEnemy == null) {
                return;
            }
            var distance = (ClosestEnemy.transform.position - transform.position).magnitude;
            if (_lastDistance == -1) {
                _lastDistance = distance;
            }

            if (distance - _distanceRewardStep > _lastDistance) {
                _lastDistance = distance;
                AddReward(-_distanceReward);
            }
            if (distance + _distanceRewardStep < _lastDistance) {
                _lastDistance = distance;
                AddReward(_distanceReward);
            }
        }
    }
}