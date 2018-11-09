using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

/**
 * <summary>Implement communication with the ETUDriver service.
 * Requires to provide:
 * 1. buttons to send commands to ETUDriver service (+ object to display the device name)
 * 2. container with object that can be interacted with gaze
 * 3. gaze pointer (object on the UI layer)
 * 
 * USAGE: throw on the EventSystem object, or an object with EventSystem
 * </summary>
 * */
public class GazeClient : NetworkBehaviour
{
    // visible in editor

    public bool simulate;

    public GameObject gazeControls;
    public Button options;
    public Button calibrate;
    public Button toggleTracking;
    public Text deviceName;
    public RectTransform pointer;

    // readonly

    public Vector2 gazeLocation { get { return pointer.position; } }
    public bool isTracking { get { return _isTracking; } }
    public RawPoint location { get { return _location; } }

    // events

    public event EventHandler GameStart = delegate { };
    public event EventHandler GameStop = delegate { };
    public event EventHandler State = delegate { };
    public event EventHandler Sample = delegate { };

    // internal

    WebSocketSharp.WebSocket _ws = null;
    GazeSimulator _simulator = null;

    Queue<string> _messages = new Queue<string>();
    Vector2 _scale = new Vector2(1f, 1f);
    Vector2 _offset = new Vector2(0f, 0f);
    RawPoint _location = new RawPoint(0, 0f, 0f);

    Smoother<RawPoint> _smoother;

    bool _isTracking = false;
    bool _hasInitiatedTracking = false;
    bool _gameWasActvated = false;
    bool _simulate = false;

    DebugDesk _debug;       // external

    private static int sIndex = 0;

    // overrides

    public override void OnStartServer()
    {
        gazeControls.SetActive(false);
    }

    public override void OnStartClient()
    {
        gazeControls.SetActive(true);

        _smoother = new Smoother<RawPoint>();   // original
        _smoother.saccadeThreshold = 30;        // 30
        _smoother.timeWindow = 100;             // 150
        _smoother.dampFixation = 250;           // 700

        _simulate = simulate || (Environment.UserName == "csolsp" && Screen.currentResolution.width == 1920);

        if (_simulate)
        {
            _simulator = FindObjectOfType<GazeSimulator>();
            _simulator.Sample += onSimulatorSample;
            _simulator.State += onSimulatorState;
            _simulator.Device += onSimulatorDevice;
            _simulator.Initialize();
            return;
        }

        _ws = new WebSocketSharp.WebSocket("ws://localhost:8086/");
        _ws.OnOpen += (sender, e) =>
        {
            print("WS:> Connected");
        };
        _ws.OnClose += (sender, e) =>
        {
            print("WS:> Disconnected");
        };
        _ws.OnError += (sender, e) =>
        {
            print($"WS:> Error {e.Message}");
        };
        _ws.OnMessage += (sender, e) =>
        {
            //print($"WS:> MSG {e.Data}");
            lock (_messages)
            {
                _messages.Enqueue(e.Data);
            }
        };

        _ws.ConnectAsync();
    }

    void Awake()
    {
        _debug = FindObjectOfType<DebugDesk>();
        name = $"gaze-{sIndex++}";
    }

    void Update()
    {
        lock (_messages)
        {
            while (_messages.Count > 0)
            {
                ParseMessage(_messages.Dequeue());
            }
        }
    }

    void OnDestroy()
    {
        if (_ws != null)
            _ws.Close();
    }

    void OnApplicationQuit()
    {
        if (_ws != null && _isTracking && _hasInitiatedTracking)
            _ws.Send(GazeIO.Request.toggleTracking);
    }

    // public methods

    public void ShowOptions()
    {
        if (!_simulate)
            _ws.Send(GazeIO.Request.showOptions);
    }

    public void Calibrate()
    {
        if (!_simulate)
            _ws.Send(GazeIO.Request.calibrate);
    }

    public void ToggleTracking()
    {
        if (!_isTracking)
        {
            _hasInitiatedTracking = true;

            var _networkGUI = FindObjectOfType<NetworkManagerHUD>();
            _networkGUI.showGUI = false;
        }

        if (_simulate)
            _simulator.ToggleTracking();
        else
            _ws.Send(GazeIO.Request.toggleTracking);
    }

    // internal methods

    void ParseMessage(string aMessage)
    {
        GazeIO.Sample sample = JsonUtility.FromJson<GazeIO.Sample>(aMessage);
        if (sample.isValid)
        {
            //print($"WS:> sample = {sample.x}, {sample.y}");
            UpdateCursorLocation(sample);
            return;
        }

        GazeIO.State state = JsonUtility.FromJson<GazeIO.State>(aMessage);
        if (state.isValid)
        {
            //print($"WS:> status = {state.value}");
            UpdateState(state);
            return;
        }

        GazeIO.Device device = JsonUtility.FromJson<GazeIO.Device>(aMessage);
        if (device.isValid)
        {
            //print($"WS:> device name = {device.name}");
            UpdateDeviceInfo(device);
            return;
        }
    }

    void UpdateDeviceInfo(GazeIO.Device aDevice)
    {
        deviceName.text = aDevice.name;
    }

    void UpdateState(GazeIO.State aState)
    {
        bool trackingChanged = aState.isTracking != _isTracking;

        _isTracking = aState.isTracking;

        // gaze ui and controls
        options.interactable = !_isTracking && !aState.isBusy;
        calibrate.interactable = !_isTracking && aState.isConnected && !aState.isBusy;
        toggleTracking.interactable = aState.isConnected && aState.isCalibrated && !aState.isBusy;
        toggleTracking.GetComponentInChildren<Text>().text = _isTracking ? "Stop" : "Start";
        gazeControls.SetActive(!_isTracking);

        State(this, new EventArgs());

        if (_isTracking && !_gameWasActvated)
            ActivateGame();

        if (trackingChanged)
        {
            // input module
            GetComponent<StandaloneInputModule>().enabled = !_isTracking;
            //GetComponent<GazeInput>().enabled = _isTracking;

            FirstPersonController fps = PlayerAvatar.getLocalPlayer()?.GetComponent< FirstPersonController >();
            if (fps != null)
            {
                fps.enabled = _isTracking;
            }

            if (_isTracking)
                GameStart(this, new EventArgs());
            else
                GameStop(this, new EventArgs());
        }
    }

    void UpdateCursorLocation(GazeIO.Sample aSample)
    {
        Vector2 location = GazeToGameWindow(aSample);
        pointer.anchoredPosition = location;

        _location = _smoother.Feed(new RawPoint(aSample.ts, location.x, location.y));

        Sample(this, new EventArgs());
    }

    Vector2 GazeToGameWindow(GazeIO.Sample aSample)
    {
        return new Vector2(
            aSample.x - Screen.width / 2 - _offset.x,
            Screen.height / 2 - (aSample.y - _offset.y)
        );
    }

    void ActivateGame()
    {
        // gaze pointer and conditions related to it
        //pointer.gameObject.SetActive(_isTracking);

        // avatar
        Rect rc = Camera.main.pixelRect;
        _scale.x = rc.width / Screen.currentResolution.width;
        _scale.y = rc.height / Screen.currentResolution.height;

        //_debug.print($"{rc.width} {rc.height}", "CAM_PIX");
        //_debug.print($"{Screen.currentResolution.width} {Screen.currentResolution.height}", "SCREEN_RES");
        //_debug.print($"{Screen.width} {Screen.height}", "SCREEN_SIZE");

        try
        {
            rc = Utils.GetWindowRect();
            //_debug.print($"{rc.x} {rc.y} {rc.width} {rc.height}", "WIN_SIZE");

            _offset = new Vector2(
                rc.x + (rc.width - Screen.width) / 2,
                rc.y + (rc.height - Screen.height) / 2 + 17 // toolbar
            );
            //_debug.print($"{_offset.x} {_offset.y}", "OFFSET");
        }
        catch (Exception e)
        {
            _debug.print(e.Message, "ERROR");
        }

        _gameWasActvated = true;
    }

    // Simulator
    void onSimulatorDevice(object aHandler, GazeSimulator.DeviceArgs aArgs)
    {
        UpdateDeviceInfo(aArgs.device);
    }

    void onSimulatorState(object aHandler, GazeSimulator.StateArgs aArgs)
    {
        UpdateState(aArgs.state);
    }

    void onSimulatorSample(object aHandler, GazeSimulator.SampleArgs aArgs)
    {
        UpdateCursorLocation(aArgs.sample);
    }
}
