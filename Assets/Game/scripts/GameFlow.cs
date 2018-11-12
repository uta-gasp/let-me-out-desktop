using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class GameFlow : NetworkBehaviour
{
    // visible in editor

    public Rect winArea;
    public InfoDock infoPanel;
    public Light[] winLights;
    public Setup setup;

    // readonly

    public bool isFinished { get { return _isFinished; } }

    // internal 

    const float HEALTH_DECREASE_PER_SECOND = 0.2f;

    DebugDesk _debug;               // external
    Sounds _sounds = null;          // internal
    Logger.LogDomain _logGeneral;
    Logger.LogDomain _logErrors;

    bool _isFinished = false;

    // overrides

    void Awake()
    {
        _debug = FindObjectOfType<DebugDesk>();
        Logger logger = FindObjectOfType<Logger>();
        if (logger)
        {
            _logGeneral = logger.register("game");
            _logErrors = logger.register("error");
        }

        Application.logMessageReceived += HandleLog;
    }

    void Start()
    {
        setup.gameObject.SetActive(false);
    }

    public override void OnStartClient()
    {
        _sounds = GetComponent<Sounds>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (!isServer || _isFinished)
            return;

        // check for win: everybody should be outside

        Transform[] players = FindObjectsOfType<FirstPersonController>().Select(obj => obj.transform).ToArray();
        if (players.Length == 0)
            return;

        bool isSomeoneIsStillInside = false;
        foreach (Transform player in players)
        {
            Vector2 playerPositionOnTheFloor = new Vector2(player.position.x, player.position.z);
            if (!winArea.Contains(playerPositionOnTheFloor))
            {
                isSomeoneIsStillInside = true;
                break;
            }
        }

        if (!isSomeoneIsStillInside)
        {
            _isFinished = true;
            End();
        }
    }

    // public methods

    // server-side
    public void CaptureKey(Key aKey)
    {
        string keyName = aKey.name;
        string doorName = aKey.door.name;

        // on a server
        OpenDoor(aKey.door);
        DisplayKeyOnInfoPanel(keyName);

        // on clients
        RpcPlayKeyCaptureSound();
        RpcDisplayKeyOnInfoPanel(keyName);
        RpcOpenDoor(doorName);
    }

    // server-side
    public void HitPlayer(Transform aPlayer, float aWeight)
    {
        if (_isFinished)
            return;

        PlayerAvatar player = aPlayer.gameObject.GetComponent<PlayerAvatar>();
        if (!player.isAlive)
            return;

        player.DecreaseHealth(HEALTH_DECREASE_PER_SECOND * Time.deltaTime * aWeight);  

        if (!player.isAlive)
        {
            player.respawn();
        }
    }

    // internal methods

    // server-side
    void End()
    {
        if (!isServer)
            return;

        RpcLightsOn();
        RpcEndGame();
    }

    // client-side
    void ShowCompleteMessage()
    {
        infoPanel.showMessage("Completed!");
    }

    void OpenDoor(GameObject aDoor)
    {
        aDoor.GetComponent<BoxCollider>().enabled = false;
        aDoor.GetComponent<Animator>().SetBool("open", true);
    }

    void DisplayKeyOnInfoPanel(string aName)
    {
        infoPanel.AcquireKey(aName);
    }

    void Exit()
    {
        Application.Quit();
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            if (_logErrors != null)
                _logErrors.add(logString);
            Debug.Log($"ERROR: {logString} [{stackTrace}]");
        }
        /*
        else
        {
            _debug.print(logString);
        }*/
    }

    [ClientRpc]
    void RpcPlayKeyCaptureSound()
    {
        if (_sounds != null)
            _sounds.getKey();
    }

    [ClientRpc]
    void RpcOpenDoor(string aName)
    {
        if (_logGeneral != null)
            _logGeneral.add($"open-door\t{aName}");

        GameObject door = FindObjectsOfType<GameObject>().Single(obj => obj.tag == "door" && obj.name == aName);
        OpenDoor(door);
    }

    [ClientRpc]
    void RpcDisplayKeyOnInfoPanel(string aName)
    {
        DisplayKeyOnInfoPanel(aName);
    }

    [ClientRpc]
    void RpcLightsOn()
    {
        if (_logGeneral != null)
            _logGeneral.add("lights-on");

        foreach (Light light in winLights)
        {
            light.gameObject.SetActive(true);
        }
    }

    [ClientRpc]
    void RpcEndGame()
    {
        if (_logGeneral != null)
            _logGeneral.add($"finished");

        Invoke("ShowCompleteMessage", 2);
        Invoke("Exit", 4);
    }
}
