using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    private void Start()
    {

        DontDestroyOnLoad(gameObject);

    }
}