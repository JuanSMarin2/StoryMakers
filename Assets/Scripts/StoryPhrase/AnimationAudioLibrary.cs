using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "StoryPhrase/Animation Audio Library", fileName = "AnimationAudioLibrary")]
public class AnimationAudioLibrary : ScriptableObject
{
    [Serializable]
    public struct AudioEntry
    {
        public string name;
        public AudioClip clip;
    }

    [SerializeField] private List<AudioEntry> entries = new List<AudioEntry>();

    public bool TryGetClip(string clipName, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(clipName))
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            AudioEntry entry = entries[i];
            if (!string.IsNullOrWhiteSpace(entry.name) && entry.name == clipName)
            {
                clip = entry.clip;
                return clip != null;
            }
        }

        return false;
    }
}
