using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static BansheeGz.BGDatabase.BGDBField;

public class SaveLoadManager : MonoBehaviour
{
    //이미저장된 파일이 있는지 체크하는 메서드 
    //데이터 세이브 메서드 매개변수로 path 받아오기
    //데이터 로드 메서드 매개변수로 path 받아오기

    public static SaveLoadManager _instance;

    public static SaveLoadManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveLoadManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("SaveLoadManager");
                    _instance = singleton.AddComponent<SaveLoadManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }

    public bool HasSavedFile
    {
        get { return File.Exists(SaveFilePath); }
    }

    public string SaveFilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "localDatabase.dat"); }
    }

    public void SaveData()
    {
        byte[] bytes = BGRepo.I.Addons.Get<BGAddonSaveLoad>().Save();

        File.WriteAllBytes(SaveFilePath, bytes); // 파일에 저장
    }
    public void LoadData()
    {
        if(HasSavedFile)
        {
            var content = File.ReadAllBytes(SaveFilePath);
            BGRepo.I.Addons.Get<BGAddonSaveLoad>().Load(content);
            Debug.Log("Save file path: " + SaveFilePath);
        }
        else
        {
            //새 로컬 데이터 생성
            Debug.Log("no save file found at " + SaveFilePath);
            SaveData();
        }
    }



    // 디버그 전용 기능 - 런타임 저장 데이터를 에디터 데이터베이스에 적용
    public void ApplySavedDataToEditor()
    {
        if (HasSavedFile)
        {
            var content = File.ReadAllBytes(SaveFilePath);

            // 저장된 데이터 로드
            BGRepo.I.Addons.Get<BGAddonSaveLoad>().Load(
                new BGSaveLoadAddonLoadContext(
                    new BGSaveLoadAddonLoadContext.LoadRequest(BGAddonSaveLoad.DefaultSettingsName, content))
                {
                    ReloadDatabase = true,
                    FireAfterLoadEvents = true
                }
            );

            // 현재 상태를 에디터 데이터베이스에 저장
            BGRepo.I.Save();

            Debug.Log("저장된 데이터가 에디터 데이터베이스에 적용되었습니다.");
        }
        else
        {
            Debug.LogWarning("저장된 파일을 찾을 수 없습니다: " + SaveFilePath);
        }
    }




}
