using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
/// <summary>
/// Просто EventManager в который можно через инспектор добавить события
/// и вызывать в нужном месте в коде, например нажатия кнопок
/// </summary>
public class EventManager : MonoBehaviour
{
    public UnityEvent OnStartEventManager;
    public void StartEventManager()
    {
        OnStartEventManager?.Invoke();
    }
}
