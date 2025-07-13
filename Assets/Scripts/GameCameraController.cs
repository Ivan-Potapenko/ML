using UnityEngine;

namespace HideAndSeek {

    public class GameCameraController : MonoBehaviour {

        [SerializeField]
        private float _smooth;

        private Vector3 _velocity = Vector3.zero;

        [SerializeField]
        private Vector3 _cameraPlayerPositionDifference;

        private GameObject _character;

        public void Init(GameObject character) {
            _character = character;
            transform.position = _character.gameObject.transform.position + _cameraPlayerPositionDifference;
        }

        public void Update() {
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition() {
            if (_character == null) {
                return;
            }

            var newPosition = Vector3.SmoothDamp(transform.position, _character.gameObject.transform.position + _cameraPlayerPositionDifference, ref _velocity, _smooth);
            transform.position = newPosition;
            transform.rotation = _character.transform.rotation;
        }
    }
}