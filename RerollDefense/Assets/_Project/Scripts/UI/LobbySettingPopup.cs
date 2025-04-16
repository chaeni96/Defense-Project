using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("LobbySettingPopup", "LobbySettingPopup", false)]

public class LobbySettingPopup : FloatingPopupBase
{
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;


    public override void InitializeUI()
    {
        base.InitializeUI();

        // �����̴� �ʱⰪ ����
        float savedVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        bgmVolumeSlider.value = savedVolume;

        // ������ �� ����� �Ŵ����� ���� ����
        AudioManager.Instance.SetBGMVolume(savedVolume);

        // �����̴� �� ���� �̺�Ʈ�� �޼��� ����
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        sfxVolumeSlider.value = savedSFXVolume;
        AudioManager.Instance.SetSFXVolume(savedSFXVolume);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void OnBGMVolumeChanged(float volume)
    {
        // �����̴� ���� ���� ������ AudioManager�� ���� ���� �޼��� ȣ��
        AudioManager.Instance.SetBGMVolume(volume);

        // ���� �� ���� (���� ����۽ÿ��� ����)
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float volume)
    {
        AudioManager.Instance.SetSFXVolume(volume);
    }

    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnClickClosePopup()
    {
        UIManager.Instance.CloseUI<LobbySettingPopup>();
    }

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // ���ø����̼� ����
#endif
    }

}
