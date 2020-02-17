using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractiveDoorAxisAlignment
{
    ZAxis,
    YAxis,
    XAxis,
}

[System.Serializable]
public class InteractiveDoorInfo
{
    public Transform Transform = null;
    public Vector3 Rotation = Vector3.zero;
    public Vector3 Movement = Vector3.zero;
    [HideInInspector]
    public Quaternion ClosedRotation = Quaternion.identity;
    [HideInInspector]
    public Quaternion OpenRotation = Quaternion.identity;
    [HideInInspector]
    public Vector3 OpenPosition = Vector3.zero;
    [HideInInspector]
    public Vector3 ClosedPosition = Vector3.zero;
}

[RequireComponent(typeof(BoxCollider))]
public class InteractiveDoor : InteractiveItem
{
    [Header("Activation Properties")]
    [Tooltip("Does the door start open or closed")]
    [SerializeField] bool _isClosed = true;

    [Tooltip("Does the door open in both directions")]
    [SerializeField] bool _isTwoWay = true;

    [Tooltip("Does the door open automatically when the player walks into its trigger?")]
    [SerializeField] bool _autoOpen = true;

    [Tooltip("Does the door close automatically after a certain period of time")]
    [SerializeField] bool _autoclose = true;

    [Tooltip("Disable manual activation")]
    [SerializeField] bool _disabledManualActivation = false;

    [Tooltip("The Random time range for the auto close delay")]
    [SerializeField] Vector2 _autoCloseDelay = new Vector2(5f, 5f);

    [Tooltip("How should the size of the box collider grow when the door is open")]
    [SerializeField] float _colliderLengthOpenScale = 3f;

    [Tooltip("Should we offset the center of the collider when open")]
    [SerializeField] bool _offsetCollider = true;

    [Tooltip("A container object used as a parent for any objects the open door should reveal")]
    [SerializeField] Transform _contentsMount = null;

    [SerializeField] protected InteractiveDoorAxisAlignment _localForwardAxis = InteractiveDoorAxisAlignment.ZAxis;

    [Header("Game state management")]
    [SerializeField] protected List<GameState> _requiredStates = new List<GameState>();
    [SerializeField] protected List<string> _requiredItems = new List<string>();

    [Header("Message")]
    [TextArea(3, 10)]
    [SerializeField] protected string _openedHintText = "Door: Press 'Use' to close.";
    [TextArea(3, 10)]
    [SerializeField] protected string _closedHintText = "Door: Press 'Use' to open";
    [TextArea(3, 10)]
    [SerializeField] protected string _canActivateHintText = "Door: It's Locked";

    [Header("Door transforms")]
    [Tooltip("A list of child transforms to animate")]
    [SerializeField] List<InteractiveDoorInfo> _doors = new List<InteractiveDoorInfo>();

    [Header("Sounds")]
    [SerializeField] AudioCollection _doorSounds = null;
    [Tooltip("Optional assignment of an audio punch in punch out Database")]
    [SerializeField] AudioPunchInPunchOutDatabase _audioPunchInPunchOutDatabase = null;


    IEnumerator _coroutine = null;
    Vector3 _closedColliderSize = Vector3.zero;
    Vector3 _closedColliderCenter = Vector3.zero;
    Vector3 _openColliderSize = Vector3.zero;
    Vector3 _openColliderCenter = Vector3.zero;
    protected BoxCollider _boxCollider = null;
    protected Plane _plane;
    protected float _normalizedTime = 0f;
    protected bool _openFrontSide = true;
    protected ulong _oneShotSoundID = 0;


    public override string GetText()
    {
        if (_disabledManualActivation) return null;

        bool haveInventoryItems = HaveRequiredInventoryItems();
        bool haveRequiredStates = true;

        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else {
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
            }
        }

        if (_isClosed) {
            if (!haveRequiredStates || !haveInventoryItems) {
                return _canActivateHintText;
            } else {
                return _closedHintText;
            }
        } else {
            return _openedHintText;
        }
    }

    protected bool HaveRequiredInventoryItems() {
        return true;
    }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        _boxCollider = _collider as BoxCollider;

        if (_boxCollider != null) {
            _closedColliderSize = _openColliderSize = _boxCollider.size;
            _closedColliderCenter = _openColliderCenter = _boxCollider.center;
            float offset = 0f;

            switch (_localForwardAxis) {
                case InteractiveDoorAxisAlignment.XAxis:
                    _plane = new Plane(transform.right, transform.position);
                    _openColliderSize.x *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.x - (_openColliderSize.x / 2) +_closedColliderSize.x / 2;
                    _openColliderCenter = new Vector3(offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    _plane = new Plane(transform.up, transform.position);
                    _openColliderSize.y *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.y - (_openColliderSize.y / 2) + _closedColliderSize.y / 2;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, offset, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    _plane = new Plane(transform.forward, transform.position);
                    _openColliderSize.z *= _colliderLengthOpenScale;
                    offset = _closedColliderCenter.z - (_openColliderSize.z / 2) + +_closedColliderSize.z / 2; ;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, offset);
                    break;
                default:
                    break;
            }

            if (!_isClosed) {
                _boxCollider.size = _openColliderSize;
                if (_offsetCollider) {
                    _boxCollider.center = _openColliderCenter;
                }
                _openFrontSide = true;
            }
        }

        // Set all the doors this object manages to the starting orientations
        foreach(InteractiveDoorInfo door in _doors) {
            if (door != null && door.Transform != null) {
                // It is assumed that all doors are set in the closed rotation
                // So grab the current rotation and store it as the closed rotation
                door.ClosedRotation = door.Transform.localRotation;
                door.ClosedPosition = door.Transform.position;
                door.OpenPosition = door.Transform.position - door.Transform.TransformDirection(door.Movement);

                Quaternion rotationToOpen = Quaternion.Euler(door.Rotation);

                if (!_isClosed) {
                    door.Transform.localRotation = door.ClosedRotation * rotationToOpen;
                    door.Transform.position = door.OpenPosition;
                }
            }
        }

        // Finally disable colliders of any content of any contents if in the closed position: 
        if (_contentsMount != null) {
            Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
            foreach(Collider col in colliders) {
                if (_isClosed) {
                    col.enabled = false;
                } else {
                    col.enabled = true;
                }
            }
        }

        // Animation is not currently in progress
        _coroutine = null;
    }

    public override void Activate(CharacterManager characterManager)
    {
        if (_disabledManualActivation) return;

        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) haveRequiredStates = false;
            else {
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
            }
        }

        if (haveRequiredStates && HaveRequiredInventoryItems()) {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(characterManager.transform.position));
            StartCoroutine(_coroutine);
        } else {
            if (_doorSounds && AudioManager.instance) {
                AudioClip clip = _doorSounds[2];
                if (clip != null) {
                    AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                                                          clip,
                                                           transform.position,
                                                           _doorSounds.volume,
                                                           _doorSounds.spatialBlend,
                                                           _doorSounds.priority);
                }
            }
        }
    }

    IEnumerator Activate(bool frontSide, bool autoClosing = false, float delay = 0f) {

        AudioClip clip = null;


        yield return new WaitForSeconds(delay);
        float duration = 1.5f;
        float time = 0f;
        float startAnimTime = 0f;

        // Ping pong normalized time
        if (_normalizedTime > 0f) {
            _normalizedTime = 1 - _normalizedTime;
        }

        if (!_isTwoWay) frontSide = true;

        if (_isClosed) {
            _isClosed = false;

            if (_normalizedTime > 0)
                frontSide = _openFrontSide;

            _openFrontSide = frontSide;

            if (_doorSounds && AudioManager.instance) {
                AudioManager.instance.StopSound(_oneShotSoundID);

                clip = _doorSounds[0];
                if (clip != null) {
                    duration = clip.length;
                    if (_audioPunchInPunchOutDatabase != null) {
                        AudioPunchInPunchOutInfo info = _audioPunchInPunchOutDatabase.GetClipInfo(clip);
                        if (info != null) {
                            startAnimTime = Mathf.Min(info.startTime, clip.length);
                            if (info.endTime >= startAnimTime) {
                                duration = info.endTime - startAnimTime;
                            } else {
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }
                    float playbackOffset = 0f;
                    if (_normalizedTime > 0f) {
                        playbackOffset = startAnimTime + (duration * _normalizedTime);
                        startAnimTime = 0f;
                    }

                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                                                          clip,
                                                           transform.position,
                                                           _doorSounds.volume,
                                                           _doorSounds.spatialBlend,
                                                                             _doorSounds.priority,
                                                                             playbackOffset);
                }
            }

            // Determine perceived forward axis and offset and scale the collider in that dimension
            float offset = 0f;
            switch(_localForwardAxis) {
                case InteractiveDoorAxisAlignment.XAxis:
                    offset = _openColliderSize.x / 2f - _closedColliderSize.x * 2;
                    if (frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x - offset, _closedColliderCenter.y, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.YAxis:
                    offset = _openColliderSize.y / 2f - _closedColliderSize.y * 2; ;
                    if (frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y - offset, _closedColliderCenter.z);
                    break;
                case InteractiveDoorAxisAlignment.ZAxis:
                    offset = _openColliderSize.z / 2f - +_closedColliderSize.z * 2; ;
                    if (frontSide) offset = -offset;
                    _openColliderCenter = new Vector3(_closedColliderCenter.x, _closedColliderCenter.y, _closedColliderCenter.z - offset);
                    break;
                default:
                    break;
            }

            if (_offsetCollider) {
                _boxCollider.center = _openColliderCenter;
            }
            _boxCollider.size = _openColliderSize;

            if (startAnimTime > 0) {
                yield return new WaitForSeconds(startAnimTime);
            }

            time = duration * _normalizedTime;

            while (time <= duration)
            {
                _normalizedTime = time / duration;
                foreach (InteractiveDoorInfo door in _doors) {
                    if (door != null && door.Transform != null) {
                        door.Transform.position = Vector3.Lerp(door.ClosedPosition, door.OpenPosition, _normalizedTime);
                        door.Transform.localRotation = door.ClosedRotation * Quaternion.Euler(frontSide || !_isTwoWay ? door.Rotation * _normalizedTime : -door.Rotation * _normalizedTime);
                    }
                }
                time += Time.deltaTime;
                yield return null;
            }

            // Finally enable colliders of any contents if in the closed position
            if (_contentsMount != null) {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach(Collider col in colliders) {
                    col.enabled = true;
                }
            }

            // Reset time to zero
            _normalizedTime = 0f;

            if (_autoclose) {
                _coroutine = Activate(frontSide, true, Random.Range(_autoCloseDelay.x, _autoCloseDelay.y));
                StartCoroutine(_coroutine);
            }

            yield break;

        } 
        // The door is open so we wish to close it
        else {
            _isClosed = true;
            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    Quaternion rotationToOpen = Quaternion.Euler(_openFrontSide ? door.Rotation : -door.Rotation);
                    door.OpenRotation = door.ClosedRotation * rotationToOpen;
                }
            }

            // Finally disable colliders of any contents if in the closed position
            if (_contentsMount != null)
            {
                Collider[] colliders = _contentsMount.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
            }

            if (_doorSounds && AudioManager.instance)
            {
                AudioManager.instance.StopSound(_oneShotSoundID);

                clip = _doorSounds[autoClosing ? 3 : 1];
                if (clip != null)
                {
                    duration = clip.length;

                    if (_audioPunchInPunchOutDatabase != null)
                    {
                        AudioPunchInPunchOutInfo info = _audioPunchInPunchOutDatabase.GetClipInfo(clip);
                        if (info != null)
                        {
                            startAnimTime = Mathf.Min(info.startTime, clip.length);
                            if (info.endTime >= startAnimTime)
                            {
                                duration = info.endTime - startAnimTime;
                            }
                            else
                            {
                                duration = clip.length - startAnimTime;
                            }
                        }
                    }
                    float playbackOffset = 0f;

                    if (_normalizedTime > 0f)
                    {
                        playbackOffset = startAnimTime + (duration * _normalizedTime);
                        startAnimTime = 0f;
                    }

                    _oneShotSoundID = AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                                                          clip,
                                                           transform.position,
                                                           _doorSounds.volume,
                                                           _doorSounds.spatialBlend,
                                                           _doorSounds.priority,playbackOffset);
                }
            }

            if (startAnimTime > 0)
                yield return new WaitForSeconds(startAnimTime);

            time = duration * _normalizedTime;

            while (time <= duration)
            {
                _normalizedTime = time / duration;
                foreach (InteractiveDoorInfo door in _doors)
                {
                    if (door != null && door.Transform != null)
                    {
                        door.Transform.position = Vector3.Lerp(door.OpenPosition, door.ClosedPosition, _normalizedTime);
                        door.Transform.localRotation = Quaternion.Lerp(door.OpenRotation, door.ClosedRotation, _normalizedTime);
                    }
                }
                time += Time.deltaTime;
                yield return null;
            }

            foreach (InteractiveDoorInfo door in _doors)
            {
                if (door != null && door.Transform != null)
                {
                    door.Transform.position = door.ClosedPosition;
                    door.Transform.localRotation = door.ClosedRotation;
                }
            }

            _boxCollider.size = _closedColliderSize;
            _boxCollider.center = _closedColliderCenter;
        }

        _normalizedTime = 0f;
        _coroutine = null;
        yield break;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (!_autoOpen || !_isClosed) return;

        bool haveRequiredStates = true;
        if (_requiredStates.Count > 0) {
            if (ApplicationManager.instance == null) {
                haveRequiredStates = false;
            } else {
                haveRequiredStates = ApplicationManager.instance.AreStatesSet(_requiredStates);
            }
        }

        if (haveRequiredStates && HaveRequiredInventoryItems()) {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = Activate(_plane.GetSide(other.transform.position));
            StartCoroutine(_coroutine);
        } else {
            if (_doorSounds && AudioManager.instance)
            {
                AudioClip clip = _doorSounds[2];
                if (clip != null)
                {
                    AudioManager.instance.PlayOneShotSound(_doorSounds.audioGroup,
                                                          clip,
                                                           transform.position,
                                                           _doorSounds.volume,
                                                           _doorSounds.spatialBlend,
                                                           _doorSounds.priority);
                }
            }
        }
    }

}
