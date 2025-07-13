using UnityEngine;

namespace HideAndSeek {

    public class Box : MonoBehaviour {

        [SerializeField]
        private Health _health;

        public void Init(HideAndSeekAgent agent) {
            _health.Init(agent.AgentTeamId);
            _health.onDead += () => Destroy(gameObject);
        }
    }
}