using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class TrackInfo {
    public string Name = string.Empty;
    public AudioMixerGroup Group = null;
    public IEnumerator TrackFader = null;
}

public class AudioPoolItem {
    public GameObject GameObject = null;
    public Transform Transform = null;
    public AudioSource AudioSource = null;
    public float Unimportance = float.MaxValue;
    public bool Playing = false;
    public IEnumerator Coroutine = null;
    public ulong ID = 0;
}

public class AudioManager : MonoBehaviour
{
    static AudioManager _instance = null;
    public static AudioManager instance { get {
            if (_instance == null)
                _instance = (AudioManager)FindObjectOfType(typeof(AudioManager));
            return _instance; 
        }
    }

    [SerializeField] AudioMixer _mixer = null;
    [SerializeField] int _maxSounds = 10;

    // Private
    Dictionary<string, TrackInfo> _tracks = new Dictionary<string, TrackInfo>();
    List<AudioPoolItem> _pool = new List<AudioPoolItem>();
    Dictionary<ulong, AudioPoolItem> _activePool = new Dictionary<ulong, AudioPoolItem>();
    List<LayeredAudioSource> _layeredAudio = new List<LayeredAudioSource>();
    ulong _idGiver = 0;
    Transform _listenerPos = null;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (_mixer == null) return;
        // By passing in string.Empty, we are getting all the mixer groups
        AudioMixerGroup[] groups = _mixer.FindMatchingGroups(string.Empty);
        foreach(AudioMixerGroup group in groups) {
            TrackInfo track = new TrackInfo();
            track.Name = group.name;
            track.Group = group;
            track.TrackFader = null;
            _tracks.Add(group.name, track);
        }

        for (int i = 0; i < _maxSounds; i++) {
            GameObject o = new GameObject("Pool Item");
            AudioSource audioSource = o.AddComponent<AudioSource>();
            o.transform.parent = transform;

            AudioPoolItem item = new AudioPoolItem();
            item.GameObject = o;
            item.AudioSource = audioSource;
            item.Transform = o.transform;
            item.Playing = false;
            o.SetActive(false);
            _pool.Add(item);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        AudioListener audioListener = FindObjectOfType<AudioListener>();
        if (audioListener != null)
            _listenerPos = audioListener.transform;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (LayeredAudioSource las in _layeredAudio) {
            if (_layeredAudio != null) {
                las.Update();
            }
        }
    }

    public float GetTrackVolume(string track) {
        if (_mixer == null) return float.MinValue;

        TrackInfo trackInfo;
        if (_tracks.TryGetValue(track, out trackInfo)) {
            float volume = 0f;
            _mixer.GetFloat(track, out volume);
            return volume;
        }

        return float.MinValue;
    }

    public AudioMixerGroup GetAudioGroupFromATrackName(string track) {
        TrackInfo trackInfo;
        if (_tracks.TryGetValue(track, out trackInfo)) {
            return trackInfo.Group;
        }

        return null;
    }

    public void SetTrackVolume(string track, float volume, float fadeTime = 0f) {
        if (_mixer) return;
        TrackInfo trackInfo;

        if(_tracks.TryGetValue(track, out trackInfo)) {
            if (trackInfo.TrackFader != null) StopCoroutine(trackInfo.TrackFader);

            if (fadeTime.Equals(0f)) {
                _mixer.SetFloat(track, volume);
            } else {
                trackInfo.TrackFader = SetTrackVolumeInternal(track, volume, fadeTime);
                StartCoroutine(trackInfo.TrackFader);
            }
        }
    }

    protected IEnumerator SetTrackVolumeInternal(string track, float volume, float fadeTime) {
        float startVolume = 0f;
        float timer = 0f;

        _mixer.GetFloat(track, out startVolume);

        while (timer < fadeTime) {
            // Unscaled delta time is never paused
            timer += Time.unscaledDeltaTime;
            _mixer.SetFloat(track, Mathf.Lerp(startVolume, volume, timer / fadeTime));
            yield return null;
        }

        _mixer.SetFloat(track, volume);
    }

    protected ulong ConfigurePoolObject(int poolIndex, string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, float unimportance, float startTime, bool ignoreAudioListenerPause = false) {
        if (poolIndex < 0 || poolIndex >= _pool.Count) return 0;

        AudioPoolItem poolItem = _pool[poolIndex];
        _idGiver++;
        AudioSource source = poolItem.AudioSource;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.ignoreListenerPause = ignoreAudioListenerPause;

        source.outputAudioMixerGroup = _tracks[track].Group;

        source.transform.position = position;

        poolItem.Playing = true;
        poolItem.Unimportance = unimportance;
        poolItem.ID = _idGiver;
        source.time = Mathf.Min(startTime, source.clip.length);
        poolItem.GameObject.SetActive(true);
        source.Play();
        poolItem.Coroutine = StopSoundDelayed(_idGiver, source.clip.length);
        StartCoroutine(poolItem.Coroutine);

        _activePool.Add(_idGiver, poolItem);

        return _idGiver;
    }

    IEnumerator StopSoundDelayed(ulong id, float duration) {

        yield return new WaitForSeconds(duration);

        AudioPoolItem activeSound;
        if (_activePool.TryGetValue(id, out activeSound))
        {
            activeSound.AudioSource.Stop();
            activeSound.AudioSource.clip = null;
            activeSound.GameObject.SetActive(false);
            _activePool.Remove(id);

            activeSound.Playing = false;
        }
    }

    public void StopSound(ulong id)
    {
        AudioPoolItem activeSound;
        if (_activePool.TryGetValue(id, out activeSound))
        {
            activeSound.AudioSource.Stop();
            activeSound.AudioSource.clip = null;
            activeSound.GameObject.SetActive(false);
            _activePool.Remove(id);

            activeSound.Playing = false;
        }
    }

    public void StopOneShotSound(ulong id) {
        AudioPoolItem activeSound;

        if (_activePool.TryGetValue(id, out activeSound))
        {
            StopCoroutine(activeSound.Coroutine);

            activeSound.AudioSource.Stop();
            activeSound.AudioSource.clip = null;
            activeSound.GameObject.SetActive(false);
            _activePool.Remove(id);

            activeSound.Playing = false;
        }
    }

    // -------------------------------------------------------------------------------
    // Name :   PlayOneShotSound
    // Desc :   Scores the priority of the sound and search for an unused pool item
    //          to use as the audio source. If one is not available an audio source
    //          with a lower priority will be killed and reused
    // -------------------------------------------------------------------------------
    public ulong PlayOneShotSound(string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, int priority = 128, float startTime = 0f, bool ignoreAudioListenerPause = false)
    {
        // Do nothing if track does not exist, clip is null or volume is zero
        if (!_tracks.ContainsKey(track) || clip == null || volume.Equals(0.0f)) return 0;

        float unimportance = (_listenerPos.position - position).sqrMagnitude / Mathf.Max(1, priority);

        int leastImportantIndex = -1;
        float leastImportanceValue = float.MaxValue;

        // Find an available audio source to use
        for (int i = 0; i < _pool.Count; i++)
        {
            AudioPoolItem poolItem = _pool[i];

            // Is this source available
            if (!poolItem.Playing) {
                return ConfigurePoolObject(i, track, clip, position, volume, spatialBlend, unimportance, startTime, ignoreAudioListenerPause);
            }
                
            else
            // We have a pool item that is less important than the one we are going to play
            if (poolItem.Unimportance > leastImportanceValue)
            {
                // Record the least important sound we have found so far
                // as a candidate to relace with our new sound request
                leastImportanceValue = poolItem.Unimportance;
                leastImportantIndex = i;
            }
        }

        // If we get here all sounds are being used but we know the least important sound currently being
        // played so if it is less important than our sound request then use replace it
        if (leastImportanceValue > unimportance)
            return ConfigurePoolObject(leastImportantIndex, track, clip, position, volume, spatialBlend, unimportance, startTime, ignoreAudioListenerPause);


        // Could not be played (no sound in the pool available)
        return 0;
    }

    public IEnumerator PlayOneShotSoundDelayed(string track, AudioClip clip, Vector3 position, float volume, float spatialBlend, float duration, int priority = 128, bool ignoreAudioListenerPause = false)
    {
        yield return new WaitForSeconds(duration);
        PlayOneShotSound(track, clip, position, volume, spatialBlend, priority);
    }

    public ILayeredAudioSource RegisterLayeredAudioSource(AudioSource audioSource, int layers) {
        if (audioSource != null && layers > 0) {
            for (int i = 0; i < _layeredAudio.Count; i++) {
                LayeredAudioSource item = _layeredAudio[i];
                if (item != null) {
                    if (item.audioSource == audioSource) {
                        return item;
                    }
                }
            }

            LayeredAudioSource newLayeredAudioSource = new LayeredAudioSource(audioSource, layers);
            _layeredAudio.Add(newLayeredAudioSource);

            return newLayeredAudioSource;
        }

        return null;
    }

    public void UnregisterLayeredAudioSource(ILayeredAudioSource source) {
        _layeredAudio.Remove((LayeredAudioSource) source);
    }

    public void UnregisterLayeredAudioSource(AudioSource source)
    {
        for (int i = 0; i < _layeredAudio.Count; i++) {
            LayeredAudioSource item = _layeredAudio[i];
            if (item != null) {
                if (item.audioSource == source) {
                    _layeredAudio.Remove((LayeredAudioSource)item);
                    return;
                }
            }
        }

    }
}
