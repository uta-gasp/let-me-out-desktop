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

    public override void OnStartClient()
    {
        _sounds = GetComponent<Sounds>();
    }

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

        /*
        NetworkManager networkManager = FindObjectOfType<NetworkManager>();

        string[] args = System.Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            _debug.print($"{arg}");
            if (arg == "--server")
            {
                networkManager.StartServer();
                FindObjectOfType<NetworkManagerHUD>().showGUI = false;
                _debug.print("server started");
            }
        }*/
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

        bool isAnyoneIsStillInside = false;
        foreach (Transform player in players)
        {
            Vector2 playerPositionOnTheFloor = new Vector2(player.position.x, player.position.z);
            if (!winArea.Contains(playerPositionOnTheFloor))
            {
                isAnyoneIsStillInside = true;
                break;
            }
        }

        if (!isAnyoneIsStillInside)
        {
            _isFinished = true;
            Win();
        }
    }

    // public methods

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
            _isFinished = true;
            Lost();
        }
    }

    // internal methods

    void OpenDoor(GameObject aDoor)
    {
        aDoor.GetComponent<BoxCollider>().enabled = false;
        aDoor.GetComponent<Animator>().SetBool("open", true);
    }

    void DisplayKeyOnInfoPanel(string aName)
    {
        infoPanel.AcquireKey(aName);
    }

    void Lost()
    {
        if (!isServer)
            return;

        // disable players
        PlayerAvatar[] players = FindObjectsOfType<PlayerAvatar>().Where(player => player.isAlive).ToArray();
        foreach (var player in players)
        {
            player.RpcDisable();
        }

        ShowLostMessage();

        RpcEndGame("Lost");
    }

    void Win()
    {
        if (!isServer)
            return;

        ShowWinMessage();

        RpcLightsOn();
        RpcEndGame("Win");
    }

    void ShowLostMessage()
    {
        infoPanel.messageCenter.GetComponentInChildren<Text>().text = "Game over...";
        infoPanel.messageCenter.SetActive(true);
    }

    void ShowWinMessage()
    {
        infoPanel.messageCenter.GetComponentInChildren<Text>().text = "Completed!";
        infoPanel.messageCenter.SetActive(true);
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
    void RpcEndGame(string aResult)
    {
        if (_logGeneral != null)
            _logGeneral.add($"finished\t{aResult}");

        Invoke($"Show{aResult}Message", 2);
        Invoke("Exit", 4);
    }
}
