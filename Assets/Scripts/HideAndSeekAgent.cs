using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace HideAndSeek {

    public class HideAndSeekAgent : Agent {

        public enum TeamId {
            Seeker,
            Hider,
        }

        [SerializeField]
        private BufferSensorComponent _enemyBufferSensor;
        [SerializeField]
        private BufferSensorComponent _allyBufferSensor;

        [SerializeField]
        private TeamId _teamId;
        public TeamId AgentTeamId => _teamId;

        [SerializeField]
        private Health _health;
        public Health Health => _health;

        [SerializeField]
        private AbstractWeapon _weapon;

        [Header("Move")]
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private float _smoothingFactor = 0.2f;

        [SerializeField]
        private int _stepsBetweenUpdateReward;

        [SerializeField]
        private float _angleToEnemyForReward;

        [SerializeField]
        private float _angleReward;

        [SerializeField]
        private BehaviorParameters _behaviorParameters;
        public BehaviorParameters BehaviorParameters => _behaviorParameters;

        [SerializeField]
        private DemonstrationRecorder _demonstrationRecorder;
        public DemonstrationRecorder DemonstrationRecorder => _demonstrationRecorder;

        private int _currentStepBetweenUpdateReward;
        public HideAndSeekAgent ClosestEnemy { get; private set; }

        public Environment Environment { get; private set; }

        public void Init(Environment environment) {
            Environment = environment;
            _weapon.Init(this);
            _health.onDead += OnDead;
        }

        public virtual void ResetAgent() {
            _health.Init(_teamId);
            gameObject.SetActive(true);
            ClosestEnemy = null;
            _currentStepBetweenUpdateReward = _stepsBetweenUpdateReward;
        }

        private void OnDead() {
            gameObject.SetActive(false);
        }

        public override void OnActionReceived(ActionBuffers actions) {
            base.OnActionReceived(actions);
            TrackAgentActions(actions);
            float reward = GetCumulativeReward();
            Color rewardColor = Color.blue;
            if (reward > 0f) rewardColor = Color.green;
            if (reward < 0f) rewardColor = Color.red;
            Debug.DrawRay(transform.position, 20f * Mathf.Abs(reward) * Vector3.up, rewardColor);
        }

        private void CalculateClosestEnemy() {
            ClosestEnemy = null;
            var minDistance = float.MaxValue;
            var enemyTeamId = _teamId == TeamId.Hider ? TeamId.Seeker : TeamId.Hider;
            for (var i = 0; i < Environment.AgentInstancesByTeamId[enemyTeamId].Count; i++) {
                if (Environment.AgentInstancesByTeamId[enemyTeamId][i].Health.IsDead) {
                    continue;
                }
                var distanceToEnemy = 
                    (Environment.AgentInstancesByTeamId[enemyTeamId][i].transform.position 
                    - transform.position).sqrMagnitude;
                if (distanceToEnemy < minDistance) {
                    ClosestEnemy = Environment.AgentInstancesByTeamId[enemyTeamId][i];
                    minDistance = distanceToEnemy;
                }
            }
        }

        public override void CollectObservations(VectorSensor sensor) {
            base.CollectObservations(sensor);
            CalculateClosestEnemy();
            if (ClosestEnemy == null) {
                return;
            }
            var vectorToEnemy = ClosestEnemy.transform.position - transform.position;
            sensor.AddObservation(vectorToEnemy);
            sensor.AddObservation(Vector3.Angle(vectorToEnemy, transform.forward));
            sensor.AddObservation(transform.position - Environment.CenterPoint.position);
            sensor.AddObservation(NormalizeAngle(transform.rotation.eulerAngles.y));
            sensor.AddObservation(_rigidbody.velocity);
            sensor.AddObservation(_weapon.CanAttack);
            sensor.AddObservation(Environment.StepProgress);

            foreach (var agents in Environment.AgentInstancesByTeamId) {
                foreach (var agent in agents.Value) {
                    if (agent.Health.IsDead || agent._teamId == _teamId) {
                        continue;
                    }
                    var bufferSensorComponent = _enemyBufferSensor;
                    var obs = new float[9];
                    var agentPosition = agent.transform.position - Environment.CenterPoint.position;
                    obs[0] = agentPosition.x;
                    obs[1] = agentPosition.y;
                    obs[2] = agentPosition.z;
                    obs[3] = NormalizeAngle(agent.transform.rotation.eulerAngles.y);
                    obs[4] = agent._rigidbody.velocity.x;
                    obs[5] = agent._rigidbody.velocity.y;
                    obs[6] = agent._rigidbody.velocity.z;
                    obs[7] = agent.Health.IsDead ? 1 : 0;
                    obs[8] = agent.AgentTeamId == _teamId ? 0 : 1;
                    bufferSensorComponent.AppendObservation(obs);
                }
            }
        }

        protected virtual void FixedUpdate() {
            if (_currentStepBetweenUpdateReward <= 0) {
                UpdateStepReward();
                _currentStepBetweenUpdateReward = _stepsBetweenUpdateReward;
            }
            _currentStepBetweenUpdateReward--;
        }

        protected virtual void UpdateStepReward() {
            if (ClosestEnemy == null) {
                return;
            }
            var vectorToEnemy = ClosestEnemy.transform.position - transform.position;
            if (Mathf.Abs(Vector3.SignedAngle(vectorToEnemy, transform.forward, Vector3.up)) <= _angleToEnemyForReward) {
                AddReward(_angleReward);
            } else {
                AddReward(-_angleReward);
            }
        }

        protected virtual void TrackAgentActions(ActionBuffers actions) {
            if (_health.IsDead) {
                return;
            }
            var moveX = actions.DiscreteActions[0] - 1;
            var moveZ = actions.DiscreteActions[1] - 1;
            if (actions.DiscreteActions[2] > 0) {
                _weapon.Attack();
            }
            Vector3 targetVel = (transform.forward * moveX + transform.right * -moveZ) * _speed;
            _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetVel, _smoothingFactor * Time.fixedDeltaTime);
            _rigidbody.rotation = _rigidbody.rotation * Quaternion.AngleAxis(actions.ContinuousActions[0], Vector3.up);
        }

        public override void Heuristic(in ActionBuffers actionsOut) {
            var actions = actionsOut.DiscreteActions;
            actions[0] = (int)(Input.GetAxis("Vertical") + 1);
            actions[1] = (int)(-Input.GetAxis("Horizontal") + 1);
            actions[2] = Input.GetMouseButton(0) ? 1 : 0;
            var continuousAction = actionsOut.ContinuousActions;
            continuousAction[0] = Input.mousePositionDelta.x;
        }

        private float NormalizeAngle(float angle) {
            angle = (angle + 180f) % 360f - 180f;
            angle *= Mathf.Deg2Rad;
            return angle;
        }
    }
}