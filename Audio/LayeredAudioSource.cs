using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLayer {
    public AudioClip Clip = null;
    public AudioCollection Collection = null;
    public int Bank = 0;
    public bool Looping = true;
    public float Time = 0f;
    public float Duration = 0f;
    public bool Muted = false;

}

public interface ILayeredAudioSource
{
    bool Play(AudioCollection pool, int bank, int layer, bool lopping = true);
    void Stop(int layerIndex);
    void Mute(int layerIndex, bool mute);
    void Mute(bool mute);
}

public class LayeredAudioSource : ILayeredAudioSource
{
    AudioSource _audioSource = null;
    List<AudioLayer> _audioLayers = new List<AudioLayer>();
    int _activeLayer = -1;

    public AudioSource audioSource { get { return _audioSource; } }

    public LayeredAudioSource(AudioSource source, int layers) {
        if (source != null && layers > 0) {
            _audioSource = source;
            for (int i = 0; i < layers; i++) {
                AudioLayer layer = new AudioLayer();
                layer.Collection = null;
                layer.Bank = 0;
                layer.Looping = true;
                layer.Time = 0f;
                layer.Duration = 0f;
                layer.Muted = false;

                _audioLayers.Add(layer);
            }
        }
    }

    public bool Play(AudioCollection collection, int bank, int layer, bool looping = true) {
        // Layer must be in range
        if (layer >= _audioLayers.Count) return false;

        AudioLayer audioLayer = _audioLayers[layer];

        // If it's already doing what we want, then we just return
        if (audioLayer.Collection == collection && audioLayer.Bank == bank && audioLayer.Looping == looping) return true;

        audioLayer.Collection = collection;
        audioLayer.Bank = bank;
        audioLayer.Looping = looping;
        audioLayer.Time = 0f;
        audioLayer.Duration = 0f;
        audioLayer.Muted = false;
        audioLayer.Clip = null;

        return true;
    }

    public void Stop(int layerIndex) {
        // Layer must be in range
        if (layerIndex >= _audioLayers.Count) return;

        AudioLayer layer = _audioLayers[layerIndex];
        if (layer != null) {
            layer.Looping = false;
            layer.Time = layer.Duration;
        }
    }

    public void Mute(int layerIndex, bool mute) {
        // Layer must be in range
        if (layerIndex >= _audioLayers.Count) return;

        AudioLayer layer = _audioLayers[layerIndex];
        if (layer != null)
        {
            layer.Muted = mute;
        }
    }

    public void Mute(bool mute) {
        for (int i = 0; i < _audioLayers.Count; i++) {
            Mute(i, mute);
        }
    }

    public void Update()
    {
        int newActiveLayer = -1;
        bool refreshAudioSource = false;

        for (int i = _audioLayers.Count - 1; i >= 0; i--) {
            AudioLayer layer = _audioLayers[i];

            if (layer.Collection == null) continue;

            layer.Time += Time.deltaTime;

            if (layer.Time >= layer.Duration) {
                if (layer.Looping || layer.Clip == null) {
                    AudioClip clip = layer.Collection[layer.Bank];

                    if (clip == layer.Clip) {
                        layer.Time = layer.Time % layer.Clip.length;
                    } else {
                        layer.Time = 0f;
                    }

                    layer.Duration = clip.length;
                    layer.Clip = clip;

                    if (newActiveLayer < i) {
                        newActiveLayer = i;
                        refreshAudioSource = true;
                    }
                } else {
                    layer.Clip = null;
                    layer.Collection = null;
                    layer.Duration = 0f;
                    layer.Bank = 0;
                    layer.Looping = false;
                    layer.Time = 0f;
                }
            } else {
                if (newActiveLayer < i) newActiveLayer = i;
            }
        } 

        // If we found a new active layer (or none)
        if (newActiveLayer != _activeLayer || refreshAudioSource) {
            // Previous layers expired and no new layer, stop audioSource
            if (newActiveLayer == -1) {
                _audioSource.Stop();
                _audioSource.clip = null;
            } else {
                AudioLayer layer = _audioLayers[newActiveLayer];
                _audioSource.clip = layer.Clip;
                _audioSource.volume = layer.Muted ? 0f : layer.Collection.volume;
                _audioSource.spatialBlend = layer.Collection.spatialBlend;
                _audioSource.time = layer.Time;
                _audioSource.loop = layer.Looping;
                _audioSource.outputAudioMixerGroup = AudioManager.instance.GetAudioGroupFromATrackName(layer.Collection.audioGroup);
                _audioSource.Play();
            }
        }

        // Remember the currently active layer for the next update
        _activeLayer = newActiveLayer;

        if (_activeLayer != -1 && _audioSource != null) {
            AudioLayer audioLayer = _audioLayers[_activeLayer];
            if (audioLayer.Muted) _audioSource.volume = 0f;
            else _audioSource.volume = audioLayer.Collection.volume;
        }
    }
}
