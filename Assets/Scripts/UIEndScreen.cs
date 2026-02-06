using UnityEngine;
using UnityEngine.UI;

public class UIEndScreen : MonoBehaviour
{
    public static UIEndScreen Instance { get; private set; }

    public GameObject panel;
    public Text messageText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    public static void ShowFailure(string message)
    {
        if (Instance == null) { Debug.LogWarning("UIEndScreen not present."); return; }
        Instance.Show(message);
    }

    public static void ShowSuccess(string message)
    {
        if (Instance == null) { Debug.LogWarning("UIEndScreen not present."); return; }
        Instance.Show(message);
    }

    void Show(string msg)
    {
        if (panel != null) panel.SetActive(true);
        if (messageText != null) messageText.text = msg;
    }
}
