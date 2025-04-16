using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] public string soundName;
    [SerializeField] public float volume;

    public void PlaySound()
    {
        AudioManager.Instance.PlaySoundEffect(soundName, volume);   
    }
}
