using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    public class CreationCubeBoundary : MonoBehaviour
    {
        [SerializeField] private Hand hand;

        private void OnDisable()
        {
            if (AtomicModeController.Instance != null)
            {
                if (hand == Hand.Right)
                    AtomicModeController.Instance.rightCreationCubeInBounds = false;

                if (hand == Hand.Left)
                    AtomicModeController.Instance.leftCreationCubeInBounds = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hand == Hand.Right && other.tag == "Activator_Right")
                AtomicModeController.Instance.rightCreationCubeInBounds = true;

            if (hand == Hand.Left && other.tag == "Activator_Left")
                AtomicModeController.Instance.leftCreationCubeInBounds = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (hand == Hand.Right && other.tag == "Activator_Right")
                AtomicModeController.Instance.rightCreationCubeInBounds = false;

            if (hand == Hand.Left && other.tag == "Activator_Left")
                AtomicModeController.Instance.leftCreationCubeInBounds = false;
        }
    }
}
