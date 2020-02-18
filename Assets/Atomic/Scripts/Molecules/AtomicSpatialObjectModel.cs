using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Molecules
{
    // Saves, loads, and manages all SOM elements including atoms, molecules, data connections, and execution connections.
    // The SOM is like the DOM in web dev, but with focus on objects within a space rather than objects within a document.
    public class AtomicSpatialObjectModel : Singleton<AtomicSpatialObjectModel>
    {
        private Dictionary<Guid, Atom> allSessionAtoms;

        private void SaveAllDataToHardDisk()
        {
            // save all atoms
            // save all molecules
            // save all adjacency matrices
            // save execution tree
            // save session metadata
        }

        private void LoadAll()
        {

        }

        private void StartNewSession()
        {

        }

        private void LoadPreviousSession()
        {

        }

        public Guid CreateAtom(GameObject primitive)
        {
            Guid atomGuid = Guid.NewGuid();
            allSessionAtoms.Add(atomGuid, new Atom(atomGuid, primitive));
            return atomGuid;
        }
    }
}
