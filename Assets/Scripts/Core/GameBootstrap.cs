using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameBootstrap — Tự động đảm bảo GameManager tồn tại ở bất kỳ scene nào.
/// Nếu bấm Play từ sai scene (không phải MainMenu), tự redirect về MainMenu.
/// Đặt script này vào 1 empty GameObject trong MẾT scene nào đó.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnGameStart()
    {
        // Đảm bảo GameManager luôn tồn tại
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            DontDestroyOnLoad(gm);
        }
    }
}
