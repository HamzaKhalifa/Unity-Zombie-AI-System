using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo {
    public Collider collider = null;
    public CharacterManager characterManager = null;
    public Camera camera = null;
    public CapsuleCollider meleeTrigger = null;
}

public class GameSceneManager : MonoBehaviour
{
    // Inspector assigned variables
    [SerializeField] ParticleSystem _bloodParticles = null;

    // Statics
    static GameSceneManager _instance = null;

    public static GameSceneManager instance {
        get {
            if (_instance == null) {
                _instance = (GameSceneManager)FindObjectOfType(typeof(GameSceneManager));
            }
            return _instance;
        }
    }

    // Private
    Dictionary<int, AIStateMachine> _stateMachines = new Dictionary<int, AIStateMachine>();
    Dictionary<int, PlayerInfo> _playerInfos = new Dictionary<int, PlayerInfo>();
    Dictionary<int, InteractiveItem> _interactiveItems = new Dictionary<int, InteractiveItem>();
    Dictionary<int, MaterialController> _materialControllers = new Dictionary<int, MaterialController>();

    // Properties
    public ParticleSystem bloodParticles { get { return _bloodParticles; }}


    public void RegisterAIStateMachine(int key, AIStateMachine stateMachine) {
        if (!_stateMachines.ContainsKey(key)) {
            _stateMachines[key] = stateMachine;
        }
    }

    //Get the AIState machine by its id
    public AIStateMachine GetAIStateMachine(int key) {
        AIStateMachine machine = null;
        if (_stateMachines.TryGetValue(key, out machine)) {
            return machine;
        }
        return null;
    }

    public void RegisterPlayerInfo(int key, PlayerInfo playerInfo)
    {
        if (!_playerInfos.ContainsKey(key))
        {
            _playerInfos.Add(key, playerInfo);
        }
    }

    //Get the player infor by its id
    public PlayerInfo GetPlayerInfo(int key)
    {
        PlayerInfo playerInfo = null;
        if (_playerInfos.TryGetValue(key, out playerInfo))
        {
            return playerInfo;
        }
        return null;
    }

    public void RegisterInteractiveItem(int key, InteractiveItem item) {
        if (!_interactiveItems.ContainsKey(key)) {
            _interactiveItems.Add(key, item);
        }
    }

    public InteractiveItem GetInteractiveItem(int key) {
        InteractiveItem item = null;
        _interactiveItems.TryGetValue(key, out item);
        return item;
    }

    public void RegisterMaterialController(int key, MaterialController materialController) {
        if (!_materialControllers.ContainsKey(key))
        {
            _materialControllers.Add(key, materialController);
        }
    }

    public MaterialController GetMaterialController(int key)
    {
        MaterialController materialController = null;
        _materialControllers.TryGetValue(key, out materialController);
        return materialController;
    }

    protected void OnDestroy()
    {
        foreach(KeyValuePair<int, MaterialController> controller in _materialControllers) {
            controller.Value.OnReset();
        }
    }
}
