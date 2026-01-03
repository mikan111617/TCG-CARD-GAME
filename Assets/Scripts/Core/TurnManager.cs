// TurnManager.cs - 修正版
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public partial class TurnManager : MonoBehaviour
{
    [Header("ターン情報")]
    public Player currentPlayer;
    public Player waitingPlayer;
    public TurnPhase currentPhase = TurnPhase.None;
    public int turnCount = 0;
    
    // ターンフェイズ遷移イベント
    public UnityEvent<TurnPhase> OnPhaseChanged = new UnityEvent<TurnPhase>();
    public UnityEvent<Player> OnTurnChanged = new UnityEvent<Player>();

    public Player firstPlayer;        // 先行プレイヤーの参照

    private bool isFirstTurn = true;  // 1ターン目かどうか
    
    // ターン処理中フラグ（重複実行防止）
    private bool isProcessingTurn = false;
    
    // ゲーム開始
    public void StartGame()
    {
        Debug.Log("TurnManager: ゲーム開始");
        turnCount = 0;
        isProcessingTurn = false;
        
        // 先攻プレイヤーをランダムに決定
        DetermineFirstPlayer();
        
        // ゲーム開始後、最初のターンを開始
        StartTurn(currentPlayer);
    }
    
    // 先攻プレイヤー決定
    private void DetermineFirstPlayer()
    {
        currentPlayer = GameManager.Instance.player1;
        waitingPlayer = GameManager.Instance.player2;
        Debug.Log($"先攻プレイヤー: {currentPlayer.playerName}");

        firstPlayer = currentPlayer;
        isFirstTurn = true;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFirstPlayer(firstPlayer);
        }
    }

    // 先行1ターン目かどうかを確認するためのメソッド
    public bool IsFirstPlayerFirstTurn()
    {
        return currentPlayer == firstPlayer && isFirstTurn;
    }
    
    // ターン開始メソッド
    public void StartTurn(Player player)
    {
        // 重複実行防止
        if (isProcessingTurn)
        {
            Debug.LogWarning("StartTurn: 既にターン処理中です。スキップします。");
            return;
        }
        
        isProcessingTurn = true;
        turnCount++;
        
        currentPlayer = player;
        waitingPlayer = GameManager.Instance.GetOpponent(player);
        
        Debug.Log($"===== ターン {turnCount}: {currentPlayer.playerName}のターン開始 =====");

        // 2ターン目以降はisFirstTurnをfalseにして状態を更新
        if (turnCount > 1 && isFirstTurn)
        {
            isFirstTurn = false;
            GameManager.Instance?.UpdateFirstTurnState(false);
        }

        // ゲームマネージャーにターン開始を通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleTurnStart(currentPlayer, waitingPlayer);
        }
        
        // ターン変更イベントを発火
        GameEventInfo turnEvent = new GameEventInfo(
            GameEventType.TurnChanged,
            currentPlayer,
            waitingPlayer
        );
        
        GameManager.Instance.TriggerGameEvent(turnEvent);
        
        // フェーズ進行
        ChangePhase(TurnPhase.Draw);
        
        // イベント発火
        if (OnTurnChanged != null)
        {
            OnTurnChanged.Invoke(currentPlayer);
        }
        
        // UI更新
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdateAllUI();
        }
        
        // ターン変更時のハンドラを呼び出し
        HandleTurnChanged(currentPlayer);
        
        // AIプレイヤーの場合は少し遅延してから自動プレイ
        if (currentPlayer is AIPlayer aiPlayer)
        {
            Debug.Log($"AIプレイヤーのターンです。意思決定を開始します。");
            StartCoroutine(DelayedAIDecision(aiPlayer));
        }
        
        // ターン処理完了
        isProcessingTurn = false;
    }
    
    // AIの意思決定を遅延実行するコルーチン
    private IEnumerator DelayedAIDecision(AIPlayer aiPlayer)
    {
        // ドローフェーズが完了するまで待機
        yield return new WaitForSecondsRealtime(1.5f);
        
        // 現在のプレイヤーがまだこのAIであることを確認
        if (currentPlayer == aiPlayer && currentPhase == TurnPhase.Action)
        {
            aiPlayer.SetPaused(false);
            aiPlayer.MakeDecision();
        }
    }
    
    // フェーズ変更
    public void ChangePhase(TurnPhase newPhase)
    {
        TurnPhase previousPhase = currentPhase;
        currentPhase = newPhase;

        Debug.Log($"フェーズ変更: {previousPhase} -> {currentPhase}");
        
        // フェーズ変更イベントを発火
        GameEventInfo phaseEvent = new GameEventInfo(
            GameEventType.PhaseChanged,
            currentPlayer,
            waitingPlayer
        );
        
        GameManager.Instance.TriggerGameEvent(phaseEvent);
        
        // フェーズ処理
        ProcessPhase();
        
        // イベント発火
        if (OnPhaseChanged != null)
        {
            OnPhaseChanged.Invoke(currentPhase);
        }
        
        // UI更新
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdatePhaseInfo(currentPhase);
            GameManager.Instance.uiManager.UpdateEndTurnButton();
        }
    }
    
    // 現在のフェーズの処理
    private void ProcessPhase()
    {
        switch (currentPhase)
        {
            case TurnPhase.Draw:
                Debug.Log($"{currentPlayer.playerName}のドローフェーズ");
                // カードを1枚ドロー
                currentPlayer.DrawCard();
                
                // 自動的にアクションフェーズへ
                ChangePhase(TurnPhase.Action);
                break;
                
            case TurnPhase.Action:
                Debug.Log($"{currentPlayer.playerName}のアクションフェーズ");
                // プレイヤーの操作を待つだけ（手動で進める）
                break;
                
            case TurnPhase.End:
                Debug.Log($"{currentPlayer.playerName}のエンドフェーズ");
                // ターン終了処理
                EndTurn();
                break;
        }
    }
    
    // ターン終了（アクションフェイズからEndTurnで呼ばれる）
    public void EndTurn()
    {
        Debug.Log($"===== {currentPlayer.playerName}のターン終了 =====");

        // 両プレイヤーのAI行動を停止（安全のため）
        if (currentPlayer is AIPlayer currentAI)
        {
            currentAI.StopActions();
            currentAI.SetPaused(true);
        }
        if (waitingPlayer is AIPlayer waitingAI)
        {
            waitingAI.StopActions();
            waitingAI.SetPaused(true);
        }
        
        // プレイヤー交代
        Player temp = currentPlayer;
        currentPlayer = waitingPlayer;
        waitingPlayer = temp;
        
        Debug.Log($"プレイヤー交代: 次は {currentPlayer.playerName} のターン");
        
        // 少し遅延を入れてから次のターンを開始
        StartCoroutine(DelayedTurnStart());
    }
    
    // ターン開始を遅延させるコルーチン
    private IEnumerator DelayedTurnStart()
    {
        // 通知表示中でも確実に待機するためRealTimeを使用
        yield return new WaitForSecondsRealtime(1.0f);
        
        // ターン処理中フラグをリセット
        isProcessingTurn = false;
        
        // 次のターン開始
        StartTurn(currentPlayer);
    }
    
    // アクションフェイズからエンドフェイズへ
    public void GoToEndPhase()
    {
        if (currentPhase == TurnPhase.Action)
        {
            ChangePhase(TurnPhase.End);
        }
    }

    // 新メソッド: ターン変更時のイベントハンドラ
    public void HandleTurnChanged(Player newTurnPlayer)
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.ResetAttackSelection();
        }
    }
}

// ターンフェイズ
public enum TurnPhase
{
    None,
    Draw,
    Action,
    End
}