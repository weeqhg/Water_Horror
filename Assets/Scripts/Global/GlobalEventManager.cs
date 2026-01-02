using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SimpleVoiceChat;

public static class GlobalEventManager
{
    public static readonly UnityEvent<ulong> LoadGame = new();

    //Вызывается с консоли в подводной лодки
    public static readonly UnityEvent<int> StartGame = new();
    //Пока что не используется
    public static readonly UnityEvent FinishedGame = new();

    public static readonly UnityEvent<Vector3> TeleportPos = new();

    public static readonly UnityEvent<ulong> DeathPlayer = new();

    public static readonly UnityEvent RebornPlayer = new();
    /// <summary>
    /// При использование объектов с вызывающимся Canvas
    /// Locker
    /// Computer
    /// Когда игрок нажимает ESC
    /// И когда мы нажимаем крести в Canvas интерфейсе
    /// </summary>
    
    public static readonly UnityEvent KeyCancel = new();
    public static readonly UnityEvent BlockMove = new();
    public static readonly UnityEvent UnBlockMove = new();
    

}
