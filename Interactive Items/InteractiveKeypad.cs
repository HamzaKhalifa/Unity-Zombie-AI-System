using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveKeypad : InteractiveItem
{
    [SerializeField] Transform _elevator = null;
    [SerializeField] AudioCollection _collection = null;
    [SerializeField] protected int _bank = 0;
    [SerializeField] protected float _activationDelay = 0f;

    bool _isActivated = false;

    public override string GetText()
    {
        ApplicationManager appDatabase = ApplicationManager.instance;

        if (appDatabase == null) return string.Empty;

        string powerState = appDatabase.GetGameState("POWER");
        string lockDownState = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE")) {
            return "Keypad : No power";
        }
        else if (string.IsNullOrEmpty(lockDownState) || !lockDownState.Equals("FALSE"))
        {
            return "Keypad : Under Lockdown";
        }

        return "Keypad";
    }

    public override void Activate(CharacterManager characterManager)
    {
        if (_isActivated) return;

        ApplicationManager appDatabase = ApplicationManager.instance;

        if (appDatabase == null) return;

        string powerState = appDatabase.GetGameState("POWER");
        string lockDownState = appDatabase.GetGameState("LOCKDOWN");
        string accessCodeState = appDatabase.GetGameState("ACCESSCODE");

        if (string.IsNullOrEmpty(powerState) || !powerState.Equals("TRUE"))
        {
            return;
        }
        else if (string.IsNullOrEmpty(lockDownState) || !lockDownState.Equals("FALSE"))
        {
            return;
        }
        else if (string.IsNullOrEmpty(accessCodeState) || !accessCodeState.Equals("TRUE"))
        {
            return;
        }

        StartCoroutine(DoDelayedAnimation(characterManager));

        _isActivated = true;
    }

    IEnumerator DoDelayedAnimation(CharacterManager characterManager) {
        if (_elevator == null) yield break;

        if (_collection != null) {
            AudioClip clip = _collection[_bank];
            if (clip) {
                if (AudioManager.instance) {
                    AudioManager.instance.PlayOneShotSound(_collection.audioGroup,
                                                           clip,
                                                           transform.position,
                                                           _collection.volume,
                                                           _collection.spatialBlend,
                                                           _collection.priority);
                }
            }
        }

        yield return new WaitForSeconds(_activationDelay);

        if (characterManager != null) {
            characterManager.transform.parent = _elevator;

            Animator animator = _elevator.GetComponent<Animator>();
            if (animator != null) {
                animator.SetTrigger("Activate");
            }
            if (characterManager.fbsController) {
                characterManager.fbsController.freezeMovement = true;
            }
        }
    }
}
