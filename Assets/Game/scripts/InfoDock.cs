using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InfoDock : MonoBehaviour
{
    // visible in editor

    public Text healthPercentage;
    public RawImage healthImage;
    public GameObject messageCenter;

    // internal 

    DebugDesk _debug;           // external
    Canvas _infoDockOverlay;    // internal
    RawImage[] _keyInfo;        // external

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _infoDockOverlay = GetComponent<Canvas>();
        _infoDockOverlay.enabled = false;

        _keyInfo = FindObjectsOfType<RawImage>()
            .Where(image => image.tag == "key-info")
            .ToArray();

        GazeClient gazeClient = FindObjectOfType<GazeClient>();
        if (gazeClient != null)
        {
            gazeClient.GameStart += onGameStart;
            gazeClient.GameStop += onGameStop;
        }
    }

    // public methods

    public void AcquireKey(string aKeyName)
    {
        //_debug.print($"AcquireKey {aKeyName}", "INFO");
        RawImage keyImage = _keyInfo.Single(key => key.name == aKeyName);
        keyImage.uvRect = new Rect(0.5f, 0, keyImage.uvRect.width, keyImage.uvRect.height);
    }

    public void SetHealth(float aHealth)
    {
        healthPercentage.text = (aHealth * 100).ToString("0") + "%";
        healthImage.uvRect = new Rect(0.5f * (1f - aHealth), healthImage.uvRect.y, healthImage.uvRect.width, healthImage.uvRect.height);
    }

    // internal methods

    void onGameStart(object sender, EventArgs e)
    {
        _infoDockOverlay.enabled = true;
    }

    void onGameStop(object sender, EventArgs e)
    {
        _infoDockOverlay.enabled = false;
    }
}
