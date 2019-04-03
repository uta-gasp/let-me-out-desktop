using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerAvatar player = other.GetComponent<PlayerAvatar>();
        if (player)
        {
            player.hitsDoor(name);
        }
    }
}
