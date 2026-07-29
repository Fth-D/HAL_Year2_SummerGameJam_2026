using UnityEngine;
using TMPro;

public class PlayerConfirmUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField]
    private Player player;

    [Header("显示文本")]
    [SerializeField]
    private TMP_Text startText;

    [SerializeField]
    private TMP_Text quitText;

    [Header("确认需要的时间")]
    [SerializeField]
    private float confirmTime = 3.0f;

    private float startTimer;
    private float quitTimer;

    // 以后真正开始游戏时可以读取这个
    public bool IsStartConfirmed =>
        startTimer >= confirmTime;

    // 以后真正退出游戏时可以读取这个
    public bool IsQuitConfirmed =>
        quitTimer >= confirmTime;

    private void Awake()
    {
        ClearStartText();
        ClearQuitText();
    }

    private void Update()
    {
        UpdateStartText();
        UpdateQuitText();
    }

    private void UpdateStartText()
    {
        if (player != null && player.IsStart)
        {
            startTimer += Time.unscaledDeltaTime;

            float remainingTime =
     Mathf.Max(
         confirmTime - startTimer,
         0.0f
     );

            startText.enabled = true;

            startText.text =
                 $"start?\n{remainingTime:F1}";
        }
        else
        {
            ClearStartText();
        }
    }

    private void UpdateQuitText()
    {
        if (player != null && player.IsQuit)
        {
            quitTimer += Time.unscaledDeltaTime;

            float remainingTime =
    Mathf.Max(
        confirmTime - quitTimer,
        0.0f
    );

            startText.enabled = true;

            startText.text =
                 $"Quit?\n{remainingTime:F1}";
        }
        else
        {
            ClearQuitText();
        }
    }

    private void ClearStartText()
    {
        startTimer = 0.0f;

        if (startText != null)
        {
            startText.text = "";
            startText.enabled = false;
        }
    }

    private void ClearQuitText()
    {
        quitTimer = 0.0f;

        if (quitText != null)
        {
            quitText.text = "";
            quitText.enabled = false;
        }
    }
}