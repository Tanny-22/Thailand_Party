using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Shared
{
    public sealed class ModalDialogController : MonoBehaviour
    {
        [SerializeField] TMP_Text messageLabel;
        [SerializeField] Button closeButton;
        void Awake() { if (closeButton) closeButton.onClick.AddListener(Hide); Hide(); }
        void OnDestroy() { if (closeButton) closeButton.onClick.RemoveListener(Hide); }
        public void Show(string message) { if (messageLabel) messageLabel.text = message; gameObject.SetActive(true); }
        public void Hide() => gameObject.SetActive(false);
    }
}
