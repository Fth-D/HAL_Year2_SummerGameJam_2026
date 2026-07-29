using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("开始后进入的场景")]
    [SerializeField]
    private string startSceneName = "GameScene";

    private float startTimer;
    private float quitTimer;

    // 防止执行多次
    private bool actionTriggered;

    public bool IsStartConfirmed =>
        startTimer >= confirmTime;

    public bool IsQuitConfirmed =>
        quitTimer >= confirmTime;

    private void Awake()
    {
        actionTriggered = false;

        ClearStartText();
        ClearQuitText();
    }

    private void Update()
    {
        // 已经执行开始或退出后，不再继续计时
        if (actionTriggered)
        {
            return;
        }

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

            if (startText != null)
            {
                startText.enabled = true;

                startText.text =
                    $"start?\n{remainingTime:F1}";
            }

            // 时间到了，开始游戏
            if (startTimer >= confirmTime)
            {
                StartGame();
            }
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

            if (quitText != null)
            {
                quitText.enabled = true;

                quitText.text =
                    $"quit?\n{remainingTime:F1}";
            }

            // 时间到了，退出游戏
            if (quitTimer >= confirmTime)
            {
                QuitGame();
            }
        }
        else
        {
            ClearQuitText();
        }
    }

    private void StartGame()
    {
        if (actionTriggered)
        {
            return;
        }

        actionTriggered = true;

        // 防止ESC菜单之前暂停了游戏
        Time.timeScale = 1.0f;

        SceneManager.LoadScene(startSceneName);
    }

    private void QuitGame()
    {
        if (actionTriggered)
        {
            return;
        }

        actionTriggered = true;

#if UNITY_EDITOR
        // 在Unity编辑器中测试时，停止Play模式
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // 打包后的游戏中，真正退出程序
        Application.Quit();
#endif
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