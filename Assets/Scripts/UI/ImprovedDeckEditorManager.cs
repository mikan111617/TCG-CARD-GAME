using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// 改良版デッキエディタマネージャークラス
/// </summary>
public class ImprovedDeckEditorManager : MonoBehaviour
{
    [Header("パネル参照")]
    public GameObject mainPanel;               // メインパネル
    public GameObject detailPanel;             // 詳細パネル
    public GameObject createCardPanel;         // カード作成パネル
    public GameObject filterPanel;             // フィルターパネル
    
    [Header("カード表示")]
    public Transform cardCollectionGrid;       // コレクショングリッド
    public Transform deckGrid;                 // デッキグリッド
    public GameObject cardPrefab;              // カードプレハブ
    public float gridCardScale = 0.8f;         // グリッド内のカードスケール
    
    [Header("UI要素")]
    public TextMeshProUGUI collectionCountText; // 倉庫枚数表示
    public TextMeshProUGUI deckCountText;       // デッキ枚数表示
    public TMP_InputField searchInput;          // 検索入力フィールド
    public Button saveButton;                   // 保存ボタン
    public Button backButton;                   // 戻るボタン
    public Button createCardButton;             // カード作成ボタン
    public Button filterButton;                 // フィルターボタン
    public TMP_Dropdown sortDropdown;           // ソートドロップダウン
    
    [Header("カード詳細")]
    public Image detailCardImage;              // 詳細カード画像
    public TextMeshProUGUI cardNameText;       // カード名テキスト
    public TextMeshProUGUI cardTypeText;       // カードタイプテキスト
    public TextMeshProUGUI cardDescriptionText; // カード説明テキスト
    public TextMeshProUGUI cardAttributesText;  // カード属性テキスト
    public TextMeshProUGUI cardEffectText;      // カード効果テキスト
    public TextMeshProUGUI cardCountText;       // カード枚数テキスト
    public Button addCardButton;                // カード追加ボタン
    public Button removeCardButton;             // カード削除ボタン
    public Button closeDetailButton;            // 詳細を閉じるボタン
    
    // フィルタリング設定
    private CardType? filterCardType = null;
    private ElementType? filterElementType = null;
    private string searchText = "";
    
    // ソート設定
    private enum SortType { Name, Cost, Type, Rarity }
    private SortType currentSortType = SortType.Name;
    private bool isAscending = true;
    
    // データリスト
    private List<Card> cardCollection = new List<Card>();  // プレイヤーの所持カード
    private List<Card> currentDeck = new List<Card>();     // 現在編集中のデッキ
    private Card currentDetailCard;                        // 詳細表示中のカード
    
    // デッキデータ
    private DeckData currentDeckData;
    
    private void Start()
    {
        // パネルの初期状態設定
        if (detailPanel != null) detailPanel.SetActive(false);
        if (createCardPanel != null) createCardPanel.SetActive(false);
        if (filterPanel != null) filterPanel.SetActive(false);
        
        // ボタンイベントの設定
        SetupButtonEvents();
        
        // カードコレクションとデッキのロード
        LoadCardCollection();
        LoadDeckData();
        
        // UI初期更新
        UpdateCollectionGrid();
        UpdateDeckGrid();
        UpdateCountTexts();
    }
    
    /// <summary>
    /// ボタンイベントの設定
    /// </summary>
    private void SetupButtonEvents()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveDeck);
            
        if (backButton != null)
            backButton.onClick.AddListener(ReturnToMainMenu);
            
        if (createCardButton != null)
            createCardButton.onClick.AddListener(ShowCreateCardPanel);
            
        if (filterButton != null)
            filterButton.onClick.AddListener(ToggleFilterPanel);
            
        if (sortDropdown != null)
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
            
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchTextChanged);
            
        if (closeDetailButton != null)
            closeDetailButton.onClick.AddListener(CloseDetailPanel);
            
        if (addCardButton != null)
            addCardButton.onClick.AddListener(AddCardToDeck);
            
        if (removeCardButton != null)
            removeCardButton.onClick.AddListener(RemoveCardFromDeck);
    }
    
    /// <summary>
    /// カードコレクションをロード
    /// </summary>
    private void LoadCardCollection()
    {
        cardCollection.Clear();
        
        // カードデータベースからカードをロード
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        if (database == null)
        {
            Debug.LogError("カードデータベースが見つかりません");
            return;
        }
        
        // 所持カードの設定（本来はセーブデータから読み込む）
        // 実装例：各カードを最大2枚ずつ所持
        List<Card> allCards = database.GetAllCards();
        foreach (Card card in allCards)
        {
            for (int i = 0; i < 2; i++) // 各カード2枚ずつ
            {
                Card cardCopy = Instantiate(card);
                cardCollection.Add(cardCopy);
            }
        }
    }
    
    /// <summary>
    /// デッキデータをロード
    /// </summary>
    private void LoadDeckData()
    {
        currentDeck.Clear();
        
        // DeckDataManagerからデッキをロード（存在する場合）
        if (DeckDataManager.Instance != null)
        {
            DeckData deckData = DeckDataManager.Instance.GetCurrentDeck();
            
            if (deckData != null)
            {
                currentDeckData = deckData;
                
                // カードIDからカードを取得
                CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
                
                foreach (int cardId in deckData.cardIds)
                {
                    Card card = database.GetCardById(cardId);
                    if (card != null)
                    {
                        Card cardCopy = Instantiate(card);
                        currentDeck.Add(cardCopy);
                    }
                }
            }
            else
            {
                // デッキがなければ新規作成
                currentDeckData = DeckDataManager.Instance.CreateNewDeck("新規デッキ", "説明を入力してください");
            }
        }
        else
        {
            // DeckDataManagerがない場合はデフォルトデッキを作成
            CreateDefaultDeck();
        }
    }
    
    /// <summary>
    /// デフォルトデッキを作成
    /// </summary>
    private void CreateDefaultDeck()
    {
        // 例：カードコレクションから最初の40枚を選ぶ
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        if (database != null)
        {
            List<Card> allCards = database.GetAllCards();
            int count = Mathf.Min(40, allCards.Count);
            
            for (int i = 0; i < count; i++)
            {
                Card cardCopy = Instantiate(allCards[i]);
                currentDeck.Add(cardCopy);
            }
        }
    }
    
    /// <summary>
    /// コレクションのグリッド更新
    /// </summary>
    private void UpdateCollectionGrid()
    {
        // グリッドのクリア
        foreach (Transform child in cardCollectionGrid)
        {
            Destroy(child.gameObject);
        }
        
        // ドロップエリアコンポーネントがなければ追加
        CardDropArea dropArea = cardCollectionGrid.GetComponent<CardDropArea>();
        if (dropArea == null)
        {
            dropArea = cardCollectionGrid.gameObject.AddComponent<CardDropArea>();
            dropArea.areaType = CardDropArea.AreaType.CollectionGrid;
        }
        
        // フィルタリングされたカードリスト
        List<Card> filteredCards = cardCollection
            .Where(card => MatchesFilter(card))
            .ToList();
        
        // カードのソート
        SortCardList(filteredCards);
        
        // カード表示
        foreach (Card card in filteredCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardCollectionGrid);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // カードをスケーリング
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one * gridCardScale;
                }
                
                // カードの追加可能状態を視覚的に示す
                bool canAddToDeck = CanAddCardToDeck(card);
                cardUI.SetInteractable(canAddToDeck);
                
                // ドラッグハンドラの追加
                if (cardObj.GetComponent<CardDragHandler>() == null)
                {
                    cardObj.AddComponent<CardDragHandler>();
                }
                
                // クリックイベント追加
                Button cardButton = cardObj.GetComponent<Button>();
                Card cardRef = card; // ラムダ内でキャプチャ
                cardButton.onClick.AddListener(() => ShowCardDetail(cardRef, true));
            }
        }
    }
    
    /// <summary>
    /// デッキのグリッド更新
    /// </summary>
    private void UpdateDeckGrid()
    {
        // グリッドのクリア
        foreach (Transform child in deckGrid)
        {
            Destroy(child.gameObject);
        }
        
        // ドロップエリアコンポーネントがなければ追加
        CardDropArea dropArea = deckGrid.GetComponent<CardDropArea>();
        if (dropArea == null)
        {
            dropArea = deckGrid.gameObject.AddComponent<CardDropArea>();
            dropArea.areaType = CardDropArea.AreaType.DeckGrid;
        }
        
        // ソートしたデッキリスト
        List<Card> sortedDeck = new List<Card>(currentDeck);
        SortCardList(sortedDeck);
        
        // カード表示
        foreach (Card card in sortedDeck)
        {
            GameObject cardObj = Instantiate(cardPrefab, deckGrid);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // カードをスケーリング
                RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one * gridCardScale;
                }
                
                // ドラッグハンドラの追加
                if (cardObj.GetComponent<CardDragHandler>() == null)
                {
                    cardObj.AddComponent<CardDragHandler>();
                }
                
                // クリックイベント追加
                Button cardButton = cardObj.GetComponent<Button>();
                Card cardRef = card; // ラムダ内でキャプチャ
                cardButton.onClick.AddListener(() => ShowCardDetail(cardRef, false));
            }
        }
    }
    
    /// <summary>
    /// カードリストをソート
    /// </summary>
    private void SortCardList(List<Card> cards)
    {
        switch (currentSortType)
        {
            case SortType.Name:
                cards.Sort((a, b) => isAscending 
                    ? a.cardName.CompareTo(b.cardName) 
                    : b.cardName.CompareTo(a.cardName));
                break;
                
            case SortType.Cost:
                cards.Sort((a, b) => isAscending 
                    ? a.cost.CompareTo(b.cost) 
                    : b.cost.CompareTo(a.cost));
                break;
                
            case SortType.Type:
                cards.Sort((a, b) => {
                    int typeCompare = isAscending 
                        ? a.type.CompareTo(b.type) 
                        : b.type.CompareTo(a.type);
                    
                    // 同じタイプの場合は名前でソート
                    if (typeCompare == 0)
                    {
                        return a.cardName.CompareTo(b.cardName);
                    }
                    return typeCompare;
                });
                break;
                
            case SortType.Rarity:
                cards.Sort((a, b) => {
                    // キャラクターカードの攻撃力をレア度の代わりに使用
                    int aValue = a is CharacterCard ca ? ca.attackPower : a.cost;
                    int bValue = b is CharacterCard cb ? cb.attackPower : b.cost;
                    
                    return isAscending 
                        ? aValue.CompareTo(bValue) 
                        : bValue.CompareTo(aValue);
                });
                break;
        }
    }
    
    /// <summary>
    /// カード枚数表示の更新
    /// </summary>
    private void UpdateCountTexts()
    {
        if (collectionCountText != null)
        {
            collectionCountText.text = $"倉庫枚数\n{cardCollection.Count}枚";
        }
        
        if (deckCountText != null)
        {
            int maxDeckSize = DeckDataManager.Instance != null ? 
                DeckDataManager.Instance.maxDeckSize : 60;
                
            deckCountText.text = $"デッキ枚数\n{currentDeck.Count}/{maxDeckSize}";
            
            // 最小デッキサイズを下回る場合は赤色に
            int minDeckSize = DeckDataManager.Instance != null ? 
                DeckDataManager.Instance.minDeckSize : 40;
                
            deckCountText.color = currentDeck.Count < minDeckSize ? Color.red : Color.white;
        }
    }
    
    /// <summary>
    /// カード詳細を表示
    /// </summary>
    public void ShowCardDetail(Card card, bool isFromCollection)
    {
        if (card == null || detailPanel == null) return;
        
        currentDetailCard = card;
        detailPanel.SetActive(true);
        
        // カード画像の表示 - artworkがstring型に変更されたため修正
        if (detailCardImage != null && !string.IsNullOrEmpty(card.artwork))
        {
            // 文字列からSpriteをロード
            Sprite artworkSprite = LoadCardSprite(card);
            if (artworkSprite != null) 
            {
                detailCardImage.sprite = artworkSprite;
                detailCardImage.color = Color.white;
            }
            else
            {
                // 画像が見つからない場合
                detailCardImage.sprite = null;
                detailCardImage.color = Color.gray;
                Debug.LogWarning($"カード {card.cardName} のアートワークをロードできませんでした: {card.artwork}");
            }
        }
        
        // カード情報の表示
        if (cardNameText != null)
            cardNameText.text = card.cardName;
            
        if (cardTypeText != null)
            cardTypeText.text = GetCardTypeText(card);
            
        if (cardDescriptionText != null)
            cardDescriptionText.text = card.description;
        
        // カードタイプ固有の情報
        if (card is CharacterCard charCard)
        {
            // キャラクターカード
            if (cardAttributesText != null)
                cardAttributesText.text = $"属性: {GetElementText(charCard.element)}\n" +
                                         $"攻撃力: {charCard.attackPower}\n" +
                                         $"守備力: {charCard.defensePower}\n" +
                                         $"カテゴリー: {GetCategoryDisplayText(charCard)}";
                                         
            if (cardEffectText != null)
                cardEffectText.text = GetCardEffectText(charCard);
        }
        else if (card is SpellCard spellCard)
        {
            // スペルカード
            if (cardAttributesText != null)
                cardAttributesText.text = $"コスト: {spellCard.cost}\n" +
                                         $"スペルタイプ: {GetSpellTypeText(spellCard.spellType)}";
                                         
            if (cardEffectText != null)
                cardEffectText.text = GetCardEffectText(spellCard);
        }
        else if (card is FieldCard fieldCard)
        {
            // フィールドカード
            if (cardAttributesText != null)
                cardAttributesText.text = $"コスト: {fieldCard.cost}";
                
            if (cardEffectText != null)
                cardEffectText.text = GetCardEffectText(fieldCard);
        }
        
        // カード枚数表示
        int inDeckCount = CountCardInDeck(card.id);
        int maxCopies = DeckDataManager.Instance != null ? 
                       DeckDataManager.Instance.maxCardCopies : 2;
                       
        if (cardCountText != null)
            cardCountText.text = $"投入枚数\nデッキ/倉庫\n{inDeckCount}/{maxCopies}";
            
        // ボタンの有効/無効設定
        if (addCardButton != null)
            addCardButton.interactable = isFromCollection && CanAddCardToDeck(card);
            
        if (removeCardButton != null)
            removeCardButton.interactable = !isFromCollection && inDeckCount > 0;
    }

    private string GetCategoryDisplayText(CharacterCard card)
    {
        // 既存のGetCategoryDisplayTextメソッドがあれば使用
        var method = card.GetType().GetMethod("GetCategoryDisplayText");
        if (method != null)
        {
            try
            {
                return method.Invoke(card, null) as string;
            }
            catch
            {
                return "不明";
            }
        }
        
        // カテゴリーリストから直接表示文字列を作成
        if (card.categories != null && card.categories.Count > 0)
        {
            List<string> categoryNames = new List<string>();
            foreach (var category in card.categories)
            {
                if (category != null)
                    categoryNames.Add(category.categoryName);
            }
            return string.Join(", ", categoryNames);
        }
        
        return "なし";
    }
    
    /// <summary>
    /// カードタイプの表示テキストを取得
    /// </summary>
    private string GetCardTypeText(Card card)
    {
        switch (card.type)
        {
            case CardType.Character: return "キャラクター";
            case CardType.Spell: return "スペル";
            case CardType.Field: return "フィールド";
            default: return "不明";
        }
    }
    
    /// <summary>
    /// 属性の表示テキストを取得
    /// </summary>
    private string GetElementText(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Fire: return "火";
            case ElementType.Water: return "水";
            case ElementType.Earth: return "地";
            case ElementType.Wind: return "風";
            case ElementType.Light: return "光";
            case ElementType.Dark: return "闇";
            case ElementType.Neutral: return "無";
            default: return "不明";
        }
    }
    
    /// <summary>
    /// スペルタイプの表示テキストを取得
    /// </summary>
    private string GetSpellTypeText(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.Draw: return "ドロー";
            case SpellType.Buff: return "強化";
            case SpellType.Debuff: return "弱体化";
            case SpellType.LifeDamage: return "ダメージ";
            case SpellType.LifeHeal: return "回復";
            case SpellType.Resurrection: return "蘇生";
            case SpellType.DeckDestruction: return "デッキ破壊";
            case SpellType.CardDestruction: return "カード破壊";
            case SpellType.HandDestruction: return "手札破壊";
            default: return "その他";
        }
    }
    
    /// <summary>
    /// カード効果テキストを取得
    /// </summary>
    private string GetCardEffectText(Card card)
    {
        if (card is CharacterCard charCard && charCard.effects != null)
        {
            // キャラクターカードの効果
            string effectText = "カードの効果:\n";
            foreach (var effect in charCard.effects)
            {
                if (effect != null)
                {
                    try
                    {
                        // GetDescriptionメソッドを持つ場合は使用
                        var method = effect.GetType().GetMethod("GetDescription");
                        if (method != null)
                        {
                            effectText += method.Invoke(effect, null) as string + "\n";
                        }
                        else
                        {
                            effectText += "効果の詳細不明\n";
                        }
                    }
                    catch
                    {
                        effectText += "効果の取得エラー\n";
                    }
                }
            }
            return effectText;
        }
        else if (card is SpellCard spellCard && spellCard.effects != null)
        {
            // スペルカードの効果
            string effectText = "カードの効果:\n";
            foreach (var effect in spellCard.effects)
            {
                if (effect != null)
                {
                    try
                    {
                        var method = effect.GetType().GetMethod("GetDescription");
                        if (method != null)
                        {
                            effectText += method.Invoke(effect, null) as string + "\n";
                        }
                        else
                        {
                            effectText += "効果の詳細不明\n";
                        }
                    }
                    catch
                    {
                        effectText += "効果の取得エラー\n";
                    }
                }
            }
            return effectText;
        }
        else if (card is FieldCard fieldCard && fieldCard.effects != null)
        {
            // フィールドカードの効果
            string effectText = "カードの効果:\n";
            foreach (var effect in fieldCard.effects)
            {
                if (effect != null)
                {
                    try
                    {
                        var method = effect.GetType().GetMethod("GetDescription");
                        if (method != null)
                        {
                            effectText += method.Invoke(effect, null) as string + "\n";
                        }
                        else
                        {
                            effectText += "効果の詳細不明\n";
                        }
                    }
                    catch
                    {
                        effectText += "効果の取得エラー\n";
                    }
                }
            }
            return effectText;
        }
        
        return "効果なし";
    }
    
    /// <summary>
    /// 詳細パネルを閉じる
    /// </summary>
    public void CloseDetailPanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        currentDetailCard = null;
    }
    
    /// <summary>
    /// カードをデッキに追加
    /// </summary>
    public void AddCardToDeck()
    {
        if (currentDetailCard == null) return;
        
        // 追加処理を共通メソッドに委譲
        AddCardToCurrentDeck(currentDetailCard);
        
        // 詳細表示も更新
        ShowCardDetail(currentDetailCard, true);
    }
    
    /// <summary>
    /// カードをデッキから削除
    /// </summary>
    public void RemoveCardFromDeck()
    {
        if (currentDetailCard == null) return;
        
        // 削除処理を共通メソッドに委譲
        RemoveCardFromCurrentDeck(currentDetailCard);
        
        // 詳細表示も更新
        ShowCardDetail(currentDetailCard, false);
    }
    
    /// <summary>
    /// 指定したカードをデッキに追加（ドラッグ＆ドロップ対応）
    /// </summary>
    public bool AddCardToCurrentDeck(Card card)
    {
        if (card == null) return false;
        
        // デッキに追加可能か確認
        if (CanAddCardToDeck(card))
        {
            // カードをコピーしてデッキに追加
            Card cardCopy = Instantiate(card);
            currentDeck.Add(cardCopy);
            
            // UIを更新
            UpdateDeckGrid();
            UpdateCountTexts();
            
            // 追加成功
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 指定したカードをデッキから削除（ドラッグ＆ドロップ対応）
    /// </summary>
    public bool RemoveCardFromCurrentDeck(Card card)
    {
        if (card == null) return false;
        
        // デッキから一致するカードを探して削除
        for (int i = 0; i < currentDeck.Count; i++)
        {
            if (currentDeck[i].id == card.id)
            {
                Card cardToRemove = currentDeck[i];
                currentDeck.RemoveAt(i);
                Destroy(cardToRemove); // カードインスタンスを破棄
                
                // UIを更新
                UpdateDeckGrid();
                UpdateCountTexts();
                
                // 削除成功
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// デッキ内の特定IDのカード枚数をカウント
    /// </summary>
    private int CountCardInDeck(int cardId)
    {
        int count = 0;
        foreach (Card card in currentDeck)
        {
            if (card.id == cardId)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// カードをデッキに追加可能か確認
    /// </summary>
    private bool CanAddCardToDeck(Card card)
    {
        if (card == null) return false;
        
        // デッキ最大サイズチェック
        int maxDeckSize = DeckDataManager.Instance != null ? 
                         DeckDataManager.Instance.maxDeckSize : 60;
                         
        if (currentDeck.Count >= maxDeckSize)
            return false;
            
        // 同一カードの枚数チェック
        int inDeckCount = CountCardInDeck(card.id);
        int maxCopies = DeckDataManager.Instance != null ? 
                       DeckDataManager.Instance.maxCardCopies : 2;
                       
        return inDeckCount < maxCopies;
    }
    
    /// <summary>
    /// カードがフィルター条件に一致するか確認
    /// </summary>
    private bool MatchesFilter(Card card)
    {
        // カードタイプフィルター
        if (filterCardType.HasValue && card.type != filterCardType.Value)
            return false;
            
        // 属性フィルター（キャラクターカードの場合）
        if (filterElementType.HasValue && card is CharacterCard charCard)
        {
            if (charCard.element != filterElementType.Value)
                return false;
        }
        
        // 検索テキスト
        if (!string.IsNullOrEmpty(searchText))
        {
            string lowerSearch = searchText.ToLower();
            if (!card.cardName.ToLower().Contains(lowerSearch) && 
                !card.description.ToLower().Contains(lowerSearch))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 検索テキストが変更された時の処理
    /// </summary>
    private void OnSearchTextChanged(string text)
    {
        searchText = text;
        UpdateCollectionGrid();
    }
    
    /// <summary>
    /// ソート方法が変更された時の処理
    /// </summary>
    private void OnSortChanged(int value)
    {
        switch (value)
        {
            case 0: currentSortType = SortType.Name; break;
            case 1: currentSortType = SortType.Cost; break;
            case 2: currentSortType = SortType.Type; break;
            case 3: currentSortType = SortType.Rarity; break;
            default: currentSortType = SortType.Name; break;
        }
        
        UpdateCollectionGrid();
        UpdateDeckGrid();
    }
    
    /// <summary>
    /// フィルターパネルの表示切替
    /// </summary>
    private void ToggleFilterPanel()
    {
        if (filterPanel != null)
        {
            filterPanel.SetActive(!filterPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// カードタイプでフィルター
    /// </summary>
    public void FilterByCardType(int typeIndex)
    {
        switch (typeIndex)
        {
            case 0: filterCardType = null; break; // すべて
            case 1: filterCardType = CardType.Character; break;
            case 2: filterCardType = CardType.Spell; break;
            case 3: filterCardType = CardType.Field; break;
            default: filterCardType = null; break;
        }
        
        UpdateCollectionGrid();
    }
    
    /// <summary>
    /// 属性でフィルター
    /// </summary>
    public void FilterByElement(int elementIndex)
    {
        switch (elementIndex)
        {
            case 0: filterElementType = null; break; // すべて
            case 1: filterElementType = ElementType.Fire; break;
            case 2: filterElementType = ElementType.Water; break;
            case 3: filterElementType = ElementType.Earth; break;
            case 4: filterElementType = ElementType.Wind; break;
            case 5: filterElementType = ElementType.Light; break;
            case 6: filterElementType = ElementType.Dark; break;
            case 7: filterElementType = ElementType.Neutral; break;
            default: filterElementType = null; break;
        }
        
        UpdateCollectionGrid();
    }
    
    /// <summary>
    /// すべてのフィルターをリセット
    /// </summary>
    public void ResetFilters()
    {
        filterCardType = null;
        filterElementType = null;
        
        if (searchInput != null)
            searchInput.text = "";
            
        searchText = "";
        
        UpdateCollectionGrid();
    }
    
    /// <summary>
    /// デッキを保存
    /// </summary>
    public void SaveDeck()
    {
        // デッキサイズチェック
        int minDeckSize = DeckDataManager.Instance != null ? 
                         DeckDataManager.Instance.minDeckSize : 40;
                         
        if (currentDeck.Count < minDeckSize)
        {
            Debug.LogWarning($"デッキは最低{minDeckSize}枚必要です。");
            
            // 警告メッセージ表示
            UIManager uiManager = UnityEngine.Object.FindFirstObjectByType<UIManager>();
            if (uiManager != null)
            {
                uiManager.ShowNotification($"デッキは最低{minDeckSize}枚必要です", 3f);
            }
            
            return;
        }
        
        // デッキデータが初期化されていなければ作成
        if (currentDeckData == null && DeckDataManager.Instance != null)
        {
            currentDeckData = DeckDataManager.Instance.CreateNewDeck("マイデッキ", "");
        }
        
        if (currentDeckData != null)
        {
            // デッキデータを更新
            currentDeckData.cardIds.Clear();
            foreach (Card card in currentDeck)
            {
                currentDeckData.cardIds.Add(card.id);
            }
            
            // デッキカバーを設定（最初のカードを使用）
            if (currentDeck.Count > 0)
            {
                currentDeckData.coverCardId = currentDeck[0].id.ToString();
            }
            
            // DeckDataManagerに保存
            if (DeckDataManager.Instance != null)
            {
                DeckDataManager.Instance.UpdateDeck(currentDeckData);
                
                // 通知表示
                UIManager uiManager = UnityEngine.Object.FindFirstObjectByType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.ShowNotification("デッキを保存しました", 2f);
                }
            }
            else
            {
                Debug.LogWarning("DeckDataManagerが見つかりません。デッキの保存に失敗しました。");
            }
        }
    }
    
    /// <summary>
    /// メインメニューに戻る
    /// </summary>
    public void ReturnToMainMenu()
    {
        // 変更があれば保存確認
        // 簡略化：直接メインメニューに戻る
        SceneController sceneController = UnityEngine.Object.FindFirstObjectByType<SceneController>();
        if (sceneController != null)
        {
            sceneController.ReturnToMainMenu();
        }
        else
        {
            // SceneControllerがない場合は直接シーン遷移
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }
    }
    
    /// <summary>
    /// カード作成パネルを表示
    /// </summary>
    public void ShowCreateCardPanel()
    {
        if (createCardPanel != null)
        {
            createCardPanel.SetActive(true);
        }
    }

    /// <summary>
    /// カードスプライトをロードするヘルパーメソッド
    /// </summary>
    private Sprite LoadCardSprite(Card card)
    {
        if (card == null || string.IsNullOrEmpty(card.artwork)) return null;
        
        // 検索する候補パスのリスト
        string[] pathCandidates = new string[]
        {
            $"CardImages/character/{card.artwork}",
            $"CardImages/{card.artwork}", 
            $"CardImages/Cards/{card.artwork}",
            $"CardImages/All/{card.artwork}"
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
}