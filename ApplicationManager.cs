using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameState
{
    public string key = null;
    public string value = null;
}

public class ApplicationManager : MonoBehaviour
{
    [SerializeField] List<GameState> _startingGameStates = new List<GameState>();

    static ApplicationManager _instance = null;
    Dictionary<string, string> _gameStateDictionary = new Dictionary<string, string>();

    public static ApplicationManager instance
    {
        get
        {
            if (_instance == null) _instance = (ApplicationManager)FindObjectOfType(typeof(ApplicationManager));
            return _instance;
        }
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        ResetGameStates();
    }

    void ResetGameStates() {
        _gameStateDictionary.Clear();

        for (int i = 0; i < _startingGameStates.Count; i++)
        {
            GameState gs = _startingGameStates[i];
            _gameStateDictionary.Add(gs.key, gs.value);
        }
    }

    public bool AreStatesSet(List<GameState> states)
    {
        for (int i = 0; i < states.Count; i++)
        {
            GameState state = states[i];
            string result = GetGameState(state.key);
            if (string.IsNullOrEmpty(result) || !result.Equals(state.value)) return false;
        }

        return true;
    }

    public string GetGameState(string key)
    {
        string result = null;
        _gameStateDictionary.TryGetValue(key, out result);
        return result;
    }

    public bool SetGameState(string key, string value)
    {
        if (key == null || value == null) return false;

        if (!_gameStateDictionary.ContainsKey(key))
        {
            _gameStateDictionary.Add(key, value);
            return true;
        }
        _gameStateDictionary[key] = value;

        Debug.Log(key + ": " + _gameStateDictionary[key]);
        return false;
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void LoadGame()
    {
        ResetGameStates();
        SceneManager.LoadScene("The Game");
    }

    public void TheHospital() {
        ResetGameStates();
        SceneManager.LoadScene("The Hospital");
    }

    public void Quit()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif

    }
}
