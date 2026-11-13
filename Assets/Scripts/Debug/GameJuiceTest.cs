using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class GameJuiceTest : MonoBehaviour
{
    public GameObject floatingTextPrefab;
    public Transform damageSpawnPoint;
    public DiceVisual[] diceToRoll;
    public PoisonUIEffect playerPoison;
    private bool poisoned = false;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // 1: Floating Damage
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (floatingTextPrefab != null && damageSpawnPoint != null) {
                GameObject go = Instantiate(floatingTextPrefab, damageSpawnPoint.position, Quaternion.identity, damageSpawnPoint.parent);
                go.GetComponent<FloatingText>().Setup(Random.Range(5, 15), false);
            }
        }
        
        // 2: Pierce Damage
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (floatingTextPrefab != null && damageSpawnPoint != null) {
                GameObject go = Instantiate(floatingTextPrefab, damageSpawnPoint.position, Quaternion.identity, damageSpawnPoint.parent);
                go.GetComponent<FloatingText>().Setup(Random.Range(10, 25), true);
            }
        }

        // 3: Shake
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            if (CameraShake.Instance != null) {
                CameraShake.Instance.Shake(0.5f, 15f);
            }
        }

        // 4: Roll Dice
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            if (diceToRoll != null) {
                foreach(var d in diceToRoll) {
                    if (d != null) d.RollDice(new Color(Random.value, Random.value, Random.value));
                }
            }
        }

        // 5: Poison Toggle
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            poisoned = !poisoned;
            if (playerPoison != null) {
                playerPoison.SetPoisoned(poisoned);
            }
        }
    }
}
