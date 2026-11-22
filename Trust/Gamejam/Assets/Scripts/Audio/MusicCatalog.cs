using UnityEngine;

/// <summary>
/// Runtime-accessible list of music AudioClips stored in Resources/MusicCatalog.asset.
/// </summary>
public sealed class MusicCatalog : ScriptableObject
{
    public AudioClip[] tracks;
}
