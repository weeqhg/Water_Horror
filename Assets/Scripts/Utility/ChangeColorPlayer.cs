using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ChangeColorPlayer : NetworkBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] body;
    [SerializeField] private Material originalMaterial;
    private Material clonedMaterial;

    // Сетевая переменная для синхронизации цвета
    private NetworkVariable<Color> networkColor = new NetworkVariable<Color>(Color.white);

    public override void OnNetworkSpawn()
    {
        if (body == null || body.Length == 0)
        {
            Debug.LogError("SkinnedMeshRenderer не назначен!");
            return;
        }

        Material[] currentMaterialsRemote = body[0].materials;
        Material[] currentMaterialsLocal = body[1].materials;

        clonedMaterial = new(originalMaterial);

        // Применяем материал ко всем рендерерам
        foreach (var renderer in body)
        {
            if (renderer == null) continue;

            // Создаем новый массив материалов
            Material[] materials = renderer.materials;

            // Заменяем все материалы на клонированный (или можно заменить только определенный индекс)
            for (int i = 0; i < materials.Length; i++)
            {
                // Если нужно заменить только определенный (например, индекс 1)
                if (i == 1) materials[i] = clonedMaterial;
            }

            renderer.materials = materials;
        }

        // Подписываемся на изменение сетевой переменной
        networkColor.OnValueChanged += OnColorChanged;

        // Устанавливаем начальный цвет
        if (IsOwner && networkColor.Value != Color.white)
        {
            clonedMaterial.color = networkColor.Value;
        }
    }

    public void ChangeColorMaterial(Color color)
    {
        if (!IsOwner) return;

        // Меняем цвет локально
        clonedMaterial.color = color;

        // Синхронизируем с другими клиентами
        ChangeColorServerRpc(color);
    }

    [ServerRpc]
    private void ChangeColorServerRpc(Color newColor)
    {
        // Сервер обновляет NetworkVariable
        networkColor.Value = newColor;
    }

    private void OnColorChanged(Color oldColor, Color newColor)
    {
        // Этот метод вызывается на всех клиентах при изменении NetworkVariable
        clonedMaterial.color = newColor;
        //Debug.Log($"Цвет изменен на: {newColor} (синхронизировано)");
    }
    public override void OnNetworkDespawn()
    {
        // Отписываемся от события
        networkColor.OnValueChanged -= OnColorChanged;

        // Очищаем материалы
        if (clonedMaterial != null)
        {
            Destroy(clonedMaterial);
        }
    }
}
