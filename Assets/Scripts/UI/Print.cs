using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Print : MonoBehaviour
{
    [SerializeField] private float delayPrint = 0f;
    [SerializeField] private float durationPrint = 0.5f;
    [SerializeField] private TextMeshProUGUI textMesh;
    [SerializeField] private AudioSource sound;

    private void Start()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshProUGUI>();
    }

    [ContextMenu("Запустить анимацию")]
    public void StartPrint()
    {
        string newText = textMesh.text;
        textMesh.text = null;
        StartCoroutine(Printing(newText));
    }

    private IEnumerator Printing(string text)
    {
        int count = text.Length;
        yield return new WaitForSeconds(delayPrint);

        if (sound != null) sound.Play();
        for (int i = 0; i < count; i++)
        {
            char letter = text[i];
            textMesh.text += letter;
            yield return new WaitForSeconds(durationPrint);
        }
        if (sound != null) sound.Stop();
    }
}
