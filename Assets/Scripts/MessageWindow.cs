using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(RectXformMover))]
public class MessageWindow : MonoBehaviour
{
    public Image messageIcon;
    public TMP_Text messageText;
    public TMP_Text buttonText;

    public void ShowMessage(Sprite sprite = null, string message = "", string buttonMsg = "start")
    {
        if (messageIcon != null)
            messageIcon.sprite = sprite;
        if (messageText != null)
            messageText.text = message;
        if (buttonText != null)
            buttonText.text = buttonMsg;
    }
}
