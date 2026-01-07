using UnityEngine;

public class ControlsPanelToggle : MonoBehaviour
{
    void Update()
    {
        // °´Esc¼üÊ±£¬Òþ²ØÃæ°å
        if (Input.GetKeyDown(KeyCode.Escape) && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}