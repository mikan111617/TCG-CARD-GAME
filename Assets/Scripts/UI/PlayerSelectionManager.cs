using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;

// プレイヤー選択画面管理クラス
public class PlayerSelectionManager : MonoBehaviour
{
    [Header("UI要素")]
    public GameObject selectionPanel;           // 初期選択パネル
    public GameObject settingsPanel;            // 詳細設定パネル
    public Button vsAIButton;                   // AIと対戦ボタン
    public Button vsNetworkButton;              // ネットワーク対戦ボタン
    public Button optionsButton;                // 設定ボタン
    public Button backButton;                   // 戻るボタン
    public Button startGameButton;              // ゲーム開始ボタン
    
    [Header("プレイヤー設定")]
    public TMP_Dropdown player1DeckDropdown;    // プレイヤー1のデッキ選択
    public TMP_Dropdown aiPlayerDropdown;       // AIプレイヤー選択
    public TextMeshProUGUI aiPlayerDescriptionText; // AIプレイヤー説明テキスト
    
    [Header("シーン設定")]
    public string mainGameSceneName = "MainGameScene";  // メインゲームシーン名
    public string mainMenuSceneName = "MainMenuScene";  // メインメニューシーン名
    
    // 選択結果を保存する静的変数
    public static GameModeSettings SelectedGameMode { get; private set; }
    
    // AIプレイヤープロファイルリスト
    private List<AIPlayerProfile> aiProfiles = new List<AIPlayerProfile>();
    
    private void Start()
    {
        // AIプロファイルデータをロード
        LoadAIProfiles();
        
        // 初期化
        InitializeUI();
        
        // ボタンイベントの設定
        SetupButtonEvents();

        // DeckDataManagerをチェック・作成
        InitializeDeckDataManager();
        
        // ドロップダウンの設定
        PopulateDropdowns();

        // 初期状態では詳細設定パネルを非表示
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 選択パネルを表示
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(true);
        }
    }

    // デッキデータを直接読み込む（DeckDataManagerを使わずに）
    private void LoadDecksDirectly()
    {   
        try {
            // Resources/Decksフォルダから全てのJSONファイルを読み込む
            TextAsset[] deckFiles = Resources.LoadAll<TextAsset>("Decks");
            Debug.Log($"デッキファイル数: {deckFiles.Length}");
            
            List<DeckData> decks = new List<DeckData>();
            
            foreach(TextAsset file in deckFiles)
            {
                try
                {
                    Debug.Log($"デッキファイル読み込み: {file.name}");
                    string json = file.text;
                    DeckData deck = JsonUtility.FromJson<DeckData>(json);
                    
                    if (deck != null)
                    {
                        Debug.Log($"デッキ読み込み成功: {deck.deckName}");
                        decks.Add(deck);
                        
                        // DeckDataManagerのインスタンスがあれば登録
                        if (DeckDataManager.Instance != null)
                        {
                            DeckDataManager.Instance.UpdateDeck(deck);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"デッキファイル '{file.name}' の解析エラー: {ex.Message}");
                }
            }
            
            // デッキが1つも読み込めなかった場合はデフォルトのデッキを作成
            if (decks.Count == 0 && DeckDataManager.Instance != null)
            {
                CreateDefaultDeck();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"デッキデータ読み込み全体のエラー: {ex.Message}");
        }
    }

    // DeckDataManagerの初期化
    private void InitializeDeckDataManager()
    {
        if (DeckDataManager.Instance == null)
        {          
            // 新規GameObject作成
            GameObject ddmObject = new GameObject("DeckDataManager");
            DeckDataManager ddManager = ddmObject.AddComponent<DeckDataManager>();
            
            // シーン切り替えで破棄されないようにする
            DontDestroyOnLoad(ddmObject);
            
            // デッキデータを読み込み
            LoadDecksDirectly();
        }
    }

    // デフォルトデッキの作成
    private void CreateDefaultDeck()
    {
        Debug.Log("デフォルトデッキを作成します");
        
        DeckData defaultDeck = new DeckData
        {
            deckId = 1,
            deckName = "デフォルトデッキ",
            description = "システムが自動生成したデフォルトデッキ",
            cardIds = new List<int>()
        };
        
        // デフォルトカードIDの追加（例えば最初の40枚のカード）
        for (int i = 1; i <= 40; i++)
        {
            defaultDeck.cardIds.Add(i);
        }
        
        // DeckDataManagerに登録
        if (DeckDataManager.Instance != null)
        {
            DeckDataManager.Instance.UpdateDeck(defaultDeck);
            DeckDataManager.Instance.SaveUserDecks();
        }
    }
    
    // AIプロファイルをロード
    private void LoadAIProfiles()
    {
        aiProfiles.Clear();
        
        // Resources内のJSON読み込み
        TextAsset profilesAsset = Resources.Load<TextAsset>("AIPlayerProfiles");
        if (profilesAsset != null)
        {
            try
            {
                AIPlayerProfileList profileList = JsonUtility.FromJson<AIPlayerProfileList>(profilesAsset.text);
                if (profileList != null && profileList.profiles != null)
                {
                    aiProfiles = profileList.profiles;
                    Debug.Log($"AIプロファイル読み込み成功: {aiProfiles.Count}件");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AIプロファイル読み込みエラー: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("AIPlayerProfiles.jsonが見つかりません。デフォルトプロファイルを使用します。");
            // デフォルトプロファイルを作成
            CreateDefaultAIProfiles();
        }
    }
    
    // デフォルトAIプロファイル作成
    private void CreateDefaultAIProfiles()
    {
        // 初期AIプロファイル
        AIPlayerProfile easyAI = new AIPlayerProfile
        {
            name = "初級AI",
            difficulty = AIDifficulty.Easy,
            deckId = 1, // デフォルトデッキID
            description = "初心者向けAI。基本的な戦略のみ使用します。"
        };
        
        AIPlayerProfile normalAI = new AIPlayerProfile
        {
            name = "中級AI",
            difficulty = AIDifficulty.Normal,
            deckId = 2, // 別のデッキID
            description = "標準的なAI。バランスの取れた戦略を使用します。"
        };
        
        AIPlayerProfile hardAI = new AIPlayerProfile
        {
            name = "上級AI",
            difficulty = AIDifficulty.Hard,
            deckId = 3, // さらに別のデッキID
            description = "上級者向けAI。高度な戦略を使用します。"
        };
        
        aiProfiles.Add(easyAI);
        aiProfiles.Add(normalAI);
        aiProfiles.Add(hardAI);
    }
    
    // UI初期化
    private void InitializeUI()
    {
        // ゲームモード設定の初期化（初回または前回の設定を引き継ぐ）
        if (SelectedGameMode == null)
        {
            SelectedGameMode = new GameModeSettings
            {
                gameMode = GameMode.VsAI,
                player1DeckId = 0,
                aiProfileId = 0  // 最初のAIプロファイルを選択
            };
        }
        
        // 詳細設定パネルがない場合は作成（必要に応じて）
        if (settingsPanel == null)
        {
            Debug.LogWarning("詳細設定パネルが未定義です。UIを確認してください。");
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(transform);
        }
    }
    
    // ボタンイベントの設定
    private void SetupButtonEvents()
    {
        if (vsAIButton != null)
        {
            vsAIButton.onClick.RemoveAllListeners();
            vsAIButton.onClick.AddListener(OnVsAIButtonClicked);
        }
        else
        {
            Debug.LogError("vsAIButtonがnullです！インスペクタで設定してください。");
        }
            
        if (vsNetworkButton != null)
        {
            vsNetworkButton.onClick.RemoveAllListeners();
            vsNetworkButton.onClick.AddListener(OnVsNetworkButtonClicked);
        }
            
        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(OnOptionsButtonClicked);
        }
            
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }
        else
        {
            Debug.LogError("startGameButtonがnullです！インスペクタで設定してください。");
        }
    }
    
    // ドロップダウンにデータを設定
    private void PopulateDropdowns()
    {
        // プレイヤー1のデッキ選択ドロップダウン
        if (player1DeckDropdown != null)
        {
            player1DeckDropdown.ClearOptions();
            
            List<string> deckOptions = new List<string>();
            List<DeckData> allDecks = new List<DeckData>();
            
            // DeckDataManagerからデッキ一覧を取得
            if (DeckDataManager.Instance != null)
            {
                allDecks = DeckDataManager.Instance.GetAllDecks();
                Debug.Log($"利用可能なデッキ数: {allDecks.Count}");
                
                // デッキが見つからない場合はResourcesからデッキを直接読み込む
                if (allDecks.Count == 0)
                {
                    Debug.Log("DeckDataManagerからデッキが取得できないため、Resourcesからデッキを読み込みます");
                    LoadDecksFromResources();
                    allDecks = DeckDataManager.Instance.GetAllDecks();
                    Debug.Log($"Resourcesから読み込んだデッキ数: {allDecks.Count}");
                }
                
                foreach (DeckData deck in allDecks)
                {
                    deckOptions.Add(deck.deckName);
                    Debug.Log($"デッキを追加: {deck.deckName} (ID: {deck.deckId})");
                }
            }
            else
            {
                Debug.LogError("DeckDataManagerのインスタンスが見つかりません！");
                // DeckDataManagerがなければ最小限の設定で作成
                GameObject dmObj = new GameObject("DeckDataManager");
                DeckDataManager deckManager = dmObj.AddComponent<DeckDataManager>();
                DontDestroyOnLoad(dmObj);
                
                // 一度だけデッキをロード
                LoadDecksFromResources();
                
                if (DeckDataManager.Instance != null)
                {
                    allDecks = DeckDataManager.Instance.GetAllDecks();
                    foreach (DeckData deck in allDecks)
                    {
                        deckOptions.Add(deck.deckName);
                    }
                }
            }
            
            // デッキがそれでも見つからない場合はデフォルトオプションを追加
            if (deckOptions.Count == 0)
            {
                Debug.LogWarning("デッキが見つかりません。デフォルトオプションを使用します。");
                deckOptions.Add("デフォルトデッキ");
            }
            
            player1DeckDropdown.AddOptions(deckOptions);
            
            // 選択値の設定（範囲外の場合は0に設定）
            int selectedDeckIndex = 0;
            if (SelectedGameMode.player1DeckId >= 0 && SelectedGameMode.player1DeckId < deckOptions.Count)
            {
                selectedDeckIndex = SelectedGameMode.player1DeckId;
            }
            
            player1DeckDropdown.value = selectedDeckIndex;
            SelectedGameMode.player1DeckId = allDecks.Count > 0 ? allDecks[selectedDeckIndex].deckId : 0;
            
            player1DeckDropdown.onValueChanged.AddListener(OnDeckSelected);
            
            Debug.Log("デッキドロップダウン設定完了: " + player1DeckDropdown.options.Count + "個のオプション");
        }
        else
        {
            Debug.LogWarning("player1DeckDropdownが設定されていません。インスペクタで設定してください。");
        }
        
        // AIプレイヤー選択ドロップダウン
        if (aiPlayerDropdown != null)
        {
            aiPlayerDropdown.ClearOptions();
            
            List<string> aiOptions = new List<string>();
            
            // AIプロファイルからオプションを追加
            foreach (AIPlayerProfile profile in aiProfiles)
            {
                aiOptions.Add(profile.name);
            }
            
            // AIプロファイルが見つからない場合はデフォルトオプションを追加
            if (aiOptions.Count == 0)
            {
                Debug.LogWarning("AIプロファイルが見つかりません。デフォルトオプションを使用します。");
                aiOptions.Add("初級AI");
                aiOptions.Add("中級AI");
                aiOptions.Add("上級AI");
            }
            
            aiPlayerDropdown.AddOptions(aiOptions);
            
            // 選択値の設定（範囲外の場合は0に設定）
            int selectedAIIndex = 0;
            if (SelectedGameMode.aiProfileId >= 0 && SelectedGameMode.aiProfileId < aiOptions.Count)
            {
                selectedAIIndex = SelectedGameMode.aiProfileId;
            }
            
            aiPlayerDropdown.value = selectedAIIndex;
            SelectedGameMode.aiProfileId = selectedAIIndex;
            
            // 初期値選択時のイベントを手動で呼び出す
            OnAIPlayerSelected(aiPlayerDropdown.value);
            
            aiPlayerDropdown.onValueChanged.AddListener(OnAIPlayerSelected);
            
            // 初期説明を表示
            UpdateAIPlayerDescription(SelectedGameMode.aiProfileId);
            
            Debug.Log("AIプレイヤードロップダウン設定完了: " + aiPlayerDropdown.options.Count + "個のオプション");
        }
        else
        {
            Debug.LogWarning("aiPlayerDropdownが設定されていません。インスペクタで設定してください。");
        }
    }

    // Resources内のデッキファイルを直接読み込むメソッドを追加
    private void LoadDecksFromResources()
    {
        Debug.Log("Resourcesからデッキを読み込み開始");
        
        // Resources/Decksフォルダからすべてのテキストアセットを取得
        TextAsset[] deckAssets = Resources.LoadAll<TextAsset>("Decks");
        Debug.Log($"デッキJSONファイル数: {deckAssets.Length}");
        
        foreach (TextAsset deckAsset in deckAssets)
        {
            try
            {
                // JSON文字列からDeckDataオブジェクトをデシリアライズ
                DeckData deck = JsonUtility.FromJson<DeckData>(deckAsset.text);
                
                if (deck != null)
                {
                    Debug.Log($"デッキ読み込み成功: {deck.deckName} (ID: {deck.deckId})");
                    
                    // DeckDataManagerに追加
                    if (DeckDataManager.Instance != null)
                    {
                        DeckDataManager.Instance.UpdateDeck(deck);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"デッキファイル {deckAsset.name} の読み込みエラー: {e.Message}");
            }
        }
        
        // DeckDataManagerに変更を保存
        if (DeckDataManager.Instance != null)
        {
            DeckDataManager.Instance.SaveUserDecks();
        }
    }
    
    // AIと対戦ボタンクリック時
    private void OnVsAIButtonClicked()
    {
        Debug.Log("AIと対戦ボタンがクリックされました");
        SelectedGameMode.gameMode = GameMode.VsAI;
        
        // 選択パネルを非表示にし、詳細設定パネルを表示
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
            
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
            
        // 現在の設定を表示
        UpdateSettingsDisplay();
    }
    
    // 設定表示を更新
    private void UpdateSettingsDisplay()
    {
        // ドロップダウンの値を現在の設定に合わせる
        if (player1DeckDropdown != null)
            player1DeckDropdown.value = SelectedGameMode.player1DeckId;
            
        if (aiPlayerDropdown != null)
            aiPlayerDropdown.value = SelectedGameMode.aiProfileId;
    }
    
    // ネットワーク対戦ボタンクリック時
    private void OnVsNetworkButtonClicked()
    {
        Debug.Log("ネットワーク対戦ボタンがクリックされました");
        SelectedGameMode.gameMode = GameMode.VsNetwork;
        // SelectedGameMode.aiProfileId = aiPlayerDropdown.value;
        
        // 選択パネルを非表示にし、詳細設定パネルを表示（ネットワーク用）
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
            
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            
            // ネットワークモードではAIプレイヤー選択を非表示/無効化
            if (aiPlayerDropdown != null)
                aiPlayerDropdown.gameObject.SetActive(false);
                
            if (aiPlayerDescriptionText != null)
                aiPlayerDescriptionText.gameObject.SetActive(false);
        }
        
        // 現在の設定を表示
        UpdateSettingsDisplay();
    }
    
    // 設定ボタンクリック時
    private void OnOptionsButtonClicked()
    {
        // 設定画面を表示（必要に応じて実装）
        Debug.Log("オプション画面を表示");
    }
    
    // 戻るボタンクリック時
    private void OnBackButtonClicked()
    {
        // 詳細設定パネルが表示されている場合は選択パネルに戻る
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            settingsPanel.SetActive(false);
            if (selectionPanel != null)
                selectionPanel.SetActive(true);
                
            return;
        }
        
        // それ以外はメインメニューに戻る
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    // デッキ選択時
    private void OnDeckSelected(int dropdownIndex)
    {
        Debug.Log($"デッキドロップダウンが選択されました: インデックス={dropdownIndex}");
        
        // ドロップダウンのインデックスから実際のデッキIDを取得
        List<DeckData> allDecks = DeckDataManager.Instance.GetAllDecks();
        if (allDecks != null && dropdownIndex < allDecks.Count && dropdownIndex >= 0)
        {
            int actualDeckId = allDecks[dropdownIndex].deckId;
            Debug.Log($"実際のデッキID: {actualDeckId}, デッキ名: {allDecks[dropdownIndex].deckName}");
            SelectedGameMode.player1DeckId = actualDeckId;
        }
        else
        {
            Debug.LogError($"無効なデッキインデックス: {dropdownIndex}, 利用可能なデッキ数: {(allDecks != null ? allDecks.Count : 0)}");
            // デフォルトデッキIDを設定（最初のデッキ）
            if (allDecks != null && allDecks.Count > 0)
            {
                SelectedGameMode.player1DeckId = allDecks[0].deckId;
                Debug.Log($"デフォルトデッキIDに設定: {SelectedGameMode.player1DeckId}");
            }
        }
        
        // 選択されたデッキの情報を表示
        DisplaySelectedDeckInfo();
    }
    
    // AIプレイヤー選択時
    private void OnAIPlayerSelected(int profileId)
    {
        Debug.Log($"AIプレイヤーが選択されました: {profileId}");
        SelectedGameMode.aiProfileId = profileId;
        
        // AIプレイヤーの説明を表示
        UpdateAIPlayerDescription(profileId);
    }
    
    // 選択されたデッキ情報を表示
    private void DisplaySelectedDeckInfo()
    {
        if (player1DeckDropdown != null)
        {
            int selectedDeckId = player1DeckDropdown.value;
            DeckData deckData = GetDeckById(selectedDeckId);
            
            // デッキ情報の表示（必要に応じて実装）
        }
    }
    
    // AIプレイヤーの説明を更新
    private void UpdateAIPlayerDescription(int profileId)
    {
        if (aiPlayerDescriptionText != null)
        {
            if (profileId >= 0 && profileId < aiProfiles.Count)
            {
                // AIプロファイルから説明を取得
                aiPlayerDescriptionText.text = aiProfiles[profileId].description;
            }
            else
            {
                // 該当するプロファイルがない場合のデフォルトメッセージ
                aiPlayerDescriptionText.text = "AIプレイヤーの説明がありません。";
            }
        }
    }
        
    // ゲーム開始ボタンクリック時（詳細設定後）
    private void OnStartGameButtonClicked()
    {
        Debug.Log("ゲーム開始ボタンがクリックされました");
        StartGame();
    }
    
    // ゲーム開始処理
    private void StartGame()
    {
        // 選択内容のログ出力
        Debug.Log($"ゲーム開始: モード={SelectedGameMode.gameMode}, デッキID={SelectedGameMode.player1DeckId}, AIプロファイルID={SelectedGameMode.aiProfileId}");
        
        // ゲーム設定の準備が完了したらシーン遷移
        SceneManager.LoadScene(mainGameSceneName);
    }
    
    // デッキIDからデッキデータを取得
    private DeckData GetDeckById(int deckId)
    {
        if (DeckDataManager.Instance != null)
        {
            return DeckDataManager.Instance.GetDeckById(deckId);
        }
        return null;
    }
}