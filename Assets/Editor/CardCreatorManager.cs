using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;

#if UNITY_EDITOR
public class CardCreatorManager : EditorWindow
{
    // カードタイプ
    private enum CardTypeSelection
    {
        Character,
        Spell,
        Field
    }
    
    // タブインデックス
    private enum TabIndex
    {
        CreateCard,
        EditCard,
        BatchImport
    }
    
    // UI設定
    private Vector2 scrollPosition;
    private TabIndex currentTab = TabIndex.CreateCard;
    private CardTypeSelection cardType = CardTypeSelection.Character;
    
    // カードデータベース
    private CardDatabase cardDatabase;
    
    // カード検索
    private string searchQuery = "";
    private Card selectedCard;
    
    // バッチインポート
    private TextAsset csvFile;
    
    // 新規カードデータ
    private string cardName = "";
    private string cardDescription = "";
    private int cardCost = 1;
    
    // アートワーク関連変数
    private Sprite selectedArtwork;
    private List<Sprite> availableArtworks = new List<Sprite>();
    private Sprite defaultCharacterArtwork;
    private Sprite defaultSpellArtwork;
    private Sprite defaultFieldArtwork;
    private Vector2 artworkScrollPosition;
    private string artworkSearchQuery = "";
    private List<Sprite> filteredArtworks = new List<Sprite>();
    private bool showArtworkBrowser = false;
    private CardImageImporter.CardImageType selectedArtworkType = CardImageImporter.CardImageType.Character;
    
    // キャラクターカードデータ
    private ElementType elementType = ElementType.Neutral;
    private int attackPower = 1000;
    private int defensePower = 1000;
    
    // スペルカードデータ
    private bool canActivateOnOpponentTurn = false;
    
    // フィールドカードデータ
    private string targetCardIds = "";

    // エフェクト選択
    private int selectedEffectIndex = 0;
    private string[] effectTypes = new string[] { 
        "なし", 
        "ドロー効果", 
        "攻撃対象効果", 
        "バフ効果", 
        "デバフ効果", 
        "ライフダメージ", 
        "ライフ回復" 
    };

    // エフェクト選択
    private int drawCount = 1;
    private int attackBonus = 0;
    private int defenseBonus = 0;
    private string targetCategory = "";
    private int damageAmount = 500;
    private bool targetOpponent = true;

    private bool isEditingCardArtwork = false;

    // カテゴリーデータベース参照
    private CategoryDatabase categoryDatabase;

    // カテゴリー選択関連
    private List<CardCategory> selectedCategories = new List<CardCategory>();
    private List<bool> categorySelections = new List<bool>();
    private Vector2 categoriesScrollPos;
    private bool showCategorySelector = false;

    // フィールドカード特有の変数
    private List<CardCategory> selectedFieldCategories = new List<CardCategory>();
    private List<bool> fieldCategorySelections = new List<bool>();
    private List<ElementType> selectedElements = new List<ElementType>();
    private List<bool> elementSelections = new List<bool>();
    private bool affectsOwnField = true;
    private bool affectsOpponentField = false;
    private bool modifiesStats = false;
    private int attackModifier = 0;
    private int defenseModifier = 0;
    private bool allowsDeckSearch = false;
    private bool allowsGraveyardRecovery = false;
    private bool revealOpponentHand = false;
    private bool preventBattleDestruction = false;
    private bool preventSpellDestruction = false;
    private bool providesLifeRecovery = false;
    private int lifeRecoveryAmount = 500;

    // 追加の効果パラメータを保持するフィールド変数
    private int maxSearchCards = 1;
    private int maxGraveyardCards = 1;
    private int boostAttack = 500;
    private int boostDefense = 500;
    private int costReduction = 1;
    private string victoryEffectName = "特殊勝利効果";
    private int requiredCards = 5;
    private int energyChange = 1;
    private bool affectSelf = true;
    private bool affectOpponent = false;
    private bool protectBattle = true;
    private bool protectSpell = false;
    private CardCategory tempSelectedCategory;
    private System.Action<CardCategory> categorySelectionCallback;
    
    // 効果リスト - カード作成時の一時的な効果データを保存
    private List<CardEffect> tempEffects = new List<CardEffect>();

    [MenuItem("Tools/Card Game/Card Creator Manager")]
    public static void ShowWindow()
    {
        CardCreatorManager window = GetWindow<CardCreatorManager>("カード作成・編集");
        window.minSize = new Vector2(600, 700);
        window.Show();
    }

    public void OpenImageImporter()
    {
        // クラスを直接参照する代わりにメニュー経由で起動
        EditorApplication.ExecuteMenuItem("Tools/Card Game/Card Image Importer");
    }
    
    private void OnEnable()
    {
        // カードデータベースをロード
        cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
        if (cardDatabase == null)
        {
            Debug.LogWarning("CardDatabaseが見つかりません。新規作成します。");
            cardDatabase = CreateCardDatabase();
        }

        LoadCategoryDatabase();
        
        // アートワークブラウザの初期化
        InitializeArtworkBrowser();

        // 効果リストの初期化
        tempEffects = new List<CardEffect>();
    }

    private void LoadCategoryDatabase()
    {
        // カテゴリーデータベースをロード
        categoryDatabase = Resources.Load<CategoryDatabase>("CategoryDatabase");
        if (categoryDatabase == null)
        {
            Debug.LogWarning("CategoryDatabaseが見つかりません。");
        }
        else
        {
            Debug.Log($"CategoryDatabaseのロード成功: {categoryDatabase.GetAllCategories().Count}個のカテゴリーを読み込みました");
            // カテゴリー選択状態の初期化
            InitializeCategorySelections();
        }
    }

    // カテゴリー選択状態の初期化
    private void InitializeCategorySelections()
    {
        if (categoryDatabase == null)
        {
            Debug.LogWarning("CategoryDatabaseが見つかりません。");
            return;
        }
        
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        Debug.Log($"カテゴリー初期化: カテゴリー数 {allCategories.Count}");
        
        // 必ず新しいリストを作成
        categorySelections = new List<bool>(new bool[allCategories.Count]);
        fieldCategorySelections = new List<bool>(new bool[allCategories.Count]);
        
        // 属性選択の初期化
        elementSelections = new List<bool>(new bool[System.Enum.GetNames(typeof(ElementType)).Length]);
    }
    
    // カードデータベースがない場合に新規作成
    private CardDatabase CreateCardDatabase()
    {
        // カードデータベースのScriptableObjectを作成
        CardDatabase newDatabase = ScriptableObject.CreateInstance<CardDatabase>();
        
        // アセットとして保存
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }
        
        AssetDatabase.CreateAsset(newDatabase, "Assets/Resources/CardDatabase.asset");
        AssetDatabase.SaveAssets();
        return newDatabase;
    }
    
    private void OnGUI()
    {
        // タイトル
        GUILayout.Space(10);
        GUILayout.Label("カード制作ツール", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // エラーチェック
        if (cardDatabase == null)
        {
            EditorGUILayout.HelpBox("CardDatabaseの読み込みに失敗しました。", MessageType.Error);
            if (GUILayout.Button("データベースを作成"))
            {
                cardDatabase = CreateCardDatabase();
            }
            return;
        }
        
        // タブ切り替え
        DrawTabs();
        
        // アートワークブラウザ表示中は他の UI を無効化
        EditorGUI.BeginDisabledGroup(showArtworkBrowser);
        
        // スクロール開始
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // 現在のタブに応じた描画処理
        switch (currentTab)
        {
            case TabIndex.CreateCard:
                DrawCreateCardTab();
                break;
                
            case TabIndex.EditCard:
                DrawEditCardTab();
                break;
                
            case TabIndex.BatchImport:
                DrawBatchImportTab();
                break;
        }
        
        CheckCategorySelector();

        // スクロール終了
        EditorGUILayout.EndScrollView();
        
        // UI を再び有効化
        EditorGUI.EndDisabledGroup();
        
        // アートワークブラウザの描画（ポップアップとして）
        if (showArtworkBrowser)
        {
            // 背景を半透明のレイヤーで覆う
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0, 0, 0, 0.5f));
            
            DrawArtworkBrowser();
        }
    }

    private void DrawCharacterCategoryUI()
    {
        // カテゴリー選択UI
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カテゴリー設定", EditorStyles.boldLabel);
        
        // 選択済みカテゴリー表示
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (selectedCategories.Count == 0)
        {
            EditorGUILayout.LabelField("カテゴリー未選択", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("選択中のカテゴリー:", EditorStyles.boldLabel);
            
            for (int i = 0; i < selectedCategories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // カテゴリー名
                GUI.backgroundColor = selectedCategories[i].categoryColor;
                EditorGUILayout.LabelField(selectedCategories[i].categoryName, EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                
                // 削除ボタン
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    selectedCategories.RemoveAt(i);
                    // カテゴリー選択状態も更新
                    UpdateCategorySelections();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // カテゴリー選択ボタン（最大3つまで）
        EditorGUI.BeginDisabledGroup(selectedCategories.Count >= 3);
        if (GUILayout.Button("カテゴリーを選択 (最大3つ)"))
        {
            showCategorySelector = true;
        }
        EditorGUI.EndDisabledGroup();
    }

    private void CheckCategorySelector()
    {
        // カテゴリー選択ポップアップの表示
        if (showCategorySelector)
        {
            Debug.Log("カテゴリー選択UIを表示します");
            DrawCategorySelector();
            return;
        }
    }

    // カテゴリー選択UIの描画
    private void DrawCategorySelector()
    {
        // カテゴリーデータベースが存在するか確認
        if (categoryDatabase == null)
        {
            Debug.LogError("カテゴリーデータベースが見つかりません");
            showCategorySelector = false;
            return;
        }
        
        // カテゴリーリストを取得
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        Debug.Log($"カテゴリー数: {allCategories.Count}");
        
        // ポップアップのような背景
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("カテゴリー選択", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // スクロールエリア
        categoriesScrollPos = EditorGUILayout.BeginScrollView(categoriesScrollPos, GUILayout.Height(300));
        
        // カテゴリーがない場合
        if (allCategories.Count == 0)
        {
            EditorGUILayout.HelpBox("カテゴリーがありません。カテゴリーマネージャーでカテゴリーを作成してください。", MessageType.Info);
        }
        else
        {
            // カテゴリー選択状態が未初期化または、サイズが合わない場合
            if (categorySelections == null || categorySelections.Count != allCategories.Count)
            {
                InitializeCategorySelections();
            }
            
            // 参照する選択状態
            List<bool> targetSelections = cardType == CardTypeSelection.Character 
                ? categorySelections 
                : fieldCategorySelections;
            
            // サイズ確認（念のため）
            if (targetSelections.Count < allCategories.Count)
            {
                while(targetSelections.Count < allCategories.Count)
                {
                    targetSelections.Add(false);
                }
            }
            
            // すべてのカテゴリーを表示
            for (int i = 0; i < allCategories.Count; i++)
            {
                CardCategory category = allCategories[i];
                if (category == null) continue;
                
                // 選択状態
                bool oldState = targetSelections.Count > i ? targetSelections[i] : false;
                
                EditorGUILayout.BeginHorizontal();
                
                // カラーサンプル
                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(20)), category.categoryColor);
                
                // チェックボックス
                bool newState = EditorGUILayout.ToggleLeft(category.categoryName, oldState);
                
                // 説明
                EditorGUILayout.LabelField(category.description, EditorStyles.miniLabel);
                
                EditorGUILayout.EndHorizontal();
                
                // 選択状態が変更された場合
                if (newState != oldState)
                {
                    // 配列サイズ確保
                    while (targetSelections.Count <= i)
                    {
                        targetSelections.Add(false);
                    }
                    
                    targetSelections[i] = newState;
                    
                    // 選択リストを更新
                    if (cardType == CardTypeSelection.Character)
                    {
                        UpdateSelectedCategories();
                    }
                    else
                    {
                        UpdateSelectedFieldCategories();
                    }
                }
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // キャラクターカードの場合、最大3つまでの制限
        if (cardType == CardTypeSelection.Character && selectedCategories.Count > 3)
        {
            EditorGUILayout.HelpBox("最大3つまでのカテゴリーが選択可能です。", MessageType.Warning);
        }
        
        // 完了ボタン
        GUILayout.Space(10);
        
        if (GUILayout.Button("選択完了", GUILayout.Height(30)))
        {
            showCategorySelector = false;
            
            // カード編集中の場合は、選択したカテゴリーをカードに適用
            if (currentTab == TabIndex.EditCard && selectedCard is CharacterCard editCard)
            {
                editCard.categories.Clear();
                foreach (var category in selectedCategories)
                {
                    if (category != null)
                    {
                        editCard.categories.Add(category);
                    }
                }
                EditorUtility.SetDirty(editCard);
            }
            
            Repaint(); // 再描画を強制
        }
        
        EditorGUILayout.EndVertical();
    }

    // 選択されたカテゴリーをリストに反映（キャラクターカード用）
    private void UpdateSelectedCategories()
    {
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        selectedCategories.Clear();
        
        for (int i = 0; i < categorySelections.Count && i < allCategories.Count; i++)
        {
            if (categorySelections[i])
            {
                CardCategory category = allCategories[i];
                
                // カテゴリーIDチェック
                if (category != null && string.IsNullOrEmpty(category.categoryId))
                {
                    Debug.LogWarning($"カテゴリー '{category.categoryName}' にIDが設定されていません。");
                }
                
                selectedCategories.Add(category);
                
                // 最大3つまで
                if (selectedCategories.Count >= 3)
                    break;
            }
        }
        
        // カテゴリー選択内容をログに出力（デバッグ用）
        if (selectedCategories.Count > 0)
        {
            string categoryNames = string.Join(", ", selectedCategories.Select(c => c.categoryName));
            Debug.Log($"選択されたカテゴリー: {categoryNames}");
        }
    }

    
    // 選択されたカテゴリーをリストに反映（フィールドカード用）
    private void UpdateSelectedFieldCategories()
    {
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        selectedFieldCategories.Clear();
        
        for (int i = 0; i < fieldCategorySelections.Count && i < allCategories.Count; i++)
        {
            if (fieldCategorySelections[i])
            {
                CardCategory category = allCategories[i];
                
                // カテゴリーIDチェック
                if (category != null && string.IsNullOrEmpty(category.categoryId))
                {
                    Debug.LogWarning($"カテゴリー '{category.categoryName}' にIDが設定されていません。");
                }
                
                selectedFieldCategories.Add(category);
            }
        }
        
        // カテゴリー選択内容をログに出力（デバッグ用）
        if (selectedFieldCategories.Count > 0)
        {
            string categoryNames = string.Join(", ", selectedFieldCategories.Select(c => c.categoryName));
            Debug.Log($"選択されたフィールドカテゴリー: {categoryNames}");
        }
    }
    
    // カテゴリー選択状態の更新（キャラクターカード用）
    private void UpdateCategorySelections()
    {
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        // サイズ確保
        while (categorySelections.Count < allCategories.Count)
        {
            categorySelections.Add(false);
        }
        
        // 選択状態をリセット
        for (int i = 0; i < categorySelections.Count; i++)
        {
            categorySelections[i] = false;
        }
        
        // 現在選択中のカテゴリーを反映
        foreach (CardCategory category in selectedCategories)
        {
            int index = allCategories.IndexOf(category);
            if (index >= 0 && index < categorySelections.Count)
            {
                categorySelections[index] = true;
            }
        }
    }
    
    // カテゴリー選択状態の更新（フィールドカード用）
    private void UpdateFieldCategorySelections()
    {
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        // サイズ確保
        while (fieldCategorySelections.Count < allCategories.Count)
        {
            fieldCategorySelections.Add(false);
        }
        
        // 選択状態をリセット
        for (int i = 0; i < fieldCategorySelections.Count; i++)
        {
            fieldCategorySelections[i] = false;
        }
        
        // 現在選択中のカテゴリーを反映
        foreach (CardCategory category in selectedFieldCategories)
        {
            int index = allCategories.IndexOf(category);
            if (index >= 0 && index < fieldCategorySelections.Count)
            {
                fieldCategorySelections[index] = true;
            }
        }
    }
    
    // タブ切り替えUIの描画
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        if (GUILayout.Toggle(currentTab == TabIndex.CreateCard, "新規カード作成", EditorStyles.toolbarButton))
            currentTab = TabIndex.CreateCard;
            
        if (GUILayout.Toggle(currentTab == TabIndex.EditCard, "カード編集", EditorStyles.toolbarButton))
            currentTab = TabIndex.EditCard;
            
        if (GUILayout.Toggle(currentTab == TabIndex.BatchImport, "一括インポート", EditorStyles.toolbarButton))
            currentTab = TabIndex.BatchImport;
            
        EditorGUILayout.EndHorizontal();
    }
    
    // エフェクト選択UIの統一版を作成
    private void DrawEffectSelection()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カード効果", EditorStyles.boldLabel);
        
        selectedEffectIndex = EditorGUILayout.Popup("効果タイプ", selectedEffectIndex, GetEffectTypesForCurrentCardType());
        
        // 選択された効果タイプに応じた詳細設定UIを表示
        DrawEffectDetailsForSelection();
    }

    // 選択された効果タイプに応じた詳細設定UIを表示
    private void DrawEffectDetailsForSelection()
    {
        switch (selectedEffectIndex)
        {
            case 0: // なし
                break;
                
            case 1: // カードドロー（全カードタイプ共通）
                EditorGUILayout.HelpBox("ドロー効果: プレイヤーがカードを引く効果です。", MessageType.Info);
                drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawCount, 1, 5);
                break;
                
            default:
                // キャラクターカードの場合
                if (cardType == CardTypeSelection.Character)
                {
                    DrawCharacterEffectDetails();
                }
                // スペルカードの場合
                else if (cardType == CardTypeSelection.Spell)
                {
                    DrawSpellEffectDetails();
                }
                // フィールドカードの場合
                else
                {
                    DrawFieldEffectDetails();
                }
                break;
        }
    }

    // キャラクターカード特有の効果詳細表示
    private void DrawCharacterEffectDetails()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ステータス変更
                EditorGUILayout.HelpBox("ステータス変更: キャラクターのステータスを変更する効果です。", MessageType.Info);
                attackBonus = EditorGUILayout.IntSlider("攻撃力変更", attackBonus, -1000, 1000);
                defenseBonus = EditorGUILayout.IntSlider("防御力変更", defenseBonus, -1000, 1000);
                targetCategory = EditorGUILayout.TextField("対象カテゴリー", targetCategory);
                break;
                
            case 3: // ダメージ
                EditorGUILayout.HelpBox("ダメージ効果: プレイヤーにダメージを与える効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", targetOpponent);
                break;
                
            case 4: // 回復
                EditorGUILayout.HelpBox("回復効果: プレイヤーのライフを回復する効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("回復量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーを回復", targetOpponent);
                break;
                
            case 5: // カテゴリー検索
                EditorGUILayout.HelpBox("カテゴリー検索: デッキから特定カテゴリーのカードを手札に加えます。", MessageType.Info);
                DrawCategorySelectionField("対象カテゴリー");
                maxSearchCards = EditorGUILayout.IntSlider("最大枚数", maxSearchCards, 1, 3);
                break;
                
            case 6: // 捨て札回収
                EditorGUILayout.HelpBox("捨て札回収: 捨て札から特定カテゴリーのカードを手札に加えます。", MessageType.Info);
                DrawCategorySelectionField("対象カテゴリー");
                maxGraveyardCards = EditorGUILayout.IntSlider("最大枚数", maxGraveyardCards, 1, 3);
                break;
                
            case 7: // ステータス強化
                EditorGUILayout.HelpBox("ステータス強化: 特定カテゴリーのカードのステータスを強化します。", MessageType.Info);
                DrawCategorySelectionField("対象カテゴリー");
                boostAttack = EditorGUILayout.IntSlider("攻撃力上昇", boostAttack, 0, 2000);
                boostDefense = EditorGUILayout.IntSlider("防御力上昇", boostDefense, 0, 2000);
                break;
                
            case 8: // コスト削減
                EditorGUILayout.HelpBox("コスト削減: 特定カテゴリーのカードのコストを削減します。", MessageType.Info);
                DrawCategorySelectionField("対象カテゴリー");
                costReduction = EditorGUILayout.IntSlider("コスト削減量", costReduction, 1, 3);
                break;
                
            case 9: // 特殊勝利
                EditorGUILayout.HelpBox("特殊勝利: 特定条件を満たすことで勝利します。", MessageType.Info);
                victoryEffectName = EditorGUILayout.TextField("効果名", victoryEffectName);
                requiredCards = EditorGUILayout.IntSlider("必要カード枚数", requiredCards, 2, 7);
                break;
        }
    }

    // カテゴリー選択フィールドの描画（共通）
    private void DrawCategorySelectionField(string label)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        
        if (tempSelectedCategory != null)
        {
            GUI.backgroundColor = tempSelectedCategory.categoryColor;
            if (GUILayout.Button(tempSelectedCategory.categoryName, GUILayout.Width(150)))
            {
                ShowCategorySelectMenu(tempSelectedCategory, (newCategory) => {
                    tempSelectedCategory = newCategory;
                });
            }
            GUI.backgroundColor = Color.white;
            
            // クリアボタン
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                tempSelectedCategory = null;
            }
        }
        else
        {
            if (GUILayout.Button("カテゴリーを選択", GUILayout.Width(150)))
            {
                ShowCategorySelectMenu(null, (newCategory) => {
                    tempSelectedCategory = newCategory;
                });
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    // スペルカード特有の効果詳細表示
    private void DrawSpellEffectDetails()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ダメージ
                EditorGUILayout.HelpBox("ダメージ効果: プレイヤーにダメージを与える効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", targetOpponent);
                break;
                
            case 3: // 回復
                EditorGUILayout.HelpBox("回復効果: プレイヤーのライフを回復する効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("回復量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーを回復", targetOpponent);
                break;
                
            case 4: // ステータス変更
                EditorGUILayout.HelpBox("ステータス変更: キャラクターのステータスを変更する効果です。", MessageType.Info);
                attackBonus = EditorGUILayout.IntSlider("攻撃力変更", attackBonus, -1000, 1000);
                defenseBonus = EditorGUILayout.IntSlider("防御力変更", defenseBonus, -1000, 1000);
                // テキスト入力からカテゴリー選択に変更
                DrawCategorySelectionField("対象カテゴリー");
                break;
                
            case 5: // 攻撃対象効果
                EditorGUILayout.HelpBox("攻撃対象効果: 特定のカードを攻撃対象にする効果です。", MessageType.Info);
                break;
                
            case 6: // エナジー操作
                EditorGUILayout.HelpBox("エナジー操作: エナジーを増減させる効果です。", MessageType.Info);
                energyChange = EditorGUILayout.IntSlider("エナジー変更量", energyChange, -5, 5);
                affectSelf = EditorGUILayout.Toggle("自分のエナジーに影響", affectSelf);
                affectOpponent = EditorGUILayout.Toggle("相手のエナジーに影響", affectOpponent);
                break;
        }
    }

    // フィールドカード特有の効果詳細表示
    private void DrawFieldEffectDetails()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ダメージ
                EditorGUILayout.HelpBox("ダメージ効果: プレイヤーにダメージを与える効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", targetOpponent);
                break;
                
            case 3: // 回復
                EditorGUILayout.HelpBox("回復効果: プレイヤーのライフを回復する効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("回復量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーを回復", targetOpponent);
                break;
                
            case 4: // ステータス変更
                EditorGUILayout.HelpBox("ステータス変更: キャラクターのステータスを変更する効果です。", MessageType.Info);
                attackBonus = EditorGUILayout.IntSlider("攻撃力変更", attackBonus, -1000, 1000);
                defenseBonus = EditorGUILayout.IntSlider("防御力変更", defenseBonus, -1000, 1000);
                targetCategory = EditorGUILayout.TextField("対象カテゴリー", targetCategory);
                break;
                
            case 5: // カード保護
                EditorGUILayout.HelpBox("カード保護: カードを破壊から保護する効果です。", MessageType.Info);
                protectBattle = EditorGUILayout.Toggle("戦闘破壊から保護", protectBattle);
                protectSpell = EditorGUILayout.Toggle("スペル効果破壊から保護", protectSpell);
                break;
        }
    }

    // カードタイプに応じた効果タイプ一覧を取得
    private string[] GetEffectTypesForCurrentCardType()
    {
        if (cardType == CardTypeSelection.Character)
        {
            return new string[] {
                "なし",
                "カードドロー",
                "ステータス変更",
                "ダメージ",
                "回復",
                "カテゴリー検索",
                "捨て札回収",
                "ステータス強化",
                "コスト削減",
                "特殊勝利"
            };
        }
        else if (cardType == CardTypeSelection.Spell)
        {
            return new string[] {
                "なし",
                "カードドロー",
                "ダメージ",
                "回復",
                "ステータス変更",
                "攻撃対象効果",
                "エナジー操作"
            };
        }
        else // フィールドカード
        {
            return new string[] {
                "なし",
                "カードドロー",
                "ダメージ",
                "回復",
                "ステータス変更",
                "カード保護"
            };
        }
    }


    // カードに効果を追加する処理を修正
    private void AddEffectToCard(Card card)
    {
        if (selectedEffectIndex <= 0) return;
        
        CardEffect effect = null;
        
        // 共通効果
        if (selectedEffectIndex == 1) // カードドロー
        {
            DrawCardEffect drawEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
            drawEffect.drawCount = drawCount;
            effect = drawEffect;
        }
        else
        {
            // カードタイプ別の効果
            if (card is CharacterCard)
            {
                effect = CreateCharacterCardEffect();
            }
            else if (card is SpellCard)
            {
                effect = CreateSpellCardEffect();
            }
            else if (card is FieldCard)
            {
                effect = CreateFieldCardEffect();
            }
        }
        
        if (effect != null)
        {
            // カードタイプに応じて効果を追加
            if (card is CharacterCard characterCard)
            {
                characterCard.effects.Add(effect);
            }
            else if (card is SpellCard spellCard)
            {
                if (spellCard.effects == null)
                    spellCard.effects = new List<CardEffect>();
                spellCard.effects.Add(effect);
            }
            else if (card is FieldCard fieldCard)
            {
                fieldCard.effects.Add(effect);
            }
            
            // 効果をアセットとして追加
            AssetDatabase.AddObjectToAsset(effect, AssetDatabase.GetAssetPath(cardDatabase));
        }
    }

    // キャラクターカードの効果を作成
    private CardEffect CreateCharacterCardEffect()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ステータス変更
                StatModifierEffect statEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                statEffect.attackBonus = attackBonus;
                statEffect.defenseBonus = defenseBonus;
                statEffect.targetCategory = targetCategory;
                return statEffect;
                
            case 3: // ダメージ
                DamageEffect damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                damageEffect.damageAmount = damageAmount;
                damageEffect.targetOpponent = targetOpponent;
                return damageEffect;
                
            case 4: // 回復
                HealEffect healEffect = ScriptableObject.CreateInstance<HealEffect>();
                healEffect.healAmount = damageAmount;
                healEffect.targetSelf = !targetOpponent;
                return healEffect;
                
            case 5: // カテゴリー検索
                CategorySearchEffect searchEffect = ScriptableObject.CreateInstance<CategorySearchEffect>();
                searchEffect.targetCategory = tempSelectedCategory;
                searchEffect.maxCards = maxSearchCards;
                return searchEffect;
                
            case 6: // 捨て札回収
                CategoryGraveyardEffect graveyardEffect = ScriptableObject.CreateInstance<CategoryGraveyardEffect>();
                graveyardEffect.targetCategory = tempSelectedCategory;
                graveyardEffect.maxCards = maxGraveyardCards;
                return graveyardEffect;
                
            case 7: // ステータス強化
                CategoryBoostEffect boostEffect = ScriptableObject.CreateInstance<CategoryBoostEffect>();
                boostEffect.targetCategory = tempSelectedCategory;
                boostEffect.attackBoost = boostAttack;
                boostEffect.defenseBoost = boostDefense;
                return boostEffect;
                
            case 8: // コスト削減
                CategoryCostReductionEffect costEffect = ScriptableObject.CreateInstance<CategoryCostReductionEffect>();
                costEffect.targetCategory = tempSelectedCategory;
                costEffect.costReduction = costReduction;
                return costEffect;
                
            case 9: // 特殊勝利
                SpecialVictoryEffect victoryEffect = ScriptableObject.CreateInstance<SpecialVictoryEffect>();
                victoryEffect.requiredEffectName = victoryEffectName;
                victoryEffect.requiredCards = requiredCards;
                return victoryEffect;
                
            default:
                return null;
        }
    }

    // スペルカードの効果を作成
    private CardEffect CreateSpellCardEffect()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ダメージ
                DamageEffect damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                damageEffect.damageAmount = damageAmount;
                damageEffect.targetOpponent = targetOpponent;
                return damageEffect;
                
            case 3: // 回復
                HealEffect healEffect = ScriptableObject.CreateInstance<HealEffect>();
                healEffect.healAmount = damageAmount;
                healEffect.targetSelf = !targetOpponent;
                return healEffect;
                
            case 4: // ステータス変更
                StatModifierEffect statEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                statEffect.attackBonus = attackBonus;
                statEffect.defenseBonus = defenseBonus;

                // 選択されたカテゴリーが存在する場合は、その名前を設定
                if (tempSelectedCategory != null)
                {
                    statEffect.targetCategory = tempSelectedCategory.categoryName;
                }
                else
                {
                    statEffect.targetCategory = "";
                }
                return statEffect;
                
            case 5: // 攻撃対象効果
                AttackTargetEffect attackEffect = ScriptableObject.CreateInstance<AttackTargetEffect>();
                return attackEffect;
                
            case 6: // エナジー操作
                EnergyManipulationEffect energyEffect = ScriptableObject.CreateInstance<EnergyManipulationEffect>();
                energyEffect.energyChange = energyChange;
                energyEffect.affectSelf = affectSelf;
                energyEffect.affectOpponent = affectOpponent;
                return energyEffect;
                
            default:
                return null;
        }
    }

    // フィールドカードの効果を作成
    private CardEffect CreateFieldCardEffect()
    {
        switch (selectedEffectIndex)
        {
            case 2: // ダメージ
                DamageEffect damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                damageEffect.damageAmount = damageAmount;
                damageEffect.targetOpponent = targetOpponent;
                return damageEffect;
                
            case 3: // 回復
                HealEffect healEffect = ScriptableObject.CreateInstance<HealEffect>();
                healEffect.healAmount = damageAmount;
                healEffect.targetSelf = !targetOpponent;
                return healEffect;
                
            case 4: // ステータス変更
                StatModifierEffect statEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                statEffect.attackBonus = attackBonus;
                statEffect.defenseBonus = defenseBonus;
                statEffect.targetCategory = targetCategory;
                return statEffect;
                
            case 5: // カード保護
                RemovalEffect removalEffect = ScriptableObject.CreateInstance<RemovalEffect>();
                // 保護効果の設定
                return removalEffect;
                
            default:
                return null;
        }
    }

    // カード作成タブの描画
    private void DrawCreateCardTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("新規カード作成", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // カード種類選択
        cardType = (CardTypeSelection)EditorGUILayout.EnumPopup("カード種類", cardType);
        
        GUILayout.Space(10);
        
        // 共通カード情報入力
        DrawCommonCardFields();
        
        // カード種類に応じた固有フィールド
        DrawCardTypeSpecificFields();
        
        // カードエフェクト (フィールドカードの場合は表示しない)
        if (cardType != CardTypeSelection.Field)
        {
            DrawEffectSelection();
        }
        
        // 作成ボタン
        GUILayout.Space(20);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("カードを作成", GUILayout.Height(30)))
        {
            CreateCard();
        }
        GUI.backgroundColor = Color.white;
    }

    // フィールドカードの作成メソッドを更新
    private FieldCard CreateFieldCardWithEffects(int id)
    {
        FieldCard card = ScriptableObject.CreateInstance<FieldCard>();
        
        // 共通プロパティの設定
        SetupCommonCardProperties(card, id, CardType.Field);
        
        // フィールドカード固有情報の設定
        card.affectedCategories = new List<CardCategory>(selectedFieldCategories);
        card.affectedElements = new List<ElementType>(selectedElements);
        card.affectsOwnField = affectsOwnField;
        card.affectsOpponentField = affectsOpponentField;
        card.modifiesStats = modifiesStats;
        card.attackModifier = attackModifier;
        card.defenseModifier = defenseModifier;
        card.allowsDeckSearch = allowsDeckSearch;
        card.allowsGraveyardRecovery = allowsGraveyardRecovery;
        card.revealOpponentHand = revealOpponentHand;
        card.preventBattleDestruction = preventBattleDestruction;
        card.preventSpellDestruction = preventSpellDestruction;
        card.providesLifeRecovery = providesLifeRecovery;
        card.lifeRecoveryAmount = lifeRecoveryAmount;
        card.effects = new List<CardEffect>();
        
        return card;
    }

    private void ResetExtendedCardInputFields()
    {
        // 追加されたフィールドのリセット
        selectedCategories.Clear();
        selectedFieldCategories.Clear();
        selectedElements.Clear();
        
        // カテゴリー選択状態のリセット
        for (int i = 0; i < categorySelections.Count; i++)
        {
            categorySelections[i] = false;
        }
        
        // フィールドカテゴリー選択状態のリセット
        for (int i = 0; i < fieldCategorySelections.Count; i++)
        {
            fieldCategorySelections[i] = false;
        }
        
        // 属性選択状態のリセット
        for (int i = 0; i < elementSelections.Count; i++)
        {
            elementSelections[i] = false;
        }
        
        // フィールドカード固有フィールドのリセット
        affectsOwnField = true;
        affectsOpponentField = false;
        modifiesStats = false;
        attackModifier = 0;
        defenseModifier = 0;
        allowsDeckSearch = false;
        allowsGraveyardRecovery = false;
        revealOpponentHand = false;
        preventBattleDestruction = false;
        preventSpellDestruction = false;
        providesLifeRecovery = false;
        lifeRecoveryAmount = 500;
    }

    // キャラクターカードの作成メソッドを更新
    private CharacterCard CreateCharacterCardWithCategories(int id)
    {
        CharacterCard card = ScriptableObject.CreateInstance<CharacterCard>();
        
        // 共通プロパティの設定
        SetupCommonCardProperties(card, id, CardType.Character);
        
        // キャラクター固有プロパティの設定
        card.element = elementType;
        card.attackPower = attackPower;
        card.defensePower = defensePower;
        card.effects = new List<CardEffect>();

        // カテゴリーリストが初期化されていることを確認
        card.categories = new List<CardCategory>();
        
        // 選択されたカテゴリーをコピー
        foreach (var category in selectedCategories)
        {
            if (category != null)
            {
                // IDがない場合のデバッグ情報
                if (string.IsNullOrEmpty(category.categoryId))
                {
                    Debug.LogWarning($"カード作成時: カテゴリー '{category.categoryName}' にIDが設定されていません");
                }
                
                card.categories.Add(category);
                Debug.Log($"カードにカテゴリーを追加: {category.categoryName}, ID: {category.categoryId}");
            }
        }            
        return card;
    }
    
    // 共通カードフィールドの描画
    private void DrawCommonCardFields()
    {
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        
        cardName = EditorGUILayout.TextField("カード名", cardName);
        
        EditorGUILayout.LabelField("説明");
        cardDescription = EditorGUILayout.TextArea(cardDescription, GUILayout.Height(60));
        
        cardCost = EditorGUILayout.IntSlider("コスト", cardCost, 0, 10);
        
        // アートワーク選択UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("アートワーク");

        // nullチェックを追加
        Rect previewRect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100), GUILayout.Height(100));
        
        // selectedArtworkがnullの場合の処理を追加
        if (selectedArtwork != null)
        {
            // アートワークプレビューの表示
            Texture2D texture = selectedArtwork.texture;
            if (texture != null)
            {
                GUI.DrawTextureWithTexCoords(
                    previewRect,
                    texture,
                    new Rect(
                        selectedArtwork.rect.x / texture.width,
                        selectedArtwork.rect.y / texture.height,
                        selectedArtwork.rect.width / texture.width,
                        selectedArtwork.rect.height / texture.height
                    )
                );
            }
            else
            {
                GUI.Box(previewRect, "テクスチャなし");
            }
        }
        else
        {
            GUI.Box(previewRect, "アートワークなし");
        }

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("アートワーク選択...", GUILayout.Width(150)))
        {
            ShowArtworkSelector();
        }

        if (GUILayout.Button("アートワークをクリア", GUILayout.Width(150)))
        {
            selectedArtwork = null;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }
    
    // カード種類に応じたフィールドの描画
    private void DrawCardTypeSpecificFields()
    {
        GUILayout.Space(10);
        
        switch (cardType)
        {
            case CardTypeSelection.Character:
                DrawCharacterCardFields();
                break;
                
            case CardTypeSelection.Spell:
                DrawSpellCardFields();
                break;
                
            case CardTypeSelection.Field:
                DrawFieldCardFields();
                break;
        }
    }
    
    // キャラクターカード固有フィールドの描画
    private void DrawCharacterCardFields()
    {
        EditorGUILayout.LabelField("キャラクターカード情報", EditorStyles.boldLabel);
        
        elementType = (ElementType)EditorGUILayout.EnumPopup("属性", elementType);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("攻撃力", GUILayout.Width(120));
        attackPower = EditorGUILayout.IntSlider(attackPower, 0, 5000);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("防御力", GUILayout.Width(120));
        defensePower = EditorGUILayout.IntSlider(defensePower, 0, 5000);
        EditorGUILayout.EndHorizontal();
        
        // カテゴリー選択UI
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カテゴリー設定", EditorStyles.boldLabel);
        
        // 選択済みカテゴリー表示
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (selectedCategories.Count == 0)
        {
            EditorGUILayout.LabelField("カテゴリー未選択", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("選択中のカテゴリー:", EditorStyles.boldLabel);
            
            for (int i = 0; i < selectedCategories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // カテゴリー名
                GUI.backgroundColor = selectedCategories[i].categoryColor;
                EditorGUILayout.LabelField(selectedCategories[i].categoryName, EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                
                // 削除ボタン
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    selectedCategories.RemoveAt(i);
                    // カテゴリー選択状態も更新
                    UpdateCategorySelections();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // カテゴリー選択ボタン（最大3つまで）
        EditorGUI.BeginDisabledGroup(selectedCategories.Count >= 3);
        if (GUILayout.Button("カテゴリーを選択 (最大3つ)"))
        {
            // Debug.Log を残して問題追跡に役立てる
            Debug.Log("カテゴリー選択ボタンがクリックされました");
            showCategorySelector = true;
            
            // 初期状態ではカテゴリー選択状態を初期化しておく
            if (categoryDatabase != null)
            {
                InitializeCategorySelections();
                UpdateCategorySelections(); // 既存の選択を反映
            }
            
            // 強制的に再描画を行う
            Repaint();
        }
        EditorGUI.EndDisabledGroup();
    }
    
    // スペルカード固有フィールドの描画
    private void DrawSpellCardFields()
    {   
        canActivateOnOpponentTurn = EditorGUILayout.Toggle("相手ターンで発動可能", canActivateOnOpponentTurn);
    }
    
    private void DrawFieldCardFields()
    {
        EditorGUILayout.LabelField("フィールドカード情報", EditorStyles.boldLabel);
        
        // 効果適用範囲
        EditorGUILayout.LabelField("効果適用範囲", EditorStyles.boldLabel);
        affectsOwnField = EditorGUILayout.Toggle("自分のフィールドに適用", affectsOwnField);
        affectsOpponentField = EditorGUILayout.Toggle("相手のフィールドに適用", affectsOpponentField);
        
        // 影響を与えるカテゴリー/属性
        GUILayout.Space(10);
        EditorGUILayout.LabelField("影響対象", EditorStyles.boldLabel);
        
        // 選択済みカテゴリー表示
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("対象カテゴリー:", EditorStyles.boldLabel);
        
        if (selectedFieldCategories.Count == 0)
        {
            EditorGUILayout.LabelField("すべてのカテゴリー", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            for (int i = 0; i < selectedFieldCategories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // カテゴリー名
                GUI.backgroundColor = selectedFieldCategories[i].categoryColor;
                EditorGUILayout.LabelField(selectedFieldCategories[i].categoryName, EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;
                
                // 削除ボタン
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    selectedFieldCategories.RemoveAt(i);
                    UpdateFieldCategorySelections();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // カテゴリー選択ボタン
        if (GUILayout.Button("対象カテゴリーを選択"))
        {
            // カテゴリー選択UIを表示
            showCategorySelector = true;
        }
        
        // 選択済み属性表示
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("対象属性:", EditorStyles.boldLabel);
        
        if (selectedElements.Count == 0)
        {
            EditorGUILayout.LabelField("すべての属性", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < selectedElements.Count; i++)
            {
                EditorGUILayout.LabelField(selectedElements[i].ToString(), EditorStyles.helpBox, GUILayout.Width(80));
                
                if ((i + 1) % 3 == 0 && i < selectedElements.Count - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("属性選択をクリア"))
            {
                selectedElements.Clear();
                for (int i = 0; i < elementSelections.Count; i++)
                {
                    elementSelections[i] = false;
                }
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // 属性選択UI
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("属性を選択:", EditorStyles.boldLabel);
        
        string[] elementNames = System.Enum.GetNames(typeof(ElementType));
        EditorGUILayout.BeginHorizontal();
        
        for (int i = 0; i < elementNames.Length; i++)
        {
            bool oldState = elementSelections.Count > i ? elementSelections[i] : false;
            bool newState = EditorGUILayout.ToggleLeft(elementNames[i], oldState, GUILayout.Width(100));
            
            // 選択状態が変更された場合
            if (newState != oldState)
            {
                // 配列サイズ確保
                while (elementSelections.Count <= i)
                {
                    elementSelections.Add(false);
                }
                
                elementSelections[i] = newState;
                
                // 選択リストを更新
                ElementType element = (ElementType)i;
                if (newState)
                {
                    if (!selectedElements.Contains(element))
                    {
                        selectedElements.Add(element);
                    }
                }
                else
                {
                    selectedElements.Remove(element);
                }
            }
            
            // 3列に並べる
            if ((i + 1) % 3 == 0 && i < elementNames.Length - 1)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        // 効果タイプ
        GUILayout.Space(10);
        EditorGUILayout.LabelField("効果設定", EditorStyles.boldLabel);
        
        // ステータス修正効果
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        modifiesStats = EditorGUILayout.ToggleLeft("攻撃力/防御力を修正する", modifiesStats);
        
        if (modifiesStats)
        {
            EditorGUI.indentLevel++;
            attackModifier = EditorGUILayout.IntSlider("攻撃力修正値", attackModifier, -2000, 2000);
            defenseModifier = EditorGUILayout.IntSlider("防御力修正値", defenseModifier, -2000, 2000);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
        
        // カード移動効果
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        allowsDeckSearch = EditorGUILayout.ToggleLeft("デッキからカードを手札に加える効果 (1ターン1回)", allowsDeckSearch);
        allowsGraveyardRecovery = EditorGUILayout.ToggleLeft("捨て札からカードを手札に加える効果 (1ターン1回)", allowsGraveyardRecovery);
        EditorGUILayout.EndVertical();
        
        // 視覚効果
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        revealOpponentHand = EditorGUILayout.ToggleLeft("相手の手札を見えるようにする", revealOpponentHand);
        EditorGUILayout.EndVertical();
        
        // 保護効果
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        preventBattleDestruction = EditorGUILayout.ToggleLeft("キャラクターが戦闘で破壊されなくなる (ダメージは受ける)", preventBattleDestruction);
        preventSpellDestruction = EditorGUILayout.ToggleLeft("キャラクターがスペルで破壊されなくなる", preventSpellDestruction);
        EditorGUILayout.EndVertical();
        
        // ライフ効果
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        providesLifeRecovery = EditorGUILayout.ToggleLeft("ライフポイントを回復する (1ターン1回)", providesLifeRecovery);
        
        if (providesLifeRecovery)
        {
            EditorGUI.indentLevel++;
            lifeRecoveryAmount = EditorGUILayout.IntSlider("回復量", lifeRecoveryAmount, 100, 2000);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();
    }
    
    // カード作成メソッドを修正
    private void CreateCard()
    {
        // 入力チェック
        if (string.IsNullOrEmpty(cardName))
        {
            EditorUtility.DisplayDialog("入力エラー", "カード名を入力してください。", "OK");
            return;
        }
        
        // アートワーク選択必須チェック
        if (selectedArtwork == null)
        {
            EditorUtility.DisplayDialog("入力エラー", "アートワークを選択してください。", "OK");
            return;
        }
        
        // 新規IDの生成
        int newId = GenerateNewCardId();
        
        // カード種類に応じた作成処理
        Card newCard = null;
        
        switch (cardType)
        {
            case CardTypeSelection.Character:
                newCard = CreateCharacterCardWithCategories(newId);
                break;
                
            case CardTypeSelection.Spell:
                newCard = CreateSpellCard(newId);
                break;
                
            case CardTypeSelection.Field:
                newCard = CreateFieldCardWithEffects(newId);
                break;
        }
        
        // データベースにカードを追加
        if (newCard != null)
        {
            cardDatabase.AddCard(newCard);
            
            // カードのアセットを保存
            AssetDatabase.AddObjectToAsset(newCard, AssetDatabase.GetAssetPath(cardDatabase));
            
            // カード効果があれば追加 (フィールドカード以外)
            if (cardType != CardTypeSelection.Field)
            {
                AddEffectToCard(newCard);
            }
            
            EditorUtility.SetDirty(cardDatabase);
            AssetDatabase.SaveAssets();
            
            // 成功メッセージと入力フィールドのクリア
            EditorUtility.DisplayDialog("カード作成", $"カード「{cardName}」を作成しました。\nID: {newId}", "OK");
            ResetCardInputFields();
        }
    }

    // キャラクターカードの作成
    private CharacterCard CreateCharacterCard(int id)
    {
        CharacterCard card = ScriptableObject.CreateInstance<CharacterCard>();
        
        // 共通プロパティの設定
        SetupCommonCardProperties(card, id, CardType.Character);
        
        // キャラクター固有プロパティの設定
        card.element = elementType;
        card.attackPower = attackPower;
        card.defensePower = defensePower;
        card.categories = new List<CardCategory>(selectedCategories);
        card.effects = new List<CardEffect>();
        
        return card;
    }
    
    // スペルカードの作成
    private SpellCard CreateSpellCard(int id)
    {
        SpellCard card = ScriptableObject.CreateInstance<SpellCard>();
        
        // 共通プロパティの設定
        SetupCommonCardProperties(card, id, CardType.Spell);
        
        // スペル固有プロパティの設定
        card.effects = new List<CardEffect>();
        
        // スペルカード拡張機能のプロパティ
        card.canActivateOnOpponentTurn = canActivateOnOpponentTurn;
        
        return card;
    }
    
    // フィールドカードの作成
    private FieldCard CreateFieldCard(int id)
    {
        FieldCard card = ScriptableObject.CreateInstance<FieldCard>();
        
        // 共通プロパティの設定
        SetupCommonCardProperties(card, id, CardType.Field);
        
        // フィールド固有プロパティの設定
        card.effects = new List<CardEffect>();
        card.affectedCategories = new List<CardCategory>(selectedFieldCategories);
        card.affectedElements = new List<ElementType>(selectedElements);
        card.affectsOwnField = affectsOwnField;
        card.affectsOpponentField = affectsOpponentField;
        card.modifiesStats = modifiesStats;
        card.attackModifier = attackModifier;
        card.defenseModifier = defenseModifier;
        
        return card;
    }
    
    // 共通カードプロパティの設定
    private void SetupCommonCardProperties(Card card, int id, CardType type)
    {
        card.id = id;
        card.cardName = cardName;
        card.description = cardDescription;
        card.cost = cardCost;
        card.type = type;
        
        // アートワークがある場合は設定（ファイル名のみを保存）
        if (selectedArtwork != null)
        {
            card.artwork = selectedArtwork.name;
        }
        else
        {
            // カードタイプに応じたデフォルトアートワークのファイル名を設定
            switch (card.type)
            {
                case CardType.Character:
                    card.artwork = defaultCharacterArtwork != null ? defaultCharacterArtwork.name : "";
                    break;
                case CardType.Spell:
                    card.artwork = defaultSpellArtwork != null ? defaultSpellArtwork.name : "";
                    break;
                case CardType.Field:
                    card.artwork = defaultFieldArtwork != null ? defaultFieldArtwork.name : "";
                    break;
            }
        }
    }
    
    // 新しいカードIDを生成
    private int GenerateNewCardId()
    {
        int maxId = 0;
        
        foreach (Card card in cardDatabase.GetAllCards())
        {
            if (card.id > maxId)
                maxId = card.id;
        }
        
        return maxId + 1;
    }
    
    // 入力フィールドのリセット
    private void ResetCardInputFields()
    {
        cardName = "";
        cardDescription = "";
        cardCost = 1;
        
        // キャラクターカードフィールドのリセット
        attackPower = 1000;
        defensePower = 1000;
        
        // スペルカードフィールドのリセット
        canActivateOnOpponentTurn = false;
        
        // フィールドカードフィールドのリセット
        targetCardIds = "";
        // カテゴリー選択状態のリセット
        selectedCategories.Clear();
        selectedFieldCategories.Clear();

        // アートワークをリセット
        selectedArtwork = null;
        
        // エフェクト選択のリセット
        selectedEffectIndex = 0;

        // 追加効果パラメータのリセット
        maxSearchCards = 1;
        maxGraveyardCards = 1;
        boostAttack = 500;
        boostDefense = 500;
        costReduction = 1;
        victoryEffectName = "特殊勝利効果";
        requiredCards = 5;
        energyChange = 1;
        affectSelf = true;
        affectOpponent = false;
        protectBattle = true;
        protectSpell = false;
        tempSelectedCategory = null;
    }
    
    // ターゲットカードIDのパース
    private List<int> ParseTargetCardIds()
    {
        List<int> result = new List<int>();
        
        if (!string.IsNullOrEmpty(targetCardIds))
        {
            string[] idStrings = targetCardIds.Split(',');
            
            foreach (string idStr in idStrings)
            {
                if (int.TryParse(idStr.Trim(), out int id))
                {
                    result.Add(id);
                }
            }
        }
        
        return result;
    }
    
    // カード編集タブの描画
    private void DrawEditCardTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カード編集", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // カード検索
        EditorGUILayout.BeginHorizontal();
        searchQuery = EditorGUILayout.TextField("カード検索", searchQuery);
        if (GUILayout.Button("検索", GUILayout.Width(60)))
        {
            // 検索処理
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // カード一覧
        DrawCardList();
        
        // 選択されたカードの編集UI
        if (selectedCard != null)
        {
            GUILayout.Space(15);
            EditorGUILayout.LabelField($"カード編集: {selectedCard.cardName}", EditorStyles.boldLabel);
            
            // カード種類に応じた編集UI
            DrawSelectedCardEditFields();
            
            // 更新ボタン
            GUILayout.Space(10);
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("カードを更新", GUILayout.Height(30)))
            {
                UpdateSelectedCard();
            }
            GUI.backgroundColor = Color.white;
        }
    }
    
    // カード一覧の描画
    private void DrawCardList()
    {
        EditorGUILayout.LabelField("カード一覧", EditorStyles.boldLabel);
        
        GUILayout.BeginVertical("box");
        
        // カードデータベースからカードを取得
        List<Card> allCards = cardDatabase.GetAllCards();
        
        // 検索フィルタリング
        if (!string.IsNullOrEmpty(searchQuery))
        {
            allCards = allCards.FindAll(c => 
                c.cardName.ToLower().Contains(searchQuery.ToLower()) ||
                c.description.ToLower().Contains(searchQuery.ToLower())
            );
        }
        
        if (allCards.Count > 0)
        {
            foreach (Card card in allCards)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // カード選択ボタン
                if (GUILayout.Button(card.id.ToString(), GUILayout.Width(50)))
                {
                    // データベースから最新のカード情報を取得
                    selectedCard = cardDatabase.GetCardById(card.id);
                    
                    // UI更新を強制
                    Repaint();
                }
                
                // カード情報
                string cardTypeStr = card.type.ToString();
                string cardInfo = $"{card.cardName} (コスト: {card.cost})";
                
                EditorGUILayout.LabelField(cardTypeStr, GUILayout.Width(80));
                EditorGUILayout.LabelField(cardInfo);
                
                // カード削除ボタン
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("カード削除", $"カード「{card.cardName}」を削除しますか？", "削除", "キャンセル"))
                    {
                        DeleteCard(card);
                    }
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("カードが見つかりません。");
        }
        
        GUILayout.EndVertical();
    }
    
    // カードの編集フィールドの描画
    private void DrawSelectedCardEditFields()
    {
        // 共通フィールド
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        
        selectedCard.cardName = EditorGUILayout.TextField("カード名", selectedCard.cardName);
        
        EditorGUILayout.LabelField("説明");
        string oldDescription = selectedCard.description;
        string newDescription = EditorGUILayout.TextArea(oldDescription, GUILayout.Height(60));

        // 説明が変更された場合のみ更新
        if (newDescription != oldDescription)
        {
            selectedCard.description = newDescription;
            EditorUtility.SetDirty(selectedCard);
        }
        
        selectedCard.cost = EditorGUILayout.IntSlider("コスト", selectedCard.cost, 0, 10);
        
        // アートワーク選択UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("アートワーク");

        // アートワークプレビュー
        Rect previewRect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100), GUILayout.Height(100));
        
        // ファイル名からSpriteをロード
        Sprite artworkSprite = null;
        if (!string.IsNullOrEmpty(selectedCard.artwork))
        {
            // アートワークのパスを推測
            string artworkPath = "";
            if (selectedCard is CharacterCard)
            {
                artworkPath = "CardImages/Characters/" + selectedCard.artwork;
            }
            else if (selectedCard is SpellCard)
            {
                artworkPath = "CardImages/Spells/" + selectedCard.artwork;
            }
            else if (selectedCard is FieldCard)
            {
                artworkPath = "CardImages/Fields/" + selectedCard.artwork;
            }
            
            // リソースからロード
            artworkSprite = Resources.Load<Sprite>(artworkPath);
        }
        
        if (artworkSprite != null)
        {
            // アートワークプレビューの表示
            Texture2D texture = artworkSprite.texture;
            if (texture != null)
            {
                GUI.DrawTextureWithTexCoords(
                    previewRect,
                    texture,
                    new Rect(
                        artworkSprite.rect.x / texture.width,
                        artworkSprite.rect.y / texture.height,
                        artworkSprite.rect.width / texture.width,
                        artworkSprite.rect.height / texture.height
                    )
                );
            }
            else
            {
                GUI.Box(previewRect, "テクスチャなし");
            }
        }
        else
        {
            GUI.Box(previewRect, "アートワークなし");
        }

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button("アートワーク選択", GUILayout.Width(150)))
        {
            // アートワークセレクターを表示
            ShowArtworkSelectorForEditingCard();
        }

        if (GUILayout.Button("アートワーククリア", GUILayout.Width(150)))
        {
            // アートワークをクリア
            selectedCard.artwork = "";
            EditorUtility.SetDirty(selectedCard);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        
        // カード種類に応じた特殊フィールド
        if (selectedCard is CharacterCard characterCard)
        {
            // キャラクターカード編集時に選択中カテゴリーを初期化
            // このカードが持っているカテゴリーでselectedCategoriesを更新
            if (characterCard.categories != null && selectedCategories.Count == 0)
            {
                selectedCategories.Clear();
                foreach (var category in characterCard.categories)
                {
                    if (category != null)
                        selectedCategories.Add(category);
                }
                UpdateCategorySelections();
            }
            
            DrawCharacterCardEditFields(characterCard);
        }
        else if (selectedCard is SpellCard spellCard)
        {
            DrawSpellCardEditFields(spellCard);
        }
        else if (selectedCard is FieldCard fieldCard)
        {
            DrawFieldCardEditFields(fieldCard);
        }
        
        // 効果の編集 (フィールドカード以外)
        if (!(selectedCard is FieldCard))
        {
            DrawCardEffectEdit();
        }
    }

    // 編集中のカード用のアートワークセレクター
    private void ShowArtworkSelectorForEditingCard()
    {
        // アートワークブラウザを開く
        showArtworkBrowser = true;
        isEditingCardArtwork = true;
        
        // カードタイプに応じてアートワークタイプを設定
        if (selectedCard is CharacterCard)
        {
            selectedArtworkType = CardImageImporter.CardImageType.Character;
        }
        else if (selectedCard is SpellCard)
        {
            selectedArtworkType = CardImageImporter.CardImageType.Spell;
        }
        else if (selectedCard is FieldCard)
        {
            selectedArtworkType = CardImageImporter.CardImageType.Field;
        }
        
        // アートワークリストを更新
        RefreshArtworkList();
    }

    private void DrawCardEffectEdit()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カード効果", EditorStyles.boldLabel);
        
        // 効果のリストを表示
        List<CardEffect> currentEffects = GetCurrentCardEffects();
        
        // 効果が存在しない場合のメッセージ
        if (currentEffects == null || currentEffects.Count == 0)
        {
            EditorGUILayout.HelpBox("このカードには効果がありません。", MessageType.Info);
        }
        else
        {
            // 最大2つまでの効果のみ表示（キャラクターカードの制限）
            int maxEffects = selectedCard is CharacterCard ? 2 : 3;
            
            // 既存の効果を表示
            for (int i = 0; i < Math.Min(currentEffects.Count, maxEffects); i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // 効果タイプの選択
                currentEffects[i] = DrawEffectTypePopup(currentEffects[i]);
                
                // カード種類に応じて効果の詳細編集
                if (selectedCard is CharacterCard)
                {
                    DrawCharacterCardEffectDetails(currentEffects[i]);
                }
                else if (selectedCard is SpellCard)
                {
                    DrawSpellCardEffectDetails(currentEffects[i]);
                }
                else if (selectedCard is FieldCard)
                {
                    DrawFieldCardEffectDetails(currentEffects[i]);
                }
                
                // 効果の削除ボタン
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    if (EditorUtility.DisplayDialog("効果削除", "この効果を削除しますか？", "削除", "キャンセル"))
                    {
                        // エフェクトをアセットから削除
                        AssetDatabase.RemoveObjectFromAsset(currentEffects[i]);
                        currentEffects.RemoveAt(i);
                        break;
                    }
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            // 最大効果数に達した場合は追加できないことを表示
            if (selectedCard is CharacterCard && currentEffects.Count >= maxEffects)
            {
                EditorGUILayout.HelpBox($"キャラクターカードの効果は最大{maxEffects}つまでしか追加できません。", MessageType.Warning);
            }
        }
        
        GUILayout.Space(10);
        
        // 新しい効果の追加ボタン
        EditorGUI.BeginDisabledGroup(selectedCard is CharacterCard && currentEffects != null && currentEffects.Count >= 2);
        if (GUILayout.Button("新しい効果を追加"))
        {
            // 新しい効果の追加ダイアログを表示
            ShowEffectAddMenu();
        }
        EditorGUI.EndDisabledGroup();
    }

    // 各カードタイプに応じた効果詳細表示メソッド
    private void DrawCharacterCardEffectDetails(CardEffect effect)
    {
        // キャラクターカード専用効果の詳細表示
        if (effect is DrawCardEffect drawEffect)
        {
            drawEffect.drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawEffect.drawCount, 1, 5);
        }
        else if (effect is StatModifierEffect statEffect)
        {
            statEffect.attackBonus = EditorGUILayout.IntSlider("攻撃力ボーナス", statEffect.attackBonus, -1000, 1000);
            statEffect.defenseBonus = EditorGUILayout.IntSlider("防御力ボーナス", statEffect.defenseBonus, -1000, 1000);
            statEffect.targetCategory = EditorGUILayout.TextField("対象カテゴリー", statEffect.targetCategory);
        }
        else if (effect is DamageEffect damageEffect)
        {
            damageEffect.damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageEffect.damageAmount, 0, 5000);
            damageEffect.targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", damageEffect.targetOpponent);
        }
        else if (effect is HealEffect healEffect)
        {
            healEffect.healAmount = EditorGUILayout.IntSlider("回復量", healEffect.healAmount, 0, 5000);
            healEffect.targetSelf = EditorGUILayout.Toggle("自分を回復", healEffect.targetSelf);
        }
        else if (effect is CategorySearchEffect searchEffect)
        {
            DrawCategorySelection("検索するカテゴリー", searchEffect.targetCategory, (newCategory) => {
                searchEffect.targetCategory = newCategory;
            });
            searchEffect.maxCards = EditorGUILayout.IntSlider("最大カード枚数", searchEffect.maxCards, 1, 3);
        }
        else if (effect is CategoryGraveyardEffect graveyardEffect)
        {
            DrawCategorySelection("検索するカテゴリー", graveyardEffect.targetCategory, (newCategory) => {
                graveyardEffect.targetCategory = newCategory;
            });
            graveyardEffect.maxCards = EditorGUILayout.IntSlider("最大カード枚数", graveyardEffect.maxCards, 1, 3);
        }
        else if (effect is CategoryBoostEffect boostEffect)
        {
            DrawCategorySelection("強化するカテゴリー", boostEffect.targetCategory, (newCategory) => {
                boostEffect.targetCategory = newCategory;
            });
            boostEffect.attackBoost = EditorGUILayout.IntSlider("攻撃力上昇", boostEffect.attackBoost, 0, 2000);
            boostEffect.defenseBoost = EditorGUILayout.IntSlider("防御力上昇", boostEffect.defenseBoost, 0, 2000);
        }
        else if (effect is CategoryCostReductionEffect costEffect)
        {
            DrawCategorySelection("対象カテゴリー", costEffect.targetCategory, (newCategory) => {
                costEffect.targetCategory = newCategory;
            });
            costEffect.costReduction = EditorGUILayout.IntSlider("コスト削減量", costEffect.costReduction, 1, 3);
        }
        else if (effect is SpecialVictoryEffect victoryEffect)
        {
            victoryEffect.requiredEffectName = EditorGUILayout.TextField("効果名", victoryEffect.requiredEffectName);
            victoryEffect.requiredCards = EditorGUILayout.IntSlider("必要カード枚数", victoryEffect.requiredCards, 2, 7);
        }
    }

    // DrawCategorySelectionメソッドを修正
    private void DrawCategorySelection(string label, CardCategory selectedCategory, Action<CardCategory> onCategorySelected)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        
        if (selectedCategory != null)
        {
            GUI.backgroundColor = selectedCategory.categoryColor;
            if (GUILayout.Button(selectedCategory.categoryName, GUILayout.Width(150)))
            {
                ShowCategorySelectMenu(selectedCategory, (newCategory) => {
                    onCategorySelected?.Invoke(newCategory);
                });
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (GUILayout.Button("カテゴリーを選択", GUILayout.Width(150)))
            {
                ShowCategorySelectMenu(null, (newCategory) => {
                    onCategorySelected?.Invoke(newCategory);
                });
            }
        }
        
        // クリアボタン
        if (selectedCategory != null)
        {
            if (GUILayout.Button("×", GUILayout.Width(25)))
            {
                onCategorySelected?.Invoke(null);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }

    // カテゴリー選択メニューを表示
    private void ShowCategorySelectMenu(CardCategory currentCategory, System.Action<CardCategory> onSelectionComplete)
    {
        if (categoryDatabase == null)
            return;
        
        // 一時変数と完了時のコールバックを保存
        tempSelectedCategory = currentCategory;
        categorySelectionCallback = onSelectionComplete;
        
        GenericMenu menu = new GenericMenu();
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        foreach (var category in allCategories)
        {
            bool isSelected = currentCategory == category;
            CardCategory capturedCategory = category; // 各ループでカテゴリーをキャプチャ
            
            menu.AddItem(
                new GUIContent(category.categoryName),
                isSelected,
                () => {
                    SelectCategory(capturedCategory);
                }
            );
        }
        
        menu.ShowAsContext();
    }

    // カテゴリーを選択する処理
    private void SelectCategory(CardCategory category)
    {
        // 選択されたカテゴリーをコールバックに渡す
        categorySelectionCallback?.Invoke(category);
    }

    // 現在のカードの効果リストを取得
    private List<CardEffect> GetCurrentCardEffects()
    {
        if (selectedCard is CharacterCard characterCard)
        {
            if (characterCard.effects == null)
                characterCard.effects = new List<CardEffect>();
            return characterCard.effects;
        }
        else if (selectedCard is SpellCard spellCard)
        {
            if (spellCard.effects == null)
                spellCard.effects = new List<CardEffect>();
            return spellCard.effects;
        }
        else if (selectedCard is FieldCard fieldCard)
        {
            if (fieldCard.effects == null)
                fieldCard.effects = new List<CardEffect>();
            return fieldCard.effects;
        }
        
        return null;
    }

    // スペルカード効果の詳細表示
    private void DrawSpellCardEffectDetails(CardEffect effect)
    {
        if (effect is DrawCardEffect drawEffect)
        {
            drawEffect.drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawEffect.drawCount, 1, 5);
        }
        else if (effect is DamageEffect damageEffect)
        {
            damageEffect.damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageEffect.damageAmount, 0, 5000);
            damageEffect.targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", damageEffect.targetOpponent);
        }
        else if (effect is HealEffect healEffect)
        {
            healEffect.healAmount = EditorGUILayout.IntSlider("回復量", healEffect.healAmount, 0, 5000);
            healEffect.targetSelf = EditorGUILayout.Toggle("自分を回復", healEffect.targetSelf);
        }
        else if (effect is StatModifierEffect statEffect)
        {
            statEffect.attackBonus = EditorGUILayout.IntSlider("攻撃力変更", statEffect.attackBonus, -1000, 1000);
            statEffect.defenseBonus = EditorGUILayout.IntSlider("防御力変更", statEffect.defenseBonus, -1000, 1000);
            
            // カテゴリー選択用のUI
            string currentCategory = statEffect.targetCategory;
            CardCategory selectedCat = null;
            
            if (!string.IsNullOrEmpty(currentCategory) && categoryDatabase != null)
            {
                selectedCat = categoryDatabase.GetCategoryByName(currentCategory);
            }
            
            DrawCategorySelection("対象カテゴリー", selectedCat, (newCategory) => {
                statEffect.targetCategory = newCategory != null ? newCategory.categoryName : "";
            });
        }
        else if (effect is AttackTargetEffect)
        {
            EditorGUILayout.HelpBox("攻撃対象効果: 設定はありません", MessageType.Info);
        }
        else if (effect is EnergyManipulationEffect energyEffect)
        {
            energyEffect.energyChange = EditorGUILayout.IntSlider("エナジー変更量", energyEffect.energyChange, -5, 5);
            energyEffect.affectSelf = EditorGUILayout.Toggle("自分のエナジーに影響", energyEffect.affectSelf);
            energyEffect.affectOpponent = EditorGUILayout.Toggle("相手のエナジーに影響", energyEffect.affectOpponent);
        }
    }

    // フィールドカード効果の詳細表示
    private void DrawFieldCardEffectDetails(CardEffect effect)
    {
        if (effect is DrawCardEffect drawEffect)
        {
            drawEffect.drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawEffect.drawCount, 1, 5);
        }
        else if (effect is DamageEffect damageEffect)
        {
            damageEffect.damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageEffect.damageAmount, 0, 5000);
            damageEffect.targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", damageEffect.targetOpponent);
        }
        else if (effect is HealEffect healEffect)
        {
            healEffect.healAmount = EditorGUILayout.IntSlider("回復量", healEffect.healAmount, 0, 5000);
            healEffect.targetSelf = EditorGUILayout.Toggle("自分を回復", healEffect.targetSelf);
        }
        else if (effect is StatModifierEffect statEffect)
        {
            statEffect.attackBonus = EditorGUILayout.IntSlider("攻撃力変更", statEffect.attackBonus, -1000, 1000);
            statEffect.defenseBonus = EditorGUILayout.IntSlider("防御力変更", statEffect.defenseBonus, -1000, 1000);
            statEffect.targetCategory = EditorGUILayout.TextField("対象カテゴリー", statEffect.targetCategory);
        }
        else if (effect is RemovalEffect)
        {
            EditorGUILayout.HelpBox("カード保護効果: 設定はありません", MessageType.Info);
        }
    }

    // 効果タイプの選択ポップアップ
    private CardEffect DrawEffectTypePopup(CardEffect currentEffect)
    {
        // 効果タイプの選択肢
        string[] effectTypes = GetEffectTypesForCurrentCardType();
        
        // 現在の効果タイプのインデックスを取得
        int currentTypeIndex = 0;
        
        if (currentEffect is DrawCardEffect) currentTypeIndex = 1;
        else if (currentEffect is StatModifierEffect) currentTypeIndex = 2;
        else if (currentEffect is DamageEffect) currentTypeIndex = 3;
        else if (currentEffect is HealEffect) currentTypeIndex = 4;
        else if (currentEffect is CategorySearchEffect) currentTypeIndex = 5;
        else if (currentEffect is CategoryGraveyardEffect) currentTypeIndex = 6;
        else if (currentEffect is CategoryBoostEffect) currentTypeIndex = 7;
        else if (currentEffect is CategoryCostReductionEffect) currentTypeIndex = 8;
        else if (currentEffect is SpecialVictoryEffect) currentTypeIndex = 9;
        else if (currentEffect is AttackTargetEffect) currentTypeIndex = 5;
        else if (currentEffect is EnergyManipulationEffect) currentTypeIndex = 6;
        else if (currentEffect is RemovalEffect) currentTypeIndex = 5;
        
        // エフェクトタイプを選択
        int selectedTypeIndex = EditorGUILayout.Popup("効果タイプ", currentTypeIndex, effectTypes);
        
        // 選択が変わらない場合は既存の効果を返す
        if (selectedTypeIndex == currentTypeIndex)
            return currentEffect;
            
        // 新しいタイプが「なし」の場合
        if (selectedTypeIndex == 0)
            return null;
            
        // 選択に応じて新しい効果を作成
        CardEffect newEffect = null;
        
        // カード種類に応じた効果作成
        if (selectedCard is CharacterCard)
        {
            switch (selectedTypeIndex)
            {
                case 1: // カードドロー
                    newEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
                    ((DrawCardEffect)newEffect).drawCount = 1;
                    break;
                case 2: // ステータス変更
                    newEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                    break;
                case 3: // ダメージ
                    newEffect = ScriptableObject.CreateInstance<DamageEffect>();
                    ((DamageEffect)newEffect).damageAmount = 500;
                    break;
                case 4: // 回復
                    newEffect = ScriptableObject.CreateInstance<HealEffect>();
                    ((HealEffect)newEffect).healAmount = 500;
                    break;
                case 5: // カテゴリー検索
                    newEffect = ScriptableObject.CreateInstance<CategorySearchEffect>();
                    break;
                case 6: // 捨て札回収
                    newEffect = ScriptableObject.CreateInstance<CategoryGraveyardEffect>();
                    break;
                case 7: // ステータス強化
                    newEffect = ScriptableObject.CreateInstance<CategoryBoostEffect>();
                    break;
                case 8: // コスト削減
                    newEffect = ScriptableObject.CreateInstance<CategoryCostReductionEffect>();
                    break;
                case 9: // 特殊勝利
                    newEffect = ScriptableObject.CreateInstance<SpecialVictoryEffect>();
                    break;
            }
        }
        else if (selectedCard is SpellCard)
        {
            switch (selectedTypeIndex)
            {
                case 1: // カードドロー
                    newEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
                    ((DrawCardEffect)newEffect).drawCount = 1;
                    break;
                case 2: // ダメージ
                    newEffect = ScriptableObject.CreateInstance<DamageEffect>();
                    ((DamageEffect)newEffect).damageAmount = 500;
                    break;
                case 3: // 回復
                    newEffect = ScriptableObject.CreateInstance<HealEffect>();
                    ((HealEffect)newEffect).healAmount = 500;
                    break;
                case 4: // ステータス変更
                    newEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                    break;
                case 5: // 攻撃対象効果
                    newEffect = ScriptableObject.CreateInstance<AttackTargetEffect>();
                    break;
                case 6: // エナジー操作
                    newEffect = ScriptableObject.CreateInstance<EnergyManipulationEffect>();
                    break;
            }
        }
        else if (selectedCard is FieldCard)
        {
            switch (selectedTypeIndex)
            {
                case 1: // カードドロー
                    newEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
                    ((DrawCardEffect)newEffect).drawCount = 1;
                    break;
                case 2: // ダメージ
                    newEffect = ScriptableObject.CreateInstance<DamageEffect>();
                    ((DamageEffect)newEffect).damageAmount = 500;
                    break;
                case 3: // 回復
                    newEffect = ScriptableObject.CreateInstance<HealEffect>();
                    ((HealEffect)newEffect).healAmount = 500;
                    break;
                case 4: // ステータス変更
                    newEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                    break;
                case 5: // カード保護
                    newEffect = ScriptableObject.CreateInstance<RemovalEffect>();
                    break;
            }
        }
        
        // 新しい効果をアセットに追加
        if (newEffect != null)
        {
            AssetDatabase.AddObjectToAsset(newEffect, AssetDatabase.GetAssetPath(cardDatabase));
            
            // 既存の効果をアセットから削除
            if (currentEffect != null)
            {
                AssetDatabase.RemoveObjectFromAsset(currentEffect);
            }
        }
        
        return newEffect;
    }

    // 新しい効果の追加メニュー
    private void ShowEffectAddMenu()
    {
        // 効果タイプの選択肢
        string[] effectTypes = GetEffectTypesForCurrentCardType();
        
        GenericMenu menu = new GenericMenu();
        
        // 「なし」オプションは不要なのでスキップ
        for (int i = 1; i < effectTypes.Length; i++)
        {
            int effectIndex = i; // クロージャで使用するためにキャプチャ
            menu.AddItem(new GUIContent(effectTypes[i]), false, () => AddEffectWithType(effectIndex));
        }
        
        menu.ShowAsContext();
    }
    
    // 指定タイプの効果を追加
    private void AddEffectWithType(int effectTypeIndex)
    {
        // 現在のエフェクトインデックスを一時的に保存
        int prevIndex = selectedEffectIndex;
        
        // 選択されたエフェクトタイプをセット
        selectedEffectIndex = effectTypeIndex;
        
        // 現在のカードタイプに応じた効果を作成
        CardEffect newEffect = null;
        
        if (selectedCard is CharacterCard)
        {
            newEffect = CreateCharacterCardEffect();
        }
        else if (selectedCard is SpellCard)
        {
            newEffect = CreateSpellCardEffect();
        }
        else if (selectedCard is FieldCard)
        {
            newEffect = CreateFieldCardEffect();
        }
        
        // 効果を追加
        if (newEffect != null)
        {
            List<CardEffect> effects = GetCurrentCardEffects();
            if (effects != null)
            {
                // アセットに追加
                AssetDatabase.AddObjectToAsset(newEffect, AssetDatabase.GetAssetPath(cardDatabase));
                
                // リストに追加
                effects.Add(newEffect);
                
                // 変更を保存
                EditorUtility.SetDirty(selectedCard);
            }
        }
        
        // 選択を元に戻す
        selectedEffectIndex = prevIndex;
    }

    // キャラクターカード編集フィールドの描画
    private void DrawCharacterCardEditFields(CharacterCard card)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("キャラクターカード情報", EditorStyles.boldLabel);
        
        card.element = (ElementType)EditorGUILayout.EnumPopup("属性", card.element);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("攻撃力", GUILayout.Width(120));
        card.attackPower = EditorGUILayout.IntSlider(card.attackPower, 0, 5000);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("防御力", GUILayout.Width(120));
        card.defensePower = EditorGUILayout.IntSlider(card.defensePower, 0, 5000);
        EditorGUILayout.EndHorizontal();
        
        // カテゴリー関連UI
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カテゴリー設定", EditorStyles.boldLabel);
        
        // 現在のカテゴリーを表示（categories から）
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (card.categories == null || card.categories.Count == 0)
        {
            EditorGUILayout.LabelField("カテゴリー未選択", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("選択中のカテゴリー:", EditorStyles.boldLabel);
            
            for (int i = 0; i < card.categories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // カテゴリー名
                if (card.categories[i] != null)
                {
                    GUI.backgroundColor = card.categories[i].categoryColor;
                    EditorGUILayout.LabelField(card.categories[i].categoryName, EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;
                    
                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        card.categories.RemoveAt(i);
                        EditorUtility.SetDirty(card);
                        break;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("不明なカテゴリー", EditorStyles.helpBox);
                    
                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        card.categories.RemoveAt(i);
                        EditorUtility.SetDirty(card);
                        break;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndVertical();
        
        // カテゴリー編集ボタン（最大3つまで）
        EditorGUI.BeginDisabledGroup(card.categories != null && card.categories.Count >= 3);
        if (GUILayout.Button("カテゴリーを追加"))
        {
            // 一時的に選択カテゴリーをコピー
            selectedCategories.Clear();
            if (card.categories != null)
            {
                foreach (var category in card.categories)
                {
                    if (category != null)
                        selectedCategories.Add(category);
                }
            }
            
            // カテゴリー選択状態を更新
            UpdateCategorySelections();
            
            // カテゴリー選択UIを表示
            showCategorySelector = true;
            
            // 編集対象のカードを保持
            selectedCard = card;
        }
        EditorGUI.EndDisabledGroup();
    }
    
    // カード編集用のカテゴリー追加メニュー
    private void ShowAddCategoryMenu(CharacterCard card)
    {
        if (categoryDatabase == null) return;
        
        GenericMenu menu = new GenericMenu();
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        foreach (var category in allCategories)
        {
            // 既にカードに追加されているカテゴリーはスキップ
            bool alreadyAdded = card.categories != null && card.categories.Contains(category);
            if (alreadyAdded) continue;
            
            CardCategory capturedCategory = category; // クロージャ用
            menu.AddItem(new GUIContent(category.categoryName), false, () => {
                // カテゴリーリストが初期化されていない場合
                if (card.categories == null)
                    card.categories = new List<CardCategory>();
                    
                // カテゴリーを追加
                card.categories.Add(capturedCategory);
                EditorUtility.SetDirty(card);
            });
        }
        
        menu.ShowAsContext();
    }
    
    // スペルカード編集フィールドの描画
    private void DrawSpellCardEditFields(SpellCard card)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("スペルカード情報", EditorStyles.boldLabel);
        
        // スペルカード拡張機能のプロパティ
        card.canActivateOnOpponentTurn = EditorGUILayout.Toggle("相手ターンで発動可能", card.canActivateOnOpponentTurn);
    }
    
    // フィールドカード編集フィールドの描画
    private void DrawFieldCardEditFields(FieldCard card)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("フィールドカード情報", EditorStyles.boldLabel);
        
        // フィールドカードの新しいプロパティを編集UI
        card.affectsOwnField = EditorGUILayout.Toggle("自分のフィールドに適用", card.affectsOwnField);
        card.affectsOpponentField = EditorGUILayout.Toggle("相手のフィールドに適用", card.affectsOpponentField);
        
        // モードと修正値
        card.modifiesStats = EditorGUILayout.Toggle("ステータス修正を行う", card.modifiesStats);
        if (card.modifiesStats)
        {
            card.attackModifier = EditorGUILayout.IntSlider("攻撃力修正値", card.attackModifier, -2000, 2000);
            card.defenseModifier = EditorGUILayout.IntSlider("防御力修正値", card.defenseModifier, -2000, 2000);
        }
        
        // その他のプロパティ
        card.allowsDeckSearch = EditorGUILayout.Toggle("デッキ検索機能", card.allowsDeckSearch);
        card.allowsGraveyardRecovery = EditorGUILayout.Toggle("墓地回収機能", card.allowsGraveyardRecovery);
        
        // 視覚効果と保護効果
        card.revealOpponentHand = EditorGUILayout.Toggle("相手の手札公開", card.revealOpponentHand);
        card.preventBattleDestruction = EditorGUILayout.Toggle("戦闘破壊防止", card.preventBattleDestruction);
        card.preventSpellDestruction = EditorGUILayout.Toggle("スペル破壊防止", card.preventSpellDestruction);
        
        // ライフ回復効果
        card.providesLifeRecovery = EditorGUILayout.Toggle("ライフ回復機能", card.providesLifeRecovery);
        if (card.providesLifeRecovery)
        {
            card.lifeRecoveryAmount = EditorGUILayout.IntSlider("回復量", card.lifeRecoveryAmount, 100, 2000);
        }
        
        // 影響するカテゴリーと属性の表示（新しい実装に合わせる）
        GUILayout.Space(10);
        EditorGUILayout.LabelField("影響するカテゴリー");
        
        // カテゴリー表示
        if (card.affectedCategories != null && card.affectedCategories.Count > 0)
        {
            for (int i = 0; i < card.affectedCategories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                CardCategory category = card.affectedCategories[i];
                
                if (category != null)
                {
                    GUI.backgroundColor = category.categoryColor;
                    EditorGUILayout.LabelField(category.categoryName, EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;
                    
                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        card.affectedCategories.RemoveAt(i);
                        EditorUtility.SetDirty(card);
                        break;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("不明なカテゴリー", EditorStyles.helpBox);
                    
                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        card.affectedCategories.RemoveAt(i);
                        EditorUtility.SetDirty(card);
                        break;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("すべてのカテゴリー", EditorStyles.miniLabel);
        }
        
        // カテゴリー追加ボタン
        if (GUILayout.Button("カテゴリーを追加"))
        {
            // カテゴリー選択メニューを表示
            ShowAddFieldCategoryMenu(card);
        }
        
        // 属性表示
        EditorGUILayout.LabelField("影響する属性");
        if (card.affectedElements != null && card.affectedElements.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int i = 0; i < card.affectedElements.Count; i++)
            {
                ElementType element = card.affectedElements[i];
                
                // 属性表示
                EditorGUILayout.LabelField(element.ToString(), EditorStyles.helpBox, GUILayout.Width(80));
                
                // 3つごとに改行
                if ((i + 1) % 3 == 0 && i < card.affectedElements.Count - 1)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("属性をクリア"))
            {
                card.affectedElements.Clear();
                EditorUtility.SetDirty(card);
            }
        }
        else
        {
            EditorGUILayout.LabelField("すべての属性", EditorStyles.miniLabel);
        }
        
        // 属性追加ボタン
        if (GUILayout.Button("属性を追加"))
        {
            // 属性選択メニューを表示
            ShowAddElementMenu(card);
        }
    }
    
    // フィールドカード用のカテゴリー追加メニュー
    private void ShowAddFieldCategoryMenu(FieldCard card)
    {
        if (categoryDatabase == null) return;
        
        GenericMenu menu = new GenericMenu();
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        foreach (var category in allCategories)
        {
            // 既にカードに追加されているカテゴリーはスキップ
            bool alreadyAdded = card.affectedCategories != null && card.affectedCategories.Contains(category);
            if (alreadyAdded) continue;
            
            CardCategory capturedCategory = category; // クロージャ用
            menu.AddItem(new GUIContent(category.categoryName), false, () => {
                // カテゴリーリストが初期化されていない場合
                if (card.affectedCategories == null)
                    card.affectedCategories = new List<CardCategory>();
                    
                // カテゴリーを追加
                card.affectedCategories.Add(capturedCategory);
                EditorUtility.SetDirty(card);
            });
        }
        
        menu.ShowAsContext();
    }
    
    // 属性追加メニュー
    private void ShowAddElementMenu(FieldCard card)
    {
        GenericMenu menu = new GenericMenu();
        string[] elementNames = System.Enum.GetNames(typeof(ElementType));
        
        for (int i = 0; i < elementNames.Length; i++)
        {
            ElementType element = (ElementType)i;
            
            // 既に追加されている属性はスキップ
            bool alreadyAdded = card.affectedElements != null && card.affectedElements.Contains(element);
            if (alreadyAdded) continue;
            
            ElementType capturedElement = element; // クロージャ用
            menu.AddItem(new GUIContent(elementNames[i]), false, () => {
                // 属性リストが初期化されていない場合
                if (card.affectedElements == null)
                    card.affectedElements = new List<ElementType>();
                    
                // 属性を追加
                card.affectedElements.Add(capturedElement);
                EditorUtility.SetDirty(card);
            });
        }
        
        menu.ShowAsContext();
    }
    
    // 選択されたカードの更新処理
    private void UpdateSelectedCard()
    {
        if (selectedCard != null)
        {
            // キャラクターカードの場合、選択されたカテゴリーを反映
            if (selectedCard is CharacterCard characterCard && characterCard.categories != null)
            {
                // 既存のカテゴリーをクリア
                characterCard.categories.Clear();
                
                // 選択されたカテゴリーを追加
                foreach (var category in selectedCategories)
                {
                    if (category != null)
                    {
                        characterCard.categories.Add(category);
                    }
                }
            }
            
            // カードを更新
            EditorUtility.SetDirty(selectedCard);
            EditorUtility.SetDirty(cardDatabase);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("カード更新", $"カード「{selectedCard.cardName}」を更新しました。", "OK");
        }
    }
    
    // カード削除処理
    private void DeleteCard(Card card)
    {
        if (card == null || cardDatabase == null)
        {
            Debug.LogError("カードまたはデータベースがnullです");
            return;
        }

        // カードをデータベースから削除
        cardDatabase.RemoveCard(card);

        // アセットから効果を削除
        if (card is CharacterCard characterCard)
        {
            foreach (var effect in characterCard.effects.ToList())
            {
                if (effect != null)
                    AssetDatabase.RemoveObjectFromAsset(effect);
            }
        }
        else if (card is SpellCard spellCard)
        {
            if (spellCard.effects != null)
            {
                foreach (var effect in spellCard.effects.ToList())
                {
                    if (effect != null)
                        AssetDatabase.RemoveObjectFromAsset(effect);
                }
            }
        }
        else if (card is FieldCard fieldCard)
        {
            if (fieldCard.effects != null)
            {
                foreach (var effect in fieldCard.effects.ToList())
                {
                    if (effect != null)
                        AssetDatabase.RemoveObjectFromAsset(effect);
                }
            }
        }
        
        // アセットから元のカードも削除
        AssetDatabase.RemoveObjectFromAsset(card);
        
        // UI更新
        selectedCard = null;
        
        // データベースを更新
        EditorUtility.SetDirty(cardDatabase);
        AssetDatabase.SaveAssets();
    }

    // 一括インポートタブの描画
    private void DrawBatchImportTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カード一括インポート", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.HelpBox("CSVファイルからカードを一括インポートできます。", MessageType.Info);
        
        // CSVファイル選択
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSVファイル", csvFile, typeof(TextAsset), false);
        
        // CSVのサンプルフォーマット表示
        GUILayout.Space(10);
        EditorGUILayout.LabelField("CSVフォーマット", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "CSVフォーマット例:\n\n" +
            "Type,Name,Description,Cost,Element,Attack,Defense,Category,SpellType,EffectValue\n" +
            "Character,戦士A,基本的な戦士,2,Neutral,1000,1000,戦士,,\n" +
            "Spell,魔法の矢,相手に500ダメージ,1,,,,,LifeDamage,500\n" +
            "Field,魔法森,戦士の攻撃力UP,3,,,,戦士,,", 
            MessageType.None
        );
        
        // インポートボタン
        GUILayout.Space(10);
        GUI.backgroundColor = csvFile != null ? Color.green : Color.gray;
        EditorGUI.BeginDisabledGroup(csvFile == null);
        if (GUILayout.Button("カードをインポート", GUILayout.Height(30)))
        {
            ImportCardsFromCSV();
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;
    }
    
    // CSVからカードをインポート
    private void ImportCardsFromCSV()
    {
        if (csvFile == null) return;
        
        string csv = csvFile.text;
        string[] lines = csv.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length <= 1)
        {
            EditorUtility.DisplayDialog("インポートエラー", "CSVデータが見つかりません。", "OK");
            return;
        }
        
        // ヘッダー行をスキップして1行目から処理
        int successCount = 0;
        int errorCount = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split(',');
            
            try
            {
                if (values.Length < 5)  // 最低限の列数チェック
                {
                    errorCount++;
                    continue;
                }
                
                // 基本情報の取得
                string typeStr = values[0].Trim();
                string name = values[1].Trim();
                string description = values[2].Trim();
                int cost = int.Parse(values[3].Trim());
                
                // 新規IDの生成
                int newId = GenerateNewCardId();
                
                // カード種類に応じた作成処理
                Card newCard = null;
                
                if (typeStr.Equals("Character", StringComparison.OrdinalIgnoreCase))
                {
                    // キャラクターカード作成
                    ElementType element = (ElementType)Enum.Parse(typeof(ElementType), values[4].Trim());
                    int attack = int.Parse(values[5].Trim());
                    int defense = int.Parse(values[6].Trim());
                    string categoryStr = values[7].Trim();
                    
                    CharacterCard card = ScriptableObject.CreateInstance<CharacterCard>();
                    card.id = newId;
                    card.cardName = name;
                    card.description = description;
                    card.cost = cost;
                    card.type = CardType.Character;
                    card.element = element;
                    card.attackPower = attack;
                    card.defensePower = defense;
                    
                    // 新しいカテゴリーシステムを使用
                    card.categories = new List<CardCategory>();
                    
                    // カテゴリーの追加
                    if (!string.IsNullOrEmpty(categoryStr) && categoryDatabase != null)
                    {
                        CardCategory foundCategory = categoryDatabase.GetCategoryByName(categoryStr);
                        if (foundCategory != null)
                        {
                            card.categories.Add(foundCategory);
                        }
                    }
                    
                    card.effects = new List<CardEffect>();
                    
                    newCard = card;
                }
                else if (typeStr.Equals("Spell", StringComparison.OrdinalIgnoreCase))
                {
                    // スペルカード作成
                    SpellType spellType = SpellType.Draw; // デフォルト値
                    if (values.Length > 8 && !string.IsNullOrEmpty(values[8].Trim()))
                    {
                        spellType = (SpellType)Enum.Parse(typeof(SpellType), values[8].Trim());
                    }
                    
                    int effectValue = 0;
                    if (values.Length > 9 && !string.IsNullOrEmpty(values[9].Trim()))
                    {
                        effectValue = int.Parse(values[9].Trim());
                    }
                    
                    SpellCard card = ScriptableObject.CreateInstance<SpellCard>();
                    card.id = newId;
                    card.cardName = name;
                    card.description = description;
                    card.cost = cost;
                    card.type = CardType.Spell;
                    
                    // スペルタイプの設定
                    card.spellType = spellType;
                    card.effects = new List<CardEffect>();
                    
                    newCard = card;
                }
                else if (typeStr.Equals("Field", StringComparison.OrdinalIgnoreCase))
                {
                    // フィールドカード作成
                    string categoryStr = values.Length > 7 ? values[7].Trim() : "";
                    
                    FieldCard card = ScriptableObject.CreateInstance<FieldCard>();
                    card.id = newId;
                    card.cardName = name;
                    card.description = description;
                    card.cost = cost;
                    card.type = CardType.Field;
                    card.effects = new List<CardEffect>();
                    
                    // 新しいフィールドカードプロパティの設定
                    card.affectedCategories = new List<CardCategory>();
                    card.affectedElements = new List<ElementType>();
                    
                    // カテゴリーの追加
                    if (!string.IsNullOrEmpty(categoryStr) && categoryDatabase != null)
                    {
                        CardCategory foundCategory = categoryDatabase.GetCategoryByName(categoryStr);
                        if (foundCategory != null)
                        {
                            card.affectedCategories.Add(foundCategory);
                        }
                    }
                    
                    newCard = card;
                }
                
                // データベースにカードを追加
                if (newCard != null)
                {
                    cardDatabase.AddCard(newCard);
                    AssetDatabase.AddObjectToAsset(newCard, AssetDatabase.GetAssetPath(cardDatabase));
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"行 {i+1} の処理中にエラーが発生しました: {ex.Message}");
                errorCount++;
            }
        }
        
        EditorUtility.SetDirty(cardDatabase);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("インポート完了", 
            $"カードインポートが完了しました。\n成功: {successCount}枚\nエラー: {errorCount}枚", 
            "OK");
    }

    // アートワークブラウザの初期化
    private void InitializeArtworkBrowser()
    {
        RefreshArtworkList();
    }

    // 利用可能なアートワークリストの更新
    private void RefreshArtworkList()
    {
        availableArtworks.Clear();
        filteredArtworks.Clear();
        
        // Resources/CardImagesフォルダからアートワークをロード
        string basePath = "CardImages";
        string typeFolderName = "";
        
        switch (selectedArtworkType)
        {
            case CardImageImporter.CardImageType.Character:
                typeFolderName = "Characters";
                break;
            case CardImageImporter.CardImageType.Spell:
                typeFolderName = "Spells";
                break;
            case CardImageImporter.CardImageType.Field:
                typeFolderName = "Fields";
                break;
            case CardImageImporter.CardImageType.Frame:
                typeFolderName = "Frames";
                break;
            case CardImageImporter.CardImageType.Icon:
                typeFolderName = "Icons";
                break;
        }
        
        // 指定フォルダ内のSprite型アセットをすべて読み込む
        string folderPath = Path.Combine(basePath, typeFolderName);
        Sprite[] sprites = Resources.LoadAll<Sprite>(folderPath);
        
        if (sprites != null && sprites.Length > 0)
        {
            availableArtworks.AddRange(sprites);
            filteredArtworks.AddRange(sprites);
        }
        
        // デフォルトアートワークの設定
        if (filteredArtworks.Count > 0)
        {
            switch (cardType)
            {
                case CardTypeSelection.Character:
                    defaultCharacterArtwork = filteredArtworks[0];
                    break;
                case CardTypeSelection.Spell:
                    defaultSpellArtwork = filteredArtworks[0];
                    break;
                case CardTypeSelection.Field:
                    defaultFieldArtwork = filteredArtworks[0];
                    break;
            }
        }
    }

    // アートワーク検索処理
    private void FilterArtworks()
    {
        if (string.IsNullOrEmpty(artworkSearchQuery))
        {
            filteredArtworks = new List<Sprite>(availableArtworks);
            return;
        }
        
        string query = artworkSearchQuery.ToLower();
        filteredArtworks = availableArtworks.FindAll(
            artwork => artwork.name.ToLower().Contains(query)
        );
    }

    // アートワーク選択UIを表示
    private void ShowArtworkSelector()
    {
        showArtworkBrowser = true;
        isEditingCardArtwork = false;
        
        // 初回表示時にアートワークリストを更新
        if (filteredArtworks.Count == 0)
        {
            InitializeArtworkBrowser();
        }
    }

    // アートワークブラウザの描画メソッド
    private void DrawArtworkBrowser()
    {
        // ポップアップウィンドウ
        Rect popupRect = new Rect(
            (position.width - 600) / 2,
            (position.height - 500) / 2,
            600,
            500
        );
        
        // ポップアップの背景
        GUI.Box(popupRect, "", "window");
        
        // ポップアップ内のコンテンツ
        GUILayout.BeginArea(popupRect);
        EditorGUILayout.BeginVertical();
        
        // タイトル
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("アートワーク選択", EditorStyles.boldLabel);
        
        // 閉じるボタン
        if (GUILayout.Button("✕", GUILayout.Width(30)))
        {
            showArtworkBrowser = false;
        }
        EditorGUILayout.EndHorizontal();
        
        // セパレータ
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        // アートワークタイプ選択
        CardImageImporter.CardImageType prevType = selectedArtworkType;
        selectedArtworkType = (CardImageImporter.CardImageType)EditorGUILayout.EnumPopup("アートワークタイプ", selectedArtworkType);
        
        // タイプが変更されたらリストを更新
        if (prevType != selectedArtworkType)
        {
            RefreshArtworkList();
        }
        
        // 検索フィールド
        EditorGUILayout.BeginHorizontal();
        string newSearchQuery = EditorGUILayout.TextField("検索", artworkSearchQuery);
        if (newSearchQuery != artworkSearchQuery)
        {
            artworkSearchQuery = newSearchQuery;
            FilterArtworks();
        }
        EditorGUILayout.EndHorizontal();
        
        // スクロールビュー
        artworkScrollPosition = EditorGUILayout.BeginScrollView(artworkScrollPosition, GUILayout.Height(350));
        
        int columns = 4;
        int count = filteredArtworks.Count;
        int rows = (count + columns - 1) / columns;
        
        for (int row = 0; row < rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            
            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index < count)
                {
                    Sprite artwork = filteredArtworks[index];
                    
                    EditorGUILayout.BeginVertical(GUILayout.Width(130));
                    
                    // プレビュー
                    Rect previewRect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100), GUILayout.Height(100));
                    
                    if (artwork != null)
                    {
                        // スプライトのプレビュー表示
                        Texture2D texture = artwork.texture;
                        if (texture != null)
                        {
                            GUI.DrawTextureWithTexCoords(
                                previewRect,
                                texture,
                                new Rect(
                                    artwork.rect.x / texture.width,
                                    artwork.rect.y / texture.height,
                                    artwork.rect.width / texture.width,
                                    artwork.rect.height / texture.height
                                )
                            );
                        }
                        else
                        {
                            GUI.Box(previewRect, "テクスチャなし");
                        }
                    }
                    else
                    {
                        GUI.Box(previewRect, "画像なし");
                    }
                    
                    // アートワーク名
                    EditorGUILayout.LabelField(artwork.name, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(100));
                    
                    if (GUILayout.Button("選択", GUILayout.Width(100)))
                    {
                        // 選択されたアートワークを設定
                        selectedArtwork = artwork;
                        
                        // カード種類に応じてデフォルトアートワークも更新
                        switch (cardType)
                        {
                            case CardTypeSelection.Character:
                                defaultCharacterArtwork = artwork;
                                break;
                            case CardTypeSelection.Spell:
                                defaultSpellArtwork = artwork;
                                break;
                            case CardTypeSelection.Field:
                                defaultFieldArtwork = artwork;
                                break;
                        }
                        
                        // 編集中のカードのアートワークを更新
                        if (isEditingCardArtwork && selectedCard != null)
                        {
                            // ファイル名のみを保存するように変更
                            selectedCard.artwork = artwork.name;
                            EditorUtility.SetDirty(selectedCard);
                        }
                        // ポップアップを閉じる
                        showArtworkBrowser = false;
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    // 空のセル
                    EditorGUILayout.BeginVertical(GUILayout.Width(130));
                    EditorGUILayout.Space(120);
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        // 新規画像インポートボタン
        if (GUILayout.Button("新しい画像をインポート"))
        {
            // CardImageImporterを呼び出す
            OpenImageImporter();
        }
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
#endif