using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InfoDock : MonoBehaviour
{
    // visible in editor

    public Text healthPercentage;
    public RawImage healthImage;
    public GameObject message;
    public Text notification;

    // internal 

    DebugDesk _debug;           // external
    Canvas _infoDockOverlay;    // internal
    RawImage[] _keyInfo;        // external
    Text _message;              // child-internal

    string _invokedMessage;
    float _invokedDuration;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _infoDockOverlay = GetComponent<Canvas>();
        _infoDockOverlay.enabled = false;

        _message = message.GetComponentInChildren<Text>();
        message.SetActive(false);

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

    public void showMessage(string aMessage, float aDuration = -1, float aDelay = 0f)
    {
        if (aDelay > 0)
        {
            _invokedMessage = aMessage;
            _invokedDuration = aDuration;
            Invoke("ShowMessageInvoked", aDelay);
        }
        else
        {
            _message.text = aMessage;
            message.SetActive(true);

            if (aDuration > 0)
            {
                Invoke("hideMessage", aDuration);
            }
        }
    }

    public void hideMessage()
    {
        _message.text = "";
        message.SetActive(false);
    }

    public void notify(string aNotification)
    {
        notification.text = aNotification;
    }

    public void clearNotification()
    {
        notification.text = "";
    }

    // internal methods

    private void ShowMessageInvoked()
    {
        _message.text = _invokedMessage;
        message.SetActive(true);

        if (_invokedDuration > 0)
        {
            Invoke("hideMessage", _invokedDuration);
        }
    }

    void onGameStart(object sender, EventArgs e)
    {
        _infoDockOverlay.enabled = true;
    }

    void onGameStop(object sender, EventArgs e)
    {
        _infoDockOverlay.enabled = false;
    }
}
