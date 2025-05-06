using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HideAndSeek {

    public class Environment : MonoBehaviour {

        [SerializeField]
        private Hider _hider;
        public Hider Hider => _hider;

        [SerializeField]
        private Seeker _seeker;
        public Seeker Seeker => _seeker;

        [SerializeField]
        private Transform[] _spawnPoints;

        [SerializeField]
        private MeshRenderer[] _objectsToIndicate;

        [SerializeField]
        private GameObject[] _randomWalls;

        [SerializeField]
        private int _randomWallsToDisableCount;

        [SerializeField]
        private Material _seekerWinMaterial;

        [SerializeField]
        private Material _hiderWinMaterial;

        private bool _seekerWin;

        public void ResetEnvironment() {
            var spawnPoints = _spawnPoints.ToList();

            SetIndicatorMaterial(_seekerWin);
            _seekerWin = false;

            _hider.transform.position = GetRandomPosition(spawnPoints);
            _seeker.transform.position = GetRandomPosition(spawnPoints);
            RandomWalls();
        }

        private void RandomWalls() {
            foreach (var wall in _randomWalls) {
                wall.gameObject.SetActive(true);
            }
            var wallList = new List<GameObject>(_randomWalls);
            for (var i = 0; i < _randomWallsToDisableCount; i++) {
                var randomWall = wallList[Random.Range(0, wallList.Count)];
                wallList.Remove(randomWall);
                randomWall.gameObject.SetActive(false);
            }
        }

        private Vector3 GetRandomPosition(List<Transform> spawnPoints) {
            var point = spawnPoints[Random.Range(0, spawnPoints.Count)];
            spawnPoints.Remove(point);
            return point.position;
        }

        public void TrackSeekerWin() {
            _seekerWin = true;
        }

        private void SetIndicatorMaterial(bool seekerWin) {
            foreach (var indicator in _objectsToIndicate) {
                if (seekerWin) {
                    indicator.material = _seekerWinMaterial;
                } else {
                    indicator.material = _hiderWinMaterial;
                }
            }
        }
    }
}

