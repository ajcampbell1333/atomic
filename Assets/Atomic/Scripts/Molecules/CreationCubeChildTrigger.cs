using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    public class CreationCubeChildTrigger : MonoBehaviour
    {
        [HideInInspector] public CreationCubeTrigger parentTrigger;

        public void OnTriggerEnter(Collider other)
        {
            parentTrigger.OnTriggerEnter(other);
        }

        public void OnTriggerExit(Collider other)
        {
            parentTrigger.OnTriggerExit(other);
        }
    }
}
