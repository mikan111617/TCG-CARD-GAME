using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ゲームモード選択管理クラス
public class GameModeSelector : MonoBehaviour
{
    [Header("UI要素")]
    public GameObject selectionPanel;          // 選択画面のパネル
    public Button vsAIButton;                  // AIと対戦するボタン
    public Button vsNetworkButton;             // ネットワーク対戦ボタン
    public Button optionsButton;               // オプションボタン（必要に応じて）
    
    [Header("ゲーム設定")]
    public GameObject aiPlayerPrefab;          // AIプレイヤーのプレハブ
    public GameObject networkPlayerPrefab;     // ネットワークプレイヤーのプレハブ
    
    private GameManager gameManager;           // ゲームマネージャー参照
    
    private void Awake()
    {
        // ゲームマネージャーを取得
        gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        
        if (gameManager == null)
        {
            Debug.LogError("GameManagerが見つかりません");
        }
    }
    
    private void Start()
    {
        // ボタンのイベント設定
        if (vsAIButton != null)
            vsAIButton.onClick.AddListener(StartAIGame);
            
        if (vsNetworkButton != null)
            vsNetworkButton.onClick.AddListener(StartNetworkGame);
            
        if (optionsButton != null)
            optionsButton.onClick.AddListener(ShowOptions);
        
        // 選択画面を表示
        ShowSelectionPanel();
    }
    
    // 選択画面を表示
    public void ShowSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }
    
    // 選択画面を非表示
    public void HideSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }
    
    // AIとの対戦を開始
    public void StartAIGame()
    {
        HideSelectionPanel();
        
        // Player2としてAIプレイヤーを設定
        SetupAIPlayer();
        
        // ゲーム開始
        if (gameManager != null)
        {
            gameManager.StartGame();
            UnityEngine.Object.FindFirstObjectByType<UIManager>()?.ShowNotification("AIとの対戦を開始します",1);
        }
    }
    
    // AIプレイヤーのセットアップ
    private void SetupAIPlayer()
    {
        // 既存のPlayer2を削除
        if (gameManager.player2 != null)
        {
            Destroy(gameManager.player2.gameObject);
        }
        
        // AIプレイヤーを生成
        GameObject aiPlayerObj;
        if (aiPlayerPrefab != null)
        {
            aiPlayerObj = Instantiate(aiPlayerPrefab);
            aiPlayerObj.name = "AIPlayer";
        }
        else
        {
            aiPlayerObj = new GameObject("AIPlayer");
            aiPlayerObj.AddComponent<AIPlayer>();
        }
        
        // GameManagerに設定
        AIPlayer aiPlayer = aiPlayerObj.GetComponent<AIPlayer>();
        aiPlayer.playerName = "AIプレイヤー";
        gameManager.player2 = aiPlayer;
    }
    
    // ネットワーク対戦を開始
    public void StartNetworkGame()
    {
        HideSelectionPanel();
        
        // Player2としてネットワークプレイヤーを設定
        SetupNetworkPlayer();
        
        // ゲーム開始
        if (gameManager != null)
        {
            gameManager.StartGame();
            UnityEngine.Object.FindFirstObjectByType<UIManager>()?.ShowNotification("ネットワーク対戦を開始します",1);
        }
    }
    
    // ネットワークプレイヤーのセットアップ
    private void SetupNetworkPlayer()
    {
        // 既存のPlayer2を削除
        if (gameManager.player2 != null)
        {
            Destroy(gameManager.player2.gameObject);
        }
        
        // ネットワークプレイヤーを生成
        GameObject networkPlayerObj;
        if (networkPlayerPrefab != null)
        {
            networkPlayerObj = Instantiate(networkPlayerPrefab);
            networkPlayerObj.name = "NetworkPlayer";
        }
        else
        {
            networkPlayerObj = new GameObject("NetworkPlayer");
            networkPlayerObj.AddComponent<Player>();
            // ネットワーク関連のコンポーネントも追加（必要に応じて）
        }
        
        // GameManagerに設定
        Player networkPlayer = networkPlayerObj.GetComponent<Player>();
        networkPlayer.playerName = "対戦相手";
        gameManager.player2 = networkPlayer;
    }
    
    // オプション画面を表示
    public void ShowOptions()
    {
        // オプション画面を表示する処理
        // 必要に応じて実装
    }
}