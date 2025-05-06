using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace HideAndSeek {

    public class Hider : Agent {

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private Environment _environment;

        private float _previousDistanceToHider;

        public override void OnEpisodeBegin() {
            base.OnEpisodeBegin();
            // _environment.ResetEnvironment();
        }

        public override void CollectObservations(VectorSensor sensor) {
            base.CollectObservations(sensor);
            sensor.AddObservation(transform.position - _environment.Seeker.transform.position);
        }

        public override void OnActionReceived(ActionBuffers actions) {
            base.OnActionReceived(actions);
            var moveX = actions.DiscreteActions[0] - 1;
            var moveZ = actions.DiscreteActions[1] - 1;

            _rigidbody.velocity = new Vector3(moveX, 0, moveZ) * _speed;
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            base.Heuristic(actionsOut);
            var actions = actionsOut.ContinuousActions;
            /* actions[0] = Input.GetAxis("Horizontal");
             actions[1] = Input.GetAxis("Vertical");*/
        }

        private void OnCollisionEnter(Collision collision) {
            if (collision.gameObject.tag == "Physics") {
                AddReward(0.01f);
            }
        }
    }
}
