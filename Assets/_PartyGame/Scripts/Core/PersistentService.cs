using UnityEngine;

namespace PartyGame.Core
{
    public abstract class PersistentService<T> : MonoBehaviour where T : Component
    {
        public static T Instance { get; private set; }
        protected virtual void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this as T;
        }
    }
}
