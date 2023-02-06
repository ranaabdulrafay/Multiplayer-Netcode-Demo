using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkUI : MonoBehaviour
{
    // Start is called before the first frame update
    public void _Start()
    {
        switch(PlayerPrefs.GetInt("StartType",-1))
        {
            case 0:
                StartHost();
                break;
            case 1:
                StartClient();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }
}
