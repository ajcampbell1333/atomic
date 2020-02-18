using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Input
{
    public class InputTouchStates : MonoBehaviour
    {
        [Flags]
        public enum TouchStates
        {
            none = 0,
            pointer = 1 << 0,
            thumb = 1 << 1,
            middle = 1 << 2

        }

        public enum TouchStateIndex
        {
            pointer,
            thumb,
            middle
        }
    }
}


