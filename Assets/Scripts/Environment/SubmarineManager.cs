using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class SubmarineMain : NetworkBehaviour
{
    [SerializeField] private Transform spawnPlayerPos;

    [SerializeField] private AudioSource fallingSource;
    [SerializeField] private AudioSource landingSource;

    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private Transform transformPos;


    public void StartDropSubmarine()
    {
        fallingSource.Play();
    }

    public void Fall(Vector3 pos)
    {
        transformPos.position = new Vector3(pos.x + 20f, 100f, pos.z);

        RaycastHit hit;
        if (Physics.Raycast(transformPos.position, Vector3.down, out hit, 100f, collisionLayer))
        {
            transform.position = new Vector3(
        pos.x + 20f,  // Сохраняем X
        hit.point.y + 5f, // Y точки попадания
        pos.z);
        }
    }

    public void EndDropSubmarine()
    {
        GlobalEventManager.TeleportPos?.Invoke(spawnPlayerPos.position);
        fallingSource.Stop();
        landingSource.Play();
    }
}
