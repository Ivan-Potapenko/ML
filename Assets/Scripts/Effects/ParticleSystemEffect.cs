using UnityEngine;

namespace Infrastructure {

    public class ParticleSystemEffect : AbstractEffect {

        [SerializeField]
        private ParticleSystem[] _particleSystems;

        public override void Show() {
            for (var i = 0; i < _particleSystems.Length; i++) {
                _particleSystems[i].Play();
            }
        }

        public override void Hide() {
            for (var i = 0; i < _particleSystems.Length; i++) {
                _particleSystems[i].Stop();
            }
        }
    }
}
