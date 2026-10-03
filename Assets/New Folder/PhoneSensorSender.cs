using System.Runtime.InteropServices;
using UnityEngine;

public class PhoneSensorSender : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitPeerServer(string gameId, string controllerUrl);

    [Header("Controller Settings")]
    public string controllerBaseUrl = "https://your-domain.com/controller.html"; // URL หน้าเว็บมือถือของคุณ
    public Transform racketTransform;
    public float sensitivity = 2.0f;

    // ID เดียวที่ใช้เป็น "แหล่งความจริง" (source of truth) — สคริปต์อื่นต้องมาอ่านค่าจากตรงนี้
    // ห้ามให้สคริปต์อื่นสุ่ม ID เอง ไม่งั้น QR Code จะชี้ไปคนละห้องกับ Peer host ที่เปิดจริง
    public string UniqueGameId { get; private set; }
    public string FullControllerUrl { get; private set; }

    void Awake()
    {
        // สุ่ม ID ประจำห้องของเกมเครื่องนี้ ทำใน Awake เพื่อให้พร้อมใช้งานก่อน Start ของสคริปต์อื่น
        UniqueGameId = "TENNIS-" + Random.Range(1000, 9999);
        FullControllerUrl = controllerBaseUrl + "?gameId=" + UniqueGameId;
    }

    void Start()
    {
        Debug.Log("QR Code URL: " + FullControllerUrl);

#if UNITY_WEBGL && !UNITY_EDITOR
        // เรียกใช้งาน PeerJS ผ่าน JavaScript Plugin ใน WebGL
        InitPeerServer(UniqueGameId, FullControllerUrl);
#endif
    }

    // ฟังก์ชันนี้จะถูกเรียกจาก JavaScript เมื่อมีข้อมูลส่งมาจากมือถือ
    public void OnReceiveMotionData(string data)
    {
        string[] values = data.Split(',');
        if (values.Length >= 6)
        {
            float gx = float.Parse(values[0]);
            float gy = float.Parse(values[1]);
            float gz = float.Parse(values[2]);

            // หมุนไม้เทนนิสตามค่า Gyro จากมือถือ
            if (racketTransform != null)
            {
                racketTransform.Rotate(-gx * sensitivity * Time.deltaTime, -gy * sensitivity * Time.deltaTime, gz * sensitivity * Time.deltaTime, Space.Self);
            }
        }
    }
}