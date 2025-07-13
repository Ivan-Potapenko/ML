using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Infrastructure {

    public class AnimatorEffect : AbstractEffect {

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private string _showTriggerKey;

        [SerializeField]
        private string _hideTriggerKey;

        public override void Hide() {
            _animator.SetTrigger(_hideTriggerKey);
        }

        public override void Show() {
            _animator.SetTrigger(_showTriggerKey);
        }
    }
}