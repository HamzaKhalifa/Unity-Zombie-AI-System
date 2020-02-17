using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Scriptable Objects/Shared Collections/New Timed String Queue")]
public class SharedTimedStringQueue : ScriptableObject, ISerializationCallbackReceiver
{
    [SerializeField] [TextArea(3, 10)] string _noteToDeveloper = "An automated time message delivery queue.\n\nUsage: \n\nQueue.Enqueue('My message');\n\nDebug.Log(Queue.text);\n\nA Coroutine runner instance must exist in the current scene.";

    [SerializeField] float _dequeueDelay = 3.5f;

    float _nextDequeueTime = 0f;
    IEnumerator _coroutine = null;
    bool _paused = false;
    string _text = null;

    public string text { get { return _text; }}

    Queue<string> _queue = new Queue<string>();

    public void Enqueue(string message) {
        if (CoroutineRunner.Instance == null) {
            _text = "Timed text queue error: No coroutine runner object present";
            return;
        }

        _queue.Enqueue(message);

        if (_coroutine == null) {
            _coroutine = QueueProcessor();
            CoroutineRunner.Instance.StartCoroutine(_coroutine);
        }

    }

    IEnumerator QueueProcessor() {
        while(true) {
            if (!_paused) {
                _nextDequeueTime -= Time.unscaledDeltaTime;
                if (_nextDequeueTime < 0) {
                    if (_queue.Count == 0) {
                        break;
                    }

                    _text = _queue.Dequeue();
                    _nextDequeueTime = _dequeueDelay;
                }
            }

            yield return null;
        }

        _text = null;
        _coroutine = null;
    }

    public void Dequeue(string message) {
        _queue.Dequeue();
    }

    public int Count() {
        return _queue.Count;
    }

    public void OnBeforeSerialize()
    {

    }

    public void OnAfterDeserialize()
    {
        _queue.Clear(); _text = null;
    }
}
