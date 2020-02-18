using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    public class Atom
    {
        public Atom(Guid _guid, GameObject _primitive)
        {
            guid = _guid;
            primitive = _primitive;
            if (primitive.GetComponentInChildren<AtomicSelectionModalUI>() != null)
                primitive.GetComponentInChildren<AtomicSelectionModalUI>().owner = this;
        }
        public Guid guid;
        public GameObject primitive;
    }
}
