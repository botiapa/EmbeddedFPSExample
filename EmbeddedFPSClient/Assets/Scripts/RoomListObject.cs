﻿using UnityEngine;
using UnityEngine.UI;

public class RoomListObject : MonoBehaviour
{

    [Header("References")]
    public Text NameText;
    public Text SlotText;
    public Button JoinButton;

    public void Set(RoomData data)
    {
        NameText.text = data.Name;
        SlotText.text = data.Slots + "/" + data.MaxSlots;
        JoinButton.onClick.RemoveAllListeners();
        JoinButton.onClick.AddListener(delegate { LobbyManager.Instance.SendJoinRoomRequest(data.Name); });
    }
}

