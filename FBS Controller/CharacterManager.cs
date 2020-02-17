using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    // Inspector assigned variables
    [SerializeField] CapsuleCollider _meleeTrigger = null;
    [SerializeField] CameraBloodEffect _cameraBloodEffect = null;
    [SerializeField] Camera _camera = null;
    [SerializeField] AISoundEmitter _soundEmitter = null;
    [SerializeField] float _walkRadius = 1;
    [SerializeField] float _runRadius = 7f;
    [SerializeField] float _landingRadius = 11f;
    [SerializeField] float _bloodRadiusScale = 6f;
    [SerializeField] PlayerHUD _playerHUD = null;

    [SerializeField] AudioCollection _damageSounds = null;
    [SerializeField] AudioCollection _painSounds = null;
    [SerializeField] AudioCollection _tauntSounds = null;

    [SerializeField] float _nextPainSoundTime = 0f;
    [SerializeField] float _painSoundOffset = .35f;
    [SerializeField] float _tauntRadius = 10f;
    [SerializeField] float _nextTauntTime = 0f;

    [Header("Inventory")]
    [SerializeField] GameObject _inventoryUI = null;
    [SerializeField] Inventory _inventory = null;

    [Header("Shared variables")]
    [SerializeField] SharedFloat _health = null;
    [SerializeField] SharedFloat _infection = null;
    [SerializeField] SharedString _interactionString = null;

    public FBSController fbsController { get { return _fbsController; } }

    // Private
    Collider _collider = null;
    FBSController _fbsController = null;
    CharacterController _characterController = null;
    GameSceneManager _gameSceneManager = null;
    int _aiBodyPartLayer = -1;
    int _interactiveMask = 0;
    float _nextAttackTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        _collider = GetComponent<Collider>();
        _fbsController = GetComponent<FBSController>();
        _characterController = GetComponent<CharacterController>();
        _gameSceneManager = GameSceneManager.instance;

        if (_gameSceneManager != null) {
            PlayerInfo playerInfo = new PlayerInfo();
            playerInfo.collider = _collider;
            playerInfo.characterManager = this;
            playerInfo.camera = _camera;
            playerInfo.meleeTrigger = _meleeTrigger;

            _gameSceneManager.RegisterPlayerInfo(_collider.GetInstanceID(), playerInfo);
        }

        _aiBodyPartLayer = LayerMask.NameToLayer("AI Body Part");
        _interactiveMask = 1 << LayerMask.NameToLayer("Interactive");

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Stat fading in
        if (_playerHUD != null) {
            _playerHUD.Fade(2f, ScreenFadeTime.FadeIn);
        }
    }

    public void TakeDamage(float amount, bool doDamage, bool doPain) {
        _health.value = Mathf.Max(0f, _health.value - amount * Time.deltaTime);

        if (_fbsController) {
            _fbsController.dragMultiplier = 0f;
        }

        if (_cameraBloodEffect != null) {
            _cameraBloodEffect.minBloodAmount = 1 - (_health.value / 100) * .5f;
            //_cameraBloodEffect.bloodAmount = Mathf.Min(_cameraBloodEffect.minBloodAmount + .3f, 1);
            _cameraBloodEffect.bloodAmount = 1;
        }

        if (AudioManager.instance) {
            if (doDamage && _damageSounds != null) {
                AudioManager.instance.PlayOneShotSound(_damageSounds.audioGroup, _damageSounds.audioClip,
                                                       transform.position, _damageSounds.volume, _damageSounds.spatialBlend,
                                                       _damageSounds.priority);
            }

            if (doPain && _painSounds != null) {
                AudioClip painClip = _painSounds.audioClip;
                if (painClip && Time.time >_nextPainSoundTime ) {
                    _nextPainSoundTime = Time.time + painClip.length;
                    StartCoroutine(AudioManager.instance.PlayOneShotSoundDelayed(_painSounds.audioGroup, 
                                                                          painClip, transform.position,
                                                                          _painSounds.volume, 
                                                                          _painSounds.spatialBlend,
                                                                                 _painSoundOffset,
                                                                          _painSounds.priority));
                }
            }
        }


        if (_health.value <= 0f) {
            DoDeath();
        }
    }

    public void DoDamage(int hitDirection = 0)
    {
        if (_camera == null) return;
        if (_gameSceneManager == null) return;

        Ray ray;
        RaycastHit hit;
        bool isSomethingHit = false;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        isSomethingHit = Physics.Raycast(ray, out hit, 1, 1<<_aiBodyPartLayer);
        if (isSomethingHit) {
            AIStateMachine stateMachine = _gameSceneManager.GetAIStateMachine(hit.rigidbody.GetInstanceID());
            if (stateMachine) {
                stateMachine.TakeDamage(hit.point, ray.direction * 1f, 1, hit.rigidbody, this, 0 );
                _nextAttackTime = Time.time + .5f;
            }
        }
    }

    public void DoTaunt() {
        if (_tauntSounds == null || Time.time <= _nextTauntTime) return;
        AudioClip taunt = _tauntSounds[0];
        AudioManager.instance.PlayOneShotSound(_tauntSounds.audioGroup,
                                              taunt,
                                               transform.position,
                                               _tauntSounds.volume,
                                               _tauntSounds.spatialBlend,
                                               _tauntSounds.priority);

        if (_soundEmitter != null) {
            _soundEmitter.SetRadius(_tauntRadius);
        }

        _nextTauntTime = Time.time + taunt.length;
    }

    void DoDeath() {
        if (_fbsController)
        {
            _fbsController.freezeMovement = true;
        }

        if (_playerHUD)
        {
            _playerHUD.Fade(3, ScreenFadeTime.FadeOut);
            //_playerHUD.ShowMissionText("Mission Failed");
            //_playerHUD.Invalidate(this);
        }

        Invoke("GameOver", 3f);
    }

    void Update()
    {
        if (Input.GetButtonDown("Inventory") && _inventoryUI != null) {
            if (!_inventoryUI.activeSelf) {
                _inventoryUI.SetActive(true);
                if (_playerHUD) _playerHUD.gameObject.SetActive(false);

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                return;
            } else {
                _inventoryUI.SetActive(false);
                if (_playerHUD) _playerHUD.gameObject.SetActive(true);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        Ray ray;
        RaycastHit hit;
        RaycastHit[] hits;

        ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        float rayLength = Mathf.Lerp(1, 1.8f, Mathf.Abs(Vector3.Dot(_camera.transform.forward, Vector3.up)));
        hits = Physics.RaycastAll(ray, rayLength, _interactiveMask);

        if (hits.Length > 0) {
            int highestPriority = int.MinValue;
            InteractiveItem priorityObject = null;

            for (int i = 0; i < hits.Length; i++)
            {
                hit = hits[i];

                InteractiveItem interactiveObject = _gameSceneManager.GetInteractiveItem(hit.collider.GetInstanceID());
                if (interactiveObject != null && interactiveObject.priority > highestPriority) {
                    priorityObject = interactiveObject;
                    highestPriority = interactiveObject.priority;
                }
            }

            if (priorityObject != null) {
                if (_playerHUD) {
                    if (_interactionString) {
                        _interactionString.value = priorityObject.GetText();
                    }
                }

                if (Input.GetButtonDown("Use")) {
                    priorityObject.Activate(this);
                }
            }
        } else {
            if (_interactionString)
            {
                _interactionString.value = null;
            }
        }

        if (Input.GetMouseButtonDown(0) && Time.time > _nextAttackTime) {
            DoDamage();
        }

        if (_fbsController) {
            float newRadius = Mathf.Max(_walkRadius, (100 - _health.value) / _bloodRadiusScale);
            switch(_fbsController.movementStatus) {
                case PlayerMoveStatus.Landing:
                    // We don't want to emit any sound at the start of the game
                    if (Time.time > 2f) newRadius = Mathf.Max(newRadius, _landingRadius);
                    break;
                case PlayerMoveStatus.Running:
                    newRadius = Mathf.Max(newRadius, _runRadius);
                    break;
                default: 
                    break;
            }

            _soundEmitter.SetRadius(newRadius);

            _fbsController.dragMultiplierLimit = Mathf.Max(_health.value / 100, .25f);
        }

        if (Input.GetMouseButtonDown(1))
        {
            DoTaunt();
        }
    }

    public void DoLevelComplete() {
        if (_fbsController) {
            _fbsController.freezeMovement = true;
        }

        if (_playerHUD) {
            _playerHUD.Fade(4, ScreenFadeTime.FadeOut);
            //_playerHUD.ShowMissionText("Mission completed");
            //_playerHUD.Invalidate(this);
        }

        Invoke("GameOver", 4f);
    }

    void GameOver() {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (ApplicationManager.instance) {
            ApplicationManager.instance.LoadMainMenu();
        }
    }
}
