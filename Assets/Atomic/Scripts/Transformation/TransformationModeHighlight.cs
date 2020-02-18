using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Transformation
{
    [RequireComponent(typeof(MeshRenderer))]
    public class TransformationModeHighlight : MonoBehaviour
    {
        [HideInInspector] public MeshRenderer hRenderer;

        private void Awake()
        {
            hRenderer = GetComponent<MeshRenderer>();
            hRenderer.enabled = false;
        }
    }
}


