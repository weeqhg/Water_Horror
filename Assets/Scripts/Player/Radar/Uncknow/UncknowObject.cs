using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UncknowObject : MonoBehaviour
{
    private string originalTag;

    private void Start()
    {
        originalTag = gameObject.tag;
        if (Random.Range(0f, 1f) > 0.8f)
        {
            gameObject.tag = "Uncknow";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Radar"))
        {
            if (gameObject.tag != originalTag)
                gameObject.tag = originalTag;
        }
    }
}
