using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手札表示コントローラ - 相手の手札表示に対応
/// </summary>
public class HandDisplayController : MonoBehaviour
{
    [Header("手札表示設定")]
    public Transform handCardContainer;     // 手札カードを配置する親オブジェクト
    public GameObject cardPrefab;           // カードプレハブ
    public bool isOpponentHand = false;     // 相手の手札かどうか
    
    [Header("レイアウト設定")]
    public float cardSpacing = 0.5f;         // カード間の間隔
    public int maxCardsInRow = 7;           // 1行に表示する最大カード数
    public float scaleMultiplier = 0.8f;    // 表示スケール
    
    private List<GameObject> displayedCards = new List<GameObject>();  // 表示中のカードオブジェクト
    private Player playerReference;         // 関連付けられたプレイヤー
    
    // キャッシュ用の参照
    private UIManager uiManager;

    private void Awake()
    {
        // UIManagerの参照を事前にキャッシュ
        uiManager = FindFirstObjectByType<UIManager>();
    }
    
    /// <summary>
    /// プレイヤー参照を設定
    /// </summary>
    public void SetPlayerReference(Player player)
    {
        playerReference = player;
        Debug.Log($"[HandDisplayController] プレイヤー参照を設定: {(player != null ? player.playerName : "null")}");
    }
    
    /// <summary>
    /// 手札を更新 - パフォーマンス改善
    /// </summary>
    public void UpdateHandDisplay()
    {
        if (playerReference == null || handCardContainer == null)
        {
            Debug.LogError("HandDisplayController: プレイヤー参照またはコンテナがnullです");
            return;
        }

        Debug.Log($"[HandDisplayController] 手札更新開始: {(isOpponentHand ? "相手" : "自分")}の手札, 枚数: {playerReference.hand.Count}");
        
        // 既存のカードをクリア
        ClearDisplayedCards();
        
        // 早期リターン - 手札が空の場合
        if (playerReference.hand.Count == 0) return;
        
        // プレイヤーの手札を表示
        List<Card> handCards = playerReference.hand;
        
        for (int i = 0; i < handCards.Count; i++)
        {
            Card card = handCards[i];
            if (card == null)
            {
                Debug.LogWarning($"手札の{i}番目のカードがnullです");
                continue;
            }
            
            // カードプレハブをインスタンス化
            GameObject cardObj = Instantiate(cardPrefab, handCardContainer);
            displayedCards.Add(cardObj);
            
            // カードUIを設定
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                if (isOpponentHand)
                {
                    // 相手の手札は裏面表示
                    bool isFieldCard = card is FieldCard;
                    cardUI.SetupAsOpponentHandCard(isFieldCard);
                    
                    // ボタンの設定
                    Button cardButton = cardObj.GetComponent<Button>();
                    if (cardButton != null)
                    {
                        cardButton.onClick.AddListener(OnOpponentCardClicked);
                    }
                }
                else
                {
                    // 実際のカードデータを使用して設定
                    cardUI.SetupCard(card);
                    
                    // カードのアートワークを設定 - アートワークが指定されている場合のみ処理
                    if (!string.IsNullOrEmpty(card.artwork))
                    {
                        Sprite cardSprite = LoadCardSprite(card);
                        if (cardSprite != null)
                        {
                            cardUI.SetCardArtwork(cardSprite);
                        }
                    }
                    
                    // ボタンの設定 - キャプチャ変数を使用
                    Button cardButton = cardObj.GetComponent<Button>();
                    if (cardButton != null)
                    {
                        Card capturedCard = card; // ラムダ式用にローカル変数に保存
                        cardButton.onClick.AddListener(() => OnPlayerCardClicked(capturedCard));
                    }
                }
            }
        }
        
        Debug.Log($"[HandDisplayController] 手札更新完了: {displayedCards.Count}枚のカードを配置");
        
        // 手札のレイアウトを調整
        ArrangeHandCards();
    }
    
    /// <summary>
    /// 手札カードのレイアウト調整 - 表示位置改善
    /// </summary>
    private void ArrangeHandCards()
    {
        if (displayedCards.Count == 0) return;
        
        int cardCount = displayedCards.Count;
        
        // 手札コンテナのサイズを取得
        RectTransform containerRect = handCardContainer as RectTransform;
        if (containerRect == null) return;
        
        // Image 3のスクリーンショットを参考にした値
        // Player1Handは位置Y:-900, サイズ:850x140
        float containerY = containerRect.anchoredPosition.y;
        
        // カードの基本サイズを取得
        RectTransform firstCardRect = displayedCards[0].GetComponent<RectTransform>();
        if (firstCardRect == null) return;
        
        float cardWidth = firstCardRect.rect.width;
        float cardHeight = firstCardRect.rect.height;
        
        // カードのスケールを調整 - 手札エリアに収まるよう小さく
        const float targetScale = 0.6f;
        
        // カード間隔の計算 - 枚数に応じて重ねる量を調整
        float cardSpacing = -20f; // カードを重ねる
        
        // カードが多いほど重ねる量を増やす
        if (cardCount > 5) {
            cardSpacing = -25f;
        }
        
        // 手札全体の幅
        float totalWidth = (cardWidth * targetScale * cardCount) + (cardSpacing * (cardCount - 1));
        float startX = -totalWidth / 2 + (cardWidth * targetScale / 2);
        
        // Y位置を調整 - 手札エリア内に収める
        float yPos = 0f;
        
        // 自分の手札は画面下部、相手の手札は画面上部
        if (isOpponentHand)
        {
            // 相手の手札 (画面上部)
            yPos = 70f; // コンテナ内の相対位置
        }
        else
        {
            // 自分の手札 (画面下部) - カードが見切れないように上に配置
            yPos = 70f; // コンテナ内の相対位置
        }
        
        Debug.Log($"[HandDisplayController] 手札配置: 枚数={cardCount}, スケール={targetScale}, 間隔={cardSpacing}, Y位置={yPos}, コンテナY={containerY}");
        
        // カードを配置
        for (int i = 0; i < cardCount; i++)
        {
            GameObject card = displayedCards[i];
            RectTransform rectTransform = card.GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                // X座標計算
                float xPos = startX + i * (cardWidth * targetScale + cardSpacing);
                
                // 座標を設定
                rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                
                // スケールを固定で設定
                rectTransform.localScale = Vector3.one * targetScale;
                
                // カードUIの参照を取得
                CardUI cardUI = card.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    // インタラクティブ状態を設定
                    cardUI.SetInteractable(true);
                }
            }
        }
    }
    
    /// <summary>
    /// 表示中のカードをクリア - 最適化
    /// </summary>
    private void ClearDisplayedCards()
    {
        for (int i = displayedCards.Count - 1; i >= 0; i--)
        {
            if (displayedCards[i] != null)
            {
                Destroy(displayedCards[i]);
            }
        }
        
        displayedCards.Clear();
    }
    
    /// <summary>
    /// 自分のカードがクリックされたときの処理
    /// </summary>
    private void OnPlayerCardClicked(Card card)
    {
        // UIManagerが未取得の場合は初期化
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
        
        // UIManagerの手札カードクリックメソッドを呼び出し
        uiManager?.OnPlayerHandCardClicked(card);
    }
    
    /// <summary>
    /// 相手のカードがクリックされたときの処理
    /// </summary>
    private void OnOpponentCardClicked()
    {
        // UIManagerが未取得の場合は初期化
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
        
        // 相手の手札は詳細表示できないため、通知を表示するだけ
        uiManager?.ShowNotification("相手の手札は確認できません", 1.5f);
    }

    /// <summary>
    /// カードスプライトをロードするメソッド改善 - キャッシュ機能追加
    /// </summary>
    private Sprite LoadCardSprite(Card card)
    {
        if (card == null || string.IsNullOrEmpty(card.artwork)) return null;
        
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
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }
        
        // 見つからなかった場合はnull
        Debug.LogWarning($"カード {card.cardName}（ID: {card.id}）の画像 {card.artwork} が見つかりませんでした");
        Debug.LogWarning($"試行したパス: {string.Join(", ", pathCandidates)}");
        return null;
    }
}

/// <summary>
/// UIManagerの手札関連拡張機能 - 部分クラスを使用
/// </summary>
public partial class UIManager : MonoBehaviour
{
    [Header("手札表示")]
    public HandDisplayController player1HandDisplay;
    public HandDisplayController player2HandDisplay;
    
    /// <summary>
    /// 手札の更新
    /// </summary>
    public void UpdateHand(Player player)
    {
        if (player == null)
        {
            Debug.LogError("UpdateHand: プレイヤーがnullです");
            return;
        }
        
        // プレイヤーに対応する手札表示コントローラを取得
        HandDisplayController handDisplay = GetHandDisplayForPlayer(player);
        
        if (handDisplay != null)
        {
            // プレイヤー参照を設定（まだ設定されていない場合）
            handDisplay.SetPlayerReference(player);
            
            // 手札表示を更新
            handDisplay.UpdateHandDisplay();
        }
        else
        {
            Debug.LogError($"プレイヤー {player.playerName} に対応する手札表示コントローラがありません");
        }
    }
    
    /// <summary>
    /// プレイヤーに対応する手札表示コントローラを取得
    /// </summary>
    private HandDisplayController GetHandDisplayForPlayer(Player player)
    {
        if (player == GameManager.Instance.player1)
        {
            return player1HandDisplay;
        }
        else if (player == GameManager.Instance.player2)
        {
            return player2HandDisplay;
        }
        
        return null;
    }
        
    /// <summary>
    /// UI全体を更新するメソッド
    /// </summary>
    public void UpdateUI()
    {   
        // GameManagerの参照を取得
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null) return;
        
        // プレイヤー1と2の手札を更新
        if (gameManager.player1 != null)
        {
            UpdateHand(gameManager.player1);
        }
        
        if (gameManager.player2 != null)
        {
            UpdateHand(gameManager.player2);
        }
        
        // その他のUI更新も追加可能
    }
}