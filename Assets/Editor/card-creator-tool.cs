using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq; // LINQ を追加

#if UNITY_EDITOR
/// <summary>
/// カードを作成・編集するためのエディタ拡張ツール
/// </summary>
public class CardCreatorTool : EditorWindow
{
    // カードタイプ
    private enum CardTypeSelection
    {
        Character,
        Spell,
        Field
    }
    
    // タブのインデックス
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
    private Sprite selectedArtwork; // 追加
    private List<Sprite> availableArtworks = new List<Sprite>(); // 追加
    private Sprite defaultCharacterArtwork; // 追加
    private Sprite defaultSpellArtwork; // 追加
    private Sprite defaultFieldArtwork; // 追加
    private Vector2 artworkScrollPosition; // 追加
    private string artworkSearchQuery = ""; // 追加
    private List<Sprite> filteredArtworks = new List<Sprite>(); // 追加
    private bool showArtworkBrowser = false; // 追加
    private CardImageImporter.CardImageType selectedArtworkType = CardImageImporter.CardImageType.Character; // 追加
    
    // キャラクターカードデータ
    private ElementType elementType = ElementType.Neutral;
    private int attackPower = 1000;
    private int defensePower = 1000;
    
    // スペルカードデータ
    private SpellType spellType = SpellType.Draw;
    private int effectValue = 1;
    private bool canActivateOnOpponentTurn = false;
    
    // フィールドカードデータ
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

    // カードアートワークを読み込むヘルパーメソッド
    private Sprite LoadCardArtworkSprite(Card card)
    {
        if (string.IsNullOrEmpty(card.artwork))
            return null;
            
        // カードの種類に応じたパスを決定
        string artworkPath = "";
        if (card is CharacterCard)
        {
            artworkPath = "CardImages/Characters/" + card.artwork;
        }
        else if (card is SpellCard)
        {
            artworkPath = "CardImages/Spells/" + card.artwork;
        }
        else if (card is FieldCard)
        {
            artworkPath = "CardImages/Fields/" + card.artwork;
        }
        
        // リソースからSpriteをロード
        return Resources.Load<Sprite>(artworkPath);
    }

    private void DrawEffectSelection()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カード効果", EditorStyles.boldLabel);
        
        selectedEffectIndex = EditorGUILayout.Popup("効果タイプ", selectedEffectIndex, effectTypes);
        
        switch (selectedEffectIndex)
        {
            case 1: // ドロー効果
                EditorGUILayout.HelpBox("ドロー効果: プレイヤーがカードを引く効果です。", MessageType.Info);
                drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawCount, 1, 5);
                break;
                
            case 2: // 攻撃対象効果
                EditorGUILayout.HelpBox("攻撃対象効果: 特定のカードを攻撃対象にする効果です。", MessageType.Info);
                break;
                
            case 3: // バフ効果
                EditorGUILayout.HelpBox("バフ効果: キャラクターの攻撃力/防御力を上昇させます。", MessageType.Info);
                attackBonus = EditorGUILayout.IntSlider("攻撃力ボーナス", attackBonus, 0, 1000);
                defenseBonus = EditorGUILayout.IntSlider("防御力ボーナス", defenseBonus, 0, 1000);
                targetCategory = EditorGUILayout.TextField("対象カテゴリー（オプション）", targetCategory);
                break;
                
            case 4: // デバフ効果
                EditorGUILayout.HelpBox("デバフ効果: 相手キャラクターを弱体化する効果です。", MessageType.Info);
                attackBonus = EditorGUILayout.IntSlider("攻撃力ペナルティ", attackBonus, 0, 1000);
                defenseBonus = EditorGUILayout.IntSlider("防御力ペナルティ", defenseBonus, 0, 1000);
                targetCategory = EditorGUILayout.TextField("対象カテゴリー（オプション）", targetCategory);
                break;
                
            case 5: // ライフダメージ
                EditorGUILayout.HelpBox("ライフダメージ: プレイヤーにダメージを与える効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", targetOpponent);
                break;
                
            case 6: // ライフ回復
                EditorGUILayout.HelpBox("ライフ回復: プレイヤーのライフポイントを回復する効果です。", MessageType.Info);
                damageAmount = EditorGUILayout.IntSlider("回復量", damageAmount, 100, 5000);
                targetOpponent = EditorGUILayout.Toggle("相手プレイヤーを回復", targetOpponent);
                break;
        }
    }

    // カードに効果を追加
    private void AddEffectToCard(Card card)
    {
        if (selectedEffectIndex <= 0) return;
        
        CardEffect effect = null;
        
        switch (selectedEffectIndex)
        {
            case 1: // ドロー効果
                DrawCardEffect drawEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
                drawEffect.drawCount = drawCount;
                effect = drawEffect;
                break;
                
            case 2: // 攻撃対象効果
                AttackTargetEffect attackEffect = ScriptableObject.CreateInstance<AttackTargetEffect>();
                effect = attackEffect;
                break;
                
            case 3: // バフ効果
                StatModifierEffect buffEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                buffEffect.attackBonus = attackBonus;
                buffEffect.defenseBonus = defenseBonus;
                buffEffect.targetCategory = targetCategory;
                effect = buffEffect;
                break;
                
            case 4: // デバフ効果
                StatModifierEffect debuffEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                debuffEffect.attackBonus = -attackBonus; // マイナスのボーナスでデバフ
                debuffEffect.defenseBonus = -defenseBonus;
                debuffEffect.targetCategory = targetCategory;
                effect = debuffEffect;
                break;
                
            case 5: // ライフダメージ
                DamageEffect damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                damageEffect.damageAmount = damageAmount;
                damageEffect.targetOpponent = targetOpponent;
                effect = damageEffect;
                break;
                
            case 6: // ライフ回復
                HealEffect healEffect = ScriptableObject.CreateInstance<HealEffect>();
                healEffect.healAmount = damageAmount;
                healEffect.targetSelf = !targetOpponent;
                effect = healEffect;
                break;
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
                // SpellCardにeffectsがない可能性があるため暫定対応
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
        
    // メニューにツールを追加
    [MenuItem("Tools/Card Game/Card Creator Tool")]
    public static void ShowWindow()
    {
        CardCreatorTool window = GetWindow<CardCreatorTool>("カード制作ツール");
        window.minSize = new Vector2(500, 650);
        window.Show();
    }
    
    // ウィンドウ初期化
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
            // カテゴリー選択状態の初期化
            InitializeCategorySelections();
        }
    }

    // カテゴリー選択状態の初期化
    private void InitializeCategorySelections()
    {
        if (categoryDatabase == null) return;
        
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
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
    
    // GUI描画
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
            DrawCategorySelector();
            return;
        }
    }

    // カテゴリー選択UIの描画
    private void DrawCategorySelector()
    {
        // ポップアップのような背景
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.LabelField("カテゴリー選択", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // スクロールエリア
        categoriesScrollPos = EditorGUILayout.BeginScrollView(categoriesScrollPos, GUILayout.Height(300));
        
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        // カテゴリーがない場合
        if (allCategories.Count == 0)
        {
            EditorGUILayout.HelpBox("カテゴリーがありません。カテゴリーマネージャーでカテゴリーを作成してください。", MessageType.Info);
        }
        else
        {
            // カテゴリー選択状態が未初期化の場合
            if (categorySelections.Count != allCategories.Count)
            {
                InitializeCategorySelections();
            }
            
            // 参照する選択状態
            List<bool> targetSelections = cardType == CardTypeSelection.Character 
                ? categorySelections 
                : fieldCategorySelections;
            
            // すべてのカテゴリーを表示
            for (int i = 0; i < allCategories.Count; i++)
            {
                CardCategory category = allCategories[i];
                
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
                selectedCategories.Add(allCategories[i]);
                
                // 最大3つまで
                if (selectedCategories.Count >= 3)
                    break;
            }
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
                selectedFieldCategories.Add(allCategories[i]);
            }
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
    
    #region カード作成タブ
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
        
        // カードエフェクト
        DrawEffectSelection();
        
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
        
        // 選択されたカテゴリーをコピー
        foreach (var category in selectedFieldCategories)
        {
            if (category != null)
            {
                card.affectedCategories.Add(category);
            }
        }
        
        // 属性リストが初期化されていることを確認
        card.affectedElements = new List<ElementType>();
        
        // 選択された属性をコピー
        foreach (var element in selectedElements)
        {
            card.affectedElements.Add(element);
        }
        
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
                card.categories.Add(category);
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
            GUI.DrawTextureWithTexCoords(
                previewRect,
                selectedArtwork.texture,
                new Rect(
                    selectedArtwork.rect.x / selectedArtwork.texture.width,
                    selectedArtwork.rect.y / selectedArtwork.texture.height,
                    selectedArtwork.rect.width / selectedArtwork.texture.width,
                    selectedArtwork.rect.height / selectedArtwork.texture.height
                )
            );
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
            showCategorySelector = true;
        }
        EditorGUI.EndDisabledGroup();
    }
    
    // スペルカード固有フィールドの描画
    private void DrawSpellCardFields()
    {
        EditorGUILayout.LabelField("スペルカード情報", EditorStyles.boldLabel);
        
        spellType = (SpellType)EditorGUILayout.EnumPopup("スペルタイプ", spellType);
        
        effectValue = EditorGUILayout.IntSlider("効果値", effectValue, 1, 10);
        
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
        allowsDeckSearch = EditorGUILayout.ToggleLeft("デッキからカードを手札に加える (1ターン1回)", allowsDeckSearch);
        allowsGraveyardRecovery = EditorGUILayout.ToggleLeft("捨て札からカードを手札に加える (1ターン1回)", allowsGraveyardRecovery);
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
            
            // カード効果があれば追加
            AddEffectToCard(newCard);
            
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
        // category プロパティは削除され、categories リストに置き換えられた
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
        
        // 効果リストが初期化されていることを確認
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
        // boostTargetIds プロパティは存在しなくなった、代わりに改修された機能を使用
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
        
        // アートワークがある場合は設定
        if (selectedArtwork != null)
        {
            card.artwork = selectedArtwork.name;
        }
    }
    
    // Texture2DからSpriteを作成
    private Sprite CreateSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
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
        
        // カテゴリー関連のリセット
        selectedCategories.Clear();
        selectedFieldCategories.Clear();
        UpdateCategorySelections();
        UpdateFieldCategorySelections();
        
        // スペルカードフィールドのリセット
        effectValue = 1;
        canActivateOnOpponentTurn = false;
        
        // フィールドカードフィールドのリセット
        // 属性リストもリセット
        selectedElements.Clear();
        for (int i = 0; i < elementSelections.Count; i++)
        {
            elementSelections[i] = false;
        }
        
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

         // アートワークをリセット
        selectedArtwork = null;
        
        // エフェクト選択のリセット
        selectedEffectIndex = 0;
    }
    
    // ターゲットカードIDのパース（互換性のため残す）
    private List<int> ParseTargetCardIds()
    {
        return new List<int>();
    }
    #endregion
    
    #region カード編集タブ
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
                    selectedCard = card;
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
        selectedCard.description = EditorGUILayout.TextArea(selectedCard.description, GUILayout.Height(60));
        
        selectedCard.cost = EditorGUILayout.IntSlider("コスト", selectedCard.cost, 0, 10);
        
        // アートワーク選択UI
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("アートワーク");

        // アートワークプレビュー
        Rect previewRect = EditorGUILayout.GetControlRect(false, 100, GUILayout.Width(100), GUILayout.Height(100));
        
        // ファイル名からSpriteをロード
        Sprite artworkSprite = LoadCardArtworkSprite(selectedCard);
        
        if (artworkSprite != null)
        {
            // アートワークプレビューの表示
            GUI.DrawTextureWithTexCoords(
                previewRect,
                artworkSprite.texture,
                new Rect(
                    artworkSprite.rect.x / artworkSprite.texture.width,
                    artworkSprite.rect.y / artworkSprite.texture.height,
                    artworkSprite.rect.width / artworkSprite.texture.width,
                    artworkSprite.rect.height / artworkSprite.texture.height
                )
            );
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
            selectedCard.artwork = null;
            EditorUtility.SetDirty(selectedCard);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        
        // カード種類に応じた特殊フィールド
        if (selectedCard is CharacterCard characterCard)
        {
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
        
        // 効果の編集
        DrawCardEffectEdit();
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


    // カード効果の編集
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
            // 既存の効果を表示
            for (int i = 0; i < currentEffects.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // 効果タイプの選択
                currentEffects[i] = DrawEffectTypePopup(currentEffects[i]);
                
                // 効果の詳細編集
                if (currentEffects[i] is DrawCardEffect drawEffect)
                {
                    drawEffect.drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawEffect.drawCount, 1, 5);
                }
                else if (currentEffects[i] is StatModifierEffect statEffect)
                {
                    statEffect.defenseBonus = EditorGUILayout.IntSlider("防御力ボーナス", statEffect.defenseBonus, -1000, 1000);
                    statEffect.targetCategory = EditorGUILayout.TextField("対象カテゴリー", statEffect.targetCategory);
                }
                else if (currentEffects[i] is DamageEffect damageEffect)
                {
                    damageEffect.damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageEffect.damageAmount, 0, 5000);
                    damageEffect.targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", damageEffect.targetOpponent);
                }
                else if (currentEffects[i] is HealEffect healEffect)
                {
                    healEffect.healAmount = EditorGUILayout.IntSlider("回復量", healEffect.healAmount, 0, 5000);
                    healEffect.targetSelf = EditorGUILayout.Toggle("自分を回復", healEffect.targetSelf);
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
        }
        
        // 新しい効果の追加ボタン
        GUILayout.Space(10);
        if (GUILayout.Button("新しい効果を追加"))
        {
            // 新しい効果の追加ダイアログを表示
            ShowEffectAddMenu();
        }
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

    // DrawEffectListメソッドを以下のように修正
    private void DrawEffectList(List<CardEffect> effects)
    {
        if (effects == null || effects.Count == 0)
        {
            EditorGUILayout.HelpBox("効果がありません。", MessageType.Info);
            return;
        }
        
        for (int i = 0; i < effects.Count; i++)
        {
            EditorGUILayout.BeginHorizontal("box");
            
            // 効果の種類に応じて編集フィールドを表示
            if (effects[i] is DrawCardEffect drawEffect)
            {
                drawEffect.drawCount = EditorGUILayout.IntSlider("ドロー枚数", drawEffect.drawCount, 1, 5);
            }
            else if (effects[i] is StatModifierEffect statEffect)
            {
                statEffect.attackBonus = EditorGUILayout.IntSlider("攻撃力ボーナス", statEffect.attackBonus, -1000, 1000);
                statEffect.defenseBonus = EditorGUILayout.IntSlider("防御力ボーナス", statEffect.defenseBonus, -1000, 1000);
                statEffect.targetCategory = EditorGUILayout.TextField("対象カテゴリー", statEffect.targetCategory);
            }
            else if (effects[i] is DamageEffect damageEffect)
            {
                damageEffect.damageAmount = EditorGUILayout.IntSlider("ダメージ量", damageEffect.damageAmount, 0, 5000);
                damageEffect.targetOpponent = EditorGUILayout.Toggle("相手プレイヤーにダメージ", damageEffect.targetOpponent);
            }
            else if (effects[i] is HealEffect healEffect)
            {
                healEffect.healAmount = EditorGUILayout.IntSlider("回復量", healEffect.healAmount, 0, 5000);
                healEffect.targetSelf = EditorGUILayout.Toggle("自分を回復", healEffect.targetSelf);
            }
            
            // 効果の削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("効果削除", "この効果を削除しますか？", "削除", "キャンセル"))
                {
                    // 効果を削除
                    DeleteCardEffect(selectedCard, effects[i]);
                    break;
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }
        
        // 新しい効果の追加ボタン
        if (GUILayout.Button("新しい効果を追加"))
        {
            ShowEffectAddMenu();
        }
    }

    // 効果タイプの選択ポップアップ
    private CardEffect DrawEffectTypePopup(CardEffect currentEffect)
    {
        // 効果タイプの選択肢
        string[] effectTypes = new string[] 
        { 
            "なし", 
            "カードドロー", 
            "ステータス変更", 
            "ダメージ", 
            "回復" 
        };
        
        // 現在の効果タイプのインデックスを取得
        int currentTypeIndex = 0;
        if (currentEffect is DrawCardEffect) currentTypeIndex = 1;
        else if (currentEffect is StatModifierEffect) currentTypeIndex = 2;
        else if (currentEffect is DamageEffect) currentTypeIndex = 3;
        else if (currentEffect is HealEffect) currentTypeIndex = 4;
        
        // エフェクトタイプを選択
        int selectedTypeIndex = EditorGUILayout.Popup("効果タイプ", currentTypeIndex, effectTypes);
        
        // 選択に応じて新しい効果を作成
        CardEffect newEffect = null;
        switch (selectedTypeIndex)
        {
            case 0: // なし
                return null;
            case 1: // カードドロー
                newEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
                if (currentEffect is DrawCardEffect existingDrawEffect)
                {
                    ((DrawCardEffect)newEffect).drawCount = existingDrawEffect.drawCount;
                }
                break;
            case 2: // ステータス変更
                newEffect = ScriptableObject.CreateInstance<StatModifierEffect>();
                if (currentEffect is StatModifierEffect existingStatEffect)
                {
                    var statEffect = (StatModifierEffect)newEffect;
                    statEffect.attackBonus = existingStatEffect.attackBonus;
                    statEffect.defenseBonus = existingStatEffect.defenseBonus;
                    statEffect.targetCategory = existingStatEffect.targetCategory;
                }
                break;
            case 3: // ダメージ
                newEffect = ScriptableObject.CreateInstance<DamageEffect>();
                if (currentEffect is DamageEffect existingDamageEffect)
                {
                    ((DamageEffect)newEffect).damageAmount = existingDamageEffect.damageAmount;
                    ((DamageEffect)newEffect).targetOpponent = existingDamageEffect.targetOpponent;
                }
                break;
            case 4: // 回復
                newEffect = ScriptableObject.CreateInstance<HealEffect>();
                if (currentEffect is HealEffect existingHealEffect)
                {
                    ((HealEffect)newEffect).healAmount = existingHealEffect.healAmount;
                    ((HealEffect)newEffect).targetSelf = existingHealEffect.targetSelf;
                }
                break;
        }
        
        // 新しい効果をアセットに追加
        if (newEffect != null)
        {
            AssetDatabase.AddObjectToAsset(newEffect, AssetDatabase.GetAssetPath(cardDatabase));
        }
        
        return newEffect;
    }

    // 効果の削除メソッド
    private void DeleteCardEffect(Card card, CardEffect effectToRemove)
    {
        if (card is CharacterCard characterCard)
        {
            characterCard.effects.Remove(effectToRemove);
        }
        else if (card is SpellCard spellCard)
        {
            spellCard.effects.Remove(effectToRemove);
        }
        else if (card is FieldCard fieldCard)
        {
            fieldCard.effects.Remove(effectToRemove);
        }
        
        // アセットから効果を削除
        AssetDatabase.RemoveObjectFromAsset(effectToRemove);
        
        // カードデータベースを更新
        EditorUtility.SetDirty(card);
        EditorUtility.SetDirty(cardDatabase);
        AssetDatabase.SaveAssets();
    }


    // 新しい効果の追加メニュー
    private void ShowEffectAddMenu()
    {
        // 現在のカードの効果リストを取得
        List<CardEffect> currentEffects = GetCurrentCardEffects();
        
        if (currentEffects == null) return;
        
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("カードドロー"), false, () => AddNewEffect(currentEffects, typeof(DrawCardEffect)));
        menu.AddItem(new GUIContent("ステータス変更"), false, () => AddNewEffect(currentEffects, typeof(StatModifierEffect)));
        menu.AddItem(new GUIContent("ダメージ"), false, () => AddNewEffect(currentEffects, typeof(DamageEffect)));
        menu.AddItem(new GUIContent("回復"), false, () => AddNewEffect(currentEffects, typeof(HealEffect)));
        
        menu.ShowAsContext();
    }

    // 新しい効果の追加処理
    private void AddNewEffect(List<CardEffect> currentEffects, Type effectType)
    {
        if (currentEffects == null) return;
        
        CardEffect newEffect = ScriptableObject.CreateInstance(effectType) as CardEffect;
        
        if (newEffect != null)
        {
            // デフォルト値の設定
            if (newEffect is DrawCardEffect drawEffect)
                drawEffect.drawCount = 1;
            else if (newEffect is StatModifierEffect statEffect)
            {
                statEffect.attackBonus = 0;
                statEffect.defenseBonus = 0;
            }
            else if (newEffect is DamageEffect damageEffect)
            {
                damageEffect.damageAmount = 500;
                damageEffect.targetOpponent = true;
            }
            else if (newEffect is HealEffect healEffect)
            {
                healEffect.healAmount = 500;
                healEffect.targetSelf = true;
            }
            
            // アセットに追加
            AssetDatabase.AddObjectToAsset(newEffect, AssetDatabase.GetAssetPath(cardDatabase));
            
            // 効果リストに追加
            currentEffects.Add(newEffect);
        }
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
        if (card.categories != null && card.categories.Count > 0)
        {
            EditorGUILayout.LabelField("現在のカテゴリー:");
            
            foreach (var category in card.categories)
            {
                EditorGUILayout.BeginHorizontal();
                if (category != null)
                {
                    GUI.backgroundColor = category.categoryColor;
                    EditorGUILayout.LabelField(category.categoryName, EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    EditorGUILayout.LabelField("不明なカテゴリー", EditorStyles.helpBox);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("カテゴリーなし");
        }
        
        // カテゴリー編集ボタン
        if (GUILayout.Button("カテゴリーを編集"))
        {
            // ここでカテゴリー選択UIを表示
            // 実装は省略
        }
    }
    
    // スペルカード編集フィールドの描画
    private void DrawSpellCardEditFields(SpellCard card)
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("スペルカード情報", EditorStyles.boldLabel);
        
        // SpellCardのプロパティがない場合は暫定対応
        /*
        card.spellType = (SpellType)EditorGUILayout.EnumPopup("スペルタイプ", card.spellType);
        
        card.effectValue = EditorGUILayout.IntSlider("効果値", card.effectValue, 1, 10);
        */
        
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
        
        if (card.affectedCategories != null && card.affectedCategories.Count > 0)
        {
            foreach (var category in card.affectedCategories)
            {
                if (category != null)
                {
                    GUI.backgroundColor = category.categoryColor;
                    EditorGUILayout.LabelField(category.categoryName, EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("すべてのカテゴリー");
        }
        
        EditorGUILayout.LabelField("影響する属性");
        if (card.affectedElements != null && card.affectedElements.Count > 0)
        {
            string elements = string.Join(", ", card.affectedElements);
            EditorGUILayout.LabelField(elements);
        }
        else
        {
            EditorGUILayout.LabelField("すべての属性");
        }
    }
    
    // 選択されたカードの更新処理
    private void UpdateSelectedCard()
    {
        if (selectedCard != null)
        {
            EditorUtility.SetDirty(selectedCard);
            EditorUtility.SetDirty(cardDatabase);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("カード更新", $"カード「{selectedCard.cardName}」を更新しました。", "OK");
        }
    }
    
    // 全体の削除メソッドも修正
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
            foreach (var effect in characterCard.effects)
            {
                if (effect != null)
                    AssetDatabase.RemoveObjectFromAsset(effect);
            }
        }
        else if (card is SpellCard spellCard)
        {
            if (spellCard.effects != null)
            {
                foreach (var effect in spellCard.effects)
                {
                    if (effect != null)
                        AssetDatabase.RemoveObjectFromAsset(effect);
                }
            }
        }
        else if (card is FieldCard fieldCard)
        {
            foreach (var effect in fieldCard.effects)
            {
                if (effect != null)
                    AssetDatabase.RemoveObjectFromAsset(effect);
            }
        }
        
        // アセットから元のカードも削除
        AssetDatabase.RemoveObjectFromAsset(card);
        
        // UI更新
        selectedCard = null;
        
        // データベースを更新
        EditorUtility.SetDirty(cardDatabase);
        AssetDatabase.SaveAssets();

        // リストを再読み込み
        UpdateCardList();
    }

    // カードリストを更新するメソッドを追加
    private void UpdateCardList()
    {
        // 必要に応じて、カードリストを再読み込みする処理を追加
        // 例: 現在の検索クエリで再検索するなど
    }
    
    // ターゲットIDの文字列をパース
    private List<int> ParseTargetIdsString(string idsString)
    {
        List<int> result = new List<int>();
        
        if (!string.IsNullOrEmpty(idsString))
        {
            string[] idStrings = idsString.Split(',');
            
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
    #endregion
    
    #region 一括インポートタブ
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
    #endregion

    #region アートワークブラウザ
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

    // アートワーク選択UIを表示（改良版）
    private void ShowArtworkSelector(bool isEditing = false)
    {
        isEditingCardArtwork = isEditing;
        showArtworkBrowser = true;
        
        // 初回表示時にアートワークリストを更新
        if (filteredArtworks.Count == 0)
        {
            InitializeArtworkBrowser();
        }
    }

    // アートワークブラウザの描画メソッドを改良
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
                        GUI.DrawTextureWithTexCoords(
                            previewRect,
                            artwork.texture,
                            new Rect(
                                artwork.rect.x / artwork.texture.width,
                                artwork.rect.y / artwork.texture.height,
                                artwork.rect.width / artwork.texture.width,
                                artwork.rect.height / artwork.texture.height
                            )
                        );
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
            // ここでCardImageImporterを呼び出す
            // 直接呼び出しではなく、メニュー経由で呼び出すように修正
            EditorApplication.ExecuteMenuItem("Tools/Card Game/Card Image Importer");
        }
        
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endregion
}
#endif