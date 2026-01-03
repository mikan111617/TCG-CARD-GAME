#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace CardGameUI
{
    /// <summary>
    /// JSON UI ビルダーウィンドウ - JSONファイルをドラッグ&ドロップしてUIを即座に構築
    /// </summary>
    public class JSONUIBuilderWindow : EditorWindow
    {
        // ウィンドウの状態
        private TextAsset jsonFile;
        private Object draggedObject;
        private string jsonContent = "";
        private Vector2 jsonScrollPosition;
        private bool showJsonPreview = true;
        private string statusMessage = "JSON ファイルをここにドラッグ＆ドロップしてください";
        private MessageType statusMessageType = MessageType.Info;
        private UIBuilder uiBuilder;
        private SerializedObject serializedBuilder;
        private bool showHelpSection = false;
        private bool showHistorySection = false;
        private bool showSettingsSection = true;
        private List<string> recentFiles = new List<string>();
        private int maxRecentFiles = 5;
        private GUIStyle headerStyle;
        private Color headerColor = new Color(0.3f, 0.5f, 0.85f);
        private Texture2D buildIcon;
        private Texture2D clearIcon;
        
        // UI設定
        private bool clearBeforeBuild = true;
        private bool autoSelectCanvas = true;
        
        // ウィンドウを開く
        [MenuItem("Window/UI/JSON UI Builder")]
        public static void ShowWindow()
        {
            GetWindow<JSONUIBuilderWindow>("JSON UI Builder");
        }

        private void OnEnable()
        {
            // リソースの読み込み
            LoadResources();
            
            // UIBuilderの取得または作成
            FindOrCreateUIBuilder();
            
            // 最近使用したファイルの読み込み
            LoadRecentFiles();
        }
        
        /// <summary>
        /// リソースの読み込み
        /// </summary>
        private void LoadResources()
        {
            // アイコンの読み込み
            buildIcon = EditorGUIUtility.FindTexture("d_BuildSettings.Editor");
            clearIcon = EditorGUIUtility.FindTexture("d_TreeEditor.Trash");
        }

        /// <summary>
        /// UIBuilderの取得または作成
        /// </summary>
        private void FindOrCreateUIBuilder()
        {
            // 既存のUIBuilderを検索
            uiBuilder = UnityEngine.Object.FindFirstObjectByType<UIBuilder>();
            
            // 見つからなければ新規作成
            if (uiBuilder == null)
            {
                GameObject uiBuilderObj = new GameObject("UI Builder");
                uiBuilder = uiBuilderObj.AddComponent<UIBuilder>();
                Undo.RegisterCreatedObjectUndo(uiBuilderObj, "Create UI Builder");
            }
            
            // SerializedObject作成
            serializedBuilder = new SerializedObject(uiBuilder);
        }

        /// <summary>
        /// 最近使用したファイルの読み込み
        /// </summary>
        private void LoadRecentFiles()
        {
            recentFiles.Clear();
            
            // EditorPrefsから過去のファイルを読み込む
            int count = EditorPrefs.GetInt("JSONUIBuilder_RecentFilesCount", 0);
            for (int i = 0; i < count; i++)
            {
                string path = EditorPrefs.GetString($"JSONUIBuilder_RecentFile_{i}", "");
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    recentFiles.Add(path);
                }
            }
        }

        /// <summary>
        /// 最近使用したファイルを保存
        /// </summary>
        private void SaveRecentFiles()
        {
            // 最大件数に制限
            while (recentFiles.Count > maxRecentFiles)
            {
                recentFiles.RemoveAt(recentFiles.Count - 1);
            }
            
            // EditorPrefsに保存
            EditorPrefs.SetInt("JSONUIBuilder_RecentFilesCount", recentFiles.Count);
            for (int i = 0; i < recentFiles.Count; i++)
            {
                EditorPrefs.SetString($"JSONUIBuilder_RecentFile_{i}", recentFiles[i]);
            }
        }

        /// <summary>
        /// 最近使用したファイルに追加
        /// </summary>
        private void AddToRecentFiles(string filePath)
        {
            // 既存エントリを削除（重複防止）
            recentFiles.Remove(filePath);
            
            // 先頭に追加
            recentFiles.Insert(0, filePath);
            
            // 保存
            SaveRecentFiles();
        }

        /// <summary>
        /// GUI描画
        /// </summary>
        private void OnGUI()
        {
            // スタイル初期化
            InitStyles();
            
            // ヘッダー
            DrawHeader();
            
            // メインエリア
            DrawMainArea();
            
            // フッター
            DrawFooter();
            
            // ドラッグ＆ドロップ処理
            HandleDragAndDrop();
        }

        /// <summary>
        /// スタイルの初期化
        /// </summary>
        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.fontSize = 14;
                headerStyle.normal.textColor = Color.white;
                headerStyle.padding = new RectOffset(10, 10, 8, 8);
                headerStyle.margin = new RectOffset(0, 0, 0, 10);
                headerStyle.alignment = TextAnchor.MiddleLeft;
            }
        }

        /// <summary>
        /// ヘッダーの描画
        /// </summary>
        private void DrawHeader()
        {
            // ヘッダー背景
            var headerRect = EditorGUILayout.GetControlRect(false, 40);
            EditorGUI.DrawRect(headerRect, headerColor);
            
            // ヘッダーテキスト
            EditorGUI.LabelField(headerRect, "JSON UI Builder", headerStyle);
            
            EditorGUILayout.Space(5);
        }

        /// <summary>
        /// メインエリアの描画
        /// </summary>
        private void DrawMainArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // JSONファイル選択エリア
            DrawFileSelectionArea();
            
            EditorGUILayout.Space(10);
            
            // ビルドエリア
            DrawBuildArea();
            
            // 設定セクション
            DrawSettingsSection();
            
            // 最近使用したファイルセクション
            DrawRecentFilesSection();
            
            // ヘルプセクション
            DrawHelpSection();
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// ファイル選択エリアの描画
        /// </summary>
        private void DrawFileSelectionArea()
        {
            EditorGUILayout.LabelField("JSON ファイル", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // JSONファイルフィールド
            EditorGUI.BeginChangeCheck();
            jsonFile = EditorGUILayout.ObjectField(jsonFile, typeof(TextAsset), false) as TextAsset;
            if (EditorGUI.EndChangeCheck() && jsonFile != null)
            {
                LoadJsonFile(jsonFile);
            }
            
            // ファイル選択ボタン
            if (GUILayout.Button("ファイル選択", GUILayout.Width(100)))
            {
                string path = EditorUtility.OpenFilePanel("JSONファイルを選択", Application.dataPath, "json,txt");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadJsonFileFromPath(path);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // JSONプレビュー
            DrawJsonPreview();
        }

        /// <summary>
        /// ビルドエリアの描画
        /// </summary>
        private void DrawBuildArea()
        {
            EditorGUILayout.LabelField("ビルド操作", EditorStyles.boldLabel);
            
            // ビルドボタン
            EditorGUILayout.BeginHorizontal();
            
            // UIビルドボタン
            GUI.enabled = !string.IsNullOrEmpty(jsonContent);
            if (GUILayout.Button(new GUIContent(" UIをビルド", buildIcon), GUILayout.Height(40)))
            {
                BuildUI();
            }
            
            // UIクリアボタン
            if (GUILayout.Button(new GUIContent(" UIをクリア", clearIcon), GUILayout.Height(40)))
            {
                ClearUI();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            // 状態メッセージ
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusMessageType);
            }
        }

        /// <summary>
        /// 設定セクションの描画
        /// </summary>
        private void DrawSettingsSection()
        {
            showSettingsSection = EditorGUILayout.Foldout(showSettingsSection, "設定", true);
            if (showSettingsSection)
            {
                EditorGUI.indentLevel++;
                
                clearBeforeBuild = EditorGUILayout.Toggle("ビルド前にUIをクリア", clearBeforeBuild);
                autoSelectCanvas = EditorGUILayout.Toggle("Canvasを自動選択", autoSelectCanvas);
                
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// 最近使用したファイルセクションの描画
        /// </summary>
        private void DrawRecentFilesSection()
        {
            showHistorySection = EditorGUILayout.Foldout(showHistorySection, $"最近使用したファイル ({recentFiles.Count})", true);
            if (showHistorySection && recentFiles.Count > 0)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < recentFiles.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // ファイル名
                    string fileName = Path.GetFileName(recentFiles[i]);
                    if (GUILayout.Button(fileName, EditorStyles.label))
                    {
                        LoadJsonFileFromPath(recentFiles[i]);
                    }
                    
                    // 削除ボタン
                    if (GUILayout.Button("×", GUILayout.Width(20)))
                    {
                        recentFiles.RemoveAt(i);
                        SaveRecentFiles();
                        GUIUtility.ExitGUI();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }
        }

        /// <summary>
        /// ヘルプセクションの描画
        /// </summary>
        private void DrawHelpSection()
        {
            showHelpSection = EditorGUILayout.Foldout(showHelpSection, "ヘルプ", true);
            if (showHelpSection)
            {
                EditorGUILayout.HelpBox(
                    "使用方法:\n" +
                    "1. JSONファイルを選択または上のエリアにドラッグ&ドロップ\n" +
                    "2. 「UIをビルド」ボタンをクリックしてUIを生成\n" +
                    "3. 必要に応じて「UIをクリア」でリセット\n\n" +
                    "注意: JSONファイルの形式はUIBuilderの定義に従ってください", 
                    MessageType.Info);
            }
        }

        /// <summary>
        /// JSONプレビューの描画
        /// </summary>
        private void DrawJsonPreview()
        {
            showJsonPreview = EditorGUILayout.Foldout(showJsonPreview, "JSONプレビュー", true);
            if (showJsonPreview && !string.IsNullOrEmpty(jsonContent))
            {
                EditorGUILayout.BeginVertical(EditorStyles.textArea);
                
                // 問題のある行：エディタがシリアライズできないオブジェクトを処理しようとしてエラーになる
                // jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition, GUILayout.Height(200));
                // GUI.enabled = false;
                // EditorGUILayout.TextArea(jsonContent); // この行がエラーの原因
                // GUI.enabled = true;
                // EditorGUILayout.EndScrollView();
                
                // 代わりにこのコードを使用：
                jsonScrollPosition = EditorGUILayout.BeginScrollView(jsonScrollPosition, GUILayout.Height(200));
                
                // TextAreaの代わりにLabelを使用して問題を回避
                GUIStyle previewStyle = new GUIStyle(EditorStyles.label);
                previewStyle.wordWrap = true;
                previewStyle.richText = false;
                
                // 長すぎるJSONを扱うために分割して表示
                const int maxLength = 10000; // Unityのラベルが安全に表示できる最大長
                
                if (jsonContent.Length <= maxLength)
                {
                    EditorGUILayout.LabelField(jsonContent, previewStyle);
                }
                else
                {
                    // 長いJSONを複数のラベルに分割
                    int remainingLength = jsonContent.Length;
                    int currentPos = 0;
                    
                    while (remainingLength > 0)
                    {
                        int chunkSize = Mathf.Min(maxLength, remainingLength);
                        string chunk = jsonContent.Substring(currentPos, chunkSize);
                        
                        EditorGUILayout.LabelField(chunk, previewStyle);
                        
                        currentPos += chunkSize;
                        remainingLength -= chunkSize;
                    }
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// フッターの描画
        /// </summary>
        private void DrawFooter()
        {
            EditorGUILayout.Space(10);
            
            // ドラッグ＆ドロップ領域
            DrawDragDropArea();
        }

        /// <summary>
        /// ドラッグ＆ドロップ領域の描画
        /// </summary>
        private void DrawDragDropArea()
        {
            // ドラッグ＆ドロップエリア
            var dropRect = EditorGUILayout.GetControlRect(false, 60);
            GUI.Box(dropRect, "JSONファイルをここにドラッグ＆ドロップ", EditorStyles.helpBox);
        }

        /// <summary>
        /// ドラッグ＆ドロップの処理
        /// </summary>
        private void HandleDragAndDrop()
        {
            // ドラッグ＆ドロップ操作の検出
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    // ドラッグデータがアセットパスを持っているか確認
                    if (DragAndDrop.paths.Length > 0 && DragAndDrop.paths[0].EndsWith(".json"))
                    {
                        // ドラッグカーソルを変更
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            
                            // ドロップされたJSONファイルを処理
                            string path = DragAndDrop.paths[0];
                            LoadJsonFileFromPath(path);
                        }
                        
                        evt.Use();
                    }
                    break;
            }
        }

        /// <summary>
        /// JSONファイルを読み込む (TextAsset)
        /// </summary>
        private void LoadJsonFile(TextAsset asset)
        {
            if (asset != null)
            {
                jsonContent = asset.text;
                jsonFile = asset;
                
                // 状態を更新
                statusMessage = $"JSONファイル「{asset.name}」を読み込みました";
                statusMessageType = MessageType.Info;
                
                // アセットパスを取得して最近使用したファイルに追加
                string assetPath = AssetDatabase.GetAssetPath(asset);
                string fullPath = Path.GetFullPath(assetPath);
                AddToRecentFiles(fullPath);
                
                // UIビルダーに設定
                if (uiBuilder != null)
                {
                    serializedBuilder.Update();
                    var uiDefProp = serializedBuilder.FindProperty("uiDefinitionFile");
                    uiDefProp.objectReferenceValue = asset;
                    serializedBuilder.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// JSONファイルをパスから読み込む
        /// </summary>
        private void LoadJsonFileFromPath(string path)
        {
            try
            {
                // ファイルからJSONを読み込む
                string json = File.ReadAllText(path);
                jsonContent = json;
                
                // プロジェクト内のパスならTextAssetとして読み込む
                string relativePath = path;
                if (path.StartsWith(Application.dataPath))
                {
                    relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    jsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
                }
                else
                {
                    jsonFile = null;
                }
                
                // 状態を更新
                statusMessage = $"JSONファイル「{Path.GetFileName(path)}」を読み込みました";
                statusMessageType = MessageType.Info;
                
                // 最近使用したファイルに追加
                AddToRecentFiles(path);
                
                // UIビルダーに設定
                if (uiBuilder != null && jsonFile != null)
                {
                    serializedBuilder.Update();
                    var uiDefProp = serializedBuilder.FindProperty("uiDefinitionFile");
                    uiDefProp.objectReferenceValue = jsonFile;
                    serializedBuilder.ApplyModifiedProperties();
                }
                
                // 再描画
                Repaint();
            }
            catch (System.Exception e)
            {
                // エラー表示
                statusMessage = $"JSONファイルの読み込みに失敗しました: {e.Message}";
                statusMessageType = MessageType.Error;
                Debug.LogError($"JSONファイルの読み込みエラー: {e}");
            }
        }

        /// <summary>
        /// UIをビルド
        /// </summary>
        private void BuildUI()
        {
            if (string.IsNullOrEmpty(jsonContent))
            {
                statusMessage = "JSONファイルが読み込まれていません";
                statusMessageType = MessageType.Warning;
                return;
            }
            
            try
            {
                // UIビルダーが存在しない場合は再作成
                if (uiBuilder == null)
                {
                    FindOrCreateUIBuilder();
                }
                
                // 事前にUIをクリア
                if (clearBeforeBuild)
                {
                    ClearUI();
                }
                
                // シーンを編集可能に
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                
                // UIを構築
                uiBuilder.BuildUIFromJson(jsonContent);
                
                // 状態を更新
                statusMessage = "UIを正常に構築しました";
                statusMessageType = MessageType.Info;
                
                // Canvasを自動選択
                if (autoSelectCanvas)
                {
                    Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        Selection.activeGameObject = canvas.gameObject;
                        EditorGUIUtility.PingObject(canvas.gameObject);
                    }
                }
            }
            catch (System.Exception e)
            {
                // エラー表示
                statusMessage = $"UIの構築に失敗しました: {e.Message}";
                statusMessageType = MessageType.Error;
                Debug.LogError($"UI構築エラー: {e}");
            }
        }

        /// <summary>
        /// UIをクリア
        /// </summary>
        private void ClearUI()
        {
            try
            {
                // UIビルダーがあればクリアメソッドを呼び出す
                if (uiBuilder != null)
                {
                    uiBuilder.ClearExistingUI();
                    statusMessage = "UIをクリアしました";
                    statusMessageType = MessageType.Info;
                }
                else
                {
                    // 直接Canvasを検索してクリア
                    Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        // Canvasの子オブジェクトをすべて削除
                        while (canvas.transform.childCount > 0)
                        {
                            DestroyImmediate(canvas.transform.GetChild(0).gameObject);
                        }
                        
                        statusMessage = "UIをクリアしました";
                        statusMessageType = MessageType.Info;
                    }
                    else
                    {
                        statusMessage = "クリアするCanvasが見つかりません";
                        statusMessageType = MessageType.Warning;
                    }
                }
            }
            catch (System.Exception e)
            {
                statusMessage = $"UIのクリアに失敗しました: {e.Message}";
                statusMessageType = MessageType.Error;
            }
        }
    }
}
#endif