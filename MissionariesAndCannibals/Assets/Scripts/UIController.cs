using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private GameObject _parametersPanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _parametersPanel.SetActive(!_parametersPanel.activeInHierarchy);
        }
    }
}
