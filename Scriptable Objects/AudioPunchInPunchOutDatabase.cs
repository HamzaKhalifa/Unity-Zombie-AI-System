using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioPunchInPunchOutInfo {
    public AudioClip clip = null;
    public float startTime = 0f;
    public float endTime = 0f;
}

[CreateAssetMenu(fileName = "New Audio Punch-In Punch-Out Database")]
public class AudioPunchInPunchOutDatabase : ScriptableObject
{
    [SerializeField] List<AudioPunchInPunchOutInfo> _dataList = new List<AudioPunchInPunchOutInfo>();

    protected Dictionary<AudioClip, AudioPunchInPunchOutInfo> _dataDictionary = new Dictionary<AudioClip, AudioPunchInPunchOutInfo>();

    void OnEnable()
    {
        foreach(AudioPunchInPunchOutInfo info in _dataList) {
            if (info.clip) {
                _dataDictionary[info.clip] = info;
            }
        }
    }

    public AudioPunchInPunchOutInfo GetClipInfo(AudioClip clip) {
        if (_dataDictionary.ContainsKey(clip)) {
            return _dataDictionary[clip];
        }

        return null;
    }
}
