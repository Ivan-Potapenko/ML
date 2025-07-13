using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace HideAndSeek {

    public class Hider : HideAndSeekAgent {

        [Header("Build")]
        [SerializeField]
        private Box _boxPrefab;

        [SerializeField]
        private int _buildingsCount;

        [SerializeField]
        private Transform _buildPoint;

        [SerializeField]
        private float _buildReward;

        private int _currentBuildingsCount;

        private List<Box> _boxInstances = new List<Box>();

        public override void ResetAgent() {
            base.ResetAgent();
            _currentBuildingsCount = 0;
            for (var i = 0; i < _boxInstances.Count; i++) {
                if (_boxInstances[i] != null) {
                    Destroy(_boxInstances[i].gameObject);
                }
            }
            _boxInstances.Clear();
        }

        public override void CollectObservations(VectorSensor sensor) {
            base.CollectObservations(sensor);
            sensor.AddObservation(_currentBuildingsCount);
        }

        protected override void TrackAgentActions(ActionBuffers actions) {
            base.TrackAgentActions(actions);
            var descreteActions = actions.DiscreteActions;
            if (descreteActions[3] == 1 && _currentBuildingsCount < _buildingsCount) {
                _currentBuildingsCount++;
                var box = Instantiate(_boxPrefab, _buildPoint.position, _buildPoint.rotation);
                box.Init(this);
                _boxInstances.Add(box);
                AddReward(_buildReward);
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            base.Heuristic(actionsOut);
            var actions = actionsOut.DiscreteActions;
            actions[3] = Input.GetKeyDown(KeyCode.F) ? 1 : 0;
        }
    }
}
