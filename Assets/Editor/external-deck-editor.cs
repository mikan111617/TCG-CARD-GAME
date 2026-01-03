using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

#if UNITY_EDITOR
/// <summary>
/// ゲームの機能外でデッキを作成・編集するためのエディタ拡張ツール
/// </summary>
public class ExternalDeckEditor : EditorWindow
{
    // タブのインデックス
    private enum TabIndex
    {
        DeckList,
        DeckEdit,
        DeckCreate
    }
    
    // UI設定
    private Vector2 scrollPosition;
    private TabIndex currentTab = TabIndex.DeckList;
    
    // デッキリスト
    private List<DeckData> deckList = new List<DeckData>();
    
    // 選択中のデッキ
    private DeckData selectedDeck;
    private int selectedDeckIndex = -1;
    
    // カードデータベース
    private CardDatabase cardDatabase;
    
    // カード検索
    private string cardSearchQuery = "";
    private CardType cardTypeFilter = CardType.Character;
    private bool showTypeFilter = false;
    private int costFilterMin = 0;
    private int costFilterMax = 10;
    private bool showCostFilter = false;
    
    // 新規デッキデータ
    private string newDeckName = "";
    private string newDeckDescription = "";
    
    // デッキ編集データ
    private Vector2 collectionScrollPos;
    private Vector2 deckScrollPos;
    private List<Card> filteredCards = new List<Card>();
    
    // メニューにツールを追加
    [MenuItem("Tools/Card Game/External Deck Editor")]
    public static void ShowWindow()
    {
        ExternalDeckEditor window = GetWindow<ExternalDeckEditor>("デッキエディタ");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }
    
    // ウィンドウ初期化
    private void OnEnable()
    {
        // カードデータベースをロード
        cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
        if (cardDatabase == null)
        {
            Debug.LogWarning("CardDatabaseが見つかりません。");
            return;
        }
        
        // デッキリストのロード
        LoadDeckList();
    }
    
    // デッキリストをロード
    private void LoadDeckList()
    {
        deckList.Clear();
        
        // デッキデータマネージャーがあれば利用
        DeckDataManager deckManager = UnityEngine.Object.FindFirstObjectByType<DeckDataManager>();
        if (deckManager != null)
        {
            deckList = deckManager.GetAllDecks();
        }
        else
        {
            // エディタ上でDeckDataManagerが見つからない場合
            // PlayerPrefsからデッキデータを直接ロード
            int deckCount = PlayerPrefs.GetInt("UserDeckCount", 0);
            
            for (int i = 0; i < deckCount; i++)
            {
                string deckJson = PlayerPrefs.GetString("UserDeck_" + i, "");
                if (!string.IsNullOrEmpty(deckJson))
                {
                    try
                    {
                        DeckData deck = DeckData.FromJson(deckJson);
                        deckList.Add(deck);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"デッキデータの読み込みエラー: {e.Message}");
                    }
                }
            }
            
            if (deckList.Count == 0)
            {
                // デフォルトデッキの作成
                CreateDefaultDeck();
            }
        }
    }
    
    // デッキリストを保存
    private void SaveDeckList()
    {
        // デッキマネージャーがあれば利用
        DeckDataManager deckManager = UnityEngine.Object.FindFirstObjectByType<DeckDataManager>();
        if (deckManager != null)
        {
            // 各デッキを更新
            foreach (DeckData deck in deckList)
            {
                deckManager.UpdateDeck(deck);
            }
            
            // 保存
            deckManager.SaveUserDecks();
        }
        else
        {
            // エディタ上でDeckDataManagerが見つからない場合
            // PlayerPrefsに直接保存
            PlayerPrefs.SetInt("UserDeckCount", deckList.Count);
            
            for (int i = 0; i < deckList.Count; i++)
            {
                string deckJson = deckList[i].ToJson();
                PlayerPrefs.SetString("UserDeck_" + i, deckJson);
            }
            
            PlayerPrefs.Save();
        }
        
        // また、Resourcesフォルダにデッキのバックアップとしてセーブ
        string resourcesFolder = Application.dataPath + "/Resources/Decks";
        if (!Directory.Exists(resourcesFolder))
        {
            Directory.CreateDirectory(resourcesFolder);
        }
        
        foreach (DeckData deck in deckList)
        {
            string deckJson = deck.ToJson();
            File.WriteAllText(resourcesFolder + $"/Deck_{deck.deckId}.json", deckJson);
        }
        
        AssetDatabase.Refresh();
    }
    
    // デフォルトデッキの作成
    private void CreateDefaultDeck()
    {
        if (cardDatabase == null || cardDatabase.GetAllCards().Count == 0)
        {
            Debug.LogWarning("カードデータがないためデフォルトデッキを作成できません。");
            return;
        }
        
        DeckData defaultDeck = new DeckData();
        defaultDeck.deckId = 1;
        defaultDeck.deckName = "初期デッキ";
        defaultDeck.description = "基本カードのみを使用した初期デッキです。";
        defaultDeck.lastModified = DateTime.Now;
        
        // カードをデッキに追加（最大40枚）
        List<Card> allCards = cardDatabase.GetAllCards();
        int cardsToAdd = Mathf.Min(40, allCards.Count);
        
        for (int i = 0; i < cardsToAdd; i++)
        {
            defaultDeck.cardIds.Add(allCards[i].id);
        }
        
        // コレクションに追加
        deckList.Add(defaultDeck);
        
        // 保存
        SaveDeckList();
    }
    
    // GUI描画
    private void OnGUI()
    {
        // エラーチェック
        if (cardDatabase == null)
        {
            EditorGUILayout.HelpBox("CardDatabaseのロードに失敗しました。", MessageType.Error);
            if (GUILayout.Button("再読み込み"))
            {
                OnEnable();
            }
            return;
        }
        
        // タイトル
        GUILayout.Space(10);
        GUILayout.Label("外部デッキエディタ", EditorStyles.boldLabel);
        
        // タブ切り替え
        DrawTabs();
        
        // 現在のタブに応じた描画処理
        switch (currentTab)
        {
            case TabIndex.DeckList:
                DrawDeckListTab();
                break;
                
            case TabIndex.DeckEdit:
                DrawDeckEditTab();
                break;
                
            case TabIndex.DeckCreate:
                DrawDeckCreateTab();
                break;
        }
    }
    
    // タブ切り替えUIの描画
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Toggle(currentTab == TabIndex.DeckList, "デッキ一覧", EditorStyles.toolbarButton))
            currentTab = TabIndex.DeckList;
            
        if (GUILayout.Toggle(currentTab == TabIndex.DeckEdit, "デッキ編集", EditorStyles.toolbarButton))
            currentTab = TabIndex.DeckEdit;
            
        if (GUILayout.Toggle(currentTab == TabIndex.DeckCreate, "新規デッキ作成", EditorStyles.toolbarButton))
            currentTab = TabIndex.DeckCreate;
            
        EditorGUILayout.EndHorizontal();
    }
    
    #region デッキ一覧タブ
    // デッキ一覧タブの描画
    private void DrawDeckListTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("デッキ一覧", EditorStyles.boldLabel);
        
        // リロードボタン
        if (GUILayout.Button("デッキリストの再読み込み", GUILayout.Width(200)))
        {
            LoadDeckList();
        }
        
        GUILayout.Space(10);
        
        // デッキが1つもない場合
        if (deckList.Count == 0)
        {
            EditorGUILayout.HelpBox("デッキがありません。「新規デッキ作成」タブからデッキを作成してください。", MessageType.Info);
            return;
        }
        
        // デッキリスト表示
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < deckList.Count; i++)
        {
            DeckData deck = deckList[i];
            
            EditorGUILayout.BeginVertical("box");
            
            // デッキ名と情報
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{deck.deckName} (ID: {deck.deckId})", EditorStyles.boldLabel);
            
            // 最終更新日時
            string lastModifiedStr = deck.lastModified.ToString("yyyy/MM/dd HH:mm");
            EditorGUILayout.LabelField($"最終更新: {lastModifiedStr}", GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
            
            // デッキ説明
            EditorGUILayout.LabelField($"説明: {deck.description}");
            
            // カード枚数
            EditorGUILayout.LabelField($"カード: {deck.cardIds.Count}枚");
            
            // ボタン群
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("編集", GUILayout.Width(80)))
            {
                selectedDeck = deck;
                selectedDeckIndex = i;
                currentTab = TabIndex.DeckEdit;
                UpdateFilteredCardList();
            }
            
            if (GUILayout.Button("複製", GUILayout.Width(80)))
            {
                DuplicateDeck(deck);
            }
            
            // デッキが1つしかない場合は削除不可
            EditorGUI.BeginDisabledGroup(deckList.Count <= 1);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("デッキ削除", $"デッキ「{deck.deckName}」を削除しますか？", "削除", "キャンセル"))
                {
                    DeleteDeck(i);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        // 新規デッキ作成ボタン
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("新規デッキを作成", GUILayout.Height(30)))
        {
            currentTab = TabIndex.DeckCreate;
            ResetNewDeckForm();
        }
        GUI.backgroundColor = Color.white;
    }
    
    // デッキ複製
    private void DuplicateDeck(DeckData sourceDeck)
    {
        DeckData newDeck = new DeckData();
        
        // 新しいIDを生成
        int maxId = 0;
        foreach (var deck in deckList)
        {
            if (deck.deckId > maxId)
                maxId = deck.deckId;
        }
        
        newDeck.deckId = maxId + 1;
        newDeck.deckName = sourceDeck.deckName + " (コピー)";
        newDeck.description = sourceDeck.description;
        newDeck.lastModified = DateTime.Now;
        
        // カードIDのコピー
        newDeck.cardIds = new List<int>(sourceDeck.cardIds);
        
        // リストに追加
        deckList.Add(newDeck);
        
        // 保存
        SaveDeckList();
        
        EditorUtility.DisplayDialog("デッキ複製", $"デッキ「{sourceDeck.deckName}」のコピーを作成しました。", "OK");
    }
    
    // デッキ削除
    private void DeleteDeck(int index)
    {
        deckList.RemoveAt(index);
        
        // 選択中のデッキが削除された場合
        if (selectedDeckIndex == index)
        {
            selectedDeck = null;
            selectedDeckIndex = -1;
        }
        else if (selectedDeckIndex > index)
        {
            selectedDeckIndex--;
        }
        
        // 保存
        SaveDeckList();
    }
    #endregion
    
    #region デッキ編集タブ
    // デッキ編集タブの描画
    private void DrawDeckEditTab()
    {
        if (selectedDeck == null)
        {
            EditorGUILayout.HelpBox("デッキが選択されていません。「デッキ一覧」タブからデッキを選択してください。", MessageType.Info);
            if (GUILayout.Button("デッキ一覧に戻る"))
            {
                currentTab = TabIndex.DeckList;
            }
            return;
        }
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField($"デッキ編集: {selectedDeck.deckName}", EditorStyles.boldLabel);
        
        // デッキ基本情報の編集
        selectedDeck.deckName = EditorGUILayout.TextField("デッキ名", selectedDeck.deckName);
        selectedDeck.description = EditorGUILayout.TextField("説明", selectedDeck.description);
        
        // カード検索と絞り込み
        DrawCardFilters();
        
        // 編集エリア（左：カードコレクション、右：現在のデッキ）
        EditorGUILayout.BeginHorizontal();
        
        // 左側：カードコレクション
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
        DrawCardCollection();
        EditorGUILayout.EndVertical();
        
        // 右側：現在のデッキ
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
        DrawCurrentDeck();
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // デッキ保存ボタン
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("デッキを保存", GUILayout.Height(30)))
        {
            SaveDeck();
        }
        GUI.backgroundColor = Color.white;
    }
    
    // カード検索と絞り込み
    private void DrawCardFilters()
    {
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        
        // 検索フィールド
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.LabelField("検索", GUILayout.Width(40));
        string newSearchQuery = EditorGUILayout.TextField(cardSearchQuery);
        if (newSearchQuery != cardSearchQuery)
        {
            cardSearchQuery = newSearchQuery;
            UpdateFilteredCardList();
        }
        EditorGUILayout.EndHorizontal();
        
        // カードタイプフィルター
        if (GUILayout.Button("種類", GUILayout.Width(60)))
        {
            showTypeFilter = !showTypeFilter;
            showCostFilter = false;
        }
        
        // コストフィルター
        if (GUILayout.Button("コスト", GUILayout.Width(60)))
        {
            showCostFilter = !showCostFilter;
            showTypeFilter = false;
        }
        
        EditorGUILayout.EndHorizontal();
        
        // フィルターオプションの表示
        if (showTypeFilter)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("カード種類", GUILayout.Width(80));
            
            CardType oldTypeFilter = cardTypeFilter;
            cardTypeFilter = (CardType)EditorGUILayout.EnumPopup(cardTypeFilter);
            
            if (oldTypeFilter != cardTypeFilter)
            {
                UpdateFilteredCardList();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        if (showCostFilter)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("コスト範囲", GUILayout.Width(80));
            
            // float型に変換してスライダーを使用
            float minCost = costFilterMin;
            float maxCost = costFilterMax;
            EditorGUILayout.MinMaxSlider(ref minCost, ref maxCost, 0, 10);
            // int型に変換し直す
            costFilterMin = Mathf.RoundToInt(minCost);
            costFilterMax = Mathf.RoundToInt(maxCost);
            
            EditorGUILayout.LabelField($"{costFilterMin} - {costFilterMax}", GUILayout.Width(50));
            
            if (GUILayout.Button("適用", GUILayout.Width(60)))
            {
                UpdateFilteredCardList();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    // カードコレクション表示
    private void DrawCardCollection()
    {
        EditorGUILayout.LabelField("カードコレクション", EditorStyles.boldLabel);
        
        collectionScrollPos = EditorGUILayout.BeginScrollView(collectionScrollPos, GUILayout.Height(350));
        
        if (filteredCards.Count == 0)
        {
            EditorGUILayout.HelpBox("条件に一致するカードがありません。", MessageType.Info);
        }
        else
        {
            // カードグリッド表示（4列）
            int columns = 3;
            int currentColumn = 0;
            
            EditorGUILayout.BeginHorizontal();
            
            foreach (Card card in filteredCards)
            {
                // 各カードの表示
                if (currentColumn >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }
                
                DrawCardItem(card, true);
                currentColumn++;
            }
            
            // 行を閉じる
            while (currentColumn < columns)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 / columns - 10));
                EditorGUILayout.EndVertical();
                currentColumn++;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    // 現在のデッキ表示
    private void DrawCurrentDeck()
    {
        // デッキ情報
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("現在のデッキ", EditorStyles.boldLabel);
        
        string validationMessage = ValidateDeck();
        if (string.IsNullOrEmpty(validationMessage))
        {
            EditorGUILayout.LabelField($"カード: {selectedDeck.cardIds.Count}枚", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField(validationMessage, EditorStyles.miniLabel, GUILayout.Width(200));
        }
        
        EditorGUILayout.EndHorizontal();
        
        // デッキ内のカード表示
        deckScrollPos = EditorGUILayout.BeginScrollView(deckScrollPos, GUILayout.Height(350));
        
        if (selectedDeck.cardIds.Count == 0)
        {
            EditorGUILayout.HelpBox("デッキにカードがありません。左側のカードコレクションからカードを追加してください。", MessageType.Info);
        }
        else
        {
            // カードグリッド表示（4列）
            int columns = 3;
            int currentColumn = 0;
            
            EditorGUILayout.BeginHorizontal();
            
            // ID別カード枚数の計算
            Dictionary<int, int> cardCounts = new Dictionary<int, int>();
            foreach (int cardId in selectedDeck.cardIds)
            {
                if (!cardCounts.ContainsKey(cardId))
                {
                    cardCounts[cardId] = 0;
                }
                cardCounts[cardId]++;
            }
            
            // 表示用の一意のカードリスト
            List<int> uniqueCardIds = new List<int>();
            foreach (int cardId in selectedDeck.cardIds)
            {
                if (!uniqueCardIds.Contains(cardId))
                {
                    uniqueCardIds.Add(cardId);
                }
            }
            
            // カードをコスト順にソート
            uniqueCardIds.Sort((id1, id2) => {
                Card card1 = cardDatabase.GetCardById(id1);
                Card card2 = cardDatabase.GetCardById(id2);
                return card1.cost.CompareTo(card2.cost);
            });
            
            foreach (int cardId in uniqueCardIds)
            {
                // 各カードの表示
                if (currentColumn >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }
                
                Card card = cardDatabase.GetCardById(cardId);
                if (card != null)
                {
                    int count = cardCounts[cardId];
                    DrawCardItemWithCount(card, count, false);
                    currentColumn++;
                }
            }
            
            // 行を閉じる
            while (currentColumn < columns)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 / columns - 10));
                EditorGUILayout.EndVertical();
                currentColumn++;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    // カードアイテムの表示
    private void DrawCardItem(Card card, bool isCollection)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 6 - 10));
        
        // カード名
        EditorGUILayout.LabelField(card.cardName, EditorStyles.boldLabel);
        
        // コストと種類
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"コスト: {card.cost}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(card.type.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        // カード固有情報
        if (card is CharacterCard charCard)
        {
            EditorGUILayout.LabelField($"攻/防: {charCard.attackPower}/{charCard.defensePower}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"属性: {charCard.element}", EditorStyles.miniLabel);
        }
        else if (card is SpellCard spellCard)
        {
            EditorGUILayout.LabelField($"タイプ: {spellCard.spellType}", EditorStyles.miniLabel);
        }
        
        // 追加/削除ボタン
        if (isCollection)
        {
            // デッキに追加するボタン
            bool canAdd = CanAddCardToDeck(card);
            EditorGUI.BeginDisabledGroup(!canAdd);
            
            if (GUILayout.Button("デッキに追加"))
            {
                AddCardToDeck(card);
            }
            
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            // デッキから削除するボタン
            if (GUILayout.Button("デッキから削除"))
            {
                RemoveCardFromDeck(card);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    // 枚数付きでカードアイテムを表示
    private void DrawCardItemWithCount(Card card, int count, bool isCollection)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 6 - 10));
        
        // カード名と枚数
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(card.cardName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"x{count}", EditorStyles.boldLabel, GUILayout.Width(30));
        EditorGUILayout.EndHorizontal();
        
        // コストと種類
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"コスト: {card.cost}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(card.type.ToString(), EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
        
        // カード固有情報
        if (card is CharacterCard charCard)
        {
            EditorGUILayout.LabelField($"攻/防: {charCard.attackPower}/{charCard.defensePower}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"属性: {charCard.element}", EditorStyles.miniLabel);
        }
        else if (card is SpellCard spellCard)
        {
            EditorGUILayout.LabelField($"タイプ: {spellCard.spellType}", EditorStyles.miniLabel);
        }
        
        // 追加/削除ボタン
        EditorGUILayout.BeginHorizontal();
        
        if (isCollection)
        {
            // デッキに追加するボタン
            bool canAdd = CanAddCardToDeck(card);
            EditorGUI.BeginDisabledGroup(!canAdd);
            
            if (GUILayout.Button("+"))
            {
                AddCardToDeck(card);
            }
            
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            // 1枚削除ボタン
            if (GUILayout.Button("-"))
            {
                RemoveCardFromDeck(card);
            }
            
            // 全削除ボタン
            if (count > 1)
            {
                if (GUILayout.Button("すべて削除"))
                {
                    for (int i = 0; i < count; i++)
                    {
                        RemoveCardFromDeck(card);
                    }
                }
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    // フィルタリングされたカードリストを更新
    private void UpdateFilteredCardList()
    {
        filteredCards.Clear();
        
        if (cardDatabase == null) return;
        
        List<Card> allCards = cardDatabase.GetAllCards();
        
        foreach (Card card in allCards)
        {
            // 検索クエリに一致するか
            bool matchesSearch = string.IsNullOrEmpty(cardSearchQuery) ||
                card.cardName.ToLower().Contains(cardSearchQuery.ToLower()) ||
                card.description.ToLower().Contains(cardSearchQuery.ToLower());
                
            // カードタイプフィルター
            bool matchesType = !showTypeFilter || card.type == cardTypeFilter;
            
            // コストフィルター
            bool matchesCost = !showCostFilter || (card.cost >= costFilterMin && card.cost <= costFilterMax);
            
            if (matchesSearch && matchesType && matchesCost)
            {
                filteredCards.Add(card);
            }
        }
        
        // コスト順にソート
        filteredCards.Sort((a, b) => a.cost.CompareTo(b.cost));
    }
    
    // カードをデッキに追加可能か
    private bool CanAddCardToDeck(Card card)
    {
        // 最大枚数チェック
        int maxCards = 60;
        if (selectedDeck.cardIds.Count >= maxCards)
        {
            return false;
        }
        
        // 同じカードの最大枚数チェック
        int maxCopies = 2;
        int currentCopies = 0;
        
        foreach (int cardId in selectedDeck.cardIds)
        {
            if (cardId == card.id)
            {
                currentCopies++;
            }
        }
        
        return currentCopies < maxCopies;
    }
    
    // カードをデッキに追加
    private void AddCardToDeck(Card card)
    {
        if (CanAddCardToDeck(card))
        {
            selectedDeck.cardIds.Add(card.id);
            selectedDeck.lastModified = DateTime.Now;
        }
    }
    
    // カードをデッキから削除
    private void RemoveCardFromDeck(Card card)
    {
        // 同じIDを持つカードを1枚だけ削除
        for (int i = 0; i < selectedDeck.cardIds.Count; i++)
        {
            if (selectedDeck.cardIds[i] == card.id)
            {
                selectedDeck.cardIds.RemoveAt(i);
                selectedDeck.lastModified = DateTime.Now;
                break;
            }
        }
    }
    
    // デッキの妥当性を検証
    private string ValidateDeck()
    {
        // 枚数チェック
        int minCards = 40;
        int maxCards = 60;
        
        if (selectedDeck.cardIds.Count < minCards)
        {
            return $"⚠️ 最低{minCards}枚必要 ({selectedDeck.cardIds.Count}/{minCards})";
        }
        
        if (selectedDeck.cardIds.Count > maxCards)
        {
            return $"⚠️ 最大{maxCards}枚まで ({selectedDeck.cardIds.Count}/{maxCards})";
        }
        
        // 同一カードの枚数チェック
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();
        bool hasExceededLimit = false;
        
        foreach (int cardId in selectedDeck.cardIds)
        {
            if (!cardCounts.ContainsKey(cardId))
            {
                cardCounts[cardId] = 0;
            }
            
            cardCounts[cardId]++;
            
            if (cardCounts[cardId] > 2) // 同一カード最大2枚
            {
                hasExceededLimit = true;
                break;
            }
        }
        
        if (hasExceededLimit)
        {
            return "⚠️ 同じカードは2枚までです";
        }
        
        return null; // エラーなし
    }
    
    // デッキを保存
    private void SaveDeck()
    {
        // 妥当性チェック
        string validationMessage = ValidateDeck();
        if (!string.IsNullOrEmpty(validationMessage))
        {
            EditorUtility.DisplayDialog("デッキエラー", validationMessage, "OK");
            return;
        }
        
        // 最終更新日時を更新
        selectedDeck.lastModified = DateTime.Now;
        
        // デッキを保存
        DeckDataManager deckManager = UnityEngine.Object.FindFirstObjectByType<DeckDataManager>();
        if (deckManager != null)
        {
            deckManager.UpdateDeck(selectedDeck);
            deckManager.SaveUserDecks();
        }
        else
        {
            SaveDeckList();
        }
        
        EditorUtility.DisplayDialog("デッキ保存", $"デッキ「{selectedDeck.deckName}」を保存しました。", "OK");
    }
    #endregion
    
    #region 新規デッキ作成タブ
    // 新規デッキ作成タブの描画
    private void DrawDeckCreateTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("新規デッキ作成", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // 新規デッキ情報入力
        newDeckName = EditorGUILayout.TextField("デッキ名", newDeckName);
        
        EditorGUILayout.LabelField("説明");
        newDeckDescription = EditorGUILayout.TextArea(newDeckDescription, GUILayout.Height(60));
        
        GUILayout.Space(20);
        
        // 新規デッキテンプレート選択
        EditorGUILayout.LabelField("テンプレート", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("空のデッキ", GUILayout.Height(30)))
        {
            if (ValidateNewDeckForm())
            {
                CreateEmptyDeck();
            }
        }
        
        if (GUILayout.Button("バランス型デッキ", GUILayout.Height(30)))
        {
            if (ValidateNewDeckForm())
            {
                CreateBalancedDeck();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("攻撃型デッキ", GUILayout.Height(30)))
        {
            if (ValidateNewDeckForm())
            {
                CreateAggressiveDeck();
            }
        }
        
        if (GUILayout.Button("防御型デッキ", GUILayout.Height(30)))
        {
            if (ValidateNewDeckForm())
            {
                CreateDefensiveDeck();
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    // 新規デッキフォームの検証
    private bool ValidateNewDeckForm()
    {
        if (string.IsNullOrEmpty(newDeckName))
        {
            EditorUtility.DisplayDialog("入力エラー", "デッキ名を入力してください。", "OK");
            return false;
        }
        
        return true;
    }
    
    // 空のデッキを作成
    private void CreateEmptyDeck()
    {
        DeckData newDeck = CreateBaseDeck();
        
        // リストに追加
        deckList.Add(newDeck);
        SaveDeckList();
        
        // 作成したデッキを選択
        selectedDeck = newDeck;
        selectedDeckIndex = deckList.Count - 1;
        
        // デッキ編集タブに移動
        currentTab = TabIndex.DeckEdit;
        UpdateFilteredCardList();
        
        EditorUtility.DisplayDialog("デッキ作成", $"空のデッキ「{newDeck.deckName}」を作成しました。", "OK");
    }
    
    // バランス型デッキを作成
    private void CreateBalancedDeck()
    {
        DeckData newDeck = CreateBaseDeck();
        
        // バランス型デッキ用カードを追加
        AddTemplateCardsToDeck(newDeck, CardType.Character, 20, 1000, 1000);
        AddTemplateCardsToDeck(newDeck, CardType.Spell, 14);
        AddTemplateCardsToDeck(newDeck, CardType.Field, 6);
        
        // リストに追加
        deckList.Add(newDeck);
        SaveDeckList();
        
        // 作成したデッキを選択
        selectedDeck = newDeck;
        selectedDeckIndex = deckList.Count - 1;
        
        // デッキ編集タブに移動
        currentTab = TabIndex.DeckEdit;
        UpdateFilteredCardList();
        
        EditorUtility.DisplayDialog("デッキ作成", $"バランス型デッキ「{newDeck.deckName}」を作成しました。", "OK");
    }
    
    // 攻撃型デッキを作成
    private void CreateAggressiveDeck()
    {
        DeckData newDeck = CreateBaseDeck();
        
        // 攻撃型デッキ用カードを追加
        AddTemplateCardsToDeck(newDeck, CardType.Character, 24, 1500, 800);
        AddTemplateCardsToDeck(newDeck, CardType.Spell, 12);
        AddTemplateCardsToDeck(newDeck, CardType.Field, 4);
        
        // リストに追加
        deckList.Add(newDeck);
        SaveDeckList();
        
        // 作成したデッキを選択
        selectedDeck = newDeck;
        selectedDeckIndex = deckList.Count - 1;
        
        // デッキ編集タブに移動
        currentTab = TabIndex.DeckEdit;
        UpdateFilteredCardList();
        
        EditorUtility.DisplayDialog("デッキ作成", $"攻撃型デッキ「{newDeck.deckName}」を作成しました。", "OK");
    }
    
    // 防御型デッキを作成
    private void CreateDefensiveDeck()
    {
        DeckData newDeck = CreateBaseDeck();
        
        // 防御型デッキ用カードを追加
        AddTemplateCardsToDeck(newDeck, CardType.Character, 20, 800, 1500);
        AddTemplateCardsToDeck(newDeck, CardType.Spell, 16);
        AddTemplateCardsToDeck(newDeck, CardType.Field, 4);
        
        // リストに追加
        deckList.Add(newDeck);
        SaveDeckList();
        
        // 作成したデッキを選択
        selectedDeck = newDeck;
        selectedDeckIndex = deckList.Count - 1;
        
        // デッキ編集タブに移動
        currentTab = TabIndex.DeckEdit;
        UpdateFilteredCardList();
        
        EditorUtility.DisplayDialog("デッキ作成", $"防御型デッキ「{newDeck.deckName}」を作成しました。", "OK");
    }
    
    // 基本デッキデータの作成
    private DeckData CreateBaseDeck()
    {
        // 新しいIDを生成
        int maxId = 0;
        foreach (var deck in deckList)
        {
            if (deck.deckId > maxId)
                maxId = deck.deckId;
        }
        
        DeckData newDeck = new DeckData();
        newDeck.deckId = maxId + 1;
        newDeck.deckName = newDeckName;
        newDeck.description = newDeckDescription;
        newDeck.lastModified = DateTime.Now;
        newDeck.cardIds = new List<int>();
        
        return newDeck;
    }
    
    // テンプレートカードをデッキに追加
    private void AddTemplateCardsToDeck(DeckData deck, CardType type, int count, int minAttack = 0, int minDefense = 0)
    {
        if (cardDatabase == null || count <= 0) return;
        
        // 指定タイプのカードをフィルタリング
        List<Card> availableCards = cardDatabase.GetAllCards().FindAll(card => card.type == type);
        
        // キャラクターカードの場合は攻撃力と防御力でフィルタリング
        if (type == CardType.Character && (minAttack > 0 || minDefense > 0))
        {
            availableCards = availableCards.FindAll(card => {
                CharacterCard charCard = card as CharacterCard;
                return charCard != null && 
                       charCard.attackPower >= minAttack && 
                       charCard.defensePower >= minDefense;
            });
        }
        
        if (availableCards.Count == 0) return;
        
        // カードを最大2枚までランダムに追加
        int cardsToAdd = Math.Min(count, 60 - deck.cardIds.Count);
        int added = 0;
        
        Dictionary<int, int> cardCounts = new Dictionary<int, int>();
        
        while (added < cardsToAdd && availableCards.Count > 0)
        {
            // ランダムにカードを選択
            int index = UnityEngine.Random.Range(0, availableCards.Count);
            Card card = availableCards[index];
            
            // 同じカードの枚数をカウント
            if (!cardCounts.ContainsKey(card.id))
            {
                cardCounts[card.id] = 0;
            }
            
            // 最大2枚までチェック
            if (cardCounts[card.id] < 2)
            {
                deck.cardIds.Add(card.id);
                cardCounts[card.id]++;
                added++;
            }
            else
            {
                // 既に2枚入っているカードは選択対象から外す
                availableCards.RemoveAt(index);
            }
        }
    }
    
    // 新規デッキフォームのリセット
    private void ResetNewDeckForm()
    {
        newDeckName = "";
        newDeckDescription = "";
    }
    #endregion
}
#endif