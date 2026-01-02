using DG.Tweening;
using UnityEngine;

public class BorderWorld : MonoBehaviour
{
    [SerializeField] private GameObject borderObject;
    private float initialHeight;
    private float initialWidth;
    
    private Tween shrinkTween;
    
    private void Start()
    {
        initialHeight = 128;
        initialWidth = 128;

        borderObject.transform.localScale = new Vector3(initialWidth, initialHeight, initialWidth);
    }

    public void ApplyWorldSetting(WorldScriptableObject world)
    {
        initialHeight = world.height;
        initialWidth = world.width;

        borderObject.transform.localScale = new Vector3(initialWidth*2, initialHeight, initialWidth*2);
    }
}