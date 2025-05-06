using System;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace HideAndSeek {

    public class Seeker : Agent {

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private Environment _environment;

        [SerializeField]
        private AnimationCurve _rewardCurve;

        [SerializeField]
        private int _stepsCount;

        [SerializeField]
        private float _stopNegativeRewardDistance = 1;

        private int _currentStep;
        private float _previousDistanceToHider;

        private Vector3 _previousPosition;

        public override void OnEpisodeBegin() {
            base.OnEpisodeBegin();
            _environment.ResetEnvironment();
            _previousDistanceToHider = (transform.position - _environment.Hider.transform.position).magnitude;
            _currentStep = 0;
            _previousPosition = transform.position;
        }

        public override void CollectObservations(VectorSensor sensor) {
            base.CollectObservations(sensor);
            sensor.AddObservation((-transform.position + _environment.Hider.transform.position).normalized * 5f);
            sensor.AddObservation((transform.position - _environment.Hider.transform.position).magnitude * 5f);
            sensor.AddObservation(_currentStep / _stepsCount);
            sensor.AddObservation((_previousPosition - transform.position).magnitude);
        }

        public override void OnActionReceived(ActionBuffers actions) {
            base.OnActionReceived(actions);
            var moveX = actions.DiscreteActions[0] - 1;
            var moveZ = actions.DiscreteActions[1] - 1;
            _currentStep++;
            _rigidbody.velocity = new Vector3(moveX, 0, moveZ) * _speed;

            var distanceToHider = Vector3.Distance(transform.localPosition, _environment.Hider.transform.localPosition);
            if (_previousDistanceToHider > distanceToHider) {
                AddReward((1 / _stepsCount) * _rewardCurve.Evaluate(Mathf.Clamp01(distanceToHider / 40)));
                _previousDistanceToHider = distanceToHider;
            }

            if ((_previousPosition - transform.position).magnitude < _stopNegativeRewardDistance) {
                AddReward(-0.1f / _stepsCount);
            } else {
                AddReward(1f / _stepsCount);
            }
            _previousPosition = transform.position;
            AddReward(-1f / _stepsCount);
            _environment.Hider.AddReward(1 / _stepsCount);

            if (_currentStep >= _stepsCount) {
                EndEpisode();
                _environment.Hider.EndEpisode();
                return;
            }

            if (distanceToHider < 2f) {
                _environment.TrackSeekerWin();
                AddReward(2);
                _environment.Hider.AddReward(-2.0f);
                EndEpisode();
                _environment.Hider.EndEpisode();
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            base.Heuristic(actionsOut);
            var actions = actionsOut.DiscreteActions;
            actions[0] = (int)(Input.GetAxis("Vertical") + 1);
            actions[1] = (int)(-Input.GetAxis("Horizontal") + 1);
        }

        private void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.tag == "Wall") {
                AddReward(-0.1f / _stepsCount);
            }
            if (collision.gameObject.tag == "Physics") {
                AddReward(1f / _stepsCount);
            }
        }
    }
}