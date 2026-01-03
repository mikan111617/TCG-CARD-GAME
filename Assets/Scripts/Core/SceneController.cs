using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーン管理と画面遷移を制御するクラス（拡張版）
/// </summary>
public class SceneController : MonoBehaviour
{
    [Header("シーン設定")]
    public string mainMenuSceneName = "MainMenuScene";
    public string gameSceneName = "MainGameScene";
    public string deckEditorSceneName = "DeckEditorScene";
    public string playerSelectionSceneName = "PlayerSelectionScene";
    
    [Header("遷移エフェクト")]
    public float transitionDuration = 0.5f;
    public CanvasGroup transitionPanel; // フェード用のパネル
    
    // シングルトンパターン（任意）
    public static SceneController Instance { get; private set; }
    
    private void Awake()
    {
        // シングルトンパターン（任意）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    // メインメニューに戻る
    public void ReturnToMainMenu()
    {
        StartCoroutine(LoadSceneWithTransition(mainMenuSceneName));
    }
    
    // プレイヤー選択画面に移動
    public void GoToPlayerSelection()
    {
        StartCoroutine(LoadSceneWithTransition(playerSelectionSceneName));
    }
    
    // デッキエディタ画面に移動
    public void GoToDeckEditor()
    {
        StartCoroutine(LoadSceneWithTransition(deckEditorSceneName));
    }
    
    // メインゲーム画面に移動
    public void GoToMainGame()
    {
        StartCoroutine(LoadSceneWithTransition(gameSceneName));
    }
    
    // シーン読み込みのトランジションコルーチン
    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        // トランジションパネルがある場合はフェードイン
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadePanel(transitionPanel, 0f, 1f, transitionDuration));
        }
        
        // シーン読み込み
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // 読み込みが完了するまで待機
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // トランジションパネルがある場合はフェードアウト
        if (transitionPanel != null)
        {
            yield return StartCoroutine(FadePanel(transitionPanel, 1f, 0f, transitionDuration));
        }
    }
    
    // パネルのフェードコルーチン
    private IEnumerator FadePanel(CanvasGroup panel, float startAlpha, float targetAlpha, float duration)
    {
        float time = 0f;
        panel.alpha = startAlpha;
        panel.blocksRaycasts = true;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            panel.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        
        panel.alpha = targetAlpha;
        panel.blocksRaycasts = (targetAlpha > 0.5f); // ブロックするかどうかはアルファ値による
    }
}