using Atomic.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Transformation
{
    public interface IListenForTransformation
    {
        void OnBeginDrag(Hand hand);
        void OnEndDrag(Hand hand);
    }
}


