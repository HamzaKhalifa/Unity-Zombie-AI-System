using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioOnEnter : StateMachineBehaviour
{
    [SerializeField] AudioCollection _audioCollection = null;
    [SerializeField] int _bank = 0;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        if (_audioCollection == null || AudioManager.instance == null) return;

        AudioClip clip = _audioCollection[_bank];

        if (clip != null) {

            AudioManager.instance.PlayOneShotSound(_audioCollection.audioGroup, 
                                                  clip, 
                                                   animator.transform.position,
                                                   _audioCollection.volume,
                                                   _audioCollection.spatialBlend,
                                                   _audioCollection.priority);
        }
    }
}
