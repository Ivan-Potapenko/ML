using Infrastructure;
using System.Linq;
using UnityEngine;

namespace HideAndSeek {

    public class RaycastWeapon : AbstractWeapon {

        [SerializeField]
        private float _raycastRadius;

        [SerializeField]
        private float _raycastDistance;

        [SerializeField]
        private LayerMask _layerMask;

        [SerializeField]
        private Transform _shootPoint;

        [SerializeField]
        private AbstractEffect _shootEffect;

        protected override void AttackInternal() {
            var hits = Physics.SphereCastAll(_shootPoint.transform.position, _raycastRadius, _shootPoint.transform.forward, _raycastDistance, _layerMask);
            Debug.DrawRay(_shootPoint.transform.position, _shootPoint.transform.forward);
            var orderedHits = hits.OrderBy(hit => hit.distance);
            foreach (var hit in orderedHits) {
                if (hit.collider.gameObject == Owner.gameObject) {
                    continue;
                }
                if (hit.collider.gameObject.TryGetComponent<Health>(out var health)) {
                    _shootEffect.Show();
                    DoDamage(health);
                }
                return;
            }
        }
    }
}