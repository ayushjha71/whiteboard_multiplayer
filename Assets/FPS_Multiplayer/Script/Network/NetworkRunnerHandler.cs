using Fusion;
using Fusion.Sockets;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FPS_Multiplayer.Network
{
    public class NetworkRunnerHandler : MonoBehaviour
    {
        [SerializeField]
        private NetworkRunner networkRunnerPrefab;

        private NetworkRunner mNetworkRunner;

        private void Start()
        {
            mNetworkRunner = Instantiate(networkRunnerPrefab);
            mNetworkRunner.name = "Network Runner";

            var clientTask = InitializeRunner(mNetworkRunner, GameMode.Shared, NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), null);
        }

        protected virtual Task InitializeRunner(NetworkRunner runner, GameMode gameMode, NetAddress address, SceneRef scene, Action<NetworkRunner> OnGameStarted)
        {
            var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();
            if (sceneManager != null)
            {
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }
            runner.ProvideInput = true;

            return runner.StartGame(new StartGameArgs
            {
                GameMode = gameMode,
                Address = address,
                Scene = scene,
                SessionName = "WhiteBoard",
                OnGameStarted = OnGameStarted,
                SceneManager = sceneManager
            });
        } 
    }
}

