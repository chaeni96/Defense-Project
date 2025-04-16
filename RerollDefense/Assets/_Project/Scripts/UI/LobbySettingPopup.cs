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

        // 슬라이더 초기값 설정
        float savedVolume = PlayerPrefs.GetFloat("BGMVolume", 0.3f);
        bgmVolumeSlider.value = savedVolume;

        // 시작할 때 오디오 매니저에 볼륨 설정
        AudioManager.Instance.SetBGMVolume(savedVolume);

        // 슬라이더 값 변경 이벤트에 메서드 연결
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);

        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        sfxVolumeSlider.value = savedSFXVolume;
        AudioManager.Instance.SetSFXVolume(savedSFXVolume);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    private void OnBGMVolumeChanged(float volume)
    {
        // 슬라이더 값이 변할 때마다 AudioManager의 볼륨 설정 메서드 호출
        AudioManager.Instance.SetBGMVolume(volume);

        // 볼륨 값 저장 (게임 재시작시에도 유지)
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
        Application.Quit(); // 어플리케이션 종료
#endif
    }

}
