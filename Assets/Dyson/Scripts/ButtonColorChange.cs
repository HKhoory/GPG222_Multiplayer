using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonColorChange : MonoBehaviour
{
    public Button button;

    private bool isButtonPressed = false;
    
    // Start is called before the first frame update
    void Start()
    {
        button.onClick.AddListener(ChangeColor);
    }

    void ChangeColor()
    {
        Color newColor = isButtonPressed ? Color.red : Color.green;
        button.GetComponent<Image>().color = newColor;
        isButtonPressed = false;
    }
}
