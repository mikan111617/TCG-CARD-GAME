using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AI対戦のセットアップを行うクラス - バグ修正済み
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("プレイヤー設定")]
    public GameObject aiPlayerPrefab;         // AIプレイヤープレハブ
    public GameObject networkPlayerPrefab;    // ネットワークプレイヤープレハブ
    
    [Header("デバッグ設定")]
    public bool logDebugInfo = true;           // デバッグ情報の出力
    public float gameStartDelay = 0.5f;        // ゲーム開始遅延時間
    
    private void Start()
    {
        if (logDebugInfo) Debug.Log("GameInitializer Start()開始");
        
        // ゲームモード設定の取得
        GameModeSettings settings = PlayerSelectionManager.SelectedGameMode;
        
        if (settings == null)
        {
            Debug.LogWarning("ゲームモード設定が見つかりません。デフォルト設定を使用します。");
            settings = new GameModeSettings
            {
                gameMode = GameMode.VsAI,
                player1DeckId = 0,
                aiProfileId = 0 // 最初のAIプロファイル
            };
        }
        else if (logDebugInfo)
        {
            Debug.Log($"ゲームモード設定を取得しました: モード={settings.gameMode}, デッキID={settings.player1DeckId}, AIプロファイルID={settings.aiProfileId}");
        }
        
        // ゲームマネージャーの取得
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("GameManagerが見つかりません");
            return;
        }
        
        // ゲームモードに応じたセットアップ
        switch (settings.gameMode)
        {
            case GameMode.VsAI:
                if (logDebugInfo) Debug.Log("AI対戦モードでセットアップします");
                SetupAIGame(gameManager, settings);
                break;
                
            case GameMode.VsNetwork:
                if (logDebugInfo) Debug.Log("ネットワーク対戦モードでセットアップします");
                SetupNetworkGame(gameManager, settings);
                break;
        }
        
        // プレイヤー1のデッキを明示的に設定
        SetupPlayerDeck(gameManager.player1, settings.player1DeckId);
        
        // ゲーム開始
        if (logDebugInfo) Debug.Log("ゲーム開始処理を呼び出します");
        StartCoroutine(DelayedGameStart(gameManager));
    }
    
    /// <summary>
    /// ゲーム開始を遅延させ、セットアップと初期化を確認するコルーチン
    /// </summary>
    private IEnumerator DelayedGameStart(GameManager gameManager)
    {
        yield return new WaitForSeconds(gameStartDelay);
        
        // 開始前の詳細な状態チェック
        if (gameManager.player1 == null || gameManager.player2 == null)
        {
            Debug.LogError("プレイヤーが正しく設定されていません - ゲーム開始をスキップします");
            yield break;
        }
        
        // デッキエラーを詳細に報告
        if (gameManager.player1.deck.Count == 0)
        {
            Debug.LogError("プレイヤー1のデッキが空です！緊急措置としてバックアップデッキを作成します");
        }
        
        if (gameManager.player2.deck.Count == 0)
        {
            Debug.LogError("プレイヤー2のデッキが空です！緊急措置としてバックアップデッキを作成します");
        }
        
        if (logDebugInfo)
        {
            LogDetailedGameState(gameManager);
        }
        
        // ゲーム開始
        Debug.Log("ゲーム開始シーケンス実行");
        gameManager.StartGame();
        
        // UI更新を確実に行う
        yield return new WaitForSeconds(0.2f);
        if (gameManager.uiManager != null)
        {
            gameManager.uiManager.UpdateAllUI();
        }
        
        if (logDebugInfo) LogGameStatus(gameManager);
    }
    
    /// <summary>
    /// AI対戦のセットアップ
    /// </summary>
    private void SetupAIGame(GameManager gameManager, GameModeSettings settings)
    {
        // AIプロファイルをロード
        AIPlayerProfile aiProfile = GetAIProfileById(settings.aiProfileId);
        
        // AIプロファイルが見つからない場合はデフォルト設定
        if (aiProfile == null)
        {
            Debug.LogWarning($"AIプロファイルID {settings.aiProfileId} が見つかりません。デフォルト設定を使用します。");
            aiProfile = new AIPlayerProfile
            {
                name = "標準AI",
                difficulty = AIDifficulty.Normal,
                deckId = 0,
                description = "標準的なAI"
            };
        }
        
        if (logDebugInfo) Debug.Log($"AIプロファイル: {aiProfile.name}, 難易度: {aiProfile.difficulty}, デッキID: {aiProfile.deckId}");
        
        // 既存のPlayer2を削除
        if (gameManager.player2 != null)
        {
            Destroy(gameManager.player2.gameObject);
        }
        
        // AIプレイヤーを生成
        GameObject aiPlayerObj = null;
        if (aiPlayerPrefab != null)
        {
            if (logDebugInfo) Debug.Log("AIPlayerプレハブからAIを生成します");
            aiPlayerObj = Instantiate(aiPlayerPrefab);
            aiPlayerObj.name = "AIPlayer";
        }
        else
        {
            Debug.LogWarning("AIPlayerプレハブが設定されていません。スクリプトから生成します。");
            aiPlayerObj = new GameObject("AIPlayer");
            aiPlayerObj.AddComponent<AIPlayer>();
        }
        
        // AIプレイヤーの設定
        AIPlayer aiPlayer = aiPlayerObj.GetComponent<AIPlayer>();
        if (aiPlayer != null)
        {
            // プロファイルに基づいて設定
            aiPlayer.playerName = aiProfile.name;
            
            // GameManagerに設定
            gameManager.player2 = aiPlayer;
            
            // AIのデッキを設定
            SetupAIDeck(aiPlayer, aiProfile.deckId);
        }
        else
        {
            Debug.LogError("AIPlayerコンポーネントを取得できませんでした");
        }
    }
    
    /// <summary>
    /// ネットワーク対戦のセットアップ
    /// </summary>
    private void SetupNetworkGame(GameManager gameManager, GameModeSettings settings)
    {
        // 既存のPlayer2を削除
        if (gameManager.player2 != null)
        {
            Destroy(gameManager.player2.gameObject);
        }
        
        // ネットワークプレイヤーを生成
        GameObject networkPlayerObj;
        if (networkPlayerPrefab != null)
        {
            networkPlayerObj = Instantiate(networkPlayerPrefab);
            networkPlayerObj.name = "NetworkPlayer";
        }
        else
        {
            networkPlayerObj = new GameObject("NetworkPlayer");
            networkPlayerObj.AddComponent<Player>();
            // ネットワーク関連コンポーネントを追加（必要に応じて）
        }
        
        // ネットワークプレイヤーの設定
        Player networkPlayer = networkPlayerObj.GetComponent<Player>();
        networkPlayer.playerName = "対戦相手";
        
        // GameManagerに設定
        gameManager.player2 = networkPlayer;
        
        // 仮のデッキを設定（実際はネットワーク経由でデータを受け取る）
        SetupPlayerDeck(networkPlayer, 0);
        
        if (logDebugInfo) Debug.Log("ネットワーク対戦をセットアップしました");
    }
    
    // SetupPlayerDeckメソッドの修正
    private void SetupPlayerDeck(Player player, int deckId)
    {
        if (player == null)
        {
            Debug.LogError("プレイヤーがnullです。デッキを設定できません。");
            return;
        }
        
        Debug.Log($"【重要】{player.playerName}のデッキセットアップを開始します。要求デッキID: {deckId}");
        
        // デッキを一度クリア
        player.deck.Clear();
        
        // DeckDataManagerからデッキをロード
        if (DeckDataManager.Instance != null)
        {
            // 利用可能なデッキ一覧を取得
            List<DeckData> allDecks = DeckDataManager.Instance.GetAllDecks();
            Debug.Log($"利用可能なデッキ: {(allDecks != null ? allDecks.Count : 0)}個");
            
            if (allDecks != null && allDecks.Count > 0)
            {
                foreach (DeckData deck in allDecks)
                {
                    Debug.Log($"- デッキ: ID={deck.deckId}, 名前={deck.deckName}, カード数={deck.cardIds.Count}");
                }
            }
            
            // deckIdの妥当性チェック
            bool validDeckId = false;
            if (allDecks != null)
            {
                foreach (DeckData deck in allDecks)
                {
                    if (deck.deckId == deckId)
                    {
                        validDeckId = true;
                        Debug.Log($"有効なデッキID {deckId} が見つかりました: {deck.deckName}");
                        break;
                    }
                }
            }
            
            // 無効なデッキIDの場合は最初のデッキを使用
            if (!validDeckId)
            {
                Debug.LogWarning($"デッキID {deckId} は無効です。最初のデッキを使用します。");
                if (allDecks != null && allDecks.Count > 0)
                {
                    deckId = allDecks[0].deckId;
                    Debug.Log($"代替デッキID: {deckId}, 名前: {allDecks[0].deckName}");
                }
            }
            
            // デッキデータを取得
            DeckData deckData = DeckDataManager.Instance.GetDeckById(deckId);
            if (deckData != null)
            {
                Debug.Log($"デッキデータ取得成功: ID={deckData.deckId}, 名前={deckData.deckName}, カード数={deckData.cardIds.Count}");
                
                // CardDatabaseの参照を取得
                CardDatabase cardDB = Resources.Load<CardDatabase>("CardDatabase");
                if (cardDB == null)
                {
                    Debug.LogError("CardDatabaseの読み込みに失敗しました");
                }
                
                // カードを直接生成
                foreach (int cardId in deckData.cardIds)
                {
                    Card card = cardDB.GetCardById(cardId);
                    if (card != null)
                    {
                        try
                        {
                            Card cardCopy = UnityEngine.Object.Instantiate(card);
                            player.deck.Add(cardCopy);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"カードID {cardId} のインスタンス化に失敗: {e.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"カードID {cardId} がデータベースに見つかりません");
                    }
                }
                
                // デッキをシャッフル
                ShuffleDeck(player.deck);
                
                Debug.Log($"{player.playerName}のデッキセットアップ完了: {player.deck.Count}枚");
                
                // デッキが空の場合は緊急デッキを生成
                if (player.deck.Count == 0)
                {
                    Debug.LogError("デッキが空なので緊急デッキを生成します");
                }
            }
            else
            {
                Debug.LogError($"デッキID {deckId} のデータが取得できませんでした");
            }
        }
        Debug.LogError("デッキの読み込みに失敗したため、緊急デッキを生成します");
    }
    
    /// <summary>
    /// AIのデッキを設定
    /// </summary>
    private void SetupAIDeck(AIPlayer aiPlayer, int deckId)
    {
        if (logDebugInfo) Debug.Log($"AIのデッキをセットアップします。デッキID: {deckId}");
        
        // 通常のデッキセットアップを使用
        SetupPlayerDeck(aiPlayer, deckId);
    }
    
    /// <summary>
    /// デッキをシャッフル
    /// </summary>
    private void ShuffleDeck(List<Card> deck)
    {
        if (deck == null || deck.Count == 0) return;
        
        // Fisher-Yatesアルゴリズムでデッキをシャッフル
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
    
    /// <summary>
    /// AIプロファイルIDからプロファイルを取得
    /// </summary>
    private AIPlayerProfile GetAIProfileById(int profileId)
    {
        // Resources内のJSON読み込み
        TextAsset profilesAsset = Resources.Load<TextAsset>("AIPlayerProfiles");
        if (profilesAsset != null)
        {
            try
            {
                AIPlayerProfileList profileList = JsonUtility.FromJson<AIPlayerProfileList>(profilesAsset.text);
                if (profileList != null && profileList.profiles != null && profileId >= 0 && profileId < profileList.profiles.Count)
                {
                    return profileList.profiles[profileId];
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AIプロファイル読み込みエラー: {e.Message}");
            }
        }
        
        Debug.LogWarning("AIPlayerProfiles.jsonが見つからないか、プロファイルが読み込めません。デフォルトAIプロファイルを作成します。");
        // デフォルトAIプロファイルを返す
        return CreateDefaultAIProfile();
    }
    
    /// <summary>
    /// デフォルトAIプロファイルを作成
    /// </summary>
    private AIPlayerProfile CreateDefaultAIProfile()
    {
        return new AIPlayerProfile
        {
            name = "標準AI",
            difficulty = AIDifficulty.Normal,
            deckId = 0,
            description = "標準的なAI対戦相手"
        };
    }
    
    /// <summary>
    /// ゲーム状態をログに出力
    /// </summary>
    private void LogGameStatus(GameManager gameManager)
    {
        Debug.Log($"===== ゲーム状態 =====");
        Debug.Log($"ゲーム開始済み: {gameManager.isGameStarted}, ゲーム終了: {gameManager.isGameOver}");
        Debug.Log($"プレイヤー1: {(gameManager.player1 != null ? gameManager.player1.playerName : "なし")}");
        Debug.Log($"プレイヤー2: {(gameManager.player2 != null ? gameManager.player2.playerName : "なし")}");
        
        if (gameManager.player1 != null)
        {
            Debug.Log($"プレイヤー1 ライフ: {gameManager.player1.lifePoints}, エナジー: {gameManager.player1.energy}, デッキ: {gameManager.player1.deck.Count}枚, 手札: {gameManager.player1.hand.Count}枚");
        }
        
        if (gameManager.player2 != null)
        {
            Debug.Log($"プレイヤー2 ライフ: {gameManager.player2.lifePoints}, エナジー: {gameManager.player2.energy}, デッキ: {gameManager.player2.deck.Count}枚, 手札: {gameManager.player2.hand.Count}枚");
        }
        
        if (gameManager.turnManager != null)
        {
            Debug.Log($"現在のフェーズ: {gameManager.turnManager.currentPhase}, 現在のプレイヤー: {(gameManager.turnManager.currentPlayer != null ? gameManager.turnManager.currentPlayer.playerName : "なし")}");
        }
        
        Debug.Log($"=================");
    }

    // 詳細なゲーム状態のログ出力
    private void LogDetailedGameState(GameManager gameManager)
    {
        Debug.Log("========= 詳細なゲーム状態ログ =========");
        
        // CardDatabaseの状態
        CardDatabase cardDB = Resources.Load<CardDatabase>("CardDatabase");
        Debug.Log($"CardDatabase: {(cardDB != null ? "ロード成功" : "ロード失敗")}");
        
        // CardManagerのデータベース状態
        CardManager cardManager = FindFirstObjectByType<CardManager>();
        if (cardManager != null)
        {
            Debug.Log($"CardManager: {(cardManager.cardDatabase != null ? "データベース存在" : "データベースnull")}");
            Debug.Log($"デフォルトデッキIDs: {(cardManager.defaultDeckIds != null ? cardManager.defaultDeckIds.Count + "枚" : "null")}");
        }
        else
        {
            Debug.Log("CardManager: 見つかりません");
        }
        
        // DeckDataManagerの状態
        if (DeckDataManager.Instance != null)
        {
            Debug.Log($"DeckDataManager: インスタンス存在");
            List<DeckData> allDecks = DeckDataManager.Instance.GetAllDecks();
            Debug.Log($"デッキ数: {(allDecks != null ? allDecks.Count.ToString() : "null")}");
            if (allDecks != null && allDecks.Count > 0)
            {
                foreach (DeckData deck in allDecks)
                {
                    Debug.Log($"デッキ: ID={deck.deckId}, 名前={deck.deckName}, カード数={deck.cardIds.Count}");
                }
            }
        }
        else
        {
            Debug.Log("DeckDataManager: インスタンスnull");
        }
        
        // GameModeSettingsの状態
        GameModeSettings settings = PlayerSelectionManager.SelectedGameMode;
        if (settings != null)
        {
            Debug.Log($"GameModeSettings: モード={settings.gameMode}, Player1デッキID={settings.player1DeckId}, AIプロファイルID={settings.aiProfileId}");
        }
        else
        {
            Debug.Log("GameModeSettings: null");
        }
        
        Debug.Log("========================================");
    }
}

// AIプレイヤープロファイル管理用クラス
[System.Serializable]
public class AIPlayerProfile
{
    public string name;                // AI名
    public AIDifficulty difficulty;    // 難易度（内部的には保持）
    public int deckId;                 // 使用するデッキID
    public string description;         // AI説明
    public string iconPath;            // アイコン画像のパス（オプション）
}

// AIプレイヤープロファイルリスト
[System.Serializable]
public class AIPlayerProfileList
{
    public List<AIPlayerProfile> profiles = new List<AIPlayerProfile>();
}