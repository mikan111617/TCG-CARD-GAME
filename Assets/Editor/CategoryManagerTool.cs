using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System; 
#if UNITY_EDITOR
/// <summary>
/// カードカテゴリーを管理するエディタ拡張ツール
/// </summary>
public class CategoryManagerTool : EditorWindow
{
    // カテゴリーデータベース
    private CategoryDatabase categoryDatabase;
    
    // UI設定
    private Vector2 scrollPosition;
    private string searchQuery = "";
    
    // 選択中のカテゴリー
    private CardCategory selectedCategory;
    private int selectedCategoryIndex = -1;
    
    // 新規カテゴリーデータ
    private string newCategoryName = "";
    private string newCategoryDescription = "";
    private Color newCategoryColor = Color.white;
    private Texture2D newCategoryIcon;
    
    // カテゴリーエディタでの表示モード
    private enum DisplayMode
    {
        List,
        Create,
        Edit
    }
    
    private DisplayMode currentMode = DisplayMode.List;
    
    // メニューにツールを追加
    [MenuItem("Tools/Card Game/Category Manager")]
    public static void ShowWindow()
    {
        CategoryManagerTool window = GetWindow<CategoryManagerTool>("カテゴリー管理");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }
    
    // ウィンドウ初期化
    private void OnEnable()
    {
        // カテゴリーデータベースをロード
        categoryDatabase = Resources.Load<CategoryDatabase>("CategoryDatabase");
        if (categoryDatabase == null)
        {
            Debug.LogWarning("CategoryDatabaseが見つかりません。新規作成します。");
            categoryDatabase = CreateCategoryDatabase();
        }
        
        // デフォルトカテゴリーが存在しない場合は作成
        if (categoryDatabase.GetAllCategories().Count == 0)
        {
            CreateDefaultCategories();
        }
    }
    
    // カテゴリーデータベースがない場合に新規作成
    private CategoryDatabase CreateCategoryDatabase()
    {
        // カテゴリーデータベースのScriptableObjectを作成
        CategoryDatabase newDatabase = ScriptableObject.CreateInstance<CategoryDatabase>();
        
        // Resources フォルダがなければ作成
        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }
        
        // アセットとして保存
        AssetDatabase.CreateAsset(newDatabase, "Assets/Resources/CategoryDatabase.asset");
        AssetDatabase.SaveAssets();
        return newDatabase;
    }
    
    // デフォルトカテゴリーの作成
    private void CreateDefaultCategories()
    {        
        // 変更を保存
        EditorUtility.SetDirty(categoryDatabase);
        AssetDatabase.SaveAssets();
    }
    
    // 新しいカテゴリーを作成してデータベースに追加
    private void CreateAndAddCategory(string name, string description, Color color)
    {
        CardCategory category = ScriptableObject.CreateInstance<CardCategory>();
        category.categoryName = name;
        category.description = description;
        category.categoryColor = color;
        
        // AssetDatabaseに追加
        AssetDatabase.AddObjectToAsset(category, AssetDatabase.GetAssetPath(categoryDatabase));
        
        // データベースに追加
        categoryDatabase.AddCategory(category);
    }
    
    // GUI描画
    private void OnGUI()
    {
        // タイトル
        GUILayout.Space(10);
        EditorGUILayout.LabelField("カードカテゴリー管理", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        // エラーチェック
        if (categoryDatabase == null)
        {
            EditorGUILayout.HelpBox("CategoryDatabaseの読み込みに失敗しました。", MessageType.Error);
            if (GUILayout.Button("データベースを作成"))
            {
                categoryDatabase = CreateCategoryDatabase();
            }
            return;
        }
        
        // 現在のモードに応じた描画
        switch (currentMode)
        {
            case DisplayMode.List:
                DrawCategoryListMode();
                break;
                
            case DisplayMode.Create:
                DrawCategoryCreateMode();
                break;
                
            case DisplayMode.Edit:
                if (selectedCategory != null)
                {
                    DrawCategoryEditMode();
                }
                else
                {
                    currentMode = DisplayMode.List;
                }
                break;
        }
    }
    
    // カテゴリー一覧モードの描画
    private void DrawCategoryListMode()
    {
        // 検索バー
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        searchQuery = EditorGUILayout.TextField("検索", searchQuery);
        if (GUILayout.Button("×", GUILayout.Width(25)))
        {
            searchQuery = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        
        // 新規カテゴリー作成ボタン
        GUILayout.Space(10);
        if (GUILayout.Button("新規カテゴリー作成", GUILayout.Height(30)))
        {
            currentMode = DisplayMode.Create;
            ResetNewCategoryForm();
        }
        
        GUILayout.Space(10);
        
        // カテゴリー一覧
        EditorGUILayout.LabelField("カテゴリー一覧", EditorStyles.boldLabel);
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        // 検索フィルタリング
        if (!string.IsNullOrEmpty(searchQuery))
        {
            allCategories = allCategories.FindAll(c => 
                c.categoryName.ToLower().Contains(searchQuery.ToLower()) || 
                c.description.ToLower().Contains(searchQuery.ToLower())
            );
        }
        
        // カテゴリーがない場合
        if (allCategories.Count == 0)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                EditorGUILayout.HelpBox("カテゴリーがありません。「新規カテゴリー作成」ボタンからカテゴリーを作成してください。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("検索条件に一致するカテゴリーがありません。", MessageType.Info);
            }
            return;
        }
        
        // スクロールエリアでカテゴリー一覧を表示
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < allCategories.Count; i++)
        {
            CardCategory category = allCategories[i];
            
            EditorGUILayout.BeginVertical("box");
            
            // カテゴリー名と色をヘッダーとして表示
            EditorGUILayout.BeginHorizontal();
            
            // カラーサンプル
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(20)), category.categoryColor);
            
            // カテゴリー名
            EditorGUILayout.LabelField(category.categoryName, EditorStyles.boldLabel);
            
            // 編集ボタン
            if (GUILayout.Button("編集", GUILayout.Width(60)))
            {
                selectedCategory = category;
                selectedCategoryIndex = i;
                currentMode = DisplayMode.Edit;
            }
            
            // 削除ボタン
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("削除", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("カテゴリー削除", 
                    $"カテゴリー「{category.categoryName}」を削除しますか？\n\n" +
                    "注意: このカテゴリーを使用しているカードは影響を受けます。", 
                    "削除", "キャンセル"))
                {
                    DeleteCategory(category);
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            // カテゴリー説明
            EditorGUILayout.LabelField(category.description, EditorStyles.wordWrappedLabel);
            
            // カテゴリーID（開発者向け情報）
            EditorGUILayout.LabelField($"ID: {category.categoryId}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    // カテゴリー作成モードの描画
    private void DrawCategoryCreateMode()
    {
        EditorGUILayout.LabelField("新規カテゴリー作成", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 入力フォーム
        newCategoryName = EditorGUILayout.TextField("カテゴリー名", newCategoryName);
        
        EditorGUILayout.LabelField("説明");
        newCategoryDescription = EditorGUILayout.TextArea(newCategoryDescription, GUILayout.Height(60));
        
        newCategoryColor = EditorGUILayout.ColorField("カテゴリー色", newCategoryColor);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("アイコン");
        newCategoryIcon = (Texture2D)EditorGUILayout.ObjectField(newCategoryIcon, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // ボタン類
        EditorGUILayout.BeginHorizontal();
        
        // キャンセルボタン
        if (GUILayout.Button("キャンセル", GUILayout.Height(30)))
        {
            currentMode = DisplayMode.List;
        }
        
        // 作成ボタン
        GUI.backgroundColor = Color.green;
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newCategoryName));
        if (GUILayout.Button("カテゴリーを作成", GUILayout.Height(30)))
        {
            CreateNewCategory();
            currentMode = DisplayMode.List;
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    // カテゴリー編集モードの描画
    private void DrawCategoryEditMode()
    {
        EditorGUILayout.LabelField($"カテゴリー編集: {selectedCategory.categoryName}", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 入力フォーム
        selectedCategory.categoryName = EditorGUILayout.TextField("カテゴリー名", selectedCategory.categoryName);
        
        EditorGUILayout.LabelField("説明");
        selectedCategory.description = EditorGUILayout.TextArea(selectedCategory.description, GUILayout.Height(60));
        
        selectedCategory.categoryColor = EditorGUILayout.ColorField("カテゴリー色", selectedCategory.categoryColor);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("アイコン");
        selectedCategory.categoryIcon = (Sprite)EditorGUILayout.ObjectField(
            selectedCategory.categoryIcon, 
            typeof(Sprite), 
            false
        );
        EditorGUILayout.EndHorizontal();
        
        // カテゴリーID（情報表示のみ）
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("カテゴリーID", selectedCategory.categoryId);
        EditorGUI.EndDisabledGroup();
        
        GUILayout.Space(20);
        
        // ボタン類
        EditorGUILayout.BeginHorizontal();
        
        // キャンセルボタン
        if (GUILayout.Button("一覧に戻る", GUILayout.Height(30)))
        {
            currentMode = DisplayMode.List;
        }
        
        // 更新ボタン
        GUI.backgroundColor = Color.green;
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedCategory.categoryName));
        if (GUILayout.Button("変更を保存", GUILayout.Height(30)))
        {
            // 変更を保存
            EditorUtility.SetDirty(selectedCategory);
            EditorUtility.SetDirty(categoryDatabase);
            AssetDatabase.SaveAssets();
            
            EditorUtility.DisplayDialog("カテゴリー更新", $"カテゴリー「{selectedCategory.categoryName}」を更新しました。", "OK");
            
            currentMode = DisplayMode.List;
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
    }
    
    // 新規カテゴリーの作成
    private void CreateNewCategory()
    {
        // 入力チェック
        if (string.IsNullOrEmpty(newCategoryName))
        {
            EditorUtility.DisplayDialog("入力エラー", "カテゴリー名を入力してください。", "OK");
            return;
        }
        
        // 既存カテゴリーとの名前重複チェック
        if (categoryDatabase.GetCategoryByName(newCategoryName) != null)
        {
            EditorUtility.DisplayDialog("エラー", $"カテゴリー名「{newCategoryName}」は既に存在します。", "OK");
            return;
        }
        
        // 新規カテゴリーを作成
        CardCategory newCategory = ScriptableObject.CreateInstance<CardCategory>();
        newCategory.categoryName = newCategoryName;
        newCategory.description = newCategoryDescription;
        newCategory.categoryColor = newCategoryColor;
        
        // ここで重要: カテゴリーIDを割り当てる
        newCategory.categoryId = GenerateNewCategoryId();
        Debug.Log($"新規カテゴリー作成: {newCategoryName}, ID: {newCategory.categoryId}");
        
        if (newCategoryIcon != null)
        {
            // テクスチャからスプライトを作成（簡易版）
            Sprite iconSprite = Sprite.Create(
                newCategoryIcon,
                new Rect(0, 0, newCategoryIcon.width, newCategoryIcon.height),
                Vector2.one * 0.5f
            );
            newCategory.categoryIcon = iconSprite;
        }
        
        // AssetDatabaseに追加
        AssetDatabase.AddObjectToAsset(newCategory, AssetDatabase.GetAssetPath(categoryDatabase));
        
        // データベースに追加
        categoryDatabase.AddCategory(newCategory);
        
        // 変更を保存
        EditorUtility.SetDirty(newCategory);
        EditorUtility.SetDirty(categoryDatabase);
        AssetDatabase.SaveAssets();
        
        EditorUtility.DisplayDialog("カテゴリー作成", $"カテゴリー「{newCategoryName}」を作成しました。\nID: {newCategory.categoryId}", "OK");
        
        // フォームをリセット
        ResetNewCategoryForm();
    }

    // カテゴリーIDを生成するための新しいメソッド
    private string GenerateNewCategoryId()
    {
        // すべてのカテゴリーを取得
        List<CardCategory> allCategories = categoryDatabase.GetAllCategories();
        
        // 現在の時間を基にしたプレフィックス
        string prefix = "CAT_" + DateTime.Now.ToString("yyMMdd");
        
        // 連番を探す
        int maxNumber = 0;
        
        foreach (var category in allCategories)
        {
            if (category.categoryId != null && category.categoryId.StartsWith(prefix))
            {
                // IDから数値部分を抽出
                string numStr = category.categoryId.Substring(prefix.Length);
                if (int.TryParse(numStr, out int num))
                {
                    maxNumber = Mathf.Max(maxNumber, num);
                }
            }
        }
        
        // 新しいIDを生成（プレフィックス + 連番 + 1）
        return prefix + (maxNumber + 1).ToString("D3");
    }
    
    // カテゴリーの削除
    private void DeleteCategory(CardCategory category)
    {
        // データベースから削除
        categoryDatabase.RemoveCategory(category);
        
        // AssetDatabaseから削除
        AssetDatabase.RemoveObjectFromAsset(category);
        
        // 変更を保存
        EditorUtility.SetDirty(categoryDatabase);
        AssetDatabase.SaveAssets();
        
        // 選択中のカテゴリーだった場合はクリア
        if (selectedCategory == category)
        {
            selectedCategory = null;
            selectedCategoryIndex = -1;
        }
    }
    
    // 新規カテゴリーフォームのリセット
    private void ResetNewCategoryForm()
    {
        newCategoryName = "";
        newCategoryDescription = "";
        newCategoryColor = Color.white;
        newCategoryIcon = null;
    }
}
#endif