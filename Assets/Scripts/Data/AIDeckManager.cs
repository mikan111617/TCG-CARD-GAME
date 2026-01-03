using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

#if UNITY_EDITOR
/// <summary>
/// AIプレイヤー用のデッキを管理するエディタ拡張ツール
/// </summary>
public class AIDeckManager : EditorWindow
{
    // AIプレイヤー設定
    [System.Serializable]
    public class AIPlayerProfile
    {
        public string name;                // AI名
        public AIDifficulty difficulty;    // 難易度
        public int deckId;                 // 使用するデッキID
        public string description;         // AI説明
        public string iconPath;            // アイコン画像のパス
    }
    
    // AIプレイヤー設定のリスト
    [System.Serializable]
    public class AIPlayerProfileList
    {
        public List<AIPlayerProfile> profiles = new List<AIPlayerProfile>();
    }
    
    // UI設定
    private Vector2 scrollPosition;
    private AIPlayerProfileList profileList = new AIPlayerProfileList();
    private string profilesFilePath;
    
    // 選択中のAIプロファイル
    private int selectedProfileIndex = -1;
    
    // 新規AIプロファイル用のデータ
    private string newProfileName = "";
    private AIDifficulty newProfileDifficulty = AIDifficulty.Normal;
    private int newProfileDeckId = -1;
    private string newProfileDescription = "";
    private Texture2D newProfileIcon;
    
    // デッキリスト
    private List<DeckData> availableDecks = new List<DeckData>();
    
    // メニューにツールを追加
    [MenuItem("Tools/Card Game/AI Deck Manager")]
    public static void ShowWindow()
    {
        AIDeckManager window = GetWindow<AIDeckManager>("AI デッキマネージャー");
        window.minSize = new Vector2(500, 650);
        window.Show();
    }
    
    // ウィンドウ初期化
    private void OnEnable()
    {
        // プロファイルデータのパスを設定
        profilesFilePath = Application.dataPath + "/Resources/AIPlayerProfiles.json";
        
        // プロファイルリストのロード
        LoadProfiles();
        
        // デッキリストのロード
        LoadAvailableDecks();
    }
    
    // プロファイルデータをロード
    private void LoadProfiles()
    {
        if (File.Exists(profilesFilePath))
        {
            string json = File.ReadAllText(profilesFilePath);
            try
            {
                profileList = JsonUtility.FromJson<AIPlayerProfileList>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("AIプロファイルのロードに失敗しました: " + e.Message);
                profileList = new AIPlayerProfileList();
            }
        }
        else
        {
            // ファイルがなければ新規作成
            profileList = new AIPlayerProfileList();
            SaveProfiles();
        }
    }
    
    // プロファイルデータを保存
    private void SaveProfiles()
    {
        string json = JsonUtility.ToJson(profileList, true);
        
        // Resourcesフォルダがなければ作成
        string resourcesFolder = Application.dataPath + "/Resources";
        if (!Directory.Exists(resourcesFolder))
        {
            Directory.CreateDirectory(resourcesFolder);
        }
        
        File.WriteAllText(profilesFilePath, json);
        AssetDatabase.Refresh();
    }
    
    // 利用可能なデッキをロード
    private void LoadAvailableDecks()
    {
        availableDecks.Clear();
        
        // デッキデータマネージャーがあれば利用
        DeckDataManager deckManager = UnityEngine.Object.FindFirstObjectByType<DeckDataManager>();
        if (deckManager != null)
        {
            availableDecks = deckManager.GetAllDecks();
        }
        else
        {
            // エディタ上でDeckDataManagerが見つからない場合
            // Resourcesフォルダからデッキデータをロード
            TextAsset[] deckAssets = Resources.LoadAll<TextAsset>("Decks");
            foreach (TextAsset deckAsset in deckAssets)
            {
                try
                {
                    DeckData deck = DeckData.FromJson(deckAsset.text);
                    availableDecks.Add(deck);
                }
                catch
                {
                    Debug.LogWarning($"デッキファイル {deckAsset.name} の読み込みに失敗しました。");
                }
            }
        }
    }
    
    // GUI描画
    private void OnGUI()
    {
        // タイトル
        GUILayout.Space(10);
        GUILayout.Label("AI デッキマネージャー", EditorStyles.boldLabel);
        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        
        // 左側: AIプロファイルリスト
        EditorGUILayout.BeginVertical(GUILayout.Width(200));
        DrawProfileList();
        EditorGUILayout.EndVertical();
        
        // 右側: 詳細設定/新規作成
        EditorGUILayout.BeginVertical();
        if (selectedProfileIndex >= 0 && selectedProfileIndex < profileList.profiles.Count)
        {
            DrawProfileDetails(profileList.profiles[selectedProfileIndex]);
        }
        else
        {
            DrawNewProfileForm();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndHorizontal();
    }
    
    // AIプロファイルリスト描画
    private void DrawProfileList()
    {
        EditorGUILayout.LabelField("AIプロファイル", EditorStyles.boldLabel);
        
        // スクロールビュー開始
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(500));
        
        // AIプロファイルがない場合
        if (profileList.profiles.Count == 0)
        {
            EditorGUILayout.HelpBox("AIプロファイルがありません。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < profileList.profiles.Count; i++)
            {
                AIPlayerProfile profile = profileList.profiles[i];
                
                EditorGUILayout.BeginHorizontal("box");
                
                // プロファイル選択ボタン（選択中は色を変える）
                GUI.backgroundColor = (selectedProfileIndex == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(profile.name, EditorStyles.miniButtonLeft))
                {
                    selectedProfileIndex = i;
                }
                GUI.backgroundColor = Color.white;
                
                // 削除ボタン
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("AIプロファイル削除",
                        $"AIプロファイル「{profile.name}」を削除しますか？", "削除", "キャンセル"))
                    {
                        DeleteProfile(i);
                    }
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        // 新規プロファイル作成ボタン
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("新規AIプロファイル", GUILayout.Height(30)))
        {
            selectedProfileIndex = -1; // 選択解除して新規フォームを表示
            ResetNewProfileForm();
        }
        GUI.backgroundColor = Color.white;
    }
    
    // 選択されたAIプロファイルの詳細表示と編集
    private void DrawProfileDetails(AIPlayerProfile profile)
    {
        EditorGUILayout.LabelField($"AIプロファイル編集: {profile.name}", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // プロファイル基本情報
        profile.name = EditorGUILayout.TextField("AI名", profile.name);
        profile.difficulty = (AIDifficulty)EditorGUILayout.EnumPopup("難易度", profile.difficulty);
        
        // デッキ選択
        DrawDeckSelector(profile);
        
        EditorGUILayout.LabelField("説明");
        profile.description = EditorGUILayout.TextArea(profile.description, GUILayout.Height(60));
        
        // アイコン選択
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("アイコン", GUILayout.Width(120));
        
        // 現在のアイコンを表示
        Texture2D icon = null;
        if (!string.IsNullOrEmpty(profile.iconPath))
        {
            icon = AssetDatabase.LoadAssetAtPath<Texture2D>(profile.iconPath);
        }
        
        Texture2D newIcon = (Texture2D)EditorGUILayout.ObjectField(icon, typeof(Texture2D), false);
        
        // アイコンが変更された場合
        if (newIcon != icon)
        {
            profile.iconPath = AssetDatabase.GetAssetPath(newIcon);
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // 更新ボタン
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("プロファイルを更新", GUILayout.Height(30)))
        {
            SaveProfiles();
            EditorUtility.DisplayDialog("更新完了", $"AIプロファイル「{profile.name}」を更新しました。", "OK");
        }
        GUI.backgroundColor = Color.white;
    }
    
    // 新規AIプロファイル作成フォーム
    private void DrawNewProfileForm()
    {
        EditorGUILayout.LabelField("新規AIプロファイル作成", EditorStyles.boldLabel);
        
        GUILayout.Space(10);
        
        // プロファイル基本情報
        newProfileName = EditorGUILayout.TextField("AI名", newProfileName);
        newProfileDifficulty = (AIDifficulty)EditorGUILayout.EnumPopup("難易度", newProfileDifficulty);
        
        // デッキ選択
        string[] deckNames = new string[availableDecks.Count + 1];
        deckNames[0] = "-- デッキを選択 --";
        
        for (int i = 0; i < availableDecks.Count; i++)
        {
            deckNames[i + 1] = $"{availableDecks[i].deckName} (ID: {availableDecks[i].deckId})";
        }
        
        int selectedDeckIndex = 0;
        for (int i = 0; i < availableDecks.Count; i++)
        {
            if (availableDecks[i].deckId == newProfileDeckId)
            {
                selectedDeckIndex = i + 1;
                break;
            }
        }
        
        selectedDeckIndex = EditorGUILayout.Popup("使用デッキ", selectedDeckIndex, deckNames);
        
        if (selectedDeckIndex > 0)
        {
            newProfileDeckId = availableDecks[selectedDeckIndex - 1].deckId;
        }
        else
        {
            newProfileDeckId = -1;
        }
        
        EditorGUILayout.LabelField("説明");
        newProfileDescription = EditorGUILayout.TextArea(newProfileDescription, GUILayout.Height(60));
        
        // アイコン選択
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("アイコン", GUILayout.Width(120));
        newProfileIcon = (Texture2D)EditorGUILayout.ObjectField(newProfileIcon, typeof(Texture2D), false);
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // プロファイル作成ボタン
        GUI.backgroundColor = Color.green;
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newProfileName) || newProfileDeckId < 0);
        if (GUILayout.Button("プロファイルを作成", GUILayout.Height(30)))
        {
            CreateNewProfile();
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;
    }
    
    // デッキセレクタ描画
    private void DrawDeckSelector(AIPlayerProfile profile)
    {
        if (availableDecks.Count == 0)
        {
            EditorGUILayout.HelpBox("利用可能なデッキがありません。先にデッキを作成してください。", MessageType.Warning);
            return;
        }
        
        string[] deckNames = new string[availableDecks.Count];
        int selectedIndex = 0;
        
        for (int i = 0; i < availableDecks.Count; i++)
        {
            deckNames[i] = $"{availableDecks[i].deckName} (ID: {availableDecks[i].deckId})";
            
            if (availableDecks[i].deckId == profile.deckId)
            {
                selectedIndex = i;
            }
        }
        
        selectedIndex = EditorGUILayout.Popup("使用デッキ", selectedIndex, deckNames);
        profile.deckId = availableDecks[selectedIndex].deckId;
    }
    
    // 新規プロファイル作成
    private void CreateNewProfile()
    {
        AIPlayerProfile newProfile = new AIPlayerProfile();
        newProfile.name = newProfileName;
        newProfile.difficulty = newProfileDifficulty;
        newProfile.deckId = newProfileDeckId;
        newProfile.description = newProfileDescription;
        
        if (newProfileIcon != null)
        {
            newProfile.iconPath = AssetDatabase.GetAssetPath(newProfileIcon);
        }
        
        profileList.profiles.Add(newProfile);
        SaveProfiles();
        
        // 作成したプロファイルを選択
        selectedProfileIndex = profileList.profiles.Count - 1;
        
        EditorUtility.DisplayDialog("作成完了", $"AIプロファイル「{newProfile.name}」を作成しました。", "OK");
        
        // 入力フォームをリセット
        ResetNewProfileForm();
    }
    
    // AIプロファイル削除
    private void DeleteProfile(int index)
    {
        profileList.profiles.RemoveAt(index);
        SaveProfiles();
        
        // 選択中のプロファイルが削除された場合
        if (selectedProfileIndex == index)
        {
            selectedProfileIndex = -1;
        }
        else if (selectedProfileIndex > index)
        {
            selectedProfileIndex--;
        }
    }
    
    // 新規プロファイルフォームのリセット
    private void ResetNewProfileForm()
    {
        newProfileName = "";
        newProfileDifficulty = AIDifficulty.Normal;
        newProfileDeckId = -1;
        newProfileDescription = "";
        newProfileIcon = null;
    }
}
#endif