using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// デッキデータを管理するクラス
/// </summary>
[System.Serializable]
public class DeckData
{
    public int deckId;            // デッキID
    public string deckName;       // デッキ名
    public string description;    // デッキの説明
    public List<int> cardIds = new List<int>();    // デッキに含まれるカードID
    public string coverCardId;    // デッキカバーとして表示するカードID
    public DateTime lastModified; // 最終更新日時
    
    // JSONへの変換
    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }
    
    // JSONからの復元
    public static DeckData FromJson(string json)
    {
        return JsonUtility.FromJson<DeckData>(json);
    }
}

/// <summary>
/// デッキ管理システム
/// </summary>
public class DeckDataManager : MonoBehaviour
{
    // シングルトンパターン
    public static DeckDataManager Instance { get; private set; }
    
    [Header("デッキ設定")]
    public int minDeckSize = 40;  // 最小デッキサイズ
    public int maxDeckSize = 60;  // 最大デッキサイズ
    public int maxCardCopies = 2; // 同一カードの最大所持枚数
    
    // ユーザーデッキリスト
    private List<DeckData> userDecks = new List<DeckData>();
    
    // プレイヤーが現在選択しているデッキID
    private int currentSelectedDeckId = -1;
    
    private void Awake()
    {
        // シングルトンパターン
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // DeckDataManager.csのAwake()またはStart()メソッド内に追加
        Debug.Log($"DeckDataManager: 保存されているデッキ数: {userDecks.Count}");
        foreach (var deck in userDecks)
        {
            Debug.Log($"デッキID: {deck.deckId}, 名前: {deck.deckName}, カード数: {deck.cardIds.Count}");
        }
        
    }
    
    private void Start()
    {
        // デッキデータのロード
        LoadUserDecks();
    }
    
    // ユーザーデッキをロード
    public void LoadUserDecks()
    {
        userDecks.Clear();
        
        // PlayerPrefsからデッキリストを取得
        int deckCount = PlayerPrefs.GetInt("UserDeckCount", 0);
        
        for (int i = 0; i < deckCount; i++)
        {
            string deckJson = PlayerPrefs.GetString("UserDeck_" + i, "");
            if (!string.IsNullOrEmpty(deckJson))
            {
                DeckData deck = DeckData.FromJson(deckJson);
                userDecks.Add(deck);
            }
        }
        
        // デッキが1つもない場合はデフォルトデッキを作成
        if (userDecks.Count == 0)
        {
            CreateDefaultDeck();
        }
        
        // プレイヤーが最後に選択したデッキIDをロード
        currentSelectedDeckId = PlayerPrefs.GetInt("LastSelectedDeckId", userDecks.Count > 0 ? userDecks[0].deckId : -1);
    }
    
    // デフォルトデッキを作成
    private void CreateDefaultDeck()
    {
        DeckData defaultDeck = new DeckData();
        defaultDeck.deckId = 1;
        defaultDeck.deckName = "初期デッキ";
        defaultDeck.description = "基本カードのみを使用した初期デッキです。";
        defaultDeck.lastModified = DateTime.Now;
        
        // CardDatabaseからカードを取得してデッキに追加
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        if (database != null)
        {
            List<Card> allCards = database.GetAllCards();
            
            // 単純に最初の40枚を使用（実際の実装ではバランスを考慮）
            int cardsToAdd = Mathf.Min(40, allCards.Count);
            
            for (int i = 0; i < cardsToAdd; i++)
            {
                defaultDeck.cardIds.Add(allCards[i].id);
            }
            
            // デッキカバーを設定
            if (allCards.Count > 0)
            {
                defaultDeck.coverCardId = allCards[0].id.ToString();
            }
        }
        else
        {
            Debug.LogError("CardDatabaseが見つかりません。デフォルトデッキの作成に失敗しました。");
        }
        
        userDecks.Add(defaultDeck);
        SaveUserDecks();
        
        // 初期デッキを選択状態に
        currentSelectedDeckId = defaultDeck.deckId;
    }
    
    // ユーザーデッキを保存
    public void SaveUserDecks()
    {
        // PlayerPrefsに保存
        PlayerPrefs.SetInt("UserDeckCount", userDecks.Count);
        
        for (int i = 0; i < userDecks.Count; i++)
        {
            string deckJson = userDecks[i].ToJson();
            PlayerPrefs.SetString("UserDeck_" + i, deckJson);
        }
        
        // 現在選択中のデッキIDを保存
        PlayerPrefs.SetInt("LastSelectedDeckId", currentSelectedDeckId);
        
        PlayerPrefs.Save();
    }
    
    // この修正を適用してください
    public DeckData GetDeckById(int deckId)
    {
        Debug.Log($"GetDeckById: デッキID {deckId} を検索中, 登録デッキ数: {userDecks.Count}");
        
        foreach (DeckData deck in userDecks)
        {
            Debug.Log($"- チェック: デッキID={deck.deckId}, 名前={deck.deckName}");
            if (deck.deckId == deckId)
            {
                Debug.Log($"デッキID {deckId} を発見: {deck.deckName}");
                return deck;
            }
        }
        
        Debug.LogWarning($"デッキID {deckId} は見つかりませんでした");
        
        // 代替: 最初のデッキを返す
        if (userDecks.Count > 0)
        {
            Debug.Log($"代わりに最初のデッキを返します: ID={userDecks[0].deckId}, 名前={userDecks[0].deckName}");
            return userDecks[0];
        }
        
        return null;
    }
    
    // 新しいデッキを作成
    public DeckData CreateNewDeck(string name, string description)
    {
        // 新しいデッキIDを取得（最大ID + 1）
        int newId = 1;
        foreach (DeckData deck in userDecks)
        {
            if (deck.deckId >= newId)
                newId = deck.deckId + 1;
        }
        
        DeckData newDeck = new DeckData();
        newDeck.deckId = newId;
        newDeck.deckName = name;
        newDeck.description = description;
        newDeck.lastModified = DateTime.Now;
        
        userDecks.Add(newDeck);
        SaveUserDecks();
        
        return newDeck;
    }
    
    // デッキを削除
    public bool DeleteDeck(int deckId)
    {
        // 最後の1つは削除できない
        if (userDecks.Count <= 1)
        {
            Debug.LogWarning("最後のデッキは削除できません。");
            return false;
        }
        
        for (int i = 0; i < userDecks.Count; i++)
        {
            if (userDecks[i].deckId == deckId)
            {
                userDecks.RemoveAt(i);
                
                // 削除したデッキが選択中だった場合は別のデッキを選択
                if (currentSelectedDeckId == deckId)
                {
                    currentSelectedDeckId = userDecks.Count > 0 ? userDecks[0].deckId : -1;
                }
                
                SaveUserDecks();
                return true;
            }
        }
        
        return false;
    }
    
    // デッキを更新
    public void UpdateDeck(DeckData deck)
    {
        for (int i = 0; i < userDecks.Count; i++)
        {
            if (userDecks[i].deckId == deck.deckId)
            {
                deck.lastModified = DateTime.Now;
                userDecks[i] = deck;
                SaveUserDecks();
                return;
            }
        }
        
        // 見つからなければ新しく追加
        deck.lastModified = DateTime.Now;
        userDecks.Add(deck);
        SaveUserDecks();
    }
    
    // 現在選択中のデッキIDを設定
    public void SetCurrentDeckId(int deckId)
    {
        currentSelectedDeckId = deckId;
        SaveUserDecks();
    }
    
    // 現在選択中のデッキIDを取得
    public int GetCurrentDeckId()
    {
        return currentSelectedDeckId;
    }
    
    // 現在選択中のデッキを取得
    public DeckData GetCurrentDeck()
    {
        return GetDeckById(currentSelectedDeckId);
    }
    
    // 全デッキリストを取得
    public List<DeckData> GetAllDecks()
    {
        return userDecks;
    }
    
    // デッキの妥当性チェック
    public bool IsValidDeck(DeckData deck)
    {
        // デッキサイズチェック
        if (deck.cardIds.Count < minDeckSize)
        {
            return false;
        }
        
        if (deck.cardIds.Count > maxDeckSize)
        {
            return false;
        }
        
        // 同一カードの枚数チェック
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();
        
        foreach (int cardId in deck.cardIds)
        {
            if (!cardCounts.ContainsKey(cardId))
            {
                cardCounts[cardId] = 0;
            }
            
            cardCounts[cardId]++;
            
            if (cardCounts[cardId] > maxCardCopies)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // デッキの妥当性チェック（エラーメッセージ付き）
    public string ValidateDeck(DeckData deck)
    {
        // デッキサイズチェック
        if (deck.cardIds.Count < minDeckSize)
        {
            return "デッキは少なくとも" + minDeckSize + "枚のカードが必要です。";
        }
        
        if (deck.cardIds.Count > maxDeckSize)
        {
            return "デッキは最大" + maxDeckSize + "枚までです。";
        }
        
        // 同一カードの枚数チェック
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();
        
        foreach (int cardId in deck.cardIds)
        {
            if (!cardCounts.ContainsKey(cardId))
            {
                cardCounts[cardId] = 0;
            }
            
            cardCounts[cardId]++;
            
            if (cardCounts[cardId] > maxCardCopies)
            {
                Card card = Resources.Load<CardDatabase>("CardDatabase").GetCardById(cardId);
                string cardName = card != null ? card.cardName : "ID:" + cardId;
                return "カード「" + cardName + "」は" + maxCardCopies + "枚までしか入れられません。";
            }
        }
        
        return null; // エラーなし
    }
    
    // デッキを使用可能な形に変換（ゲーム開始用）
    public List<Card> GetDeckCards(int deckId)
    {
        List<Card> deckCards = new List<Card>();
        
        // デバッグ情報
        Debug.Log($"GetDeckCards: デッキID {deckId} のカードを取得します");
        
        DeckData deckData = GetDeckById(deckId);
        
        if (deckData != null)
        {
            Debug.Log($"デッキデータを取得しました: {deckData.deckName}, カードID数: {deckData.cardIds.Count}");
            
            // CardDatabaseのロード
            CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
            
            if (database == null)
            {
                Debug.LogError("CardDatabaseが見つかりません。代替方法を試みます...");
                
                // 代替方法1: CardManagerから取得を試みる
                CardManager cardManager = FindFirstObjectByType<CardManager>();
                if (cardManager != null && cardManager.cardDatabase != null)
                {
                    Debug.Log("CardManagerからデータベースを取得しました");
                    database = cardManager.cardDatabase;
                }
                else
                {
                    Debug.LogError("CardManagerからもデータベースを取得できませんでした。デフォルトカードセットを生成します。");
                }
            }
            else
            {
                Debug.Log("CardDatabaseをResourcesから正常にロードしました");
            }
            
            foreach (int cardId in deckData.cardIds)
            {
                try
                {
                    Card card = database.GetCardById(cardId);
                    if (card != null)
                    {
                        // インスタンス化前にログ
                        Debug.Log($"カードをインスタンス化します: ID={cardId}, 名前={card.cardName}");
                        
                        // 安全なインスタンス化
                        Card cardCopy = null;
                        try
                        {
                            cardCopy = UnityEngine.Object.Instantiate(card);
                            Debug.Log($"カードのインスタンス化に成功: {card.cardName}");
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"カードのインスタンス化に失敗: {e.Message}");
                            // 例外をスキップして次のカードに進む
                            continue;
                        }
                        
                        deckCards.Add(cardCopy);
                    }
                    else
                    {
                        Debug.LogWarning($"カードID {cardId} が見つかりません");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"カードID {cardId} の処理中にエラー: {e.Message}");
                }
            }
        }
        else
        {
            Debug.LogError($"デッキID {deckId} のデッキデータが見つかりません");
        }
        
        Debug.Log($"デッキカード取得完了: {deckCards.Count}枚のカードを取得しました");
        return deckCards;
    }
}