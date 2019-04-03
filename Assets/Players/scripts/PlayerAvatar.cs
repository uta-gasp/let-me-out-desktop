using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerAvatar : NetworkBehaviour
{
    // visible in editor

    public GameObject[] avatars = new GameObject[3];
    public float offset = -1.3f;

    // readonly

    public bool isAlive { get { return _health > 0; } }
    public string avatarName { get { return _name; } }

    // internal

    static int _index = 0;
    static Mutex _mutex = new Mutex();

    DebugDesk _debug;                   // external
    GameObject _avatar;                 // instantiated
    Animator _animator;                 // _avatar internal
    Light _light;                       // child-internal
    FirstPersonController _fpc;         // internal
    InfoDock _infoPanel;                // external
    Logger.LogDomain _log;

    string _name;
    bool _isWalking = false;

    [SyncVar(hook = "OnChangeHealth")]
    float _health = 1f;

    float _fallSpeed = 1f;
    Vector3 _fallAxe = new Vector3(0, 0, 1);

    // public methods

    public float DecreaseHealth(float aAmount)
    {
        _health = Mathf.Max(0f, _health - aAmount);
        return _health;
    }

    // server-side
    public void respawn()
    {
        if (_log != null)
            _log.add("respawned");

        Invoke("RestoreProps", 1.5f);

        RpcRespawn();
    }

    public void hitsDoor(string aName)
    {
        if (!isServer)
            return;

        if (_log != null)
            _log.add($"hits-door\t{aName}");
    }

    public static PlayerAvatar getLocalPlayer()
    {
        return FindObjectsOfType<PlayerAvatar>().Single(player => player.isLocalPlayer);
    }

    // overrides

    void Start()
    {
        _debug = FindObjectOfType<DebugDesk>();

        _light = GetComponentInChildren<Light>();
        _fpc = GetComponent<FirstPersonController>();
        _infoPanel = FindObjectOfType<InfoDock>();

        int index;
        lock (_mutex)
        {
            index = _index++;
            if (_index >= avatars.Length)
                _index = 0;
        }

        CreateAvatar(index);

        if (!isLocalPlayer)
            return;

        Camera.main.transform.parent = GetComponent<FirstPersonController>().transform;
        Camera.main.transform.localPosition = new Vector3(0, 0, 0.3f);

        _avatar.transform.forward = Camera.main.transform.forward;
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!_isWalking && _fpc.isWalking)
        {
            if (_log != null)
                _log.add("move");
            //_animator.SetBool("isWalking", true);
        }
        else if (_isWalking && !_fpc.isWalking)
        {
            if (_log != null)
                _log.add("stop");
            //_animator.SetBool("isWalking", false);
        }

        _isWalking = _fpc.isWalking;
    }

    //internal mehtods

    void CreateAvatar(int aIndex)
    {
        _name = $"player-{aIndex}";

        Logger logger = FindObjectOfType<Logger>();
        if (logger)
            _log = logger.register($"player\t{aIndex}");

        _avatar = Instantiate(avatars[aIndex]);
        _avatar.transform.parent = transform;
        _avatar.transform.localPosition = new Vector3(0, offset, -0.3f);
        _avatar.transform.localRotation = new Quaternion(0, 0, 0, 0);

        _animator = _avatar.GetComponent<Animator>();
    }

    // client-side
    void Die()
    {
        if (!isLocalPlayer)
            return;

        if (_log != null)
            _log.add("dead");

        _fpc.enabled = false;
        _light.enabled = false;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (isLocalPlayer)
        {
            _infoPanel.showMessage("Respawning...", 2.0f, 0.5f);

            Invoke("RespawnAtOrigin", 1.5f);
            Invoke("EnableAfterRespawn", 3);
        }
        else
        {
            _infoPanel.notify("Respawning the partner...");
            Invoke("ClearNotification", 3);
        }
    }

    // client-side
    void RespawnAtOrigin()
    {
        NetworkStartPosition[] spawnPoints = FindObjectsOfType<NetworkStartPosition>().ToArray();
        int randomSpawnPointIndex = (int)Mathf.Round(UnityEngine.Random.Range(0, spawnPoints.Length - 1));
        NetworkStartPosition spawnPoint = spawnPoints[randomSpawnPointIndex];

        transform.position = spawnPoint.transform.position;
    }

    // client-side
    void EnableAfterRespawn()
    {
        _fpc.enabled = true;
        _light.enabled = true;

        _infoPanel.hideMessage();
    }

    void ClearNotification()
    {
        _infoPanel.clearNotification();
    }

    // server-side
    void RestoreProps()
    {
        _health = 1f;
    }

    // callbacks

    void OnChangeHealth(float aValue)
    {
        if (aValue == 0f)
        {
            Die();
        }

        if (isLocalPlayer)
        {
            _infoPanel.SetHealth(aValue);
        }
    }
}
