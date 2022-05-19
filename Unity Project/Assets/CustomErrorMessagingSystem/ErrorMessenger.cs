using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorMessenger : MonoBehaviour
{
    [SerializeField] private GameObject WarningUI;
    [SerializeField] private Text WarningTitleUI;
    [SerializeField] private Text WarningDescriptionUI;

    [SerializeField] private GameObject ErrorUI;
    [SerializeField] private Text ErrorTitleUI;
    [SerializeField] private Text ErrorDescriptionUI;


    public void DisplayWarning(string title, string description)
    {
        WarningTitleUI.text = "Warning: " + title;
        WarningDescriptionUI.text = description;
        WarningUI.SetActive(true);
    }

    public void DisplayError(string title, string description)
    {
        ErrorTitleUI.text = "Error: " + title;
        ErrorDescriptionUI.text = description;
        ErrorUI.SetActive(true);
    }
}