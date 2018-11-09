using UnityEngine;
using UnityEngine.Networking;

public class GazeLightController : NetworkBehaviour
{
    // internal

    DebugDesk _debug;           // external
    GazeClient _gazeClient;     // external
    Light _spotlight;           // child-internal

    int _id;

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _id = Random.Range(0, int.MaxValue);

        _spotlight = GetComponentInChildren<Light>();

        if (!isLocalPlayer)
            return;

        _gazeClient = FindObjectOfType<GazeClient>();
        _gazeClient.State += onGazeClientStateChanged;
        _gazeClient.Sample += onGazeClientNewSample;
    }

    // internal methods

    void onGazeClientStateChanged(object sender, System.EventArgs e)
    {
        // do nothing here
    }

    void onGazeClientNewSample(object sender, System.EventArgs e)
    {
        RawPoint location = _gazeClient.location;

        Camera cam = Camera.main;
        if (!cam)
            return;

        float x = location.x / Screen.height * cam.fieldOfView;
        float y = location.y / Screen.height * cam.fieldOfView;

        float axisAngle = Mathf.Rad2Deg * Mathf.Atan2(y, x);
        _spotlight.transform.position = cam.transform.position;
        _spotlight.transform.rotation = cam.transform.rotation;
        _spotlight.transform.Rotate(Vector3.forward, axisAngle);

        float rotAngle = Mathf.Sqrt(x * x + y * y);
        float correction = -15f * (1f - Mathf.Cos(Mathf.Deg2Rad * x) * Mathf.Cos(Mathf.Deg2Rad * y));   // -15 - magic number
        _spotlight.transform.Rotate(Vector3.up, rotAngle + correction);

        CmdReportAngle(_spotlight.transform.localRotation, _id);
    }

    [Command]
    void CmdReportAngle(Quaternion aAngles, int aClientID)
    {
        _spotlight.transform.localRotation = aAngles;
        RpcUpdateAngle(aAngles, aClientID);
    }

    [ClientRpc]
    void RpcUpdateAngle(Quaternion aAngles, int aClientID)
    {
        if (_id != aClientID && _spotlight)
        {
            _spotlight.transform.localRotation = aAngles;
        }
    }
}
