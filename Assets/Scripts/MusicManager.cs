using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public AudioSource componentAudioSource;
    public static MusicManager instance = null;
    public bool extractAllNotes = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        GetComponent<ExtractDataFromCapturedSoundByMicrophone>().GetDataFromSound();
    }

    public Dictionary<string, float> GetMusicData()
    {
        Dictionary<string, float> result = GetComponent<ExtractDataFromCapturedSoundByMicrophone>().CollectAllDataAndPrintDictionary(extractAllNotes);
        return result;
    }

    public void CleanData()
    {
        GetComponent<ExtractDataFromCapturedSoundByMicrophone>().ClearDictionarys();
    }
}
