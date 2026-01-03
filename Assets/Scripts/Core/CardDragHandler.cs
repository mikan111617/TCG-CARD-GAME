using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// カードのドラッグ＆ドロップ処理を行うコンポーネント
/// </summary>
public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("ドラッグ設定")]
    public float dragScale = 1.2f;         // ドラッグ中のカードスケール
    public float dragAlpha = 0.8f;         // ドラッグ中の透明度
    
    // 参照
    private RectTransform rectTransform;    // このオブジェクトのRectTransform
    private CanvasGroup canvasGroup;        // このオブジェクトのCanvasGroup
    private Canvas canvas;                  // 親Canvas
    private Vector3 originalScale;          // 元のスケール
    private Vector2 originalPosition;       // 元の位置
    private Transform originalParent;       // 元の親オブジェクト
    
    // ドラッグ状態
    private bool isDragging = false;
    
    // カードUI
    private CardUI cardUI;
    
    // デッキエディタマネージャー
    private ImprovedDeckEditorManager deckEditorManager;
    
    private void Awake()
    {
        // コンポーネントの取得
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardUI = GetComponent<CardUI>();
        
        // CanvasGroupがなければ追加
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        // 親Canvasを検索
        Transform parent = transform.parent;
        while (parent != null)
        {
            canvas = parent.GetComponent<Canvas>();
            if (canvas != null) break;
            parent = parent.parent;
        }
        
        // デッキエディタマネージャーを取得
        deckEditorManager = UnityEngine.Object.FindFirstObjectByType<ImprovedDeckEditorManager>();
    }
    
    private void Start()
    {
        // 元のスケールを保存
        originalScale = rectTransform.localScale;
    }
    
    /// <summary>
    /// ドラッグ開始時の処理
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // カードが操作不可の場合はドラッグ禁止
        if (cardUI != null && !cardUI.IsInteractable())
            return;
            
        isDragging = true;
        
        // 元の位置と親を保存
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        // ドラッグ中のカードをキャンバス直下に移動して最前面に表示
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        
        // ドラッグ中の見た目設定
        rectTransform.localScale = originalScale * dragScale;
        canvasGroup.alpha = dragAlpha;
        canvasGroup.blocksRaycasts = false;
    }
    
    /// <summary>
    /// ドラッグ中の処理
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // マウス位置にカードを移動
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        
        rectTransform.position = canvas.transform.TransformPoint(localPoint);
    }
    
    /// <summary>
    /// ドラッグ終了時の処理
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // レイキャストでドロップ先を取得
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        bool dropped = false;
        
        foreach (RaycastResult result in results)
        {
            // ドロップ可能なエリアかチェック
            CardDropArea dropArea = result.gameObject.GetComponent<CardDropArea>();
            if (dropArea != null)
            {
                // カードをドロップ
                dropped = HandleCardDrop(dropArea);
                if (dropped) break;
            }
        }
        
        // ドロップに失敗した場合は元の場所に戻す
        if (!dropped)
        {
            ReturnToOriginalPosition();
        }
        
        // 元の見た目に戻す
        rectTransform.localScale = originalScale;
        canvasGroup.alpha = 1.0f;
        canvasGroup.blocksRaycasts = true;
    }
    
    /// <summary>
    /// カードをドロップエリアに追加
    /// </summary>
    private bool HandleCardDrop(CardDropArea dropArea)
    {
        // カードUIと管理クラスの参照を確認
        if (cardUI == null || deckEditorManager == null) return false;
        
        Card card = cardUI.GetCard();
        if (card == null) return false;
        
        // ドロップエリアの種類によって処理を分ける
        if (dropArea.areaType == CardDropArea.AreaType.DeckGrid)
        {
            // コレクションからデッキへドラッグ
            if (transform.parent == originalParent && originalParent.name.Contains("Collection"))
            {
                return deckEditorManager.AddCardToCurrentDeck(card);
            }
        }
        else if (dropArea.areaType == CardDropArea.AreaType.CollectionGrid)
        {
            // デッキからコレクションへドラッグ
            if (transform.parent == originalParent && originalParent.name.Contains("Deck"))
            {
                return deckEditorManager.RemoveCardFromCurrentDeck(card);
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// カードを元の位置に戻す
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        rectTransform.anchoredPosition = originalPosition;
    }
}

/// <summary>
/// カードをドロップできるエリアを定義するコンポーネント
/// </summary>
public class CardDropArea : MonoBehaviour
{
    // ドロップエリアの種類
    public enum AreaType
    {
        CollectionGrid,  // カードコレクションエリア
        DeckGrid         // デッキエリア
    }
    
    public AreaType areaType;
}