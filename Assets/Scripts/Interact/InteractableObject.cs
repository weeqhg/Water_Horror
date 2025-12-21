using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

/// <summary>
/// На всех интерактивных объектах должен находиться этот скрипт
/// </summary>

public class InteractableObject : MonoBehaviour
{
    [SerializeField] private LocalizedString nameObject;
    [SerializeField] private InteractionSettings interactionSettings;
    [SerializeField] private GameObject player;
    private Item item;  


    [Header("Interaction Events")]
    public UnityEvent OnInteract;

    private Renderer objectRenderer;
    private bool isHighlighted = false;

    public LocalizedString GetName() => nameObject;

    public InteractionSettings GetSetting() => interactionSettings;

    public GameObject GetPlayer() => player;

    public float GetPrice()
    {
        if (item != null) return item.Price;
        else return 0;
    }
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        item = GetComponentInParent<Item>();
        objectRenderer.enabled = false;
    }
  
    public void Interact(GameObject player)
    {
        if (isHighlighted)
        {
            this.player = player;
            OnInteract?.Invoke();
        }
    }



    public void HighlightObject()
    {
        if (isHighlighted || objectRenderer == null) return;

        isHighlighted = true;

        objectRenderer.enabled = true;
    }

    public void DeselectObject()
    {
        if (!isHighlighted || objectRenderer == null) return;

        isHighlighted = false;

        objectRenderer.enabled = false;
    }
}