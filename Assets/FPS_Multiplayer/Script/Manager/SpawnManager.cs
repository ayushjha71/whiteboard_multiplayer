using FPS_Multiplayer.Network;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FPS_Multiplayer.Manager
{
    public class SpawnManager : NetworkBehaviour, IPlayerJoined
    {
        [SerializeField]
        private NetworkPrefabRef networkPlayer;


        public override void Spawned()
        {
           
        }

        public void PlayerLeft(PlayerRef player)
        {
            if(player == Runner.LocalPlayer)
            {
                Runner.Despawn(Object);
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            if (player == Runner.LocalPlayer)
            {
                Runner.Spawn(networkPlayer, FPS_Multiplayer.Utils.Utils.GetRandomSpawnPoint(), Quaternion.identity);
            }
        }
    }
}
