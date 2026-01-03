using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// メインメニュー画面を管理するクラス
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("メインメニューボタン")]
    public Button startGameButton;     // ゲーム開始ボタン
    public Button deckBuilderButton;   // デッキ編集ボタン
    public Button cardCollectionButton; // カードコレクションボタン
    public Button optionsButton;       // オプションボタン
    public Button quitButton;          // 終了ボタン
    
    [Header("タイトル設定")]
    public TextMeshProUGUI gameTitleText; // ゲームタイトルテキスト
    public TextMeshProUGUI versionText;   // バージョン表示
    
    [Header("アニメーション")]
    public float buttonAnimationDelay = 0.1f; // ボタンアニメーション間の遅延
    public float buttonAnimationDuration = 0.5f; // アニメーション時間
    
    // シーンコントローラーへの参照
    private SceneController sceneController;
    
    private void Awake()
    {
        // SceneControllerの取得
        sceneController = UnityEngine.Object.FindFirstObjectByType<SceneController>();
        
        if (sceneController == null)
        {
            Debug.LogWarning("SceneControllerが見つかりません。新しく作成します。");
            GameObject controllerObj = new GameObject("SceneController");
            sceneController = controllerObj.AddComponent<SceneController>();
        }
    }
    
    private void Start()
    {
        // ボタンにイベントを追加
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
            
        if (deckBuilderButton != null)
            deckBuilderButton.onClick.AddListener(OnDeckBuilderClicked);
            
        if (cardCollectionButton != null)
            cardCollectionButton.onClick.AddListener(OnCardCollectionClicked);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(OnOptionsClicked);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
            
        // バージョン表示
        if (versionText != null)
            versionText.text = "Ver " + Application.version;
            
        // ボタンのアニメーション開始
        StartCoroutine(AnimateMenuButtons());
    }
    
    // ボタンをアニメーションさせるコルーチン
    private IEnumerator AnimateMenuButtons()
    {
        // ボタンの配列を作成
        Button[] buttons = { startGameButton, deckBuilderButton, cardCollectionButton, optionsButton, quitButton };
        
        // 各ボタンを非表示にする
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
                    
                canvasGroup.alpha = 0f;
            }
        }
        
        // 各ボタンをアニメーションで表示する
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
                StartCoroutine(FadeInButton(canvasGroup));
                yield return new WaitForSeconds(buttonAnimationDelay);
            }
        }
    }
    
    // ボタンをフェードインさせるコルーチン
    private IEnumerator FadeInButton(CanvasGroup canvasGroup)
    {
        float time = 0f;
        
        while (time < buttonAnimationDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / buttonAnimationDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    // ゲーム開始ボタンクリック時の処理
    private void OnStartGameClicked()
    {
        Debug.Log("ゲーム開始ボタンがクリックされました");
        
        // プレイヤー選択画面に遷移
        if (sceneController != null)
            sceneController.GoToPlayerSelection();
        else
            SceneManager.LoadScene("PlayerSelectionScene");
    }
    
    // デッキビルダーボタンクリック時の処理
    private void OnDeckBuilderClicked()
    {
        Debug.Log("デッキビルダーボタンがクリックされました");
        
        // デッキエディタシーンに遷移
        if (sceneController != null)
            sceneController.GoToDeckEditor();
        else
            SceneManager.LoadScene("DeckEditorScene");
    }
    
    // カードコレクションボタンクリック時の処理
    private void OnCardCollectionClicked()
    {
        Debug.Log("カードコレクションボタンがクリックされました");
        
        // カードコレクション画面に遷移（必要に応じて実装）
        // 現在はデッキエディタと同じ画面を使用
        OnDeckBuilderClicked();
    }
    
    // オプションボタンクリック時の処理
    private void OnOptionsClicked()
    {
        Debug.Log("オプションボタンがクリックされました");
        
        // オプション画面表示（未実装）
        // ここにオプション画面の表示コードを追加
    }
    
    // 終了ボタンクリック時の処理
    private void OnQuitClicked()
    {
        Debug.Log("終了ボタンがクリックされました");
        
        // ゲーム終了
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}