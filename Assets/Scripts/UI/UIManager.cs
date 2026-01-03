using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // 追加：Action, Exceptionのために必要
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームUIを管理するクラス - リファクタリング済み
/// </summary>
public partial class UIManager : MonoBehaviour
{
    [Header("プレイヤー情報UI")]
    public TextMeshProUGUI player1NameText;
    public TextMeshProUGUI player1LifeText;
    public TextMeshProUGUI player1EnergyText;
    public TextMeshProUGUI player1DeckCountText;
    
    public TextMeshProUGUI player2NameText;
    public TextMeshProUGUI player2LifeText;
    public TextMeshProUGUI player2EnergyText;
    public TextMeshProUGUI player2DeckCountText;
    
    
    [Header("フィールドUI")]
    public Transform player1CharacterFieldParent;
    public Transform player2CharacterFieldParent;

    public Transform player1FieldCardParent; // フィールドカード用の親オブジェクト
    public Transform player2FieldCardParent; // フィールドカード用の親オブジェクト
    
    [Header("手札UI")]
    public Transform player1HandParent;
    public Transform player2HandParent;
    
    [Header("ゲーム情報UI")]
    public TextMeshProUGUI currentPlayerText;
    public TextMeshProUGUI phaseText;
    public Button endTurnButton;
    
    [Header("カード詳細表示")]
    public GameObject cardDetailPanel;             // カード詳細パネル
    public Transform cardDetailContainer;          // カード表示用コンテナ
    public float cardDetailScale = 2.0f;           // カード表示スケール
    private GameObject currentDetailCard;          // 現在表示中のカード
    
    [Header("プレハブ")]
    public GameObject cardPrefab;
    public GameObject characterSlotPrefab;

    [Header("スペル発動確認UI")]
    public GameObject spellActivationPanel;       // スペル発動確認パネル
    public TextMeshProUGUI eventDescriptionText;  // イベント説明テキスト
    public Transform spellCardContainer;          // スペルカード表示コンテナ
    public Button cancelButton;                   // キャンセルボタン
    public float cardDisplayScale = 0.8f;         // カード表示スケール

    // スペル発動情報
    private Player activatingPlayer;
    private List<SpellCard> activatableSpells;
    private GameEventInfo triggeringEvent;

    [Header("通知UI")]
    public GameObject notificationPanel;          // 通知パネル
    public TextMeshProUGUI notificationText;      // 通知テキスト
    public float notificationDuration = 2.0f;     // 通知表示時間

    public GameObject notificationBlocker;     // 通知中に他の操作を防ぐための透明オーバーレイ
    private bool isNotificationActive = false; // 通知表示中フラグ
    private Queue<NotificationInfo> notificationQueue = new Queue<NotificationInfo>(); // 通知キュー

    [Header("墓地UI")]
    public TextMeshProUGUI player1GraveyardText;
    public TextMeshProUGUI player2GraveyardText;
    public Image player1GraveyardImage;
    public Image player2GraveyardImage;

    [Header("確認ダイアログ")]
    public CardConfirmationDialog cardConfirmationDialog; // インスペクターから設定可能


    [Header("勝敗画面")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;
    public Button returnToMenuButton;

    [Header("手札上限ダイアログ")]
    public GameObject handLimitPanel;           // 手札上限パネル
    public TextMeshProUGUI handLimitText;       // 説明テキスト
    public Transform handLimitCardContainer;    // カード表示コンテナ
    public Button handLimitCloseButton;         // 閉じるボタン
    public Button handLimitConfirmButton;       // 確認ボタン

    private Player handLimitPlayer;            // 対象プレイヤー
    private Card handLimitNewCard;             // 新しく引いたカード
    private Card selectedCardToDiscard;        // 捨てるために選択されたカード
    private List<GameObject> displayedDiscardCards = new List<GameObject>(); // 表示されたカードオブジェクト

    // キャッシュ用参照
    private GameManager gameManager;
    private TurnManager turnManager;
    private CharacterCard selectedAttacker = null;

    [Header("先行制限表示")]
    public GameObject firstTurnRestrictionIndicator;  // 先行1ターン目の制限表示
    private bool isFirstPlayerFirstTurn = false;      // 先行1ターン目かどうか

    private float originalTimeScale = 1.0f;
    
    private void Awake()
    {
        // キャッシュ用参照の初期化
        gameManager = GameManager.Instance;
    }
    
    private void Start()
    {
        // 参照の取得
        turnManager = gameManager?.turnManager;

        // ターン終了ボタンのイベント設定
        InitializeEndTurnButton();
        
        // 初期UI更新
        UpdateAllUI();

        // スペル発動UIの初期化
        InitializeSpellActivationUI();

        // デバッグ情報出力
        Debug.Log("UIManager: 初期化完了");

        // リターンボタンのイベント設定
        if (returnToMenuButton != null)
        {
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
        }

        InitializeNotificationBlocker();
    }

    // 通知情報を管理するクラス
    private class NotificationInfo
    {
        public string Message { get; set; }
        public float Duration { get; set; }
        public NotificationType Type { get; set; }
        
        public NotificationInfo(string message, float duration, NotificationType type = NotificationType.System)
        {
            Message = message;
            Duration = duration;
            Type = type;
        }
    }

    // 先行1ターン目状態を更新
    public void UpdateFirstTurnState(bool isFirstTurn)
    {
        isFirstPlayerFirstTurn = isFirstTurn;
        
        // 表示を更新
        if (firstTurnRestrictionIndicator != null)
        {
            firstTurnRestrictionIndicator.SetActive(isFirstTurn);
        }
        
        // 先行1ターン目の場合、通知を表示
        if (isFirstTurn && GameManager.Instance.firstPlayer == GameManager.Instance.player1)
        {
            ShowNotification("先行1ターン目は攻撃できません", 3.0f);
        }
        
        Debug.Log($"UIManager: 先行1ターン目状態を更新: {isFirstTurn}");
    }

    /// <summary>
    /// メニューに戻るボタンのクリックイベント
    /// </summary>
    private void OnReturnToMenuClicked()
    {
        Debug.Log("[UIManager] メニューに戻ります");
        
        // タイトルシーンに遷移
        SceneManager.LoadScene("PlayerSelectionScene");
    }

    /// <summary>
    /// ターン終了ボタンの初期化
    /// </summary>
    private void InitializeEndTurnButton()
    {
        if (endTurnButton == null) return;
        
        // 既存のリスナーをクリア
        endTurnButton.onClick.RemoveAllListeners();
        
        // 新しいリスナーを追加
        endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        Debug.Log("ターン終了ボタンのリスナーを設定しました");
    }
    
    /// <summary>
    /// メインのUI更新メソッド - 全てのゲームUI要素を更新
    /// </summary>
    public void UpdateAllUI()
    {
        Debug.Log("全UIを更新します");
        
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("GameManagerの参照が取得できません");
                return;
            }
        }
        
        // プレイヤー情報更新
        Player player1 = gameManager.player1;
        Player player2 = gameManager.player2;
        
        if (player1 != null)
        {
            UpdatePlayerInfo(player1);
            UpdateCharacterField(player1);
            UpdateFieldCard(player1);
            UpdatePlayerHand(player1);
            UpdateGraveyard(player1);
        }
        
        if (player2 != null)
        {
            UpdatePlayerInfo(player2);
            UpdateCharacterField(player2);
            UpdateFieldCard(player2);
            UpdatePlayerHand(player2);
            UpdateGraveyard(player2);
        }
        
        // ゲーム情報更新
        UpdateGameInfo();
    }
    
    /// <summary>
    /// プレイヤー情報の更新
    /// </summary>
    public void UpdatePlayerInfo(Player player)
    {
        if (player == null) return;
        
        bool isPlayer1 = (player == gameManager.player1);
        
        // 名前の更新
        TextMeshProUGUI nameText = isPlayer1 ? player1NameText : player2NameText;
        if (nameText != null) nameText.text = player.playerName;
        
        // 各情報の更新
        UpdateLifePoints(player);
        UpdateEnergy(player);
        UpdateDeckCount(player);
    }
    
    /// <summary>
    /// ライフポイント表示の更新
    /// </summary>
    public void UpdateLifePoints(Player player)
    {
        if (player == null) return;
        
        TextMeshProUGUI lifeText = (player == gameManager.player1) ? player1LifeText : player2LifeText;
        if (lifeText != null) lifeText.text = $"ライフ: {player.lifePoints}";
    }
    
    /// <summary>
    /// エナジー表示の更新
    /// </summary>
    public void UpdateEnergy(Player player)
    {
        if (player == null) return;
        
        TextMeshProUGUI energyText = (player == gameManager.player1) ? player1EnergyText : player2EnergyText;
        if (energyText != null) energyText.text = $"エナジー: {player.energy}";
    }

    /// <summary>
    /// Player.csとのメソッド重複に対応するエイリアス
    /// </summary>
    public void UpdatePlayerEnergy(Player player)
    {
        UpdateEnergy(player);
    }

    /// <summary>
    /// 特殊勝利条件達成時の特別な表示
    /// </summary>
    public void ShowSpecialVictory(Player winner, string effectName)
    {
        StartCoroutine(ShowSpecialVictoryEffect(winner, effectName));
    }

    /// <summary>
    /// 特殊勝利演出のコルーチン
    /// </summary>
    private IEnumerator ShowSpecialVictoryEffect(Player winner, string effectName)
    {
        // 特殊な通知を表示
        yield return StartCoroutine(ShowNotificationRoutine($"特殊勝利条件「{effectName}」達成！", 3.0f));
        
        // 勝利画面を表示
        ShowGameOverScreen(winner, $"特殊勝利条件「{effectName}」を達成");
    }
            
    /// <summary>
    /// デッキ残数の更新
    /// </summary>
    public void UpdateDeckCount(Player player)
    {
        if (player == null) return;
        
        TextMeshProUGUI deckText = (player == gameManager.player1) ? player1DeckCountText : player2DeckCountText;
        if (deckText != null) deckText.text = $"デッキ: {player.deck.Count}";
    }
    
    /// <summary>
    /// キャラクターフィールドの更新 - 位置調整改善
    /// </summary>
    public void UpdateCharacterField(Player player)
    {
        if (player == null) return;
        
        Transform fieldParent = (player == gameManager.player1) ? 
            player1CharacterFieldParent : player2CharacterFieldParent;
            
        if (fieldParent == null) return;
        
        // 既存のカードを削除
        for (int i = fieldParent.childCount - 1; i >= 0; i--)
        {
            Destroy(fieldParent.GetChild(i).gameObject);
        }
        
        // フィールドが空の場合は早期リターン
        if (player.characterField.Count == 0) return;
        
        // カードを大きめに表示するための固定スケール
        const float fixedScale = 0.85f; // 大きめのスケール値
        const float cardSpacing = 10f;  // カード間の間隔
        const int maxCards = 5;         // 最大表示カード数
        
        // プレイヤー2（相手）のカードの回転角度を事前計算
        Quaternion opponentRotation = Quaternion.Euler(0, 0, 180);
        Quaternion playerRotation = Quaternion.identity;
        bool isOpponent = player == gameManager.player2;
        
        // 配置計算のための基本サイズ情報取得
        float cardWidth = 140f; // カードの標準幅と仮定
        
        // カードの総幅を計算
        int cardCount = Mathf.Min(player.characterField.Count, maxCards);
        float totalWidth = (cardWidth * fixedScale * cardCount) + (cardSpacing * (cardCount - 1));
        float startX = -totalWidth / 2 + (cardWidth * fixedScale / 2);
        
        // Y位置の調整 - 手札から離す
        float yPos = isOpponent ? 50f : -50f; // プレイヤー1は下、プレイヤー2は上に配置
        
        // すべてのカードの表示
        for (int i = 0; i < player.characterField.Count; i++)
        {
            CharacterCard card = player.characterField[i];
            if (card == null) continue;
            
            GameObject cardObj = Instantiate(cardPrefab, fieldParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // カードのアートワークを設定
                if (!string.IsNullOrEmpty(card.artwork))
                {
                    Sprite cardSprite = LoadCardSprite(card);
                    if (cardSprite != null)
                    {
                        cardUI.SetCardArtwork(cardSprite);
                    }
                }
                
                // カードのサイズと位置を調整
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 位置を設定 - Y位置を調整して手札と被らないようにする
                    float xPos = startX + i * (cardWidth * fixedScale + cardSpacing);
                    rectTransform.anchoredPosition = new Vector2(xPos, yPos);
                    
                    // スケールを固定値に設定
                    rectTransform.localScale = Vector3.one * fixedScale;
                    
                    // プレイヤーに応じた回転設定
                    rectTransform.rotation = isOpponent ? opponentRotation : playerRotation;
                }
                
                // カードクリックイベントの設定
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    CharacterCard characterCard = card; // ラムダ用にローカル変数
                    cardButton.onClick.AddListener(() => OnFieldCardClicked(characterCard, !isOpponent));
                }
            }
        }
    }

    /// <summary>
    /// カードのスプライトをロードする - キャッシュ機能追加
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
        return null;
    }
     
    /// <summary>
    /// フィールドカードの更新 - 位置を正確に設定
    /// </summary>
    public void UpdateFieldCard(Player player)
    {
        if (player == null) return;
        
        // フィールドカードの親オブジェクト取得
        Transform fieldParent = (player == gameManager.player1) ? 
            player1FieldCardParent : player2FieldCardParent;
            
        if (fieldParent == null)
        {
            Debug.LogError($"[UIManager] フィールドカード親オブジェクトが見つかりません: {player.playerName}");
            return;
        }
        
        // 既存のカードを削除
        for (int i = fieldParent.childCount - 1; i >= 0; i--)
        {
            Destroy(fieldParent.GetChild(i).gameObject);
        }
        
        // フィールドカードの表示
        for (int i = 0; i < player.fieldCards.Length; i++)
        {
            FieldCard card = player.fieldCards[i];
            if (card == null) continue;
            
            // カードの位置を計算
            float xPosition = (i - 1) * 150f; // -150, 0, 150 の位置
            
            // カードプレハブをインスタンス化
            GameObject cardObj = Instantiate(cardPrefab, fieldParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                // カード情報をセットアップ
                cardUI.SetupCard(card);
                
                // カードのアートワークを設定
                if (!string.IsNullOrEmpty(card.artwork))
                {
                    Sprite cardSprite = LoadCardSprite(card);
                    if (cardSprite != null)
                    {
                        cardUI.SetCardArtwork(cardSprite);
                    }
                }
                
                // カードの位置とサイズを調整
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // 位置を設定
                    rectTransform.anchoredPosition = new Vector2(xPosition, 0f);
                    
                    // スケールを設定
                    rectTransform.localScale = Vector3.one * 0.8f;
                    
                    // プレイヤー2の場合は回転
                    if (player == gameManager.player2)
                    {
                        rectTransform.rotation = Quaternion.Euler(0, 0, 180);
                    }
                }
                
                // クリックイベントの設定
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    FieldCard fieldCard = card; // ラムダ式用にキャプチャ
                    cardButton.onClick.AddListener(() => OnFieldCardClicked(fieldCard, player == gameManager.player1));
                }
            }
        }
        
        Debug.Log($"[UIManager] フィールドカード更新完了: {player.playerName}");
    }
    
    /// <summary>
    /// プレイヤーの手札を更新 (HandDisplayControllerと連携)
    /// </summary>
    public void UpdatePlayerHand(Player player)
    {
        if (player == null) return;
        
        // プレイヤーに対応する手札表示コントローラを取得
        HandDisplayController handDisplay = GetHandDisplayForPlayer(player);
        
        if (handDisplay != null)
        {
            // プレイヤー参照を設定
            handDisplay.SetPlayerReference(player);
            
            // 手札表示を更新
            handDisplay.UpdateHandDisplay();
        }
    }

    /// <summary>
    /// 指定された親オブジェクト内の手札表示コントローラを検索
    /// </summary>
    private HandDisplayController FindHandDisplayController(Transform parent)
    {
        if (parent == null) return null;
        
        // 親自身にHandDisplayControllerがあるか確認
        HandDisplayController controller = parent.GetComponent<HandDisplayController>();
        if (controller != null)
        {
            return controller;
        }
        
        // 子オブジェクトにHandDisplayControllerがあるか確認
        foreach (Transform child in parent)
        {
            controller = child.GetComponent<HandDisplayController>();
            if (controller != null)
            {
                return controller;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// ゲーム情報の更新 - キャッシュ参照の活用
    /// </summary>
    private void UpdateGameInfo()
    {   
        if (turnManager == null)
        {
            turnManager = gameManager?.turnManager;
            if (turnManager == null) return;
        }
        
        // 現在のプレイヤーチェック
        Player currentTurnPlayer = turnManager.currentPlayer;
        
        // プレイヤーが変わったら攻撃選択状態をリセット
        if (currentTurnPlayer != null && 
            currentTurnPlayer != gameManager.player1 && 
            selectedAttacker != null)
        {
            // AIターンの開始時には攻撃選択状態をクリア
            selectedAttacker = null;
            Debug.Log("ターン切り替えにより攻撃選択状態をリセットしました");
        }
        
        // 現在のプレイヤー表示
        if (currentPlayerText != null && currentTurnPlayer != null)
        {
            currentPlayerText.text = $"{currentTurnPlayer.playerName}のターン";
        }
        
        // フェーズ表示
        UpdatePhaseInfo(turnManager.currentPhase);
        
        // ターン終了ボタンの有効/無効
        UpdateEndTurnButton();
    }
    
    /// <summary>
    /// ターン終了ボタンの状態更新
    /// </summary>
    public void UpdateEndTurnButton()
    {
        if (endTurnButton == null)
        {
            Debug.LogWarning("UpdateEndTurnButton: endTurnButtonがnullです");
            return;
        }
        
        if (turnManager == null)
        {
            turnManager = gameManager?.turnManager;
            if (turnManager == null)
            {
                Debug.LogWarning("UpdateEndTurnButton: turnManagerがnullです");
                return;
            }
        }
        
        // プレイヤー1（自分）のターンかつアクションフェーズの時だけ有効にする
        // 条件判定を単純化し、デバッグ情報を強化
        bool isPlayer1Turn = (turnManager.currentPlayer == gameManager.player1);
        bool isActionPhase = (turnManager.currentPhase == TurnPhase.Action);
        
        // 有効状態を設定
        endTurnButton.interactable = isPlayer1Turn && isActionPhase;
        
        // 詳細なデバッグログ
        Debug.Log($"ターン終了ボタン更新: プレイヤー1のターン={isPlayer1Turn}, " +
                $"アクションフェーズ={isActionPhase}, " +
                $"有効={endTurnButton.interactable}, " +
                $"現在のフェーズ={turnManager.currentPhase}");
    }
    
    /// <summary>
    /// フェーズ情報の更新
    /// </summary>
    public void UpdatePhaseInfo(TurnPhase phase)
    {
        if (phaseText == null) return;
        
        string phaseString = phase switch
        {
            TurnPhase.Draw => "ドロー",
            TurnPhase.Action => "アクション",
            TurnPhase.End => "終了",
            _ => "なし"
        };
        
        phaseText.text = $"フェーズ: {phaseString}";
    }
    
    /// <summary>
    /// ゲームオーバー画面表示 - シーン遷移ボタン追加
    /// </summary>
    public void ShowGameOverScreen(Player winner, string reason = "")
    {
        if (gameOverPanel == null) 
        {
            Debug.LogError("[UIManager] gameOverPanelがnullです");
            return;
        }
        
        // ゲーム進行を完全停止
        Time.timeScale = 0;
        
        // 通知キューをクリア
        ClearAllNotifications();
        
        // 他のUIを徹底的に無効化
        DisableAllInteractiveUI(true);
        
        // 追加: 完全にゲームを操作不可にするオーバーレイを作成
        CreateGameOverBlocker();
        
        // キャンバス設定を最前面に
        Canvas gameOverCanvas = gameOverPanel.GetComponent<Canvas>();
        if (gameOverCanvas == null)
        {
            gameOverCanvas = gameOverPanel.AddComponent<Canvas>();
            gameOverCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameOverPanel.AddComponent<GraphicRaycaster>();
        }

        // 必ず最前面に表示
        gameOverCanvas.overrideSorting = true;
        gameOverCanvas.sortingOrder = 1100; // 他のUIより確実に上
        
        // 勝者と勝利理由を表示
        if (winnerText != null && winner != null)
        {
            // 勝利理由を表示するよう拡張
            winnerText.text = string.IsNullOrEmpty(reason) 
                ? $"{winner.playerName}の勝利！" 
                : $"{winner.playerName}の勝利！\n{reason}";
        }
        
        // パネルを表示
        gameOverPanel.SetActive(true);
        
        // シーン遷移ボタンをアクティブ化
        if (returnToMenuButton != null)
        {
            // ボタンリスナーをクリアして再設定
            returnToMenuButton.onClick.RemoveAllListeners();
            returnToMenuButton.onClick.AddListener(OnReturnToMenuClicked);
            returnToMenuButton.gameObject.SetActive(true);
            
            // ボタンを確実に選択可能にする
            returnToMenuButton.interactable = true;
            
            // ボタンを最前面に
            returnToMenuButton.transform.SetAsLastSibling();
            
            Debug.Log("[UIManager] メニュー遷移ボタン設定完了");
        }
        
        Debug.Log("[UIManager] ゲーム終了画面を表示しました");
    }

    // 追加: 完全なゲームオーバーブロッカーの作成
    private void CreateGameOverBlocker()
    {
        // ゲームオーバー専用のブロッカーを作成
        GameObject blocker = new GameObject("GameOverBlocker");
        blocker.transform.SetParent(transform, false);
        
        // 全画面をカバーするRectTransformを設定
        RectTransform rectTransform = blocker.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        
        // 半透明の背景画像を追加
        Image image = blocker.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.7f); // 暗い背景
        
        // ブロッカーキャンバスを最前面に
        Canvas blockerCanvas = blocker.AddComponent<Canvas>();
        blockerCanvas.overrideSorting = true;
        blockerCanvas.sortingOrder = 1090; // ゲームオーバーパネルのすぐ下
        blocker.AddComponent<GraphicRaycaster>();
        
        // 他のすべてのUI要素の前に配置
        blocker.transform.SetAsLastSibling();
        
        // ゲームオーバーパネルの子要素としてブロッカーを追加
        blocker.transform.SetParent(gameOverPanel.transform, true);
        blocker.transform.SetAsFirstSibling(); // パネルの背後に
    }

    private void ClearAllNotifications()
    {
        notificationQueue.Clear();
        isNotificationActive = false;
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        if (notificationBlocker != null)
        {
            notificationBlocker.SetActive(false);
        }
        
        // 進行中の通知コルーチンを停止
        StopAllCoroutines();
    }

    // 手札のインタラクションを無効化
    private void DisableHandInteractions()
    {
        // プレイヤー1の手札を無効化
        if (player1HandDisplay != null)
        {
            Transform handParent = player1HandDisplay.transform;
            foreach (Transform child in handParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
        
        // プレイヤー2の手札も念のため無効化
        if (player2HandDisplay != null)
        {
            Transform handParent = player2HandDisplay.transform;
            foreach (Transform child in handParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
    }

    // フィールドのインタラクションを無効化
    private void DisableFieldInteractions()
    {
        // プレイヤー1のフィールドを無効化
        if (player1CharacterFieldParent != null)
        {
            foreach (Transform child in player1CharacterFieldParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
        
        // プレイヤー2のフィールドを無効化
        if (player2CharacterFieldParent != null)
        {
            foreach (Transform child in player2CharacterFieldParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
        
        // フィールドカードも無効化
        if (player1FieldCardParent != null)
        {
            foreach (Transform child in player1FieldCardParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
        
        if (player2FieldCardParent != null)
        {
            foreach (Transform child in player2FieldCardParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = false;
                }
            }
        }
    }

    // 他のUI要素を一時的に無効化するヘルパーメソッド
    private void DisableAllInteractiveUI(bool isGameOver = false)
    {
        // 手札の無効化
        DisableHandInteractions();
        
        // フィールドカードの無効化
        DisableFieldInteractions();
        
        // 終了ボタンなど他のUI操作の無効化
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
        
        // 開いているダイアログがあれば閉じる
        HideCardDetail();
        
        // 確認ダイアログを閉じる
        if (cardConfirmationDialog != null && cardConfirmationDialog.gameObject.activeSelf)
        {
            cardConfirmationDialog.HideDialog();
        }
        
        // ゲームオーバー時は特に徹底した無効化
        if (isGameOver)
        {
            // 全てのボタンを無効化
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (Button button in allButtons)
            {
                if (button != returnToMenuButton) // メニューボタンだけは例外
                {
                    button.interactable = false;
                }
            }
            
            // 他のUI操作も無効化
            // AIプレイヤーの動きを停止
            if (GameManager.Instance != null && 
                GameManager.Instance.player2 is AIPlayer aiPlayer)
            {
                // aiPlayer.StopAllCoroutines();
            }
        }
        
        Debug.Log($"[UIManager] UI操作を無効化しました (ゲームオーバー：{isGameOver})");
    }

    // カード詳細表示を閉じるメソッド
    public void HideCardDetail()
    {
        if (cardDetailPanel != null)
        {
            cardDetailPanel.SetActive(false);
        }
        
        // 表示中のカードを削除
        if (currentDetailCard != null)
        {
            Destroy(currentDetailCard);
            currentDetailCard = null;
        }
    }

    /// <summary>
    /// カード詳細表示を更新 - カードを大きく表示するバージョン
    /// </summary>
    public void ShowCardDetail(Card card)
    {
        if (cardDetailPanel == null || card == null) return;
        
        // パネルを表示
        cardDetailPanel.SetActive(true);
        
        // 既に表示されているカードがあれば削除
        if (currentDetailCard != null)
        {
            Destroy(currentDetailCard);
            currentDetailCard = null;
        }
        
        // カードコンテナがない場合は作成
        if (cardDetailContainer == null)
        {
            // パネル内に新しいコンテナを作成
            cardDetailContainer = new GameObject("CardDetailContainer").transform;
            cardDetailContainer.SetParent(cardDetailPanel.transform, false);
            
            // コンテナの位置を中央に設定
            RectTransform containerRect = cardDetailContainer.gameObject.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(200, 300);
        }
        
        // カードプレハブをインスタンス化
        currentDetailCard = Instantiate(cardPrefab, cardDetailContainer);
        
        // カード情報を設定
        CardUI cardUI = currentDetailCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            // カード情報をセットアップ
            cardUI.SetupCard(card);
            
            // カードのアートワークを設定
            if (!string.IsNullOrEmpty(card.artwork))
            {
                Sprite cardSprite = LoadCardSprite(card);
                if (cardSprite != null)
                {
                    cardUI.SetCardArtwork(cardSprite);
                }
            }
            
            // ボタンコンポーネントを無効化
            Button cardButton = currentDetailCard.GetComponent<Button>();
            if (cardButton != null)
            {
                cardButton.enabled = false;
            }
            
            // カードの表示サイズを大きくする
            RectTransform rectTransform = currentDetailCard.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * cardDetailScale;
            }
        }
        
        // 必要ならデバッグ情報をログに出力
        if (GameManager.Instance.enableDebugLog)
        {
            CardUI logHelper = currentDetailCard.GetComponent<CardUI>();
            if (logHelper != null)
            {
                logHelper.LogCardInfo(card);
            }
        }
        
        // パネルを最前面に表示
        Canvas canvas = cardDetailPanel.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 90; // コンファメーションダイアログより下、通常UIより上
        }
        else
        {
            canvas = cardDetailPanel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 90;
            cardDetailPanel.AddComponent<GraphicRaycaster>(); // レイキャスターも追加
        }
    }
        
    /// <summary>
    /// フィールド上のカードがクリックされたときの処理 - 攻撃処理の追加
    /// </summary>
    private void OnFieldCardClicked(Card card, bool isPlayerCard)
    {
        if (card == null) return;
        
        // 確認ダイアログが表示中なら操作を無視
        if (CardConfirmationDialog.IsDialogActive)
        {
            return;
        }
        
        // 現在のターンプレイヤーを取得
        Player currentPlayer = turnManager?.currentPlayer;
        if (currentPlayer == null) return;
        
        // カード詳細を表示
        ShowCardDetail(card);
        
        // 自分のカードがクリックされた場合
        if (isPlayerCard && currentPlayer == gameManager.player1 && card is CharacterCard)
        {
            CharacterCard charCard = (CharacterCard)card;

            // 先行1ターン目の攻撃制限チェック
            if (isFirstPlayerFirstTurn && gameManager.firstPlayer == gameManager.player1)
            {
                ShowNotification("先行1ターン目は攻撃できません", 2.0f, NotificationType.Battle);
                return;
            }
            
            // カードが既に攻撃済みの場合
            if (charCard.hasAttackedThisTurn)
            {
                ShowNotification("このカードは既に攻撃済みです", 1.5f, NotificationType.Battle);
                return;
            }
            
            // 攻撃モードが既に有効なら、別のカードを選択して攻撃モードをリセット
            if (selectedAttacker != null)
            {
                // 同じカードをクリックした場合は選択をキャンセル
                if (selectedAttacker == charCard)
                {
                    selectedAttacker = null;
                    ShowNotification("攻撃をキャンセルしました", 1.5f, NotificationType.Battle);
                    return;
                }
                
                // 別のカードを選択した場合は新しい攻撃者に変更
                selectedAttacker = charCard;
                ShowNotification($"{charCard.cardName}を攻撃者に選択しました", 1.5f, NotificationType.Battle);
                return;
            }
            
            // 攻撃モードを有効化
            selectedAttacker = charCard;
            
            // 相手のフィールドが空かどうかをチェック
            Player opponent = gameManager.player2;
            if (opponent.characterField.Count == 0)
            {
                // 相手フィールドが空なら直接攻撃の確認ダイアログを表示
                ShowDirectAttackConfirmation(charCard, opponent);
            }
            else
            {
                ShowNotification($"{charCard.cardName}で攻撃します。相手のキャラクターをクリックしてください", 2.0f, NotificationType.Battle);
            }
        }
        // 相手のカードがクリックされた場合
        else if (!isPlayerCard && card is CharacterCard && selectedAttacker != null)
        {
            CharacterCard targetCard = (CharacterCard)card;
            
            // 攻撃処理を実行
            bool success = currentPlayer.AttackWithCharacter(selectedAttacker, targetCard);
            
            if (success)
            {
                ShowNotification($"{selectedAttacker.cardName}が{targetCard.cardName}に攻撃しました", 2.0f, NotificationType.Battle);
                
                // UI更新
                UpdateCharacterField(gameManager.player1);
                UpdateCharacterField(gameManager.player2);
                
                // 攻撃者の選択をリセット
                selectedAttacker = null;
            }
            else
            {
                ShowNotification("攻撃に失敗しました", 1.5f, NotificationType.Battle);
                selectedAttacker = null;
            }
        }
    }

    // 直接攻撃確認ダイアログを表示する新しいメソッド
    private void ShowDirectAttackConfirmation(CharacterCard attacker, Player opponent)
    {
        // コンファメーションダイアログがあれば使用
        if (cardConfirmationDialog != null)
        {
            string message = $"{opponent.playerName}に直接攻撃しますか？";
            
            cardConfirmationDialog.ShowDialog(attacker, message, () => {
                // 確認後に直接攻撃を実行
                bool success = gameManager.player1.DirectAttack(attacker);
                
                if (success)
                {
                    ShowNotification($"{attacker.cardName}が{opponent.playerName}に直接攻撃しました！", 2.0f);
                    
                    // UI更新
                    UpdateCharacterField(gameManager.player1);
                    UpdateLifePoints(opponent);
                    
                    // 攻撃者の選択をリセット
                    selectedAttacker = null;
                }
                else
                {
                    ShowNotification("直接攻撃に失敗しました", 1.5f);
                }
            });
        }
        else
        {
            // ダイアログなしの場合の処理
            ShowNotification($"{attacker.cardName}で直接攻撃するには、相手フィールドをクリックしてください", 2.0f);
        }
    }

    /// <summary>
    /// 手札のカードがクリックされたときの処理
    /// </summary>
    public void OnPlayerHandCardClicked(Card card)
    {
        // 現在のプレイヤーとフェイズを確認
        if (turnManager == null || card == null) return;
        
        // カード詳細を表示
        ShowCardDetail(card);
        
        // アクションフェーズでのみカード使用可能
        if (turnManager.currentPhase == TurnPhase.Action && 
            turnManager.currentPlayer == gameManager.player1) // プレイヤー1のみ
        {
            // カード使用の前に条件チェック
            if (!CanUseCard(card, turnManager.currentPlayer))
            {
                Debug.LogWarning($"[UIManager] カード使用条件不足: {card.cardName}");
                return; // 使用条件を満たさない場合は処理を中断
            }
            
            // カードタイプに応じた確認メッセージを作成
            string confirmMessage = "";
            
            if (card is CharacterCard charCard)
            {
                confirmMessage = $"このキャラクター「{card.cardName}」をフィールドに召喚します。\n攻撃力: {charCard.attackPower}、防御力: {charCard.defensePower}\n効果: {card.description}";
            }
            else if (card is SpellCard spellCard)
            {
                confirmMessage = $"このスペルカード「{card.cardName}」を発動します。\n効果: {card.description}";
            }
            else if (card is FieldCard fieldCard)
            {
                confirmMessage = $"このフィールドカード「{card.cardName}」を発動します。\n効果: {card.description}";
            }
            else
            {
                confirmMessage = $"このカード「{card.cardName}」を使用します。\n効果: {card.description}";
            }
            
            // ダイアログ表示
            CardConfirmationDialog dialog = FindCardConfirmationDialog();
            
            if (dialog == null)
            {
                Debug.LogError("[UIManager] CardConfirmationDialog が見つかりません");
                
                // ダイアログなしで直接使用
                ShowNotification($"カード {card.cardName} を使用します", 1.5f);
                UseCard(card, turnManager.currentPlayer);
            }
            else
            {
                // カード使用アクションをラムダでキャプチャ
                Card capturedCard = card;
                Player capturedPlayer = turnManager.currentPlayer;
                
                // UseCardメソッドをラップしたアクションを作成
                Action confirmAction = () => {
                    Debug.Log($"[UIManager] カード確認アクション実行: {capturedCard.cardName}");
                    UseCard(capturedCard, capturedPlayer);
                };
                
                // ダイアログ表示
                dialog.ShowDialog(card, confirmMessage, confirmAction);
                Debug.Log($"[UIManager] ダイアログを表示: {card.cardName}");
            }
        }
    }

    // FindCardConfirmationDialogヘルパーメソッド追加
    private CardConfirmationDialog FindCardConfirmationDialog()
    {
        // まずインスペクタで設定されたダイアログを使用
        if (cardConfirmationDialog != null)
        {
            return cardConfirmationDialog;
        }
        
        // シーン内のダイアログを検索
        CardConfirmationDialog[] dialogs = FindObjectsByType<CardConfirmationDialog>(FindObjectsSortMode.None);
        if (dialogs.Length > 0)
        {
            return dialogs[0];
        }
        
        // 非アクティブなオブジェクトも含めて検索
        CardConfirmationDialog[] allDialogs = Resources.FindObjectsOfTypeAll<CardConfirmationDialog>();
        if (allDialogs.Length > 0)
        {
            return allDialogs[0];
        }
        
        return null;
    }

    /// <summary>
    /// カードが使用可能かチェック
    /// </summary>
    private bool CanUseCard(Card card, Player player)
    {
        if (card == null || player == null)
        {
            return false;
        }
        
        // エナジーコストのチェック
        if (player.energy < card.cost)
        {
            ShowNotification($"エナジーが足りません（必要: {card.cost}, 所持: {player.energy}）", 2.0f);
            return false;
        }
        
        // カードタイプに応じたチェック
        if (card is CharacterCard)
        {
            // キャラクターフィールドの空きチェック (最大5枚)
            const int MAX_CHARACTERS = 5;
            if (player.characterField.Count >= MAX_CHARACTERS)
            {
                ShowNotification("キャラクターフィールドがいっぱいです", 2.0f);
                return false;
            }
        }
        else if (card is FieldCard)
        {
            // フィールドカードの重複チェック (最大1枚)
            const int MAX_FIELD_CARDS = 1;
            if (player.activeFieldCards.Count >= MAX_FIELD_CARDS)
            {
                ShowNotification("フィールドカードの数が上限に達しています", 2.0f);
                return false;
            }
        }
        
        return true;
    }

    // フィールドカード使用の特別処理を追加
    private void HandleFieldCardPlay(FieldCard fieldCard, Player player)
    {
        // フィールド位置の選択ダイアログを表示する代わりに、
        // とりあえず最初の空きスロットに配置
        int position = -1;
        
        // 空きフィールド位置を探す
        for (int i = 0; i < player.fieldCards.Length; i++)
        {
            if (player.fieldCards[i] == null)
            {
                position = i;
                break;
            }
        }
        
        if (position == -1)
        {
            // 空きスロットがない場合は最初のスロットを上書き
            position = 0;
            ShowNotification("フィールドスロットがいっぱいです。最初のスロットを上書きします。", 2.0f);
        }
        
        // フィールドカード効果の説明を表示
        ShowCardEffectDescription(fieldCard);
        
        // カードをプレイ
        bool success = player.PlayFieldCard(fieldCard, position);
        
        if (success)
        {
            ShowNotification($"フィールドカード「{fieldCard.cardName}」を位置 {position + 1} に配置しました", 2.0f);
            
            // UI更新
            UpdateAllUI();
        }
        else
        {
            ShowNotification($"フィールドカード「{fieldCard.cardName}」の配置に失敗しました", 2.0f);
        }
    }

    /// <summary>
    /// カードを使用する処理 - 戻り値問題修正
    /// </summary>
    private void UseCard(Card card, Player player)
    {
        if (card == null || player == null)
        {
            Debug.LogError("[UIManager] UseCard: 無効なパラメータ");
            return;
        }
        
        if (gameManager == null)
        {
            Debug.LogError("[UIManager] UseCard: GameManagerがnull");
            // ゲームマネージャーを再取得
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                ShowNotification("エラー: ゲームマネージャーが見つかりません", 2.0f);
                return;
            }
        }
        
        try
        {
            Debug.Log($"[UIManager] カード使用開始: {card.cardName}, プレイヤー: {player.playerName}, カードタイプ: {card.GetType().Name}");
            
            // カードタイプに応じた処理
            if (card is FieldCard fieldCard)
            {
                // フィールドカードの場合、特別な処理が必要
                HandleFieldCardPlay(fieldCard, player);
            }
            else
            {
                // 通常のカード処理
                // GameManagerにカードプレイを委譲
                Player opponent = gameManager.GetOpponent(player);
                if (opponent != null)
                {
                    // カード効果の説明を表示
                    ShowCardEffectDescription(card);
                    
                    // カード使用実行
                    gameManager.PlayCard(card, player, opponent);
                    
                    // 通知表示
                    ShowNotification($"{card.cardName}を使用しました", 1.5f);
                    Debug.Log($"[UIManager] カード使用成功: {card.cardName}");
                    
                    // UI更新
                    UpdateAllUI();
                }
                else
                {
                    Debug.LogError($"[UIManager] 対戦相手が見つかりません: {player.playerName}");
                    ShowNotification("エラー: 対戦相手が見つかりません", 2.0f);
                }
            }
        }
        catch (System.Exception ex)
        {
            // 例外をキャッチして安全に処理
            Debug.LogError($"[UIManager] カード使用中にエラー: {ex.Message}\n{ex.StackTrace}");
            ShowNotification($"エラー: カードの使用に失敗しました", 2.0f);
        }
    }

    // カード効果の説明を表示するメソッド
    private void ShowCardEffectDescription(Card card)
    {
        if (card == null) return;
        
        string effectMessage = "";
        
        // カードタイプ別のメッセージ
        if (card is CharacterCard charCard)
        {
            effectMessage = $"キャラクター「{card.cardName}」（ATK:{charCard.attackPower}/DEF:{charCard.defensePower}）を召喚します。\n{card.description}";
        }
        else if (card is SpellCard)
        {
            effectMessage = $"スペルカード「{card.cardName}」の効果を発動:\n{card.description}";
        }
        else if (card is FieldCard)
        {
            effectMessage = $"フィールドカード「{card.cardName}」を場に配置:\n{card.description}";
        }
        
        // 効果説明の通知
        ShowNotification(effectMessage, 3.0f);
    }

    /// <summary>
    /// ターン終了ボタンがクリックされたときの処理
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        Debug.Log("ターン終了ボタンがクリックされました");
        
        // TurnManagerの参照を確認
        if (turnManager == null)
        {
            turnManager = gameManager?.turnManager;
            if (turnManager == null)
            {
                Debug.LogError("TurnManagerがnullです");
                return;
            }
        }
        
        // 現在のフェーズを確認
        Debug.Log($"現在のフェーズ: {turnManager.currentPhase}");
        
        // アクションフェーズならエンドフェーズに移行
        if (turnManager.currentPhase == TurnPhase.Action)
        {
            Debug.Log("ターンを終了します");
            turnManager.GoToEndPhase();
        }
        else
        {
            Debug.LogWarning($"アクションフェーズではないためターン終了できません: {turnManager.currentPhase}");
        }
    }

    /// <summary>
    /// スペル発動確認UIの初期化
    /// </summary>
    private void InitializeSpellActivationUI()
    {
        if (spellActivationPanel != null)
        {
            spellActivationPanel.SetActive(false);
            
            // キャンセルボタンイベント設定
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnSpellActivationCanceled);
            }
        }
    }
    
    /// <summary>
    /// スペル発動確認UIを表示
    /// </summary>
    public void ShowSpellActivationPrompt(Player player, List<SpellCard> spells, GameEventInfo eventInfo)
    {
        if (spellActivationPanel == null)
        {
            Debug.LogError("スペル発動確認パネルが設定されていません");
            return;
        }
        
        if (player == null || spells == null || spells.Count == 0)
        {
            Debug.LogWarning("スペル発動確認UIに無効なパラメータが渡されました");
            return;
        }
        
        // 情報を保存
        activatingPlayer = player;
        activatableSpells = spells;
        triggeringEvent = eventInfo;
        
        // パネルを表示
        spellActivationPanel.SetActive(true);
        
        // イベント内容説明を表示
        if (eventDescriptionText != null)
        {
            eventDescriptionText.text = GetEventDescription(eventInfo);
        }
        
        // スペルカード表示コンテナをクリア
        ClearSpellCardContainer();
        
        // 発動可能なスペルカードを表示
        foreach (SpellCard spell in spells)
        {
            // カードプレハブをインスタンス化
            GameObject cardObj = Instantiate(cardPrefab, spellCardContainer);
            
            // サイズ調整
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one * cardDisplayScale;
            }
            
            // カード情報を設定
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(spell);
                
                // カードのアートワークを設定
                Sprite cardSprite = LoadCardSprite(spell);
                if (cardSprite != null)
                {
                    cardUI.SetCardArtwork(cardSprite);
                }
            }
            
            // クリックイベント設定
            Button cardButton = cardObj.GetComponent<Button>();
            if (cardButton != null)
            {
                SpellCard spellCard = spell; // ラムダ内でキャプチャするためのローカル変数
                cardButton.onClick.AddListener(() => OnSpellCardSelected(spellCard));
            }
        }
    }
    
    /// <summary>
    /// スペルカード表示コンテナをクリア
    /// </summary>
    private void ClearSpellCardContainer()
    {
        if (spellCardContainer == null) return;
        
        // 最適化: トップダウンでループすることで、配列のリサイズを最小限に
        for (int i = spellCardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(spellCardContainer.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// スペルカード選択時の処理
    /// </summary>
    private void OnSpellCardSelected(SpellCard spell)
    {
        if (spell == null) return;
        
        // パネルを非表示
        spellActivationPanel.SetActive(false);
        
        // スペルカードを発動
        gameManager?.ActivateSpell(activatingPlayer, spell, triggeringEvent);
    }
    
    /// <summary>
    /// スペル発動キャンセル時の処理
    /// </summary>
    private void OnSpellActivationCanceled()
    {
        // パネルを非表示
        spellActivationPanel.SetActive(false);
    }
    
    /// <summary>
    /// イベント内容を説明テキストに変換
    /// </summary>
    private string GetEventDescription(GameEventInfo eventInfo)
    {
        if (eventInfo == null) return "イベント情報がありません";
        
        // イベントタイプに応じた説明文を返す
        return eventInfo.eventType switch
        {
            GameEventType.CardPlayed => 
                $"{eventInfo.activePlayer.playerName}が{eventInfo.sourceCard.cardName}をプレイしました。\n対応するスペルカードを使いますか？",
                
            GameEventType.CharacterSummoned => 
                $"{eventInfo.activePlayer.playerName}が{eventInfo.sourceCard.cardName}を召喚しました。\n対応するスペルカードを使いますか？",
                
            GameEventType.AttackDeclared => 
                $"{eventInfo.activePlayer.playerName}の{eventInfo.sourceCard.cardName}が{eventInfo.targetCard.cardName}に攻撃します。\n対応するスペルカードを使いますか？",
                
            GameEventType.DirectAttack => 
                $"{eventInfo.activePlayer.playerName}の{eventInfo.sourceCard.cardName}があなたに直接攻撃します。\n対応するスペルカードを使いますか？",
                
            GameEventType.SpellActivated => 
                $"{eventInfo.activePlayer.playerName}が{eventInfo.sourceCard.cardName}を発動しました。\n対応するスペルカードを使いますか？",
                
            GameEventType.PhaseChanged => 
                $"{eventInfo.activePlayer.playerName}のフェーズが変更されました。\n対応するスペルカードを使いますか？",
                
            GameEventType.TurnChanged => 
                $"{eventInfo.activePlayer.playerName}のターンが開始されました。\n対応するスペルカードを使いますか？",
                
            _ => "対応するスペルカードを使いますか？"
        };
    }
    
    /// <summary>
    /// ターン開始時の通知表示
    /// </summary>
    public void ShowTurnStartNotification(Player player)
    {
        if (player == null) return;
        
        // ターン開始通知を表示
        StartCoroutine(ShowNotificationRoutine(player.playerName + "のターンです", 2.0f));

        // ターン変更イベントを監視
        if (gameManager != null && gameManager.turnManager != null)
        {
            gameManager.turnManager.OnTurnChanged.AddListener(HandleTurnChanged);
        }
    }

    // ターン変更時の処理を行うメソッド
    public void HandleTurnChanged(Player newTurnPlayer)
    {
        // 攻撃選択状態をリセット
        if (selectedAttacker != null)
        {
            selectedAttacker = null;
            Debug.Log("ターン変更イベントにより攻撃選択状態をリセットしました");
        }
    }

    // UIManager.cs に以下のメソッドを追加
    public void ResetAttackSelection()
    {
        if (selectedAttacker != null)
        {
            selectedAttacker = null;
            Debug.Log("攻撃選択状態をリセットしました");
        }
    }
    
    /// <summary>
    /// 通知を表示
    /// </summary>
    public void ShowNotification(string message, float duration, NotificationType type = NotificationType.System)
    {
        // 表示対象外の通知はスキップ
        if (!ShouldShowNotification(message, type))
        {
            return;
        }
        
        // デバッグログは常に出力（絞った内容でも）
        Debug.Log($"[通知] {message}");
        
        // 通知をキューに追加
        notificationQueue.Enqueue(new NotificationInfo(message, duration, type));
        
        // 表示中でなければ表示を開始
        if (!isNotificationActive)
        {
            ProcessNextNotification();
        }
    }

    private void ProcessNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isNotificationActive = false;
            
            // ブロッカーを非表示
            if (notificationBlocker != null)
            {
                notificationBlocker.SetActive(false);
            }
            
            // ゲーム進行を再開
            Time.timeScale = originalTimeScale;
            
            return;
        }
        
        isNotificationActive = true;
        NotificationInfo notification = notificationQueue.Dequeue();
        
        // 現在のタイムスケールを保存
        originalTimeScale = Time.timeScale;
        
        // ゲーム進行を一時停止
        Time.timeScale = 0;
        
        // ブロッカーを確実に表示して他の操作を防止
        EnsureNotificationBlocker();
        
        // 通知パネルの設定を確認・調整
        EnsureNotificationPanelSettings();
        
        // 通知テキストを設定
        if (notificationPanel != null && notificationText != null)
        {
            notificationPanel.SetActive(true);
            notificationText.text = notification.Message;
        }
        
        // 指定時間後に通知を閉じるコルーチンを開始
        StartCoroutine(CloseNotificationAfterDelay(notification.Duration));
    }


    private void EnsureNotificationBlocker()
    {
        // 通知ブロッカーが未設定または非アクティブなら設定
        if (notificationBlocker == null)
        {
            // 新しいブロッカーを作成
            GameObject blocker = new GameObject("NotificationBlocker");
            blocker.transform.SetParent(transform, false);
            
            // 全画面をカバーするRectTransformを設定
            RectTransform rectTransform = blocker.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            // 半透明の背景画像を追加して視覚的にブロック中と分かるように
            Image image = blocker.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.2f); // 少し暗くして操作不可を示す
            
            // ブロッカーボタンを追加してクリックを捕捉
            Button button = blocker.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(0, 0, 0, 0);
            button.colors = colors;
            
            // ブロッカーキャンバスを最前面に
            Canvas blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            blockerCanvas.sortingOrder = 990; // 通知より下、他のUIより上
            blocker.AddComponent<GraphicRaycaster>();
            
            // ブロッカーを参照に設定
            notificationBlocker = blocker;
        }
        
        // ブロッカーを有効化して最前面に
        notificationBlocker.SetActive(true);
        notificationBlocker.transform.SetAsLastSibling();
        
        // AIの動きを一時停止
        // タイムスケールを変更せずにAIのみ停止する場合はフラグで管理
        if (GameManager.Instance != null && 
            GameManager.Instance.player2 is AIPlayer aiPlayer)
        {
            // AIPlayerにAI行動停止フラグを追加し、それを設定
            // aiPlayer.SetPaused(true);
        }
    }

    private void EnsureNotificationPanelSettings()
    {
        if (notificationPanel == null) return;
        
        // Canvas設定の確認と調整
        Canvas canvas = notificationPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            // 新しいCanvasコンポーネントを追加
            canvas = notificationPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // 最前面に表示されるよう設定
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000; // 非常に高い値に設定
            
            // GraphicRaycasterも追加
            if (notificationPanel.GetComponent<GraphicRaycaster>() == null)
            {
                notificationPanel.AddComponent<GraphicRaycaster>();
            }
            
            Debug.Log("通知パネルのCanvas設定を調整しました");
        }
        else
        {
            // 既存のCanvasのソート順を確実に高くする
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;
        }
        
        // 通知パネルの位置調整 - 画面中央上部に表示
        RectTransform rt = notificationPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.75f);
            rt.anchorMax = new Vector2(0.5f, 0.75f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            
            // サイズ調整も可能
            rt.sizeDelta = new Vector2(700, 180); // 幅と高さを設定
        }
        
        // 背景パネルを追加/調整
        Image bgImage = notificationPanel.GetComponent<Image>();
        if (bgImage == null)
        {
            bgImage = notificationPanel.AddComponent<Image>();
        }
        bgImage.color = new Color(0, 0, 0, 0.8f); // 黒い半透明背景
        
        // テキスト設定の確認
        if (notificationText != null)
        {
            notificationText.color = Color.white;
            notificationText.fontSize = 24;
            notificationText.alignment = TextAlignmentOptions.Center;
        }
    }


    private IEnumerator CloseNotificationAfterDelay(float duration)
    {
        // リアルタイムで待機（タイムスケールが0でも動作）
        yield return new WaitForSecondsRealtime(duration);
        
        // 通知パネルを閉じる
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        // 次の通知があれば処理、なければゲーム再開
        ProcessNextNotification();
    }

    private void InitializeNotificationBlocker()
    {
        // 通知ブロッカーが未設定の場合、作成
        if (notificationBlocker == null)
        {
            // 新しいブロッカーを作成
            GameObject blocker = new GameObject("NotificationBlocker");
            blocker.transform.SetParent(transform, false);
            
            // 全画面をカバーするRectTransformを設定
            RectTransform rectTransform = blocker.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            
            // 透明な背景画像を追加
            Image image = blocker.AddComponent<Image>();
            image.color = new Color(0, 0, 0, 0.01f); // ほぼ透明
            
            // ブロッカーボタンを追加してクリックを捕捉
            Button button = blocker.AddComponent<Button>();
            button.onClick.AddListener(() => { /* クリックを捕捉するだけ */ });
            
            // ブロッカーを参照に設定
            notificationBlocker = blocker;
            
            // 初期状態は非表示
            notificationBlocker.SetActive(false);
        }
    }

    /// <summary>
    /// 通知表示コルーチン
    /// </summary>
    private IEnumerator ShowNotificationRoutine(string message, float duration)
    {
        if (notificationPanel != null && notificationText != null)
        {
            notificationPanel.SetActive(true);
            notificationText.text = message;
            
            yield return new WaitForSeconds(duration);
            
            notificationPanel.SetActive(false);
        }
        else
        {
            Debug.Log("通知: " + message);
            yield return null;
        }
    }
        
    /// <summary>
    /// ゲームモード選択画面から戻ってきたときのUI更新
    /// </summary>
    public void OnGameModeSelected()
    {
        // ゲームモード設定に応じたUI初期化
        GameModeSettings settings = PlayerSelectionManager.SelectedGameMode;
        if (settings != null)
        {
            string modeText = settings.gameMode == GameMode.VsAI ? 
                              "AI対戦" : "ネットワーク対戦";
            
            // 通知表示
            ShowNotification($"{modeText}を開始します", 1f);
            
            // UI全体を更新
            UpdateAllUI();
        }
    }

    /// <summary>
    /// JSONビルダーで作成したUIとの連携
    /// </summary>
    public void ConnectSpellActivationUI()
    {
        // UIビルダーで生成されたUI要素を取得
        Transform spellPanel = GameObject.Find("SpellActivationPanel")?.transform;
        if (spellPanel == null)
        {
            Debug.LogWarning("SpellActivationPanelが見つかりません");
            return;
        }
        
        // 既存UIの非表示
        if (this.spellActivationPanel != null)
        {
            this.spellActivationPanel.SetActive(false);
        }
        
        // 新UIへの参照
        this.spellActivationPanel = spellPanel.gameObject;
        
        // EventDescriptionTextの参照
        this.eventDescriptionText = spellPanel.Find("DialogPanel/EventDescriptionArea/EventDescriptionText")?.GetComponent<TextMeshProUGUI>();
        
        // CardContainerの参照
        this.spellCardContainer = spellPanel.Find("DialogPanel/CardDisplayArea/CardContainer");
        
        // ボタンの参照とイベント設定
        Button activateButton = spellPanel.Find("DialogPanel/ButtonArea/ActivateButton")?.GetComponent<Button>();
        if (activateButton != null)
        {
            activateButton.onClick.RemoveAllListeners();
            activateButton.onClick.AddListener(() => {
                if (activatingPlayer != null && activatableSpells != null && activatableSpells.Count > 0)
                {
                    OnSpellCardSelected(activatableSpells[0]);
                }
            });
        }
        
        Button cancelButton = spellPanel.Find("DialogPanel/ButtonArea/CancelButton")?.GetComponent<Button>();
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnSpellActivationCanceled);
        }
        
        // 初期状態は非表示
        spellPanel.gameObject.SetActive(false);
        
        Debug.Log("スペル発動確認UIの連携が完了しました");
    }

    /// <summary>
    /// 墓地の更新 - 実装
    /// </summary>
    public void UpdateGraveyard(Player player)
    {
        if (player == null) return;
        
        // 墓地テキスト表示の更新
        TextMeshProUGUI graveyardText = (player == gameManager.player1) ? player1GraveyardText : player2GraveyardText;
        
        if (graveyardText != null)
        {
            graveyardText.text = $"墓地: {player.graveyard.Count}";
            Debug.Log($"[UIManager] {player.playerName}の墓地表示を更新: {player.graveyard.Count}枚");
        }
        
        // 墓地の最上部カードを表示する場合（オプション）
        Image graveyardImage = (player == gameManager.player1) ? player1GraveyardImage : player2GraveyardImage;
        
        if (graveyardImage != null && player.graveyard.Count > 0)
        {
            // 最後に墓地に送られたカードを表示
            Card topCard = player.graveyard[player.graveyard.Count - 1];
            
            // カードのアートワークを設定
            if (!string.IsNullOrEmpty(topCard.artwork))
            {
                Sprite cardSprite = LoadCardSprite(topCard);
                if (cardSprite != null)
                {
                    graveyardImage.sprite = cardSprite;
                    graveyardImage.gameObject.SetActive(true);
                }
            }
        }
        else if (graveyardImage != null)
        {
            // 墓地が空の場合は非表示
            graveyardImage.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 手札上限ダイアログを表示
    /// </summary>
    public void ShowHandLimitDialog(Player player, Card newCard)
    {
        if (handLimitPanel == null)
        {
            Debug.LogError("手札上限パネルが設定されていません");
            return;
        }
        
        // ゲーム進行を一時停止
        Time.timeScale = 0;
        
        // 情報を保存
        handLimitPlayer = player;
        handLimitNewCard = newCard;
        selectedCardToDiscard = null; // 選択リセット
        
        // パネルを表示
        handLimitPanel.SetActive(true);
        
        // キャンバスのソート順を高く設定して最前面に表示
        Canvas canvas = handLimitPanel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = handLimitPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            handLimitPanel.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }
        
        // 確認ボタンを初期状態では無効化（カード選択まで）
        if (handLimitConfirmButton != null)
        {
            handLimitConfirmButton.interactable = false;
            handLimitConfirmButton.onClick.RemoveAllListeners();
            handLimitConfirmButton.onClick.AddListener(OnDiscardConfirmButtonClicked);
        }
        
        // 説明テキストを設定
        if (handLimitText != null)
        {
            handLimitText.text = $"手札が上限（{Player.MAX_HAND_SIZE}枚）に達しています。\n捨てるカードを選んでください。\n新しく引いたカード「{newCard.cardName}」を手札に加えるには、\n既存の手札から1枚捨てる必要があります。";
        }
        
        // カード表示コンテナをクリア
        ClearDiscardCardContainer();
        
        // 手札のカードを表示
        DisplayCardsForDiscard(player.hand, newCard);
        
        // ゲーム画面の他の操作を無効化
        DisableOtherInteractions();
        
        // 通知表示
        ShowNotification("手札上限に達しました。カードを1枚選んで捨ててください", 3.0f);
    }

    /// <summary>
    /// 他の操作を無効化
    /// </summary>
    private void DisableOtherInteractions()
    {
        // 終了ボタンなどを無効化
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
        
        // 手札の無効化
        DisableHandInteractions();
        
        // フィールドカードの無効化
        DisableFieldInteractions();
    }

    /// <summary>
    /// 捨てるカード確認ボタンの処理
    /// </summary>
    private void OnDiscardConfirmButtonClicked()
    {
        if (selectedCardToDiscard == null)
        {
            ShowNotification("捨てるカードを選んでください", 1.5f);
            return;
        }
        
        // パネルを非表示
        handLimitPanel.SetActive(false);
        
        // 選択したカードを捨てる
        if (handLimitPlayer != null)
        {
            // 新しく引いたカードを捨てる場合
            if (selectedCardToDiscard == handLimitNewCard)
            {
                handLimitPlayer.graveyard.Add(handLimitNewCard);
                ShowNotification($"新しく引いた「{handLimitNewCard.cardName}」を捨てました", 2.0f);
            }
            else
            {
                // 既存の手札から選んだカードを捨て、新しいカードを手札に加える
                handLimitPlayer.DiscardSpecificCard(selectedCardToDiscard, handLimitNewCard);
                ShowNotification($"「{selectedCardToDiscard.cardName}」を捨て、新しく「{handLimitNewCard.cardName}」を手札に加えました", 3.0f);
            }
            
            // UI更新
            UpdateHand(handLimitPlayer);
            UpdateGraveyard(handLimitPlayer);
        }
        
        // ゲーム進行を再開
        Time.timeScale = 1;
        
        // 他の操作を再有効化
        EnableOtherInteractions();
        
        // 参照をクリア
        handLimitPlayer = null;
        handLimitNewCard = null;
        selectedCardToDiscard = null;
        
        // 表示カードをクリア
        ClearDiscardCardContainer();
    }

    /// <summary>
    /// 他の操作を再有効化
    /// </summary>
    private void EnableOtherInteractions()
    {
        // 終了ボタンを再有効化（ただし条件に応じて）
        UpdateEndTurnButton();
        
        // 手札を再有効化
        if (gameManager.player1 != null)
        {
            UpdatePlayerHand(gameManager.player1);
        }
        
        // フィールドも再有効化
        if (gameManager.player1 != null)
        {
            UpdateCharacterField(gameManager.player1);
        }
    }

    /// <summary>
    /// 表示中のカードをクリア
    /// </summary>
    private void ClearDiscardCardContainer()
    {
        if (handLimitCardContainer == null) return;
        
        foreach (GameObject cardObj in displayedDiscardCards)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
            }
        }
        
        displayedDiscardCards.Clear();
        
        // 子オブジェクトも確実にクリア
        for (int i = handLimitCardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(handLimitCardContainer.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 捨てるためのカードを表示
    /// </summary>
    private void DisplayCardsForDiscard(List<Card> handCards, Card newCard)
    {
        if (handLimitCardContainer == null) return;
        
        // 既存の手札を表示
        float xPosition = -200.0f; // 開始位置
        float cardWidth = 120.0f;  // カード幅
        float spacing = 20.0f;     // カード間隔
        
        // 手札の各カードを表示
        for (int i = 0; i < handCards.Count; i++)
        {
            Card card = handCards[i];
            GameObject cardObj = Instantiate(cardPrefab, handLimitCardContainer);
            displayedDiscardCards.Add(cardObj);
            
            // カード情報を設定
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // カードのアートワークを設定
                Sprite cardSprite = LoadCardSprite(card);
                if (cardSprite != null)
                {
                    cardUI.SetCardArtwork(cardSprite);
                }
                
                // このカードは「既存の手札」として表示
                cardUI.AddLabel("手札");
            }
            
            // クリックイベント設定
            Button cardButton = cardObj.GetComponent<Button>();
            if (cardButton != null)
            {
                Card capturedCard = card; // ラムダ式用にローカル変数
                cardButton.onClick.AddListener(() => OnDiscardCardSelected(capturedCard, false));
            }
            
            // 位置設定
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // 位置調整
                rectTransform.anchoredPosition = new Vector2(xPosition + (i * (cardWidth + spacing)), 0);
                rectTransform.localScale = Vector3.one * 0.7f; // カードを少し小さく
            }
        }
        
        // 新しく引いたカードを表示（特別扱い）
        GameObject newCardObj = Instantiate(cardPrefab, handLimitCardContainer);
        displayedDiscardCards.Add(newCardObj);
        
        // カード情報を設定
        CardUI newCardUI = newCardObj.GetComponent<CardUI>();
        if (newCardUI != null)
        {
            newCardUI.SetupCard(newCard);
            
            // カードのアートワークを設定
            Sprite cardSprite = LoadCardSprite(newCard);
            if (cardSprite != null)
            {
                newCardUI.SetCardArtwork(cardSprite);
            }
            
            // このカードは「新しいカード」として表示
            newCardUI.AddLabel("新規");
        }
        
        // クリックイベント設定
        Button newCardButton = newCardObj.GetComponent<Button>();
        if (newCardButton != null)
        {
            newCardButton.onClick.AddListener(() => OnDiscardCardSelected(newCard, true));
        }
        
        // 位置設定 - 新しいカードは少し離して表示
        RectTransform newCardRect = newCardObj.GetComponent<RectTransform>();
        if (newCardRect != null)
        {
            // 位置調整 - 手札の右側に表示
            float newCardX = xPosition + (handCards.Count * (cardWidth + spacing)) + spacing * 2;
            newCardRect.anchoredPosition = new Vector2(newCardX, 0);
            newCardRect.localScale = Vector3.one * 0.8f; // 少し大きめに表示
        }
    }

    /// <summary>
    /// カードが選択された時の処理
    /// </summary>
    private void OnDiscardCardSelected(Card card, bool isNewCard)
    {
        // 選択状態を更新
        selectedCardToDiscard = card;
        
        // 全てのカードの選択状態を視覚的にリセット
        foreach (GameObject cardObj in displayedDiscardCards)
        {
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null && cardUI.GetCard() != null)
            {
                // 選択状態を解除
                cardUI.SetSelected(false);
                
                // 透明度で選択状態を表現
                if (cardUI.GetCard() == card)
                {
                    // 選択されたカード
                    cardUI.SetSelected(true);
                }
            }
        }
        
        // 選択されたカードの情報を表示
        ShowNotification($"「{card.cardName}」を捨てますか？", 2.0f);
        
        // 確認ボタンを有効化
        if (handLimitConfirmButton != null)
        {
            handLimitConfirmButton.interactable = true;
        }
    }

    /// <summary>
    /// 手札上限ダイアログでカード選択時の処理
    /// </summary>
    private void OnHandLimitCardSelected(Card card)
    {
        if (handLimitPlayer == null) return;
        
        // パネルを非表示
        handLimitPanel.SetActive(false);
        
        // 選択したカードを捨てる
        // 選択したカードが新しく引いたカードの場合
        if (card == handLimitNewCard)
        {
            // 新しく引いたカードを直接墓地へ
            handLimitPlayer.graveyard.Add(card);
            ShowNotification($"{card.cardName}を捨てました", 1.5f);
        }
        else
        {
            // 既存の手札から選んだカードを捨て、新しいカードを手札に加える
            handLimitPlayer.DiscardSpecificCard(card, handLimitNewCard);
            ShowNotification($"{card.cardName}を捨て、{handLimitNewCard.cardName}を手札に加えました", 2.0f);
        }
        
        // UI更新
        UpdateHand(handLimitPlayer);
        UpdateGraveyard(handLimitPlayer);
        
        // 参照をクリア
        handLimitPlayer = null;
        handLimitNewCard = null;
    }

    /// <summary>
    /// 手札上限ダイアログでキャンセル時の処理
    /// </summary>
    private void OnHandLimitCanceled()
    {
        // ランダムにカードを捨てる
        if (handLimitPlayer != null && handLimitPlayer.hand.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, handLimitPlayer.hand.Count);
            Card cardToDiscard = handLimitPlayer.hand[randomIndex];
            
            handLimitPlayer.DiscardSpecificCard(cardToDiscard, handLimitNewCard);
            ShowNotification($"ランダムに{cardToDiscard.cardName}を捨てました", 1.5f);
            
            // UI更新
            UpdateHand(handLimitPlayer);
            UpdateGraveyard(handLimitPlayer);
        }
        
        // パネルを非表示
        handLimitPanel.SetActive(false);
        
        // 参照をクリア
        handLimitPlayer = null;
        handLimitNewCard = null;
    }

    // 1. 通知メッセージを削減するためのフィルタリングメソッド
    private bool ShouldShowNotification(string message, NotificationType type)
    {
        // 通知メッセージを種類ごとに分類
        switch (type)
        {
            case NotificationType.Battle:
            case NotificationType.SpellActivation:
            // case NotificationType.FieldCardActivation:
                // これらの通知は常に表示
                return true;
                
            case NotificationType.GameState:
                // ゲーム状態関連の通知は重要なもののみ表示（勝敗など）
                if (message.Contains("勝利") || message.Contains("敗北") || 
                    message.Contains("ターン開始") || message.Contains("ゲーム終了"))
                    return true;
                return false;
                
            case NotificationType.CardAction:
                // カード操作関連の通知は最小限に
                return false;
                
            case NotificationType.Debug:
                // デバッグ通知は表示しない
                return false;
                
            default:
                // それ以外は警告やエラーなどの重要なもののみ
                return message.Contains("エラー") || message.Contains("警告");
        }
    }

    public void ShowGameStateNotification(string message, float duration)
    {
        ShowNotification(message, duration, NotificationType.GameState);
    }

    public void ShowBattleNotification(string message, float duration)
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            // UIManagerの通知メソッドを呼び出し（NotificationType対応）
            GameManager.Instance.uiManager.ShowNotification(message, duration, NotificationType.Battle);
        }
    }

        public void ShowSpellNotification(string message, float duration)
    {
        ShowNotification(message, duration, NotificationType.SpellActivation);
    }

    // フィールドカード発動の通知（Player.csで使用）
    public void ShowFieldCardNotification(string message, float duration)
    {
        ShowNotification(message, duration, NotificationType.FieldCardActivation);
    }

    // 名前空間を設定（プロジェクトに合わせて調整）

    
        /// <summary>
        /// 通知メッセージの種類を定義する列挙型
        /// </summary>
        public enum NotificationType
        {
            Battle,              // 戦闘関連
            SpellActivation,     // スペルカード発動
            FieldCardActivation, // フィールドカード発動
            GameState,           // ゲーム状態（ターン、フェーズなど）
            CardAction,          // カード操作（ドローなど）
            Debug,               // デバッグ情報
            System               // システム通知（エラーなど）
        }
    
}

