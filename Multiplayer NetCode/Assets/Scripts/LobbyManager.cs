using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

[System.Serializable]
public struct PlayerData
{
    public string Name;
}
[System.Serializable]
public struct LobbyContainer
{
    public string name;
    public int maxplayer;
    public bool IsPrivate;
    public Lobby TheLobbyObect;
}
public class LobbyManager : MonoBehaviour
{
    #region UI
    public TMP_InputField LobbyNameInpt;
    public TMP_InputField MaxPlayersInpt;
    public Toggle LobbyIsPrivateTgle;
    public TMP_InputField PlayerNameInpt;

    public Transform LobbyParent;
    public GameObject LobbyBtn;
    public List<GameObject> LobbyBtns = new List<GameObject>();

    public Transform PlayerBtnParent;
    public GameObject PlayerBtn;
    public List<GameObject> PlayerBtnsInRoom = new List<GameObject>();

    public TMP_InputField LobbyCodeInpt;
    public TMP_InputField LobbyCodeView;

    public GameObject LobbyPanel, CreateLobbyPanel, ProfilePanel, RoomPanel, AskTheCodePanel;
    #endregion

    public LobbyContainer lobbyInfo;
    public PlayerData PlayerInfo;
    private Player player;
    UnityTransport _transport;
    // Start is called before the first frame update
    public async void Start()
    {
        _transport = GameObject.FindObjectOfType<UnityTransport>();
        AddListnersForInfoInput();

        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += PlayerSignInSuccess;
        AuthenticationService.Instance.SignInFailed += PlayerSignInFailed;
        AuthenticationService.Instance.SignedOut += PlayerSignedOut;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        ProfilePanel?.SetActive(true);

    }

    private float heartBeatTimer;
    [SerializeField] private float MaxHeartBeat = 15f;
    // Update is called once per frame
    private void Update()
    {
        HandleHeartBeat();
    }
    public async void HandleHeartBeat()
    {
        if (lobbyInfo.TheLobbyObect != null && player.Id == lobbyInfo.TheLobbyObect.HostId )
        {
            heartBeatTimer -= Time.deltaTime;
            if (heartBeatTimer <= 0)
            {
                heartBeatTimer = MaxHeartBeat;
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyInfo.TheLobbyObect.Id);

            }
        }
    }
    public void PlayerSignInSuccess()
    {
        Debug.Log("Signed In with player id: " + AuthenticationService.Instance.PlayerId);
    }
    public void PlayerSignInFailed(RequestFailedException err)
    {
        Debug.Log("sign in failed : " + err.Message);
    }
    public void PlayerSignedOut()
    {
        Debug.Log("sign out");
    }

    public async void CreateLobby()
    {
        try
        {
            var alloc = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = lobbyInfo.IsPrivate,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {
                        "JoinCodeKey" , new DataObject(DataObject.VisibilityOptions.Public,joinCode)
                    }
                }

            };
            //if (lobbyInfo.TheLobbyObect != null)
            lobbyInfo.TheLobbyObect = await Lobbies.Instance.CreateLobbyAsync(lobbyInfo.name, lobbyInfo.maxplayer, lobbyOptions);
            Debug.Log("lobby code: " + lobbyInfo.TheLobbyObect.LobbyCode);
            LobbyCodeView.text = lobbyInfo.TheLobbyObect.LobbyCode;
            PlayerPrefs.SetInt("StartType", 0);
            _transport.SetHostRelayData(alloc.RelayServer.IpV4,(ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,alloc.Key, alloc.ConnectionData);

            //GetAllLobbies();

            LobbyPanel?.SetActive(false);
            RoomPanel?.SetActive(true);
            GetAllPlayerInTheLobby();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }

    public async void GetAllLobbies()
    {
        try
        {
            for (int i = 0; i < LobbyBtns.Count; i++)
                Destroy(LobbyBtns[i]);
            LobbyBtns.Clear();
            QueryResponse resp = await Lobbies.Instance.QueryLobbiesAsync();
            foreach (Lobby lobby in resp.Results)
            {
                Debug.Log("Lobby " + lobby.Name);
                GameObject Btn = Instantiate(LobbyBtn);
                LobbyBtn _LobbyBtn = Btn.AddComponent<LobbyBtn>();
                _LobbyBtn.LobbyInfo.name = lobby.Name;
                _LobbyBtn.LobbyInfo.maxplayer = lobby.MaxPlayers;
                _LobbyBtn.LobbyInfo.IsPrivate = lobby.IsPrivate;
                _LobbyBtn.LobbyInfo.TheLobbyObect = lobby;
                _LobbyBtn.JoinLobbyBtn = Btn.gameObject.GetComponent<Button>();
                //string code = lobby.LobbyCode;
                //Debug.Log("going with code: " + code + " should be: " + lobby.LobbyCode);
                //_LobbyBtn.JoinLobbyBtn.onClick.AddListener(() => AskTheCode());
                _LobbyBtn.JoinLobbyBtn.onClick.AddListener(() => JoinLobby(lobby));

                Btn.transform.parent = LobbyParent;
                if (Btn.transform.GetChild(0).GetComponent<TMPro.TMP_Text>())
                    Btn.transform.GetChild(0).GetComponent<TMPro.TMP_Text>().text = lobby.Name;
                LobbyBtns.Add(Btn);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
    public void AskTheCode()
    {
        AskTheCodePanel?.SetActive(true);
    }
    public void JoinLobby()
    {
        JoinLobby(LobbyCodeInpt.text);
    }
    public async void JoinLobby(Lobby _lobby)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions
            {
                Player = GetPlayer()
            };

            Debug.Log("joining id: " + _lobby.Id);
            lobbyInfo.TheLobbyObect = await Lobbies.Instance.JoinLobbyByIdAsync(_lobby.Id, options);
            PlayerPrefs.SetInt("StartType", 1);
            LobbyPanel?.SetActive(false);
            RoomPanel?.SetActive(true);
            var alloc = await RelayService.Instance.JoinAllocationAsync(
                lobbyInfo.TheLobbyObect.Data["JoinCodeKey"].Value);

            _transport.SetClientRelayData(alloc.RelayServer.IpV4, (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes, alloc.Key, alloc.ConnectionData, alloc.HostConnectionData);
            GetAllPlayerInTheLobby();
        }
        catch (LobbyServiceException err)
        {
            Debug.LogError(err);
        }
    }
    public async void JoinLobby(string LobbyCode)
    {

        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Debug.Log("joining with code: " + LobbyCode);
            lobbyInfo.TheLobbyObect = await Lobbies.Instance.JoinLobbyByCodeAsync(LobbyCode, options);
            PlayerPrefs.SetInt("StartType", 1);
            LobbyPanel?.SetActive(false);
            RoomPanel?.SetActive(true);

            GetAllPlayerInTheLobby();
        }
        catch (LobbyServiceException err)
        {
            Debug.LogError(err);
        }
    }

    public Player GetPlayer()
    {
        return player;

    }
    public void GetAllPlayerInTheLobby()
    {
        foreach (GameObject g in PlayerBtnsInRoom)
        {
            Destroy(g);
        }
        PlayerBtnsInRoom.Clear();

        foreach (Player p in lobbyInfo.TheLobbyObect.Players)
        {
            GameObject btn = Instantiate(PlayerBtn, PlayerBtnParent);
            PlayerDataObject pObject;
            if (p.Data.TryGetValue("PlayerName", out pObject))
                btn.GetComponentInChildren<TMPro.TMP_Text>().text = pObject.Value;
            btn.GetComponent<Button>().onClick.AddListener(() => { RemovePlayerFromLobby(); });
            PlayerBtnsInRoom.Add(btn);
        }
    }
    public async void RemovePlayerFromLobby()
    {
        try
        {
            await Lobbies.Instance.RemovePlayerAsync(lobbyInfo.TheLobbyObect.Id, AuthenticationService.Instance.PlayerId);
            GetAllPlayerInTheLobby();
        }
        catch (LobbyServiceException err)
        {
            Debug.LogError(err);
        }
    }
    #region SetLobbyContainerInfos

    public void AddListnersForInfoInput()
    {
        LobbyNameInpt.onEndEdit.AddListener(OnEndEditLobbyName);
        MaxPlayersInpt.onEndEdit.AddListener(OnEndEditMaxPlayers);
        LobbyIsPrivateTgle.onValueChanged.AddListener(OnIsPrivateToggle);
        PlayerNameInpt.onEndEdit.AddListener(OnEndEditPlayerName);
    }
    public void OnEndEditLobbyName(string _name)
    {
        lobbyInfo.name = _name;
    }
    public void OnEndEditMaxPlayers(string _maxPlayers)
    {
        lobbyInfo.maxplayer = int.Parse(_maxPlayers);
    }
    public void OnIsPrivateToggle(Boolean _active)
    {
        lobbyInfo.IsPrivate = _active;
    }
    public void OnEndEditPlayerName(string _name)
    {
        PlayerInfo.Name = _name;
        player = new Player(AuthenticationService.Instance.PlayerId)
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,
                PlayerInfo.Name)
                }
            }
        };
    }
    #endregion
    public void NextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex+1);
    }
}

public class LobbyBtn : MonoBehaviour
{
    public LobbyContainer LobbyInfo;
    public Button JoinLobbyBtn;
}
