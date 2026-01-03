// CardConfirmationDialog.csの修正

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// カード使用確認ダイアログ - 前面表示と他の操作無効化に対応
/// </summary>
public class CardConfirmationDialog : MonoBehaviour
{
    [Header("UI要素")]
    public GameObject dialogPanel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;
    public Transform cardDisplayPosition;
    public GameObject cardPrefab;
    
    private Card currentCard;
    private Action onConfirm;
    private GameObject displayedCard;
    
    // 静的フラグ：ダイアログが表示中かどうか
    public static bool IsDialogActive { get; private set; } = false;
    
    private void Awake()
    {
        Debug.Log("[CardConfirmationDialog] Awake呼び出し");
        
        // ダイアログを確実に最前面に表示するための設定
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            // キャンバスがない場合は追加
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            
            // GraphicRaycasterも確実に追加
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            Debug.Log("[CardConfirmationDialog] キャンバスとレイキャスターを追加しました");
        }
        else
        {
            // 既存のキャンバス設定を更新
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            
            // レイキャスター確認
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[CardConfirmationDialog] レイキャスターを追加しました");
            }
        }
        
        // ダイアログの子オブジェクトのキャンバスを無効化（必要な場合）
        if (dialogPanel != null)
        {
            Canvas panelCanvas = dialogPanel.GetComponent<Canvas>();
            if (panelCanvas != null)
            {
                // 親キャンバスのみ使用するため子キャンバスは無効化
                panelCanvas.enabled = false;
                Debug.Log("[CardConfirmationDialog] 子キャンバスを無効化しました");
            }
        }
    }
    
    private void Start()
    {
        Debug.Log("[CardConfirmationDialog] Start呼び出し");
        
        // 初期状態は非表示
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
            IsDialogActive = false;
        }
        
        // ボタンイベントを設定
        SetupButtons();
    }
    
    /// <summary>
    /// ボタンイベントを設定 - 初期化専用メソッド
    /// </summary>
    private void SetupButtons()
    {
        if (confirmButton != null)
        {
            // 既存のリスナーをクリア
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClicked);
            Debug.Log("[CardConfirmationDialog] 確認ボタンのリスナーを設定しました");
        }
        else
        {
            Debug.LogError("[CardConfirmationDialog] 確認ボタンがnullです");
        }
        
        if (cancelButton != null)
        {
            // 既存のリスナーをクリア
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClicked);
            Debug.Log("[CardConfirmationDialog] キャンセルボタンのリスナーを設定しました");
        }
        else
        {
            Debug.LogError("[CardConfirmationDialog] キャンセルボタンがnullです");
        }
    }
    
    /// <summary>
    /// ダイアログを表示する
    /// </summary>
    public void ShowDialog(Card card, string message, Action confirmAction)
    {
        Debug.Log($"[CardConfirmationDialog] ShowDialog呼び出し: カード={card?.cardName}, メッセージ={message}");
        
        // 参照を保存
        currentCard = card;
        onConfirm = confirmAction;
        
        // ボタンイベントを確認・再設定
        SetupButtons();
        
        // タイトルとメッセージを設定
        if (titleText != null)
        {
            titleText.text = $"{card.cardName}を使用しますか？";
        }
        
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        // カードを表示
        DisplayCard(card);
        
        // ダイアログを表示
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
            IsDialogActive = true; // 静的フラグをセット
        }
        
        // 確実に前面に表示するため、親オブジェクトを最後の子として設定
        Transform parent = transform.parent;
        if (parent != null)
        {
            transform.SetAsLastSibling();
        }
    }
    
    /// <summary>
    /// カードを表示する
    /// </summary>
    private void DisplayCard(Card card)
    {
        // 既存のカードをクリア
        if (displayedCard != null)
        {
            Destroy(displayedCard);
            displayedCard = null;
        }
        
        // カードプレハブがない場合は何もしない
        if (cardPrefab == null || cardDisplayPosition == null) 
        {
            Debug.LogError("[CardConfirmationDialog] カードプレハブまたは表示位置がnullです");
            return;
        }
        
        // カードをインスタンス化
        displayedCard = Instantiate(cardPrefab, cardDisplayPosition);
        
        // カード情報を設定
        CardUI cardUI = displayedCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetupCard(card);
            
            // カードのアートワークを設定
            if (card != null && !string.IsNullOrEmpty(card.artwork))
            {
                // UIManagerからカードスプライトをロードするメソッドを参照
                UIManager uiManager = FindFirstObjectByType<UIManager>(); 
                if (uiManager != null)
                {
                    // 検索する候補パスのリスト
                    string[] pathCandidates = new string[]
                    {
                        $"CardImages/Characters/{card.artwork}",
                        $"CardImages/Spells/{card.artwork}",
                        $"CardImages/Fields/{card.artwork}",
                    };
                    
                    // 各候補パスを試す
                    foreach (string path in pathCandidates)
                    {
                        Sprite cardSprite = Resources.Load<Sprite>(path);
                        if (cardSprite != null)
                        {
                            cardUI.SetCardArtwork(cardSprite);
                            break;
                        }
                    }
                }
            }
            
            // カードを少し大きく表示
            RectTransform rectTransform = displayedCard.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * 1.2f;
            }
        }
            
        // ボタンを無効化（クリックできないように）
        Button button = displayedCard.GetComponent<Button>();
        if (button != null)
        {
            button.enabled = false;
        }
    }
    
    /// <summary>
    /// 確認ボタンがクリックされた時の処理
    /// </summary>
    private void OnConfirmClicked()
    {
        Debug.Log("[CardConfirmationDialog] 確認ボタンがクリックされました");
        
        // アクションを保存（HideDialogでnullになるため）
        Action confirmAction = onConfirm;
        
        // ダイアログを閉じる
        HideDialog();
        
        // 確認アクションを実行
        if (confirmAction != null)
        {
            Debug.Log("[CardConfirmationDialog] 確認アクションを実行します");
            confirmAction.Invoke();
        }
        else
        {
            Debug.LogError("[CardConfirmationDialog] 確認アクションがnullです");
        }
    }
    
    /// <summary>
    /// キャンセルボタンがクリックされた時の処理
    /// </summary>
    private void OnCancelClicked()
    {
        Debug.Log("[CardConfirmationDialog] キャンセルボタンがクリックされました");
        
        // ダイアログを閉じる
        HideDialog();
    }
    
    /// <summary>
    /// ダイアログを閉じる
    /// </summary>
    public void HideDialog()
    {
        Debug.Log("[CardConfirmationDialog] ダイアログを閉じます");
        
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
            IsDialogActive = false; // 静的フラグをリセット
        }
        
        // 表示中のカードをクリア
        if (displayedCard != null)
        {
            Destroy(displayedCard);
            displayedCard = null;
        }
        
        // 参照をクリア
        currentCard = null;
        onConfirm = null;
    }
}