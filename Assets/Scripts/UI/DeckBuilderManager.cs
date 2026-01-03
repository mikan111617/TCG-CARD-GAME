using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// デッキビルダー画面の管理クラス
/// </summary>
public class DeckBuilderManager : MonoBehaviour
{
    [Header("デッキリスト")]
    public Transform deckListContainer;       // デッキリスト表示用の親オブジェクト
    public GameObject deckItemPrefab;         // デッキアイテムのプレハブ
    public Button newDeckButton;              // 新規デッキ作成ボタン
    public Button deleteDeckButton;           // デッキ削除ボタン
    
    [Header("デッキ編集")]
    public TMP_InputField deckNameInput;      // デッキ名入力フィールド
    public TMP_InputField deckDescriptionInput; // デッキ説明入力フィールド
    public TextMeshProUGUI cardCountText;     // カード枚数表示
    public Button saveButton;                 // 保存ボタン
    public TextMeshProUGUI validationText;    // 検証結果表示
    
    [Header("カードブラウザ")]
    public Transform cardCollectionParent;    // コレクション表示用の親オブジェクト
    public Transform currentDeckParent;       // 現在のデッキ表示用の親オブジェクト
    public GameObject cardPrefab;             // カードプレハブ
    public Button filterButton;               // フィルターボタン
    public TMP_Dropdown sortDropdown;         // ソートドロップダウン
    public TMP_InputField searchInput;        // 検索入力フィールド
    
    [Header("カード詳細")]
    public GameObject cardDetailPanel;        // カード詳細パネル
    
    // フィルタリング設定
    private CardType? filterCardType = null;
    private ElementType? filterElementType = null;
    private int filterMinCost = -1;
    private int filterMaxCost = -1;
    private string searchText = "";
    
    // ソート設定
    private enum SortType { Name, Cost, Type, Element }
    private SortType currentSortType = SortType.Name;
    private bool sortAscending = true;
    
    // 現在のコレクションとデッキ
    private List<Card> cardCollection = new List<Card>();
    private List<Card> currentDeck = new List<Card>();
    
    // 現在編集中のデッキ
    private DeckData currentDeckData;
    
    // 詳細表示中のカード
    private Card detailedCard;
    
    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start()
    {
        // ボタンにイベントリスナーを追加
        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(CreateNewDeck);
            
        if (deleteDeckButton != null)
            deleteDeckButton.onClick.AddListener(DeleteCurrentDeck);
            
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveDeck);
            
        if (filterButton != null)
            filterButton.onClick.AddListener(OpenFilterDialog);
            
        if (sortDropdown != null)
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
            
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchTextChanged);
        
        // カードコレクションをロード
        LoadCardCollection();
        
        // デッキリストを更新
        UpdateDeckList();
        
        // 前回選択したデッキをロード
        LoadLastSelectedDeck();
    }
    
    /// <summary>
    /// 前回選択したデッキをロード
    /// </summary>
    private void LoadLastSelectedDeck()
    {
        if (DeckDataManager.Instance != null)
        {
            int lastDeckId = DeckDataManager.Instance.GetCurrentDeckId();
            DeckData lastDeck = DeckDataManager.Instance.GetDeckById(lastDeckId);
            
            if (lastDeck != null)
            {
                SelectDeck(lastDeck);
            }
            else if (DeckDataManager.Instance.GetAllDecks().Count > 0)
            {
                // 前回のデッキが見つからない場合は最初のデッキを選択
                SelectDeck(DeckDataManager.Instance.GetAllDecks()[0]);
            }
        }
    }
    
    /// <summary>
    /// カードコレクションのロード
    /// </summary>
    private void LoadCardCollection()
    {
        cardCollection.Clear();
        
        // カードデータベースから全カードをロード
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        
        if (database == null)
        {
            Debug.LogError("CardDatabaseが見つかりません。カードコレクションのロードに失敗しました。");
            return;
        }
        
        // 全カードを取得
        List<Card> allCards = database.GetAllCards();
        
        // プレイヤーの所持カードを設定（本来はユーザーデータから取得）
        // 仮実装：各カードを最大枚数所持している想定
        foreach (Card card in allCards)
        {
            // 各カードの所持上限枚数（DeckDataManagerから取得）
            int maxCopies = DeckDataManager.Instance != null ? 
                            DeckDataManager.Instance.maxCardCopies : 2;
            
            // 各カードを最大枚数分所持
            for (int i = 0; i < maxCopies; i++)
            {
                Card cardCopy = Instantiate(card);
                cardCollection.Add(cardCopy);
            }
        }
        
        // カードコレクション表示を更新
        UpdateCardCollectionUI();
    }
    
    /// <summary>
    /// デッキリストの更新
    /// </summary>
    private void UpdateDeckList()
    {
        // 既存の項目をクリア
        foreach (Transform child in deckListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // デッキマネージャーから全デッキを取得
        if (DeckDataManager.Instance == null)
        {
            Debug.LogError("DeckDataManagerが見つかりません。");
            return;
        }
        
        List<DeckData> allDecks = DeckDataManager.Instance.GetAllDecks();
        
        // 現在選択中のデッキID
        int currentDeckId = currentDeckData != null ? currentDeckData.deckId : -1;
        
        // リストに追加
        foreach (DeckData deck in allDecks)
        {
            GameObject deckItemObj = Instantiate(deckItemPrefab, deckListContainer);
            DeckListItem deckItem = deckItemObj.GetComponent<DeckListItem>();
            
            if (deckItem != null)
            {
                deckItem.SetupDeckItem(deck);
                
                // 選択状態を設定
                deckItem.SetSelected(deck.deckId == currentDeckId);
                
                // 選択イベントの設定
                Button itemButton = deckItemObj.GetComponent<Button>();
                if (itemButton != null)
                {
                    DeckData deckRef = deck; // ラムダ内でキャプチャするためにローカル変数
                    itemButton.onClick.AddListener(() => SelectDeck(deckRef));
                }
            }
        }
        
        // 削除ボタンの有効/無効設定
        if (deleteDeckButton != null)
        {
            // デッキが1つしかない場合は削除ボタンを無効化
            deleteDeckButton.interactable = allDecks.Count > 1;
        }
    }
    
    /// <summary>
    /// デッキを選択
    /// </summary>
    private void SelectDeck(DeckData deck)
    {
        currentDeckData = deck;
        
        // DeckDataManagerに現在のデッキIDを設定
        if (DeckDataManager.Instance != null)
        {
            DeckDataManager.Instance.SetCurrentDeckId(deck.deckId);
        }
        
        // UI更新
        if (deckNameInput != null)
            deckNameInput.text = deck.deckName;
            
        if (deckDescriptionInput != null)
            deckDescriptionInput.text = deck.description;
        
        // デッキリスト内の選択状態を更新
        foreach (Transform child in deckListContainer)
        {
            DeckListItem item = child.GetComponent<DeckListItem>();
            if (item != null)
            {
                item.SetSelected(item.GetDeckId() == deck.deckId);
            }
        }
        
        // デッキの内容を読み込み
        LoadDeckContents(deck);
    }
    
    /// <summary>
    /// デッキの内容を読み込み
    /// </summary>
    private void LoadDeckContents(DeckData deck)
    {
        currentDeck.Clear();
        
        CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
        
        if (database == null)
        {
            Debug.LogError("CardDatabaseが見つかりません。デッキ内容のロードに失敗しました。");
            return;
        }
        
        foreach (int cardId in deck.cardIds)
        {
            Card card = database.GetCardById(cardId);
            if (card != null)
            {
                Card cardCopy = Instantiate(card);
                currentDeck.Add(cardCopy);
            }
        }
        
        // UI更新
        UpdateCurrentDeckUI();
        UpdateCardCountText();
        ValidateDeck();
    }
    
    /// <summary>
    /// カードコレクションのUI更新
    /// </summary>
    private void UpdateCardCollectionUI()
    {
        // 既存の表示をクリア
        foreach (Transform child in cardCollectionParent)
        {
            Destroy(child.gameObject);
        }
        
        // フィルタリングされたカードのリスト
        List<Card> filteredCards = cardCollection
            .Where(card => MatchesFilter(card))
            .ToList();
        
        // ソート
        SortCards(filteredCards);
        
        // カードコレクションを表示
        foreach (Card card in filteredCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardCollectionParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // カードがデッキ追加可能か判定（デッキ内の同一カード枚数）
                bool canAddToDeck = CanAddCardToDeck(card);
                cardUI.SetInteractable(canAddToDeck);
                
                // クリックイベントを追加
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    Card cardRef = card; // ラムダ内でキャプチャするためのローカル変数
                    cardButton.onClick.AddListener(() => OnCollectionCardClicked(cardRef));
                }
            }
        }
    }
    
    /// <summary>
    /// 現在のデッキのUI更新
    /// </summary>
    private void UpdateCurrentDeckUI()
    {
        // 既存の表示をクリア
        foreach (Transform child in currentDeckParent)
        {
            Destroy(child.gameObject);
        }
        
        // ソート
        List<Card> sortedDeck = new List<Card>(currentDeck);
        SortCards(sortedDeck);
        
        // 現在のデッキを表示
        foreach (Card card in sortedDeck)
        {
            GameObject cardObj = Instantiate(cardPrefab, currentDeckParent);
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // クリックイベントを追加
                Button cardButton = cardObj.GetComponent<Button>();
                if (cardButton != null)
                {
                    Card cardRef = card; // ラムダ内でキャプチャするためのローカル変数
                    cardButton.onClick.AddListener(() => OnDeckCardClicked(cardRef));
                }
            }
        }
        
        // カードコレクションの有効/無効状態も更新
        UpdateCardCollectionUI();
    }
    
    /// <summary>
    /// カード枚数表示の更新
    /// </summary>
    private void UpdateCardCountText()
    {
        if (cardCountText != null)
        {
            int minDeckSize = DeckDataManager.Instance != null ? 
                              DeckDataManager.Instance.minDeckSize : 40;
            int maxDeckSize = DeckDataManager.Instance != null ? 
                              DeckDataManager.Instance.maxDeckSize : 60;
                              
            cardCountText.text = $"{currentDeck.Count} / {maxDeckSize}";
            
            // 枚数が不足している場合は赤色に
            cardCountText.color = (currentDeck.Count < minDeckSize) ? Color.red : Color.white;
        }
    }
    
    /// <summary>
    /// デッキの妥当性チェック
    /// </summary>
    private void ValidateDeck()
    {
        if (DeckDataManager.Instance == null || currentDeckData == null)
            return;
            
        // 現在のデッキからデッキデータを更新
        currentDeckData.cardIds.Clear();
        foreach (Card card in currentDeck)
        {
            currentDeckData.cardIds.Add(card.id);
        }
        
        // デッキの妥当性をチェック
        string validationMessage = DeckDataManager.Instance.ValidateDeck(currentDeckData);
        
        // 結果を表示
        if (validationText != null)
        {
            if (string.IsNullOrEmpty(validationMessage))
            {
                validationText.text = "デッキは有効です";
                validationText.color = Color.green;
            }
            else
            {
                validationText.text = validationMessage;
                validationText.color = Color.red;
            }
        }
        
        // 保存ボタンの有効/無効設定
        if (saveButton != null)
        {
            saveButton.interactable = string.IsNullOrEmpty(validationMessage);
        }
    }
    
    /// <summary>
    /// カードがフィルター条件に一致するかチェック
    /// </summary>
    private bool MatchesFilter(Card card)
    {
        // カードタイプフィルター
        if (filterCardType.HasValue && card.type != filterCardType.Value)
        {
            return false;
        }
        
        // 属性フィルター（キャラクターカードのみ）
        if (filterElementType.HasValue && card is CharacterCard charCard)
        {
            if (charCard.element != filterElementType.Value)
            {
                return false;
            }
        }
        
        // コストフィルター
        if (filterMinCost >= 0 && card.cost < filterMinCost)
        {
            return false;
        }
        
        if (filterMaxCost >= 0 && card.cost > filterMaxCost)
        {
            return false;
        }
        
        // 検索テキスト
        if (!string.IsNullOrEmpty(searchText))
        {
            if (!card.cardName.ToLower().Contains(searchText.ToLower()) &&
                !card.description.ToLower().Contains(searchText.ToLower()))
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// カードリストをソート
    /// </summary>
    private void SortCards(List<Card> cards)
    {
        switch (currentSortType)
        {
            case SortType.Name:
                if (sortAscending)
                    cards.Sort((a, b) => a.cardName.CompareTo(b.cardName));
                else
                    cards.Sort((a, b) => b.cardName.CompareTo(a.cardName));
                break;
                
            case SortType.Cost:
                if (sortAscending)
                    cards.Sort((a, b) => a.cost.CompareTo(b.cost));
                else
                    cards.Sort((a, b) => b.cost.CompareTo(a.cost));
                break;
                
            case SortType.Type:
                if (sortAscending)
                    cards.Sort((a, b) => a.type.CompareTo(b.type));
                else
                    cards.Sort((a, b) => b.type.CompareTo(a.type));
                break;
                
            case SortType.Element:
                if (sortAscending)
                    cards.Sort((a, b) => GetCardElement(a).CompareTo(GetCardElement(b)));
                else
                    cards.Sort((a, b) => GetCardElement(b).CompareTo(GetCardElement(a)));
                break;
        }
    }
    
    /// <summary>
    /// カードの属性を取得（キャラクターカード以外は中立属性とみなす）
    /// </summary>
    private ElementType GetCardElement(Card card)
    {
        if (card is CharacterCard charCard)
        {
            return charCard.element;
        }
        
        return ElementType.Neutral;
    }
    
    /// <summary>
    /// カードがデッキに追加可能かチェック
    /// </summary>
    private bool CanAddCardToDeck(Card card)
    {
        // デッキ最大サイズチェック
        if (DeckDataManager.Instance != null && 
            currentDeck.Count >= DeckDataManager.Instance.maxDeckSize)
        {
            return false;
        }
        
        // 同一カードの枚数チェック
        int sameCardCount = 0;
        foreach (Card deckCard in currentDeck)
        {
            if (deckCard.id == card.id)
            {
                sameCardCount++;
            }
        }
        
        int maxCopies = DeckDataManager.Instance != null ? 
                        DeckDataManager.Instance.maxCardCopies : 2;
                        
        return sameCardCount < maxCopies;
    }
    
    /// <summary>
    /// コレクションのカードがクリックされた時の処理
    /// </summary>
    private void OnCollectionCardClicked(Card card)
    {
        // カード詳細表示
        ShowCardDetail(card);
        
        // カードをデッキに追加可能か確認
        if (CanAddCardToDeck(card))
        {
            AddCardToDeck(card);
        }
    }
    
    /// <summary>
    /// デッキ内のカードがクリックされた時の処理
    /// </summary>
    private void OnDeckCardClicked(Card card)
    {
        // カード詳細表示
        ShowCardDetail(card);
        
        // カードをデッキから削除
        RemoveCardFromDeck(card);
    }
    
    /// <summary>
    /// カード詳細パネルを表示
    /// </summary>
    private void ShowCardDetail(Card card)
    {
        detailedCard = card;
        
        if (cardDetailPanel != null)
        {
            cardDetailPanel.SetActive(true);
            
            // カード詳細パネルに情報を設定
            CardDetailPanel detailPanel = cardDetailPanel.GetComponent<CardDetailPanel>();
            if (detailPanel != null)
            {
                detailPanel.SetupCardDetail(card);
                
                // カード所持数と使用数をチェック
                int usedCount = CountCardsInDeck(card.id);
                int maxCopies = DeckDataManager.Instance != null ? 
                                DeckDataManager.Instance.maxCardCopies : 2;
                                
                detailPanel.UpdateCardCount(usedCount, maxCopies);
                
                // ボタンの有効/無効を設定
                detailPanel.SetAddButtonEnabled(CanAddCardToDeck(card));
                detailPanel.SetRemoveButtonEnabled(usedCount > 0);
            }
        }
    }
    
    /// <summary>
    /// デッキ内の特定カードの枚数を数える
    /// </summary>
    private int CountCardsInDeck(int cardId)
    {
        int count = 0;
        foreach (Card card in currentDeck)
        {
            if (card.id == cardId)
                count++;
        }
        return count;
    }
    
    /// <summary>
    /// カードをデッキに追加
    /// </summary>
    public void AddCardToDeck(Card card)
    {
        if (card == null || !CanAddCardToDeck(card))
            return;
            
        // カードのコピーを作成してデッキに追加
        Card cardCopy = Instantiate(card);
        currentDeck.Add(cardCopy);
        
        // UI更新
        UpdateCurrentDeckUI();
        UpdateCardCountText();
        ValidateDeck();
        
        // カード詳細表示も更新
        if (detailedCard != null && detailedCard.id == card.id)
        {
            ShowCardDetail(detailedCard);
        }
    }
    
    /// <summary>
    /// カードをデッキから削除
    /// </summary>
    public void RemoveCardFromDeck(Card card)
    {
        if (card == null)
            return;
            
        // デッキからカードを削除
        for (int i = 0; i < currentDeck.Count; i++)
        {
            if (currentDeck[i].id == card.id)
            {
                Card removedCard = currentDeck[i];
                currentDeck.RemoveAt(i);
                Destroy(removedCard);
                break;
            }
        }
        
        // UI更新
        UpdateCurrentDeckUI();
        UpdateCardCountText();
        ValidateDeck();
        
        // カード詳細表示も更新
        if (detailedCard != null && detailedCard.id == card.id)
        {
            ShowCardDetail(detailedCard);
        }
    }
    
    /// <summary>
    /// 新しいデッキを作成
    /// </summary>
    public void CreateNewDeck()
    {
        if (DeckDataManager.Instance == null)
            return;
            
        DeckData newDeck = DeckDataManager.Instance.CreateNewDeck("新しいデッキ", "説明を入力してください");
        
        // デッキリスト更新
        UpdateDeckList();
        
        // 新しいデッキを選択
        SelectDeck(newDeck);
    }
    
    /// <summary>
    /// 現在のデッキを削除
    /// </summary>
    public void DeleteCurrentDeck()
    {
        if (DeckDataManager.Instance == null || currentDeckData == null)
            return;
            
        // 削除確認（実際の実装ではダイアログ表示）
        Debug.Log("デッキを削除します: " + currentDeckData.deckName);
        
        // デッキ削除
        bool deleted = DeckDataManager.Instance.DeleteDeck(currentDeckData.deckId);
        
        if (deleted)
        {
            // デッキリスト更新
            UpdateDeckList();
            
            // 別のデッキを選択
            List<DeckData> allDecks = DeckDataManager.Instance.GetAllDecks();
            if (allDecks.Count > 0)
            {
                SelectDeck(allDecks[0]);
            }
        }
        else
        {
            Debug.LogWarning("デッキの削除に失敗しました。");
        }
    }
    
    /// <summary>
    /// デッキを保存
    /// </summary>
    public void SaveDeck()
    {
        if (DeckDataManager.Instance == null || currentDeckData == null)
            return;
            
        // デッキ枚数チェック
        if (DeckDataManager.Instance.ValidateDeck(currentDeckData) != null)
        {
            Debug.LogWarning("デッキが無効です。保存できません。");
            return;
        }
        
        // デッキ名と説明を更新
        if (deckNameInput != null)
            currentDeckData.deckName = deckNameInput.text;
            
        if (deckDescriptionInput != null)
            currentDeckData.description = deckDescriptionInput.text;
            
        // カードIDをデッキデータに保存
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
        
        // デッキマネージャーに保存
        DeckDataManager.Instance.UpdateDeck(currentDeckData);
        
        // デッキリスト更新
        UpdateDeckList();
        
        // 保存成功メッセージ
        Debug.Log("デッキを保存しました: " + currentDeckData.deckName);
        
        // 通知表示（UIManagerがある場合）
        UIManager uiManager = UnityEngine.Object.FindFirstObjectByType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowNotification("デッキを保存しました", 2.0f);
        }
    }
    
    /// <summary>
    /// フィルターダイアログを開く
    /// </summary>
    private void OpenFilterDialog()
    {
        // フィルターUIの実装（別クラスで実装）
        // フィルター設定用のダイアログを表示
        // ...
        
        // 例としてリセット処理
        ResetFilters();
    }
    
    /// <summary>
    /// フィルターをリセット
    /// </summary>
    private void ResetFilters()
    {
        filterCardType = null;
        filterElementType = null;
        filterMinCost = -1;
        filterMaxCost = -1;
        searchText = "";
        
        if (searchInput != null)
            searchInput.text = "";
            
        // カードコレクション表示を更新
        UpdateCardCollectionUI();
    }
    
    /// <summary>
    /// ソート変更時の処理
    /// </summary>
    private void OnSortChanged(int index)
    {
        // ドロップダウンの値からソート方法を設定
        switch (index)
        {
            case 0: // 名前順
                currentSortType = SortType.Name;
                break;
                
            case 1: // コスト順
                currentSortType = SortType.Cost;
                break;
                
            case 2: // タイプ順
                currentSortType = SortType.Type;
                break;
                
            case 3: // 属性順
                currentSortType = SortType.Element;
                break;
                
            default:
                currentSortType = SortType.Name;
                break;
        }
        
        // 表示を更新
        UpdateCardCollectionUI();
        UpdateCurrentDeckUI();
    }
    
    /// <summary>
    /// 検索テキスト変更時の処理
    /// </summary>
    private void OnSearchTextChanged(string text)
    {
        searchText = text;
        UpdateCardCollectionUI();
    }
}