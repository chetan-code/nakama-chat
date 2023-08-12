using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UserInfoItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI userIdText;

    public void Init(string name, string id) {
        usernameText.text = name;
        userIdText.text = id;   
    }
}
