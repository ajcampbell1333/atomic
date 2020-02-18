using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Input
{
    public enum HandGestureState
    {
        Neutral,
        Insert,
        Selection,
        NonPivotSelection,
        DragSelection,
        Click,
        SqueezeAll,
        Deselection,
        DeselectAll,
        Stop
    }
}

