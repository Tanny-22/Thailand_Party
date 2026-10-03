using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class QRCodeGenerator : MonoBehaviour
{
    [Header("UI Elements")]
    public RawImage qrCodeDisplay; // ลาก RawImage หรือ Image มาวางตรงนี้
    public TMP_Text urlDisplayText; // ลาก TextMeshPro มาวางตรงนี้

    [Header("Settings")]
    // ลาก GameObject ที่มี PhoneSensorSender ติดอยู่มาใส่ตรงนี้ใน Inspector
    // (ต้องเป็นตัวเดียวกับที่เปิด Peer host จริง ไม่งั้น QR จะชี้ผิดห้องอีก)
    public PhoneSensorSender phoneSensorSender;

    void Start()
    {
        if (phoneSensorSender == null)
        {
            Debug.LogError("QRCodeGenerator: ยังไม่ได้ลาก PhoneSensorSender มาใส่ใน Inspector — QR Code จะไม่ตรงกับ Peer host ที่เปิดจริง");
            return;
        }

        // 1. ใช้ Game ID เดียวกับที่ PhoneSensorSender สร้าง (แหล่งความจริงเดียว)
        string uniqueGameId = phoneSensorSender.UniqueGameId;

        // 2. ใช้ URL เดียวกับที่ PhoneSensorSender สร้าง
        string fullUrl = phoneSensorSender.FullControllerUrl;

        // 3. แสดง URL บนข้อความ Text
        if (urlDisplayText != null)
        {
            urlDisplayText.text = " Scan QR Code or Open:\n" + fullUrl;
        }

        // 4. เริ่มดาวน์โหลดรูป QR Code
        StartCoroutine(GenerateQRCode(fullUrl));
    }

    IEnumerator GenerateQRCode(string urlToEncode)
    {
        // ใช้ API เจนรูป QR Code ขนาด 300x300
        string apiUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" + UnityWebRequest.EscapeURL(urlToEncode);

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // ดึงภาพ Texture ที่โหลดมาใส่ลงใน RawImage
                Texture2D qrTexture = DownloadHandlerTexture.GetContent(request);
                if (qrCodeDisplay != null)
                {
                    qrCodeDisplay.texture = qrTexture;
                }
            }
            else
            {
                Debug.LogError("QR Code Load Failed: " + request.error);
            }
        }
    }
}