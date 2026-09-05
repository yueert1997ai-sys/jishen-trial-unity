using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform panel;
    private Text header;
    private readonly List<GameObject> rowObjects = new List<GameObject>();
    private GameManager gameManager;
    private EquipmentShop shop;

    private void Update()
    {
        if (panel == null || !panel.gameObject.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Continue();
        }

        RefreshHeader();
    }

    public void Show(GameManager owner, EquipmentShop equipmentShop)
    {
        gameManager = owner;
        shop = equipmentShop;
        BuildUI();
        panel.gameObject.SetActive(true);
        RefreshRows();
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    private void BuildUI()
    {
        if (canvas != null)
        {
            return;
        }

        canvas = RuntimeUIFactory.CreateCanvas("ShopCanvas");
        panel = RuntimeUIFactory.CreatePanel(canvas.transform, "ShopPanel", new Vector2(0.14f, 0.11f), new Vector2(0.86f, 0.9f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.04f, 0.07f, 0.95f));
        header = RuntimeUIFactory.CreateText(panel, "Header", "", 34, TextAnchor.UpperCenter, Color.white);
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -24f);
        headerRect.sizeDelta = new Vector2(0f, 58f);

        Button continueButton = RuntimeUIFactory.CreateButton(panel, "ContinueButton", "Start Stage 2 (E)");
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.36f, 0.04f);
        continueRect.anchorMax = new Vector2(0.64f, 0.12f);
        continueRect.offsetMin = Vector2.zero;
        continueRect.offsetMax = Vector2.zero;
        continueButton.onClick.AddListener(Continue);
    }

    private void RefreshRows()
    {
        for (int i = 0; i < rowObjects.Count; i++)
        {
            if (rowObjects[i] != null)
            {
                Destroy(rowObjects[i]);
            }
        }

        rowObjects.Clear();
        RefreshHeader();
        if (shop == null)
        {
            return;
        }

        for (int i = 0; i < shop.shopItems.Count; i++)
        {
            EquipmentData data = shop.shopItems[i];
            RectTransform row = RuntimeUIFactory.CreatePanel(panel, "ShopRow" + i, new Vector2(0.08f, 0.73f - i * 0.11f), new Vector2(0.92f, 0.82f - i * 0.11f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.17f, 0.98f));
            rowObjects.Add(row.gameObject);

            string line = data.displayName + " - " + shop.GetActionLabel(data) + " - " + shop.GetPrice(data) + " coins\n" + data.description;
            Text text = RuntimeUIFactory.CreateText(row, "Text", line, 20, TextAnchor.MiddleLeft, Color.white);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.03f, 0f);
            textRect.anchorMax = new Vector2(0.72f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Button buyButton = RuntimeUIFactory.CreateButton(row, "BuyButton", shop.GetActionLabel(data));
            RectTransform buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(0.76f, 0.18f);
            buyRect.anchorMax = new Vector2(0.97f, 0.82f);
            buyRect.offsetMin = Vector2.zero;
            buyRect.offsetMax = Vector2.zero;
            EquipmentData capturedData = data;
            buyButton.onClick.AddListener(delegate
            {
                if (shop.TryBuyOrUpgrade(gameManager, capturedData))
                {
                    RefreshRows();
                }
            });
        }

        RectTransform repairRow = RuntimeUIFactory.CreatePanel(panel, "ShopRepairRow", new Vector2(0.08f, 0.73f - shop.shopItems.Count * 0.11f), new Vector2(0.92f, 0.82f - shop.shopItems.Count * 0.11f), Vector2.zero, Vector2.zero, new Color(0.08f, 0.12f, 0.17f, 0.98f));
        rowObjects.Add(repairRow.gameObject);
        Text repairText = RuntimeUIFactory.CreateText(repairRow, "Text", "Repair HP - " + shop.repairPrice + " coins\nRestore combat armor before Stage 2.", 20, TextAnchor.MiddleLeft, Color.white);
        RectTransform repairTextRect = repairText.GetComponent<RectTransform>();
        repairTextRect.anchorMin = new Vector2(0.03f, 0f);
        repairTextRect.anchorMax = new Vector2(0.72f, 1f);
        repairTextRect.offsetMin = Vector2.zero;
        repairTextRect.offsetMax = Vector2.zero;

        Button repairButton = RuntimeUIFactory.CreateButton(repairRow, "RepairButton", "Repair");
        RectTransform repairButtonRect = repairButton.GetComponent<RectTransform>();
        repairButtonRect.anchorMin = new Vector2(0.76f, 0.18f);
        repairButtonRect.anchorMax = new Vector2(0.97f, 0.82f);
        repairButtonRect.offsetMin = Vector2.zero;
        repairButtonRect.offsetMax = Vector2.zero;
        repairButton.onClick.AddListener(delegate
        {
            if (shop.TryRepair(gameManager))
            {
                RefreshRows();
            }
        });
    }

    private void RefreshHeader()
    {
        if (header != null && gameManager != null)
        {
            header.text = "Shop - Coins " + gameManager.Coins;
        }
    }

    private void Continue()
    {
        Hide();
        if (gameManager != null)
        {
            gameManager.ContinueFromShop();
        }
    }
}
