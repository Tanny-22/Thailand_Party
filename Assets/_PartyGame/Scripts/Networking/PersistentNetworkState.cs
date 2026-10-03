using UnityEngine;

namespace PartyGame.Networking
{
    public sealed class PersistentNetworkState : MonoBehaviour
    {
        static PersistentNetworkState instance;
        void Awake()
        {
            if (instance && instance != this) { Destroy(gameObject); return; }
            instance = this; DontDestroyOnLoad(gameObject);
        }
    }
}
