using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq; // LINQ の追加

#if UNITY_EDITOR
/// <summary>
/// カード画像のインポートと管理を行うエディタ拡張
/// </summary>
public class CardImageImporter : EditorWindow
{
    // カード画像タイプ
    public enum CardImageType
    {
        Character,
        Spell,
        Field,
        Frame,
        Icon
    }
    
    // 画像サイズプリセット
    private static readonly Vector2Int[] SizePresets = new Vector2Int[]
    {
        new Vector2Int(256, 256),  // 小
        new Vector2Int(512, 512),  // 中
        new Vector2Int(1024, 1024) // 大
    };
    
    // UI設定
    private Vector2 scrollPosition;
    private CardImageType selectedImageType = CardImageType.Character;
    private Texture2D[] selectedImages;
    private int selectedSizePreset = 1; // デフォルトは中サイズ
    private bool maintainAspectRatio = true;
    private bool generateMipmaps = true;
    private bool overwriteExisting = false;
    private string customOutputFolder = "";
    private bool useTransparency = true;
    
    // インポート処理情報
    private string importStatus = "";
    private float importProgress = 0f;
    private bool isImporting = false;

    private Texture2D[] processingImages;
    private int totalImages;
    private int processedImages;
    private Vector2Int targetSize;
    private string importPath;
    
    // ディレクトリパス
    private static readonly string BaseResourcesPath = "Assets/Resources/CardImages/";
    private static readonly string[] TypeFolders = new string[]
    {
        "Characters",
        "Spells",
        "Fields",
        "Frames",
        "Icons"
    };
    
    // メニューにツールを追加
    [MenuItem("Tools/Card Game/Card Image Importer")]
    public static void ShowWindow()
    {
        CardImageImporter window = GetWindow<CardImageImporter>("カード画像インポーター");
        window.minSize = new Vector2(500, 600);
        window.Show();
    }
    
    // ウィンドウ初期化
    private void OnEnable()
    {
        // フォルダパスの初期化
        EnsureDirectoriesExist();
    }
    
    // GUI描画
    private void OnGUI()
    {
        // タイトル
        GUILayout.Space(10);
        GUILayout.Label("カード画像インポーター", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // カード画像タイプ選択
        selectedImageType = (CardImageType)EditorGUILayout.EnumPopup("画像タイプ", selectedImageType);
        GUILayout.Space(5);
        
        // 画像選択セクション
        EditorGUILayout.LabelField("インポートする画像", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        
        // ObjectFieldを修正
        UnityEngine.Object objReference = null;
        if (selectedImages != null && selectedImages.Length > 0)
            objReference = selectedImages[0];
            
        objReference = EditorGUILayout.ObjectField(
            "画像ファイル (1つ)",
            objReference,
            typeof(Texture2D),
            false
        );
        
        // 選択したオブジェクトがTexture2Dなら配列に格納
        if (EditorGUI.EndChangeCheck())
        {
            if (objReference is Texture2D)
            {
                selectedImages = new Texture2D[] { (Texture2D)objReference };
            }
            else
            {
                selectedImages = null;
            }
        }
        
        if (GUILayout.Button("ファイル選択ダイアログで選択"))
        {
            string path = EditorUtility.OpenFilePanel(
                "カード画像を選択",
                "",
                "png,jpg,jpeg,tga,bmp,psd"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                string[] paths = new string[] { path };
                ImportImagesFromPaths(paths);
            }
        }
        
        // 選択された画像の情報表示
        if (selectedImages != null && selectedImages.Length > 0)
        {
            EditorGUILayout.LabelField($"選択された画像: {selectedImages.Length}枚", EditorStyles.boldLabel);
            
            // スクロールリストで画像を表示
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            EditorGUILayout.BeginVertical();
            
            for (int i = 0; i < selectedImages.Length; i++)
            {
                if (selectedImages[i] != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    // プレビュー表示（Texture2D用のObjectField）
                    EditorGUILayout.ObjectField(selectedImages[i], typeof(Texture2D), false, GUILayout.Width(60), GUILayout.Height(60));
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(selectedImages[i].name);
                    EditorGUILayout.LabelField($"サイズ: {selectedImages[i].width}x{selectedImages[i].height}");
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        
        GUILayout.Space(10);
        
        // 画像処理オプション
        EditorGUILayout.LabelField("出力設定", EditorStyles.boldLabel);
        
        // サイズプリセット
        string[] presetLabels = new string[] { "小 (256x256)", "中 (512x512)", "大 (1024x1024)" };
        selectedSizePreset = EditorGUILayout.Popup("サイズプリセット", selectedSizePreset, presetLabels);
        
        // アスペクト比の維持
        maintainAspectRatio = EditorGUILayout.Toggle("アスペクト比を維持", maintainAspectRatio);
        
        // ミップマップの生成
        generateMipmaps = EditorGUILayout.Toggle("ミップマップを生成", generateMipmaps);
        
        // 透過処理
        useTransparency = EditorGUILayout.Toggle("透過処理を適用", useTransparency);
        
        // 既存ファイルの上書き
        overwriteExisting = EditorGUILayout.Toggle("既存ファイルを上書き", overwriteExisting);
        
        // 出力フォルダのカスタマイズ
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("カスタム出力フォルダ");
        customOutputFolder = EditorGUILayout.TextField(customOutputFolder);
        
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("出力フォルダを選択", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // プロジェクトパスの相対パスに変換
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                customOutputFolder = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        
        // インポートボタン
        GUI.enabled = selectedImages != null && selectedImages.Length > 0 && !isImporting;
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        
        if (GUILayout.Button("インポート実行", GUILayout.Height(40)))
        {
            ImportImages();
        }
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        // インポート進捗表示
        if (!string.IsNullOrEmpty(importStatus))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(importStatus, MessageType.Info);
            
            if (isImporting)
            {
                EditorGUILayout.Space(5);
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(progressRect, importProgress, (importProgress * 100).ToString("F0") + "%");
            }
        }
    }
    
    private void ImportImagesFromPaths(string[] paths)
    {
        List<Texture2D> textures = new List<Texture2D>();
        
        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path)) continue;
            
            // プロジェクト内のパスに変換
            string assetPath = path;
            if (path.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + path.Substring(Application.dataPath.Length);
            }
            else if (!path.StartsWith("Assets"))
            {
                // プロジェクト外の画像は一時的にインポート
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    tex.name = Path.GetFileNameWithoutExtension(path);
                    textures.Add(tex);
                }
                continue;
            }
            
            // プロジェクト内の画像はAssetDatabaseから取得
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                textures.Add(texture);
            }
        }
        
        if (textures.Count > 0)
        {
            selectedImages = textures.ToArray();
        }
    }
    
    // 画像のインポート処理
    private void ImportImages()
    {
        if (selectedImages == null || selectedImages.Length == 0)
        {
            EditorUtility.DisplayDialog("インポートエラー", "インポートする画像が選択されていません。", "OK");
            return;
        }
        
        // インポートディレクトリの確認
        importPath = GetImportPath();
        EnsureDirectoriesExist();
        
        // インポート進捗の初期化
        isImporting = true;
        importProgress = 0f;
        importStatus = "インポート処理中...";
        
        // クラスフィールドに設定
        processingImages = selectedImages;
        totalImages = processingImages.Length;
        processedImages = 0;
        
        // 画像の出力サイズ設定
        targetSize = SizePresets[selectedSizePreset];
        
        // イベントに追加
        EditorApplication.update += ProcessImportUpdate;
    }

    private void ProcessImportUpdate()
    {
        // 全画像の処理が完了した場合
        if (processedImages >= totalImages)
        {
            // インポート完了処理
            EditorApplication.update -= ProcessImportUpdate;
            isImporting = false;
            importStatus = $"インポート完了: {processedImages}枚の画像をインポートしました。";
            importProgress = 1f;
            
            // アセットデータベースの更新
            AssetDatabase.Refresh();
            
            Repaint();
            return;
        }
        
        // まだ処理すべき画像がある場合
        Texture2D texture = processingImages[processedImages];
        importStatus = $"処理中: {texture.name} ({processedImages + 1}/{totalImages})";
        
        try
        {
            // 画像の処理とインポート
            ProcessAndImportTexture(texture, importPath, targetSize);
            processedImages++;
            importProgress = (float)processedImages / totalImages;
            Repaint();
        }
        catch (Exception e)
        {
            // エラー処理
            Debug.LogError($"画像「{texture.name}」のインポート中にエラーが発生: {e.Message}");
            processedImages++;
            importProgress = (float)processedImages / totalImages;
            Repaint();
        }
    }
        
    // 画像の処理とインポート
    private void ProcessAndImportTexture(Texture2D sourceTexture, string importPath, Vector2Int targetSize)
    {
        // 出力ファイル名の設定
        string fileName = sourceTexture.name;
        string outputPath = Path.Combine(importPath, fileName + ".png");
        
        // 既存ファイルのチェック
        if (File.Exists(outputPath) && !overwriteExisting)
        {
            outputPath = Path.Combine(importPath, fileName + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".png");
        }
        
        // 画像のリサイズと処理
        Texture2D processedTexture;
        
        // リードが許可されていない場合はコピーを作成
        if (!sourceTexture.isReadable)
        {
            string tempPath = AssetDatabase.GetAssetPath(sourceTexture);
            if (!string.IsNullOrEmpty(tempPath))
            {
                // インポーター設定を一時的に変更
                TextureImporter importer = AssetImporter.GetAtPath(tempPath) as TextureImporter;
                if (importer != null)
                {
                    bool prevReadable = importer.isReadable;
                    TextureImporterCompression prevCompression = importer.textureCompression;
                    
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    
                    // リサイズと処理
                    processedTexture = ResizeTexture(sourceTexture, targetSize);
                    
                    // インポーター設定を元に戻す
                    importer.isReadable = prevReadable;
                    importer.textureCompression = prevCompression;
                    importer.SaveAndReimport();
                }
                else
                {
                    // インポーターが見つからない場合（プロジェクト外の画像など）
                    processedTexture = ResizeTexture(sourceTexture, targetSize);
                }
            }
            else
            {
                // ランタイムで生成されたテクスチャの場合
                processedTexture = ResizeTexture(sourceTexture, targetSize);
            }
        }
        else
        {
            // 既に読み込み可能な場合は直接処理
            processedTexture = ResizeTexture(sourceTexture, targetSize);
        }
        
        // PNG形式でファイルに保存
        byte[] pngData = processedTexture.EncodeToPNG();
        File.WriteAllBytes(outputPath, pngData);
        
        // 一時オブジェクトの破棄
        if (processedTexture != sourceTexture)
        {
            DestroyImmediate(processedTexture);
        }
        
        // インポート後の設定
        AssetDatabase.ImportAsset(outputPath);
        TextureImporter outputImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (outputImporter != null)
        {
            outputImporter.textureType = TextureImporterType.Sprite;
            outputImporter.spriteImportMode = SpriteImportMode.Single;
            outputImporter.mipmapEnabled = generateMipmaps;
            outputImporter.isReadable = true;
            
            // 透過設定
            if (useTransparency)
            {
                outputImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                outputImporter.alphaIsTransparency = true;
            }
            
            outputImporter.SaveAndReimport();
        }
    }
    
    // テクスチャのリサイズ処理
    private Texture2D ResizeTexture(Texture2D source, Vector2Int targetSize)
    {
        int width = targetSize.x;
        int height = targetSize.y;
        
        // アスペクト比を維持する場合
        if (maintainAspectRatio)
        {
            float aspectRatio = (float)source.width / source.height;
            if (source.width > source.height)
            {
                // 横長画像
                height = Mathf.RoundToInt(width / aspectRatio);
            }
            else
            {
                // 縦長画像
                width = Mathf.RoundToInt(height * aspectRatio);
            }
        }
        
        // リサイズ用のテクスチャを作成
        Texture2D result = new Texture2D(width, height, source.format, false);
        
        // ソーステクスチャのピクセルデータを取得
        Color[] sourcePixels;
        try
        {
            sourcePixels = source.GetPixels();
        }
        catch (UnityException)
        {
            // テクスチャデータが読み込めない場合（Read/Write Enabledになっていない場合など）
            Debug.LogWarning($"テクスチャ '{source.name}' からピクセルデータを読み取れません。Render Textureを使用します。");
            
            // RenderTextureを使ってテクスチャをコピー
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            
            // 現在のRenderTextureを保存
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            
            result.Reinitialize(source.width, source.height);
            result.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            result.Apply();
            
            // RenderTextureを元に戻す
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            
            // リサイズが必要な場合
            if (width != source.width || height != source.height)
            {
                Texture2D resized = new Texture2D(width, height, source.format, false);
                // ここでLINQが使われていたので修正
                Color[] colors = result.GetPixels(0, 0, result.width, result.height, 0);
                Color[] transparentColors = new Color[colors.Length];
                
                for (int j = 0; j < colors.Length; j++)
                {
                    transparentColors[j] = Color.Lerp(Color.clear, colors[j], colors[j].a);
                }
                
                resized.SetPixels(transparentColors);
                resized.Apply();
                
                // 一時テクスチャを破棄
                DestroyImmediate(result);
                result = resized;
            }
            
            return result;
        }
        
        // ピクセルデータのリサイズ
        if (width != source.width || height != source.height)
        {
            // バイリニア補間でリサイズ
            Color[] resizedPixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1);
                    float v = (float)y / (height - 1);
                    
                    float sourceX = u * (source.width - 1);
                    float sourceY = v * (source.height - 1);
                    
                    // バイリニア補間
                    Color c = BilinearSample(source, sourcePixels, sourceX, sourceY);
                    resizedPixels[y * width + x] = c;
                }
            }
            
            result.SetPixels(resizedPixels);
        }
        else
        {
            // サイズが同じ場合はそのままコピー
            result.SetPixels(sourcePixels);
        }
        
        result.Apply();
        return result;
    }
    
    // バイリニア補間によるサンプリング
    private Color BilinearSample(Texture2D tex, Color[] pixels, float x, float y)
    {
        int x1 = Mathf.FloorToInt(x);
        int y1 = Mathf.FloorToInt(y);
        int x2 = Mathf.Min(x1 + 1, tex.width - 1);
        int y2 = Mathf.Min(y1 + 1, tex.height - 1);
        
        float u = x - x1;
        float v = y - y1;
        
        Color c11 = pixels[y1 * tex.width + x1];
        Color c12 = pixels[y2 * tex.width + x1];
        Color c21 = pixels[y1 * tex.width + x2];
        Color c22 = pixels[y2 * tex.width + x2];
        
        Color c1 = Color.Lerp(c11, c12, v);
        Color c2 = Color.Lerp(c21, c22, v);
        
        return Color.Lerp(c1, c2, u);
    }
    
    // インポート先パスの取得
    private string GetImportPath()
    {
        // カスタムパスが指定されている場合はそれを使用
        if (!string.IsNullOrEmpty(customOutputFolder))
        {
            if (!Directory.Exists(customOutputFolder))
            {
                Directory.CreateDirectory(customOutputFolder);
            }
            return customOutputFolder;
        }
        
        // 画像タイプに応じたデフォルトパスを使用
        string typeFolderName = TypeFolders[(int)selectedImageType];
        string path = Path.Combine(BaseResourcesPath, typeFolderName);
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        
        return path;
    }
    
    // 必要なディレクトリが存在することを確認
    private void EnsureDirectoriesExist()
    {
        // ベースディレクトリが存在することを確認
        if (!Directory.Exists(BaseResourcesPath))
        {
            Directory.CreateDirectory(BaseResourcesPath);
        }
        
        // 各タイプのディレクトリが存在することを確認
        foreach (string folder in TypeFolders)
        {
            string path = Path.Combine(BaseResourcesPath, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif