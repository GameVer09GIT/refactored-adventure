using UnityEngine;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance; // Để sau này quái cắn thì gọi hàm dễ dàng

    [Header("Kéo thả vào đây")]
    public Transform spawnPoint;
    public GameObject player;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Vừa vào game là tự động bế nhân vật quăng về đúng chỗ SpawnPoint
        RespawnPlayer();
    }

    public void RespawnPlayer()
    {
        // Bắt buộc phải tắt Character Controller trước khi đổi tọa độ
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Ép tọa độ và góc xoay của Player bằng đúng với SpawnPoint
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        // Bật lại Character Controller để đi lại bình thường
        if (cc != null) cc.enabled = true;
    }
}