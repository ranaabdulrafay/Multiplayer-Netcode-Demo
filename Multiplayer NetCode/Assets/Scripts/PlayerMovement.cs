using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Unity.Networking.Transport.NetworkDriver;

// this must be of passing by values
[System.Serializable]
public class MyNetworkData : INetworkSerializable
{
    public Vector3 HitPoint;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HitPoint);
    }
}
public class PlayerMovement : NetworkBehaviour
{
    RaycastHit hit;
    Vector3 pos = Vector3.zero;

    // network
    public NetworkVariable<MyNetworkData> NetworkHitData = new NetworkVariable<MyNetworkData>(new MyNetworkData()
        , NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Owner);
    public void ChangeData(MyNetworkData prev , MyNetworkData newValue)
    {
        Debug.Log("value changed ");
        pos = newValue.HitPoint;
    }
    public override void OnNetworkSpawn()
    {
        NetworkHitData.OnValueChanged += ChangeData;
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                MyNetworkData data = new MyNetworkData();
                data.HitPoint = hit.point;
                NetworkHitData.Value = data;
            }
        }

        transform.position = Vector3.Lerp (transform.position, pos, Time.deltaTime*2);
    }
}
