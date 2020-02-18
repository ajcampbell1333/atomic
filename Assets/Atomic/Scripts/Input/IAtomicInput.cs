using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic {

    public interface IAtomicInput
    {
        /// <summary>
        /// a 6DOF input provider uses this method to declare that it has begun input
        /// </summary>
        /// <param name="worldSpaceInputStart">the world space position of the input provider when input began</param>
        void OnSixDofInputBegan(Vector3 worldSpaceInputStart);

        /// <summary>
        /// a 6DOF input provider (tracked hand or controller) uses this method to declare 
        /// gestural motion per frame relative to its starting position
        /// </summary>
        /// <param name="normalizedDelta">the normalized vector distance between worldSpaceStart and current position,
        ///                                 where 0 is worldSpaceStart and 1 is approximate full arm extension</param>
        void OnSixDofInputReceived(Vector3 normalizedDelta);

        /// <summary>
        /// a 6DOF input provider uses this method to declare that it has ended input
        /// </summary>
        void OnSixDofInputEnded();
    }

}

