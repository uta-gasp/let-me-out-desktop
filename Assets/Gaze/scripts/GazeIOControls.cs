using UnityEngine;

/**
 * <summary>Click handlers for UI buttons + shortcut handlers
 * 
 * USAGE: throw on the UI object that contains the buttons to send command to ETUDriver service, 
 * assign the corresponding click events for this buttons
 * </summary>
 * */
public class GazeIOControls : MonoBehaviour
{
    // internal

    DebugDesk _debug;           // external
    GazeClient _gazeClient;     // external

    bool _blockInput = false;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _gazeClient = FindObjectOfType<GazeClient>();
    }

    void Update()
    {
        if (_gazeClient.isTracking)
        {
            if (!_blockInput && Input.GetKey(KeyCode.Escape))
            {
                _blockInput = true;
                ToggleTracking();
            }
        }
        else
        {
            _blockInput = false;
        }
    }

    // public methods

    public void Options()
    {
        _gazeClient.ShowOptions();
    }

    public void Calibrate()
    {
        _gazeClient.Calibrate();
    }

    public void ToggleTracking()
    {
        _gazeClient.ToggleTracking();
    }
}
