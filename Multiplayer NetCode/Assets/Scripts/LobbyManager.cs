using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

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
    public LobbyContainer lobbyInfo;
    public PlayerData PlayerInfo;
    private Player player;

    // Start is called before the first frame update
    public async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += PlayerSignInSuccess;
        AuthenticationService.Instance.SignInFailed += PlayerSignInFailed;
        AuthenticationService.Instance.SignedOut += PlayerSignedOut;

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        player = new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member,PlayerInfo.Name) }
            }
        };
    }

    // Update is called once per frame
    private void Update()
    {

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
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = lobbyInfo.IsPrivate,
                Player = GetPlayer()
            };
            //if (lobbyInfo.TheLobbyObect != null)
            lobbyInfo.TheLobbyObect = await Lobbies.Instance.CreateLobbyAsync(lobbyInfo.name, lobbyInfo.maxplayer, lobbyOptions);
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
            QueryResponse resp = await Lobbies.Instance.QueryLobbiesAsync();
            foreach (Lobby lobby in resp.Results)
            {
                Debug.Log("Lobby " + lobby.Name);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
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
            await Lobbies.Instance.JoinLobbyByCodeAsync(LobbyCode, options);
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
}
