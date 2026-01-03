using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームマネージャークラス（バグ修正済み）
/// </summary>
public partial class GameManager : MonoBehaviour
{
    // シングルトンパターン
    public static GameManager Instance { get; private set; }
    
    [Header("マネージャー参照")]
    public TurnManager turnManager;
    public CardManager cardManager;
    public UIManager uiManager;
    
    [Header("プレイヤー情報")]
    public Player player1;
    public Player player2;
    
    [Header("ゲーム設定")]
    public int initialLifePoints = 10000;
    public int initialHandSize = 5;
    public int turnEnergyGain = 2;
    public int maxCardCopies = 2;
    
    // ゲーム状態
    [HideInInspector]
    public bool isGameStarted = false;
    [HideInInspector]
    public bool isGameOver = false;
    private Player winner = null;
    
    // デバッグ設定
    [Header("デバッグ設定")]
    public bool enableDebugLog = true;
    
    // イベント
    public delegate void GameEvent();
    public event GameEvent OnGameStart;
    public event GameEvent OnGameOver;

    // 先行プレイヤー関連
    [HideInInspector]
    public Player firstPlayer = null;      // 先行プレイヤー

    // ゲームイベント用デリゲート
    public delegate void GameEventHandler(GameEventInfo eventInfo);
    public event GameEventHandler OnGameEvent;

    [HideInInspector]
    public bool isFirstPlayerFirstTurn = false;  // 先行プレイヤーの1ターン目かどうか
    
    // 現在処理中のイベント
    private GameEventInfo currentEvent = null;

    // 先行プレイヤー設定メソッド
    public void SetFirstPlayer(Player player)
    {
        firstPlayer = player;
        isFirstPlayerFirstTurn = true;
        
        Debug.Log($"GameManager: 先行プレイヤーを設定: {firstPlayer.playerName}");
    }
    
    // 先行1ターン目の状態更新
    public void UpdateFirstTurnState(bool isFirstTurn)
    {
        isFirstPlayerFirstTurn = isFirstTurn;
        
        Debug.Log($"GameManager: 先行1ターン目状態: {isFirstPlayerFirstTurn}");
        
        // UIにも状態を通知
        if (uiManager != null)
        {
            uiManager.UpdateFirstTurnState(isFirstPlayerFirstTurn);
        }
    }
    
    /// <summary>
    /// ゲームイベントの発火
    /// </summary>
    public void TriggerGameEvent(GameEventInfo eventInfo)
    {
        if (eventInfo == null)
        {
            Debug.LogError("TriggerGameEvent: eventInfoがnullです");
            return;
        }
        
        if (enableDebugLog) Debug.Log($"ゲームイベント発生: {eventInfo.eventType}");
        
        // 現在のイベントを保存
        currentEvent = eventInfo;
        
        // イベント発火前のスペルカード発動確認
        CheckSpellActivation(eventInfo);
        
        // イベントの通知
        OnGameEvent?.Invoke(eventInfo);
        
        // 現在のイベントをクリア
        currentEvent = null;
    }
    
    /// <summary>
    /// スペルカード発動確認 - 修正版
    /// </summary>
    private void CheckSpellActivation(GameEventInfo eventInfo)
    {
        if (eventInfo == null || eventInfo.activePlayer == null) return;
        
        // アクティブプレイヤーとその対戦相手
        Player activePlayer = eventInfo.activePlayer;
        Player waitingPlayer = GetOpponent(activePlayer);
        
        if (waitingPlayer == null)
        {
            Debug.LogError("CheckSpellActivation: 対戦相手がnullです");
            return;
        }
        
        // デバッグログ
        Debug.Log($"[GameManager] スペル発動確認: イベント={eventInfo.eventType}, アクティブプレイヤー={activePlayer.playerName}, 待機プレイヤー={waitingPlayer.playerName}");
        
        // 相手ターンに発動できるスペルカードの確認
        List<SpellCard> activatableSpells = GetActivatableSpells(waitingPlayer, eventInfo);
        
        Debug.Log($"[GameManager] 発動可能なスペル: {activatableSpells.Count}枚");
        
        if (activatableSpells.Count > 0)
        {
            // 発動可能なスペルの情報をログ出力
            foreach (SpellCard spell in activatableSpells)
            {
                Debug.Log($"[GameManager] 発動可能スペル: {spell.cardName}, コスト: {spell.cost}, 説明: {spell.description}");
            }
            
            // AIプレイヤーの場合
            if (waitingPlayer is AIPlayer)
            {
                // AIは条件を満たしていれば自動的にスペルを使用
                SpellCard spellToUse = ChooseBestSpell(activatableSpells, eventInfo);
                if (spellToUse != null)
                {
                    Debug.Log($"[GameManager] AIがスペルを選択: {spellToUse.cardName}");
                    ActivateSpell(waitingPlayer, spellToUse, eventInfo);
                }
            }
            else
            {
                // 人間プレイヤーの場合、発動選択UIを表示
                if (uiManager != null)
                {
                    Debug.Log($"[GameManager] プレイヤーにスペル発動選択UIを表示");
                    uiManager.ShowSpellActivationPrompt(waitingPlayer, activatableSpells, eventInfo);
                }
                else
                {
                    Debug.LogError("CheckSpellActivation: uiManagerがnullです");
                }
            }
        }
    }
    
    /// <summary>
    /// 発動可能なスペルカードを取得
    /// </summary>
    private List<SpellCard> GetActivatableSpells(Player player, GameEventInfo eventInfo)
    {
        List<SpellCard> activatableSpells = new List<SpellCard>();
        
        if (player == null || player.hand == null) return activatableSpells;
        
        // 手札のスペルカードをチェック
        foreach (Card card in player.hand)
        {
            if (card is SpellCard spellCard && 
                player.energy >= card.cost && 
                spellCard.canActivateOnOpponentTurn &&
                spellCard.CanActivateOn(eventInfo.eventType))
            {
                activatableSpells.Add(spellCard);
            }
        }
        
        return activatableSpells;
    }
    
    /// <summary>
    /// AIが最適なスペルを選択
    /// </summary>
    private SpellCard ChooseBestSpell(List<SpellCard> spells, GameEventInfo eventInfo)
    {
        if (spells == null || spells.Count == 0) return null;
        
        // 単純な戦略: 最もコストが高いスペルを選択
        SpellCard bestSpell = null;
        int highestCost = -1;
        
        foreach (SpellCard spell in spells)
        {
            if (spell.cost > highestCost)
            {
                highestCost = spell.cost;
                bestSpell = spell;
            }
        }
        
        return bestSpell;
    }
    
    /// <summary>
    /// スペルカードの発動
    /// </summary>
    public void ActivateSpell(Player player, SpellCard spell, GameEventInfo triggeringEvent)
    {
        if (player == null || spell == null || triggeringEvent == null)
        {
            Debug.LogError("ActivateSpell: パラメータがnullです");
            return;
        }
        
        if (enableDebugLog) Debug.Log($"{player.playerName}が{spell.cardName}を発動（相手ターン）");
        
        // スペルカードをプレイ
        player.PlayCard(spell);
        
        // スペル発動イベントを通知
        GameEventInfo spellEvent = new GameEventInfo(
            GameEventType.SpellActivated,
            player,
            triggeringEvent.activePlayer,
            spell,
            triggeringEvent.sourceCard
        );
        
        // 新しいイベントを発火（ただし無限ループに注意）
        OnGameEvent?.Invoke(spellEvent);
    }
    
    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 各マネージャーの初期化
        InitializeManagers();
        
        // UI初期化
        if (uiManager != null)
        {
            uiManager.UpdateAllUI();
        }
        else
        {
            Debug.LogWarning("Start: uiManagerがnullです");
        }
    }
    
    /// <summary>
    /// ゲーム開始処理
    /// </summary>
    public void StartGame()
    {
        if (isGameStarted)
        {
            Debug.LogWarning("StartGame: ゲームはすでに開始されています");
            return;
        }
        
        if (enableDebugLog) Debug.Log("GameManager: ゲーム開始処理を実行");
        isGameStarted = true;
        isGameOver = false;
        winner = null;
        
        // マネージャーの初期化
        InitializeManagers();
        
        // プレイヤー初期化
        InitializePlayers();
        
        // 初期手札を引く
        for (int i = 0; i < initialHandSize; i++)
        {
            player1.DrawCard();
            player2.DrawCard();
        }
        
        // UI更新
        if (uiManager != null)
        {
            if (enableDebugLog) Debug.Log("UI更新を実行");
            uiManager.UpdateAllUI();
        }
        else
        {
            Debug.LogError("StartGame: uiManagerがnullです");
        }
        
        // ターン開始
        if (turnManager != null)
        {
            if (enableDebugLog) Debug.Log("ターンマネージャーでゲーム開始");
            turnManager.StartGame();
        }
        else
        {
            Debug.LogError("StartGame: turnManagerがnullです");
        }
        
        // イベント発火
        OnGameStart?.Invoke();
        
        // ゲーム状態をログに出力
        if (enableDebugLog) LogGameState();
    }
    
    /// <summary>
    /// マネージャー初期化
    /// </summary>
    private void InitializeManagers()
    {
        if (enableDebugLog) Debug.Log("マネージャー初期化");
        
        // 参照未設定の場合は自動取得
        if (turnManager == null) 
        {
            turnManager = GetComponentInChildren<TurnManager>();
            if (turnManager == null)
            {
                Debug.LogWarning("TurnManagerが見つかりません。新規作成します。");
                GameObject tmObj = new GameObject("TurnManager");
                tmObj.transform.SetParent(transform);
                turnManager = tmObj.AddComponent<TurnManager>();
            }
        }
            
        if (cardManager == null)
        {
            cardManager = GetComponentInChildren<CardManager>();
            if (cardManager == null)
            {
                Debug.LogWarning("CardManagerが見つかりません。新規作成します。");
                GameObject cmObj = new GameObject("CardManager");
                cmObj.transform.SetParent(transform);
                cardManager = cmObj.AddComponent<CardManager>();
            }
        }
            
        if (uiManager == null)
        {
            uiManager = Object.FindFirstObjectByType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("UIManagerが見つかりません。新規作成します。");
                GameObject uiObj = new GameObject("UIManager");
                uiObj.transform.SetParent(transform);
                uiManager = uiObj.AddComponent<UIManager>();
            }
        }
        
        // CardManagerのデータベースロード
        if (cardManager != null && cardManager.cardDatabase == null)
        {
            cardManager.LoadCardDatabase();
        }
    }

    /// <summary>
    /// プレイヤー初期化
    /// </summary>
    private void InitializePlayers()
    {
        if (enableDebugLog) Debug.Log("プレイヤー初期化");
        
        // プレイヤーオブジェクトがない場合は作成
        if (player1 == null)
        {
            GameObject p1Obj = new GameObject("Player1");
            player1 = p1Obj.AddComponent<Player>();
            player1.playerName = "プレイヤー1";
        }
        
        if (player2 == null)
        {
            GameObject p2Obj = new GameObject("Player2");
            player2 = p2Obj.AddComponent<Player>();
            player2.playerName = "プレイヤー2";
        }
        
        // プレイヤー情報をリセット
        player1.InitializePlayer(initialLifePoints);
        player2.InitializePlayer(initialLifePoints);
        
        // デッキを直接初期化（CardManagerではなく直接デッキIDから取得）
        // CardManagerからデッキデータを取得する代わりに、DeckDataManagerから直接取得
        DeckDataManager deckManager = FindFirstObjectByType<DeckDataManager>();
        if (deckManager != null)
        {
            // プレイヤー1のデッキを準備
            int player1DeckId = PlayerSelectionManager.SelectedGameMode?.player1DeckId ?? 2;
            DeckData player1DeckData = deckManager.GetDeckById(player1DeckId);
            if (player1DeckData != null)
            {
                LoadDeckCards(player1, player1DeckData);
            }
            
            // プレイヤー2のデッキを準備
            int player2DeckId = 3; // AIプレイヤーはデッキID=3を使用
            DeckData player2DeckData = deckManager.GetDeckById(player2DeckId);
            if (player2DeckData != null)
            {
                LoadDeckCards(player2, player2DeckData);
            }
        }
    }

    // 直接デッキカードをロードするヘルパーメソッド
    private void LoadDeckCards(Player player, DeckData deckData)
    {
        if (player == null || deckData == null) return;
        
        // カードデータベースをロード
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        if (database == null)
        {
            Debug.LogError("カードデータベースのロードに失敗しました");
            return;
        }
        
        // デッキをクリア
        player.deck.Clear();
        
        // 各カードIDに対して処理
        foreach (int cardId in deckData.cardIds)
        {
            // データベースからカードを取得
            Card cardTemplate = database.GetCardById(cardId);
            if (cardTemplate != null)
            {
                // カードの複製を作成（ScriptableObjectのコピー）
                Card cardCopy = null;
                
                // カードタイプに応じた正しいインスタンス化
                if (cardTemplate is CharacterCard)
                {
                    CharacterCard charTemplate = cardTemplate as CharacterCard;
                    CharacterCard charCopy = ScriptableObject.CreateInstance<CharacterCard>();
                    
                    // 基本プロパティをコピー
                    charCopy.id = charTemplate.id;
                    charCopy.cardName = charTemplate.cardName;
                    charCopy.description = charTemplate.description;
                    charCopy.cost = charTemplate.cost;
                    charCopy.type = CardType.Character;
                    
                    // artwork を文字列としてコピー
                    charCopy.artwork = charTemplate.artwork;
                    
                    // CharacterCard固有のプロパティ
                    charCopy.attackPower = charTemplate.attackPower;
                    charCopy.defensePower = charTemplate.defensePower;
                    charCopy.element = charTemplate.element;
                    
                    // カテゴリーのコピー
                    charCopy.categories = new List<CardCategory>();
                    if (charTemplate.categories != null)
                    {
                        foreach (var category in charTemplate.categories)
                        {
                            if (category != null)
                                charCopy.categories.Add(category);
                        }
                    }
                    
                    // 効果のコピー
                    charCopy.effects = new List<CardEffect>();
                    if (charTemplate.effects != null)
                    {
                        foreach (var effect in charTemplate.effects)
                        {
                            if (effect != null)
                                charCopy.effects.Add(effect);
                        }
                    }
                    
                    cardCopy = charCopy;
                }
                else if (cardTemplate is SpellCard)
                {
                    SpellCard spellTemplate = cardTemplate as SpellCard;
                    SpellCard spellCopy = ScriptableObject.CreateInstance<SpellCard>();
                    
                    // 基本プロパティをコピー
                    spellCopy.id = spellTemplate.id;
                    spellCopy.cardName = spellTemplate.cardName;
                    spellCopy.description = spellTemplate.description;
                    spellCopy.cost = spellTemplate.cost;
                    spellCopy.type = CardType.Spell;
                    
                    // artwork を文字列としてコピー
                    spellCopy.artwork = spellTemplate.artwork;
                    
                    // SpellCard固有のプロパティ
                    spellCopy.canActivateOnOpponentTurn = spellTemplate.canActivateOnOpponentTurn;
                    
                    // 効果のコピー
                    spellCopy.effects = new List<CardEffect>();
                    if (spellTemplate.effects != null)
                    {
                        foreach (var effect in spellTemplate.effects)
                        {
                            if (effect != null)
                                spellCopy.effects.Add(effect);
                        }
                    }
                    
                    cardCopy = spellCopy;
                }
                else if (cardTemplate is FieldCard)
                {
                    FieldCard fieldTemplate = cardTemplate as FieldCard;
                    FieldCard fieldCopy = ScriptableObject.CreateInstance<FieldCard>();
                    
                    // 基本プロパティをコピー
                    fieldCopy.id = fieldTemplate.id;
                    fieldCopy.cardName = fieldTemplate.cardName;
                    fieldCopy.description = fieldTemplate.description;
                    fieldCopy.cost = fieldTemplate.cost;
                    fieldCopy.type = CardType.Field;
                    
                    // artwork を文字列としてコピー
                    fieldCopy.artwork = fieldTemplate.artwork;
                    
                    // フィールドカード固有のプロパティをコピー
                    fieldCopy.affectsOwnField = fieldTemplate.affectsOwnField;
                    fieldCopy.affectsOpponentField = fieldTemplate.affectsOpponentField;
                    fieldCopy.modifiesStats = fieldTemplate.modifiesStats;
                    fieldCopy.attackModifier = fieldTemplate.attackModifier;
                    fieldCopy.defenseModifier = fieldTemplate.defenseModifier;
                    
                    // その他のプロパティ
                    fieldCopy.allowsDeckSearch = fieldTemplate.allowsDeckSearch;
                    fieldCopy.allowsGraveyardRecovery = fieldTemplate.allowsGraveyardRecovery;
                    fieldCopy.revealOpponentHand = fieldTemplate.revealOpponentHand;
                    fieldCopy.preventBattleDestruction = fieldTemplate.preventBattleDestruction;
                    fieldCopy.preventSpellDestruction = fieldTemplate.preventSpellDestruction;
                    fieldCopy.providesLifeRecovery = fieldTemplate.providesLifeRecovery;
                    fieldCopy.lifeRecoveryAmount = fieldTemplate.lifeRecoveryAmount;
                    
                    // カテゴリーのコピー
                    fieldCopy.affectedCategories = new List<CardCategory>();
                    if (fieldTemplate.affectedCategories != null)
                    {
                        foreach (var category in fieldTemplate.affectedCategories)
                        {
                            if (category != null)
                                fieldCopy.affectedCategories.Add(category);
                        }
                    }
                    
                    // 属性のコピー
                    fieldCopy.affectedElements = new List<ElementType>();
                    if (fieldTemplate.affectedElements != null)
                    {
                        foreach (var element in fieldTemplate.affectedElements)
                        {
                            fieldCopy.affectedElements.Add(element);
                        }
                    }
                    
                    // 効果のコピー
                    fieldCopy.effects = new List<CardEffect>();
                    if (fieldTemplate.effects != null)
                    {
                        foreach (var effect in fieldTemplate.effects)
                        {
                            if (effect != null)
                                fieldCopy.effects.Add(effect);
                        }
                    }
                    
                    cardCopy = fieldCopy;
                }
                
                // デッキに追加
                if (cardCopy != null)
                {
                    player.deck.Add(cardCopy);
                }
            }
        }
        
        // デッキをシャッフル
        ShuffleDeck(player.deck);
        
        Debug.Log($"{player.playerName}のデッキを読み込みました: {player.deck.Count}枚");
    }

    // デッキをシャッフル
    private void ShuffleDeck(List<Card> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
    
    /// <summary>
    /// 勝利条件チェック（特殊勝利条件を含む）
    /// </summary>
    public void CheckVictoryConditions()
    {
        if (player1 == null || player2 == null) return;
        
        // ライフポイントによる勝利/敗北
        if (player1.lifePoints <= 0)
        {
            EndGame(player2, "ライフポイントが0になりました");
            return;
        }
        else if (player2.lifePoints <= 0)
        {
            EndGame(player1, "ライフポイントが0になりました");
            return;
        }
        
        // デッキ枯渇による勝利/敗北
        if (player1.deck.Count <= 0 && !player1.CanDrawCard())
        {
            EndGame(player2, "デッキが0枚になりました");
            return;
        }
        else if (player2.deck.Count <= 0 && !player2.CanDrawCard())
        {
            EndGame(player1, "デッキが0枚になりました");
            return;
        }
        
        // 特殊勝利条件のチェック
        // これはPlayerのCheckFieldEffectsメソッドが各カードの
        // SpecialVictoryEffectをチェックするため、そちらに任せる
    }
        
    /// <summary>
    /// ゲーム終了処理（理由を表示）- 音楽変更・シーン遷移ボタン追加
    /// </summary>
    public void EndGame(Player winningPlayer, string reason = "")
    {
        if (isGameOver) return;
        
        isGameOver = true;
        winner = winningPlayer;
        
        if (enableDebugLog) Debug.Log($"[GameManager] {winningPlayer.playerName}の勝利！ 理由: {reason}");
        
        // 勝利/敗北BGMを再生
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            // プレイヤー1が勝利した場合は勝利BGM、そうでなければ敗北BGM
            if (winningPlayer == player1)
            {
                audioManager.PlayMusic("VictoryBGM");
            }
            else
            {
                audioManager.PlayMusic("DefeatBGM");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] AudioManagerが見つかりません");
        }
        
        // UI表示
        if (uiManager != null)
        {
            uiManager.ShowGameOverScreen(winningPlayer, reason);
        }
        else
        {
            Debug.LogError("[GameManager] EndGame: uiManagerがnullです");
        }
        
        // イベント発火
        OnGameOver?.Invoke();
    }

    /// <summary>
    /// 対戦相手の取得
    /// </summary>
    public Player GetOpponent(Player player)
    {
        if (player == null) return null;
        return player == player1 ? player2 : player1;
    }

    /// <summary>
    /// メインゲームシーンに移動
    /// </summary>
    public void GoToMainGame()
    {
        SceneManager.LoadScene("MainGameScene");
    }

    /// <summary>
    /// カードをプレイする処理（プレイヤーに委譲）
    /// </summary>
    public void PlayCard(Card card, Player player, Player opponent, int? fieldPosition = null)
    {
        if (card == null || player == null || opponent == null)
        {
            Debug.LogError("PlayCard: パラメータがnullです");
            return;
        }
        
        // エナジーコストの確認
        int costToPlay = card is CharacterCard charCard ? charCard.effectiveCost : card.cost;
        
        if (player.energy < costToPlay)
        {
            Debug.LogWarning("エナジーが足りません");
            return;
        }
        
        // プレイヤーにカードプレイを委譲
        bool success = player.PlayCard(card, fieldPosition);
        
        if (success)
        {
            // UI更新
            if (uiManager != null)
            {
                uiManager.UpdateAllUI();
            }
            else
            {
                Debug.LogError("PlayCard: uiManagerがnullです");
            }
            
            // フィールド効果のチェック
            player.CheckFieldEffects(opponent);
        }
    }

    /// <summary>
    /// TurnManagerからの通知を処理するメソッド
    /// </summary>
    public void HandleTurnStart(Player player, Player opponent)
    {
        if (player == null || opponent == null)
        {
            Debug.LogError("HandleTurnStart: プレイヤーがnullです");
            return;
        }
        
        if (enableDebugLog) Debug.Log($"{player.playerName}のターン開始の処理を実行");
        
        // プレイヤーのターン開始処理
        player.StartNewTurn();
        
        // ターンごとのエナジー増加
        player.ChangeEnergy(turnEnergyGain);
        
        // フィールドカードのターン使用フラグをリセット
        if (player.activeFieldCards != null)
        {
            foreach (FieldCard card in player.activeFieldCards)
            {
                if (card != null) card.ResetTurnUsage();
            }
        }
        
        // フィールド効果のチェック
        player.CheckFieldEffects(opponent);
        
        // UI更新
        if (uiManager != null)
        {
            uiManager.ShowTurnStartNotification(player);
            uiManager.UpdateAllUI();
        }
        else
        {
            Debug.LogError("HandleTurnStart: uiManagerがnullです");
        }
    }
    
    /// <summary>
    /// ゲーム状態をログに出力（デバッグ用）
    /// </summary>
    public void LogGameState()
    {
        Debug.Log($"===== ゲーム状態 =====");
        Debug.Log($"ゲーム開始済み: {isGameStarted}, ゲーム終了: {isGameOver}");
        Debug.Log($"プレイヤー1: {(player1 != null ? player1.playerName : "なし")}");
        Debug.Log($"プレイヤー2: {(player2 != null ? player2.playerName : "なし")}");
        
        if (player1 != null)
        {
            Debug.Log($"プレイヤー1 ライフ: {player1.lifePoints}, エナジー: {player1.energy}, デッキ: {player1.deck.Count}枚, 手札: {player1.hand.Count}枚");
        }
        
        if (player2 != null)
        {
            Debug.Log($"プレイヤー2 ライフ: {player2.lifePoints}, エナジー: {player2.energy}, デッキ: {player2.deck.Count}枚, 手札: {player2.hand.Count}枚");
        }
        
        if (turnManager != null)
        {
            Debug.Log($"現在のフェイズ: {turnManager.currentPhase}, 現在のプレイヤー: {(turnManager.currentPlayer != null ? turnManager.currentPlayer.playerName : "なし")}");
        }
        
        Debug.Log($"=================");
    }
}