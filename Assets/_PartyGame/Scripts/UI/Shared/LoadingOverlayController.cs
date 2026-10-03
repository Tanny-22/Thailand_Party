using TMPro;
using UnityEngine;

namespace PartyGame.UI.Shared
{
    public sealed class LoadingOverlayController : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        public void Show(string message = "Loading...") { if (label) label.text = message; gameObject.SetActive(true); }
        public void Hide() => gameObject.SetActive(false);
    }
}
