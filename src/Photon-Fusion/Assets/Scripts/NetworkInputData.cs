using Fusion;
using UnityEngine;

// The NetworkInputData struct is used to store data that is sent over the network as input. 
public struct NetworkInputData : INetworkInput
{
    public Vector3 Direction; // Stores the direction of movement as a Vector3
}