using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Atomic.Input
{
    public class QWERTYCustomButtonBase : MonoBehaviour
    {
        public UnityAction OnActivate;
        [SerializeField] private Material _neutralMat, _clickedMat;

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Activator_Right" || other.tag == "Activator_Left")
                OnActivate?.Invoke();
        }
    }
}
