using Fusion;
using UnityEngine;

public class WhiteboardSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkPrefabRef whiteboardPrefab;
    [SerializeField] private Vector3 spawnPosition;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {

            Quaternion rotation = Quaternion.Euler(new Vector3(0, 90, -90));
            Runner.Spawn(whiteboardPrefab, spawnPosition,rotation);
        }
    }
}
