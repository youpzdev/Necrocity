using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ButtonStyler : MonoBehaviour
{
    [SerializeField] private GameObject idleObject;
    [SerializeField] private GameObject triggeredObject;
    [Space(5)]
    [SerializeField] private Color idleColor;
    [SerializeField] private Color triggeredColor;
    [Space(5)]
    [SerializeField] private TMP_Text titleText;

    public void SwitchState(bool state)
    {
        idleObject.SetActive(!state);
        triggeredObject.SetActive(state);

        titleText.color = state ? triggeredColor : idleColor;
    }
}
