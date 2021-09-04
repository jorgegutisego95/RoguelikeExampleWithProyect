using System.Linq;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
  
[RequireComponent(typeof(AudioSource))]
public class ExtractDataFromCapturedSoundByMicrophone : MonoBehaviour
{
    #region Public properties
    public Dictionary<string, float> paramsExtracted;	
    public Dictionary<string, float> notesExtracted;
    #endregion
    
    #region Private const properties
    private const int outputDataTam = 16384;
    private const int spectrumDataTam = 4096;
    private const float RefValue = 0.000002f;
    private const float Threshold = 0f;
    private const string knowNoteName = "la";
    private const int knowNoteOctave = 3;
    private const float knowNoteFrequency = 440.0f;
    private static readonly IList<string> Notes = new ReadOnlyCollection<string>(new List<string> {"do", "do#", "re", "re#", "mi", "fa", "fa#", "sol", "sol#", "la", "la#", "si" });    	
    private const float minDb = 0f;
    #endregion

    #region Private properties	
    private float[] spectrum;
    private float[] samples;
    private float fSample; 
    private float RmsValue;
    private float DbValue;
    private IList<float> avgDbValue;
    private IList<long> timeExec;
    private float loudness;
    private IList<float> avgLoudnessValue;
    
    //A handle to the attached AudioSource  
    private AudioSource componentAudioSource;  
    
    //The maximum and minimum available recording frequencies  
    private int minFreq;  
    private int maxFreq;  

    //The name of the Device.
    private string deviceName;
    

    #endregion
    
    
    #region Private Methods

    private void Awake()
    {
        //Get the attached AudioSource component  
        componentAudioSource = this.GetComponent<AudioSource>();  
        avgDbValue = new List<float>();
        timeExec = new List<long>();
        spectrum = new float[spectrumDataTam];
        samples = new float[outputDataTam];
        avgLoudnessValue = new List<float>();
        fSample = AudioSettings.outputSampleRate;
        paramsExtracted = new Dictionary<string, float>();
        notesExtracted = new Dictionary<string, float>();
        
        this.RecordFromMic();	
        // Play audio   
        componentAudioSource.Play();  
    }
    
    private void RecordFromMic()
    {
        if(Microphone.devices.Length <= 0)  
        {  
            Debug.LogWarning("El microfono no está conectado!");  
        }  
        else //At least one microphone is present  
        {  
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);  
  
            if(minFreq == 0 && maxFreq == 0)  
            {  
                maxFreq = 48000;  
            }  
            deviceName = Microphone.devices[0];
            Debug.Log(deviceName);  
            
            componentAudioSource.clip = Microphone.Start(deviceName, true, 10, maxFreq);
            componentAudioSource.loop = true;

            while (!(Microphone.GetPosition(deviceName) > 0))
            {
            }
            
        } 
    }
    
    public float GiveMePeakFromList(IList<float> initialList)
    {
        float result = 0f;

        foreach (float item in initialList)
        {			
            if (Math.Abs(result) < Math.Abs(item) && item != minDb)
            {
                result = item;
            }
        }

        return result;
    }
    
    public float GiveMeAvgFromList(IList<float> initialList)
    {
        float avg = 0f;

        foreach (float item in initialList)
        {
            avg = avg + item;
        }

        avg = avg / initialList.Count;
        return avg;
    }
    

    public string GetSpectrumData()
    {
        this.GetComponent<AudioSource>().GetSpectrumData(spectrum, 0, FFTWindow.Hanning);
        float maxV = 0;
        int maxN = 0;

        for (int i = 0; i < spectrumDataTam; i++) 
        { 
            if ((spectrum[i] > maxV) && (spectrum[i] > 0))
            {			
                maxV = spectrum[i];
                maxN = i;
            }
        }  
                    
        float pitchValue = ((float)maxN*(fSample/2))/spectrumDataTam;
        //Debug.Log(pitchValue);
        //Ahora convertimos el pitch a la nota correspondiente.
        string noteAndScale = null;		
        double octaveMultiplier = 2;
        double noteMultiplier = Math.Pow(octaveMultiplier, (double)1/Notes.Count);
        double difPitchKnowFrequency = pitchValue/knowNoteFrequency;
        if(difPitchKnowFrequency != 0)
        {
            double distanceFromKnownNote = Math.Log(difPitchKnowFrequency, noteMultiplier);
            
            if(distanceFromKnownNote < 0 && Math.Abs((distanceFromKnownNote - Math.Truncate(distanceFromKnownNote))) > 0)
            {
                distanceFromKnownNote = distanceFromKnownNote - 1;
            }
            
            var knowNoteAbsoluteInd = knowNoteOctave * Notes.Count + Notes.IndexOf(knowNoteName);     
            var noteAbsoluteIndex = knowNoteAbsoluteInd + (int)distanceFromKnownNote;
            int noteOctave = noteAbsoluteIndex/Notes.Count;
            float noteIndexInOctave = Math.Abs(noteAbsoluteIndex % Notes.Count);
            
            string noteName = Notes[(int)noteIndexInOctave];
            
            if (noteOctave >= 0)
            {
                noteAndScale = $"{noteName}{noteOctave}";
                    
                if (notesExtracted.ContainsKey(noteAndScale))
                {
                    notesExtracted[noteAndScale] = notesExtracted[noteAndScale] + 1f;
                }
                else
                {
                    notesExtracted.Add(noteAndScale, 1f);
                }	
            }
            else
            {							
                Debug.LogError("La octava no puede ser negativa.");
            }		
        }

        return noteAndScale;
    }
    
    public void GetDBData()	
    {			
        float sum = 0;
        int i;

        for (i = 0; i < outputDataTam; i++) 
        {
            sum += samples[i] * samples[i];
        }

        RmsValue = Mathf.Sqrt(sum / outputDataTam);
        
        DbValue = 20 * Mathf.Log10(RmsValue/RefValue);
        
        if (DbValue < minDb)
        {
            DbValue = minDb;
        }

        avgDbValue.Add(DbValue);
    }
    
    private void CheckIfExistsOnDictionaryAndModify(Dictionary<string, float> dictionary, string key, float v)
    {
        if(!string.IsNullOrEmpty(key))
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = v;
            }
            else
            {
                dictionary.Add(key, v);
            }
        }
    }

    private void AddMostCommonNote(Dictionary<string, float> dictionary)
    {
        KeyValuePair<string, float> result = new KeyValuePair<string, float>();

        foreach (KeyValuePair<string, float> item in notesExtracted)
        {
            if (result.Equals(new KeyValuePair<string, float>()))
            {
                result = item;
            }
            else if (item.Value > result.Value) 
            {
                result = item;
            }
        }

        this.CheckIfExistsOnDictionaryAndModify(dictionary, result.Key, result.Value);
    }

    #endregion

    public Dictionary<string, float> CollectAllDataAndPrintDictionary(bool includeAllNotes = false)
    {
        this.CheckIfExistsOnDictionaryAndModify(paramsExtracted, "Pico de dB:", this.GiveMePeakFromList(this.avgDbValue));
        this.CheckIfExistsOnDictionaryAndModify(paramsExtracted, "Media aritmética de dB:", this.GiveMeAvgFromList(this.avgDbValue));
        
        if(includeAllNotes)
        {
            paramsExtracted = paramsExtracted.Concat(notesExtracted.Where(kvp => !paramsExtracted.ContainsKey(kvp.Key))).ToDictionary(kvp=> kvp.Key, kvp => kvp.Value);
        }
        else
        {
            this.AddMostCommonNote(paramsExtracted);
        }

        return paramsExtracted;
    }

    public void GetDataFromSound()
    {
        this.GetComponent<AudioSource>().GetOutputData(samples, 0);
        this.GetSpectrumData();
        this.GetDBData();
    }

    public void ClearDictionarys()
    {   
        paramsExtracted.Clear();
        notesExtracted.Clear();
    }
} 