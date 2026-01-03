using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.Events;

namespace CardGameUI
{
    /// <summary>
    /// JSONからUIを自動生成するシステム
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [SerializeField] private TextAsset uiDefinitionFile;
        [SerializeField] private bool buildOnStart = false;

        private void Start()
        {
            if (buildOnStart && uiDefinitionFile != null)
            {
                BuildUIFromJson(uiDefinitionFile.text);
            }
        }

        /// <summary>
        /// JSON文字列からUIを構築する
        /// </summary>
        /// <param name="jsonText">JSON形式のUI定義</param>
        public void BuildUIFromJson(string jsonText)
        {
            UIDefinition uiDefinition = JsonUtility.FromJson<UIDefinition>(jsonText);
            
            // キャンバスの取得または作成
            Canvas canvas = FindOrCreateCanvas();
            
            // UIElements（ルート要素）の構築
            foreach (UIElementDefinition elementDef in uiDefinition.elements)
            {
                CreateUIElement(elementDef, canvas.transform);
            }
            
            Debug.Log("UIの構築が完了しました");
        }

        /// <summary>
        /// JSONファイルからUIを構築する
        /// </summary>
        /// <param name="filePath">JSONファイルのパス</param>
        public void BuildUIFromJsonFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonText = File.ReadAllText(filePath);
                BuildUIFromJson(jsonText);
            }
            else
            {
                Debug.LogError("指定されたJSONファイルが見つかりません: " + filePath);
            }
        }

        /// <summary>
        /// キャンバスを検索または作成する
        /// </summary>
        private Canvas FindOrCreateCanvas()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // キャンバススケーラーを追加
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                
                // キャンバス用のRaycasterを追加
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            return canvas;
        }

        /// <summary>
        /// UI要素を作成する
        /// </summary>
        /// <param name="elementDef">UI要素の定義</param>
        /// <param name="parent">親のTransform</param>
        /// <returns>作成したGameObject</returns>
        private GameObject CreateUIElement(UIElementDefinition elementDef, Transform parent)
        {
            GameObject gameObject = null;
            
            // 要素のタイプに基づいてGameObjectを作成
            switch (elementDef.type)
            {
                case "Panel":
                case "Image":
                    gameObject = CreateImageElement(elementDef);
                    break;
                case "Text":
                    gameObject = CreateTextElement(elementDef);
                    break;
                case "Button":
                    gameObject = CreateButtonElement(elementDef);
                    break;
                case "Empty":
                default:
                    gameObject = new GameObject(elementDef.name);
                    gameObject.AddComponent<RectTransform>();
                    break;
            }
            
            // RectTransformの設定
            if (gameObject != null)
            {
                RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
                SetRectTransform(rectTransform, elementDef);
                
                // 親子関係の設定
                rectTransform.SetParent(parent, false);
                
                // 各種コンポーネントの設定
                ApplyComponentSettings(gameObject, elementDef);
                
                // 子要素の作成
                if (elementDef.children != null)
                {
                    foreach (UIElementDefinition childDef in elementDef.children)
                    {
                        CreateUIElement(childDef, gameObject.transform);
                    }
                }
            }
            
            return gameObject;
        }

        /// <summary>
        /// Image/Panel要素を作成
        /// </summary>
        private GameObject CreateImageElement(UIElementDefinition elementDef)
        {
            GameObject imageObj = new GameObject(elementDef.name);
            Image image = imageObj.AddComponent<Image>();
            
            // 画像ソースの設定（オプション）
            if (!string.IsNullOrEmpty(elementDef.imagePath))
            {
                Sprite sprite = Resources.Load<Sprite>(elementDef.imagePath);
                if (sprite != null)
                {
                    image.sprite = sprite;
                }
            }
            
            // 色の設定
            if (elementDef.color != null && elementDef.color.Length >= 4)
            {
                image.color = new Color(
                    elementDef.color[0],
                    elementDef.color[1],
                    elementDef.color[2],
                    elementDef.color[3]
                );
            }
            
            return imageObj;
        }

        /// <summary>
        /// Text要素を作成
        /// </summary>
        private GameObject CreateTextElement(UIElementDefinition elementDef)
        {
            GameObject textObj = new GameObject(elementDef.name);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            
            // テキスト設定
            text.text = elementDef.text ?? "";
            
            // フォントサイズ
            if (elementDef.fontSize > 0)
            {
                text.fontSize = elementDef.fontSize;
            }
            
            // アライメント
            if (elementDef.alignment != null)
            {
                switch (elementDef.alignment.ToLower())
                {
                    case "center":
                        text.alignment = TextAlignmentOptions.Center;
                        break;
                    case "left":
                        text.alignment = TextAlignmentOptions.Left;
                        break;
                    case "right":
                        text.alignment = TextAlignmentOptions.Right;
                        break;
                    // 他のアライメントオプション
                }
            }
            
            // 色の設定
            if (elementDef.color != null && elementDef.color.Length >= 4)
            {
                text.color = new Color(
                    elementDef.color[0],
                    elementDef.color[1],
                    elementDef.color[2],
                    elementDef.color[3]
                );
            }
            
            return textObj;
        }

        /// <summary>
        /// Button要素を作成
        /// </summary>
        private GameObject CreateButtonElement(UIElementDefinition elementDef)
        {
            GameObject buttonObj = new GameObject(elementDef.name);
            
            // ボタン用のImageコンポーネント
            Image image = buttonObj.AddComponent<Image>();
            
            // 色の設定
            if (elementDef.color != null && elementDef.color.Length >= 4)
            {
                image.color = new Color(
                    elementDef.color[0],
                    elementDef.color[1],
                    elementDef.color[2],
                    elementDef.color[3]
                );
            }
            
            // Buttonコンポーネント
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            
            // テキスト追加（オプション）
            if (!string.IsNullOrEmpty(elementDef.text))
            {
                GameObject textObj = new GameObject("Text");
                RectTransform textRect = textObj.AddComponent<RectTransform>();
                textRect.SetParent(buttonObj.transform, false);
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(0, 0);
                textRect.offsetMax = new Vector2(0, 0);
                
                TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
                text.text = elementDef.text;
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = elementDef.fontSize > 0 ? elementDef.fontSize : 24;
            }
            
            return buttonObj;
        }

        /// <summary>
        /// RectTransformの設定
        /// </summary>
        private void SetRectTransform(RectTransform rectTransform, UIElementDefinition elementDef)
        {
            // アンカー設定
            if (elementDef.anchors != null && elementDef.anchors.Length >= 4)
            {
                rectTransform.anchorMin = new Vector2(elementDef.anchors[0], elementDef.anchors[1]);
                rectTransform.anchorMax = new Vector2(elementDef.anchors[2], elementDef.anchors[3]);
            }
            else if (!string.IsNullOrEmpty(elementDef.anchorPreset))
            {
                // アンカープリセットの設定
                SetAnchorPreset(rectTransform, elementDef.anchorPreset);
            }
            
            // 位置設定
            if (elementDef.position != null && elementDef.position.Length >= 2)
            {
                rectTransform.anchoredPosition = new Vector2(
                    elementDef.position[0],
                    elementDef.position[1]
                );
            }
            
            // サイズ設定
            if (elementDef.size != null && elementDef.size.Length >= 2)
            {
                rectTransform.sizeDelta = new Vector2(
                    elementDef.size[0],
                    elementDef.size[1]
                );
            }
            
            // ピボット設定
            if (elementDef.pivot != null && elementDef.pivot.Length >= 2)
            {
                rectTransform.pivot = new Vector2(
                    elementDef.pivot[0],
                    elementDef.pivot[1]
                );
            }
        }

        /// <summary>
        /// アンカープリセットの設定
        /// </summary>
        private void SetAnchorPreset(RectTransform rectTransform, string presetName)
        {
            switch (presetName.ToLower())
            {
                case "topleft":
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(0, 1);
                    rectTransform.pivot = new Vector2(0, 1);
                    break;
                case "topcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 1);
                    rectTransform.anchorMax = new Vector2(0.5f, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1);
                    break;
                case "topright":
                    rectTransform.anchorMin = new Vector2(1, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(1, 1);
                    break;
                case "middleleft":
                    rectTransform.anchorMin = new Vector2(0, 0.5f);
                    rectTransform.anchorMax = new Vector2(0, 0.5f);
                    rectTransform.pivot = new Vector2(0, 0.5f);
                    break;
                case "middlecenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middleright":
                    rectTransform.anchorMin = new Vector2(1, 0.5f);
                    rectTransform.anchorMax = new Vector2(1, 0.5f);
                    rectTransform.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottomleft":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(0, 0);
                    rectTransform.pivot = new Vector2(0, 0);
                    break;
                case "bottomcenter":
                    rectTransform.anchorMin = new Vector2(0.5f, 0);
                    rectTransform.anchorMax = new Vector2(0.5f, 0);
                    rectTransform.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottomright":
                    rectTransform.anchorMin = new Vector2(1, 0);
                    rectTransform.anchorMax = new Vector2(1, 0);
                    rectTransform.pivot = new Vector2(1, 0);
                    break;
                case "stretch":
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    break;
                // 他のプリセット
            }
        }

        /// <summary>
        /// 追加コンポーネント設定の適用
        /// </summary>
        private void ApplyComponentSettings(GameObject gameObject, UIElementDefinition elementDef)
        {
            // レイアウトグループの設定
            if (!string.IsNullOrEmpty(elementDef.layoutGroup))
            {
                switch (elementDef.layoutGroup.ToLower())
                {
                    case "horizontal":
                        HorizontalLayoutGroup hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
                        if (elementDef.layoutSettings != null)
                        {
                            SetLayoutGroupSettings(hlg, elementDef.layoutSettings);
                        }
                        break;
                    case "vertical":
                        VerticalLayoutGroup vlg = gameObject.AddComponent<VerticalLayoutGroup>();
                        if (elementDef.layoutSettings != null)
                        {
                            SetLayoutGroupSettings(vlg, elementDef.layoutSettings);
                        }
                        break;
                    case "grid":
                        GridLayoutGroup glg = gameObject.AddComponent<GridLayoutGroup>();
                        if (elementDef.layoutSettings != null)
                        {
                            SetGridLayoutGroupSettings(glg, elementDef.layoutSettings);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// レイアウトグループ設定の適用
        /// </summary>
        private void SetLayoutGroupSettings(HorizontalOrVerticalLayoutGroup layoutGroup, LayoutGroupSettings settings)
        {
            if (settings.spacing.HasValue)
            {
                layoutGroup.spacing = settings.spacing.Value;
            }
            
            if (settings.padding != null && settings.padding.Length >= 4)
            {
                layoutGroup.padding = new RectOffset(
                    settings.padding[0],
                    settings.padding[1],
                    settings.padding[2],
                    settings.padding[3]
                );
            }
            
            if (settings.childAlignment.HasValue)
            {
                layoutGroup.childAlignment = (TextAnchor)settings.childAlignment.Value;
            }
        }

        /// <summary>
        /// グリッドレイアウト設定の適用
        /// </summary>
        private void SetGridLayoutGroupSettings(GridLayoutGroup gridLayout, LayoutGroupSettings settings)
        {
            if (settings.cellSize != null && settings.cellSize.Length >= 2)
            {
                gridLayout.cellSize = new Vector2(settings.cellSize[0], settings.cellSize[1]);
            }
            
            if (settings.spacing != null && settings.spacing.HasValue)
            {
                gridLayout.spacing = new Vector2(settings.spacing.Value, settings.spacing.Value);
            }
            
            if (settings.childAlignment.HasValue)
            {
                gridLayout.childAlignment = (TextAnchor)settings.childAlignment.Value;
            }
        }

        /// <summary>
        /// 既存のUI要素をクリア
        /// </summary>
        public void ClearExistingUI()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                // 子要素を保存してから削除（foreachループ中の削除を避けるため）
                List<GameObject> childrenToDestroy = new List<GameObject>();
                foreach (Transform child in canvas.transform)
                {
                    childrenToDestroy.Add(child.gameObject);
                }
                
                // 保存したリストの要素を削除
                foreach (GameObject child in childrenToDestroy)
                {
                    DestroyImmediate(child);
                }
            }
        }
    }

    /// <summary>
    /// UI定義のルート
    /// </summary>
    [System.Serializable]
    public class UIDefinition
    {
        public UIElementDefinition[] elements;
    }

    /// <summary>
    /// UI要素の定義
    /// </summary>
    [System.Serializable]
    public class UIElementDefinition
    {
        public string name;
        public string type;       // "Empty", "Panel", "Image", "Text", "Button" など
        public string imagePath;  // 画像パス（Resources内）
        public string text;       // テキスト内容
        public int fontSize;      // フォントサイズ
        public string alignment;  // テキストアライメント
        public float[] color;     // RGBA (0-1)
        public float[] position;  // X, Y
        public float[] size;      // Width, Height
        public float[] anchors;   // MinX, MinY, MaxX, MaxY
        public string anchorPreset; // "TopLeft", "MiddleCenter" など
        public float[] pivot;     // X, Y
        public string layoutGroup; // "Horizontal", "Vertical", "Grid"
        public LayoutGroupSettings layoutSettings;
        public UIElementDefinition[] children;
    }

    /// <summary>
    /// レイアウトグループ設定
    /// </summary>
    [System.Serializable]
    public class LayoutGroupSettings
    {
        public float? spacing;
        public int[] padding;     // Left, Right, Top, Bottom
        public int? childAlignment; // TextAnchorの値（0-8）
        public float[] cellSize;  // GridLayoutGroup用 Width, Height
    }
}