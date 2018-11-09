using System;
using UnityEngine;

public class GazeSimulator : MonoBehaviour
{
    readonly float SAMPLING_INTERVAL = 0.03333f;

    ulong _timeStamp = 0;

    public class SampleArgs : EventArgs
    {
        public readonly GazeIO.Sample sample;
        public SampleArgs(ulong aTimestamp, float aX, float aY, float aPupil)
        {
            sample = new GazeIO.Sample();
            sample.type = GazeIO.MessageType.sample;
            sample.ts = aTimestamp;
            sample.x = aX;
            sample.y = aY;
            sample.p = aPupil;
        }
    }

    public class StateArgs : EventArgs
    {
        public readonly GazeIO.State state;
        public StateArgs(GazeIO.State aState)
        {
            state = aState;
        }
    }

    public class DeviceArgs : EventArgs
    {
        public readonly GazeIO.Device device;
        public DeviceArgs(string aDeviceName)
        {
            device = new GazeIO.Device();
            device.type = GazeIO.MessageType.device;
            device.name = aDeviceName;
        }
    }

    public delegate void SampleEvent(object aHandler, SampleArgs aArgs);
    public delegate void StateEvent(object aHandler, StateArgs aArgs);
    public delegate void DeviceEvent(object aHandler, DeviceArgs aArgs);

    public event SampleEvent Sample = delegate { };
    public event StateEvent State = delegate { };
    public event DeviceEvent Device = delegate { };

    GazeIO.State _state = new GazeIO.State();
    Vector2 _offset;

    DebugDesk _debug;

    void Awake()
    {
        _debug = FindObjectOfType<DebugDesk>();

        Rect rc = Utils.GetWindowRect();

        _offset = new Vector2(
            rc.x + (rc.width - Screen.width) / 2,
            rc.y + (rc.height - Screen.height) / 2 + 17 // toolbar
        );

        _state.type = GazeIO.MessageType.state;
        _state.value = (int)GazeIO.StateValue.Connected | (int)GazeIO.StateValue.Calibrated;
    }

    public void ToggleTracking()
    {
        if ((_state.value & (int)GazeIO.StateValue.Tracking) == 0)
        {
            _state.value |= (int)GazeIO.StateValue.Tracking;
            InvokeRepeating("EmitSample", SAMPLING_INTERVAL, SAMPLING_INTERVAL);
        }
        else
        {
            _state.value &= ~(int)GazeIO.StateValue.Tracking;
            _timeStamp = 0;
            CancelInvoke();
        }

        State(this, new StateArgs(_state));
    }

    public void Initialize()
    {
        Device(this, new DeviceArgs("Simulator"));
        State(this, new StateArgs(_state));
    }

    void EmitSample()
    {
        _timeStamp += (ulong)(SAMPLING_INTERVAL * 1000);

        float x, y;
        MouseToGaze(out x, out y);

        Sample(this, new SampleArgs(_timeStamp, x, y, 6.0f));
    }

    void MouseToGaze(out float aX, out float aY)
    {
        aX = Input.mousePosition.x + _offset.x;
        aY = (Screen.height - Input.mousePosition.y) + _offset.y;
    }
}
