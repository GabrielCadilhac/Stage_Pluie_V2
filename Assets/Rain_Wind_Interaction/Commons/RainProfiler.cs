using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Config
{
    public int nbParticles = 50000;
    public string winds = "global";
    public bool turbAnim = true;
    public int nbMaxTurbulences = 10;
    public string recordName;
}

public class Configs
{
    public Config[] configs;
}

public class RainProfiler : MonoBehaviour
{
    [SerializeField] BezierCurve curve;
    [SerializeField] Camera cam;
    [SerializeField] GameObject rainPrefab;

    private Dictionary<string, List<float>> _elapsedTimes;
    private List<CustomSampler> _customSamplers;
    private List<Sampler> _samplers;

    private Config[] _configsArray;

    private int _currentConfigId = 0;

    private int _profID = 0;
    private int _currentSample = 0;
    private int _nbSamples = 2000;
    private int _nbWarmup = 200;

    RainMain _rain;

    void Awake()
    {
        _elapsedTimes = new Dictionary<string, List<float>>();

        _samplers = new List<Sampler> { Sampler.Get("PlayerLoop") };
        foreach (Sampler s in _samplers)
            _elapsedTimes.Add(s.name, new List<float>());

        LoadConfig("test_config.json");

        _rain = GameObject.Find("Rain").GetComponent<RainMain>();
    }

    void FixedUpdate()
    {
        if (_customSamplers == null)
        {
            _customSamplers = _rain.GetCustomSamplers();
            foreach (CustomSampler sampler in _customSamplers)
            {
                if (sampler.isValid)
                    _elapsedTimes.Add(sampler.name, new List<float>());
            }
            _rain.gameObject.SetActive(false);
            return;
        }

        _currentSample++;
        if (_currentSample < _nbWarmup)
            return;

        foreach (CustomSampler sampler in _customSamplers)
        {
            Recorder rec = sampler.GetRecorder();
            if (rec.isValid)
                _elapsedTimes[sampler.name].Add((float) rec.elapsedNanoseconds / 1000000f);
        }

        foreach (Sampler s in _samplers)
        {
            Recorder rec = s.GetRecorder();
            if (rec.isValid)
                _elapsedTimes[s.name].Add((float)rec.elapsedNanoseconds / 1000000f);
        }

        if (_currentSample >= _nbSamples + _nbWarmup)
            ResetBench();
    }

    private void SaveTimes()
    {
        StringBuilder sb = new StringBuilder("FrameId,");
        foreach (string sampName in _elapsedTimes.Keys)
            sb.Append($"{sampName},");
        sb.Remove(sb.Length-1, 1);

        for (int i = 0; i < _elapsedTimes["Wind_Update"].Count; i++)
        {
            sb.Append($"\n{i},");
            foreach (string sampName in _elapsedTimes.Keys)
                sb.Append($"{_elapsedTimes[sampName][i].ToString().Replace(",", ".")},");
            sb.Remove(sb.Length - 1, 1);
        }

        Config c = Constants.Config;
        string configName = $"{c.nbParticles} {c.winds} {c.turbAnim} {c.nbMaxTurbulences}-{_currentConfigId}.{_profID}";
        SaveFile(sb.ToString(), configName);
    }

    private void LoadConfig(string pConfigName)
    {
        string folder = Application.persistentDataPath;
        _configsArray = Common.LoadFile(Path.Combine(folder, pConfigName));
        Constants.Config = _configsArray[_currentConfigId];
    }

    private void ResetBench()
    {
        SaveTimes();
        _elapsedTimes.Clear();
        foreach (Sampler s in _samplers)
            _elapsedTimes.Add(s.name, new List<float>());

        foreach (CustomSampler sampler in _customSamplers)
        {
            if (sampler.isValid)
                _elapsedTimes.Add(sampler.name, new List<float>());
        }

        _currentSample = 0;
        if (_profID >= 10)
        {
            Debug.Log("Benches finished !");
            _profID = 0;
            _currentConfigId++;

            if (_currentConfigId == _configsArray.Length)
                gameObject.SetActive(false); // Disable Rain Profiler
        } else
        {
            if (_rain.isActiveAndEnabled)
                _rain.Restart(); 
            _profID++;
            Debug.Log($"Next sample {_profID}");
        }

        Constants.Config = _configsArray[_currentConfigId];
    }

    private void SaveFile(string content, string fileName)
    {
#if UNITY_EDITOR
        string folder = Application.streamingAssetsPath;

        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
#else
        string folder = Application.persistentDataPath;
#endif

        string filePath = Path.Combine(folder, fileName + ".csv");

        using (var writer = new StreamWriter(filePath, false))
        {
            writer.Write(content);
        }

        Debug.Log($"CSV file written to \"{filePath}\"");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
}
