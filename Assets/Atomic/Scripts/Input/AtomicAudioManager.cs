using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atomic.Input
{
    public class AtomicAudioManager : Singleton<AtomicAudioManager>
    {
        /// <summary>
        /// In order to account for one shot instance limits, we need to be able to scale the number of audio sources per sound effect.
        /// </summary>
        public struct AudioProperties
        {
            public AudioProperties(int _childIndex, int _tally, int _instanceIndex, AudioSource _source)
            {
                childIndex = _childIndex;
                tally = _tally;
                instanceIndex = _instanceIndex;
                source = _source;
            }
            /// <summary>
            /// index of the child of this transform that is the parent of all the source instances for the given sound
            /// </summary>
            public int childIndex;

            /// <summary>
            /// tracks the number of instances used for the current instance
            /// </summary>
            public int tally;

            /// <summary>
            /// the current instance, used to iterate to the next instance when tally is too high
            /// </summary>
            public int instanceIndex;
            public AudioSource source;
        }

        [HideInInspector] public Dictionary<string, AudioProperties> sources;

        private void Awake()
        {
            sources = new Dictionary<string, AudioProperties>();
            foreach (Transform child in transform)
                sources.Add(child.name, new AudioProperties(child.GetSiblingIndex(), 0, 0, child.GetChild(0).GetComponent<AudioSource>()));
        }

        public void Play(string key)
        {
            if (sources.ContainsKey(key))
            {
                if (sources[key].tally >= 32)
                {
                    AudioSource playSource = transform.GetChild(sources[key].childIndex).GetChild(sources[key].instanceIndex).GetComponent<AudioSource>();
                    int newInstanceIndex = (sources[key].instanceIndex >= transform.GetChild(sources[key].childIndex).childCount - 1)
                                            ? 0 : sources[key].instanceIndex + 1;
                    sources[key] = new AudioProperties(sources[key].childIndex, 0, newInstanceIndex, playSource);
                }
                else sources[key] = new AudioProperties(sources[key].childIndex, sources[key].tally + 1, sources[key].instanceIndex, sources[key].source);
                sources[key].source.PlayOneShot(sources[key].source.clip);
            }
            else Debug.LogError("Attempted to play audio source named " + key + " but no source of that name was found.");
        }

        public string GetSourceNameByIndex(int index)
        {
            return transform.GetChild(index).name;
        }
    }
}

