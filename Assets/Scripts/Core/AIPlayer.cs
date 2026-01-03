using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AIプレイヤークラス - 最適化された行動ロジック（修正版）
/// </summary>
public class AIPlayer : Player
{
    [Header("AI設定")]
    public bool logDebug = true;
    public float actionDelay = 1.0f;
    public int maxActionsPerTurn = 20;
    
    [Tooltip("攻撃の積極性（0-10、高いほど積極的）")]
    [Range(0, 10)]
    public int aggressiveness = 7;
    
    private bool isProcessingActions = false;
    private bool isPaused = true; // デフォルトでポーズ状態

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        
        if (logDebug) Debug.Log($"[AI] {playerName}: ポーズ状態を {paused} に設定");
    }

    // 現在実行中の行動を全て停止
    public void StopActions()
    {
        StopAllCoroutines();
        isProcessingActions = false;
        isPaused = true;
        
        if (logDebug) Debug.Log($"[AI] {playerName}: すべての行動を停止しました");
    }
        
    /// <summary>
    /// AIの意思決定を行うメソッド
    /// </summary>
    public void MakeDecision()
    {
        // ポーズ中なら行動しない
        if (isPaused)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: ポーズ中のため行動をスキップします");
            return;
        }
        
        if (isProcessingActions)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: すでに行動処理中のため、新たな行動をスキップします");
            return;
        }
        
        // 自分のターンかチェック
        if (GameManager.Instance == null || GameManager.Instance.turnManager == null)
        {
            Debug.LogError("[AI] エラー: GameManagerまたはTurnManagerがnullです");
            return;
        }
        
        if (GameManager.Instance.turnManager.currentPlayer != this)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: 自分のターンではないため行動をスキップします");
            return;
        }

        if (logDebug) Debug.Log($"[AI] {playerName}: 意思決定を開始します");
        
        TurnPhase currentPhase = GameManager.Instance.turnManager.currentPhase;
        
        if (logDebug) Debug.Log($"[AI] {playerName}: フェイズ={currentPhase}での行動を決定します");
        
        switch (currentPhase)
        {
            case TurnPhase.Draw:
                if (logDebug) Debug.Log($"[AI] {playerName}: ドローフェイズ - カードを自動的に引きます");
                break;
                
            case TurnPhase.Action:
                if (!isProcessingActions)
                {
                    isProcessingActions = true;
                    StartCoroutine(PerformActions());
                }
                break;
                
            case TurnPhase.End:
                if (logDebug) Debug.Log($"[AI] {playerName}: エンドフェーズ - ターン終了待機");
                break;
                
            default:
                Debug.LogWarning($"[AI] {playerName}: 不明なフェイズ({currentPhase})です");
                if (GameManager.Instance.turnManager != null)
                {
                    GameManager.Instance.turnManager.ChangePhase(TurnPhase.Action);
                }
                break;
        }
    }
    
    /// <summary>
    /// アクションフェイズでの行動を順次実行するコルーチン
    /// </summary>
    private IEnumerator PerformActions()
    {
        if (logDebug) Debug.Log($"[AI] {playerName}: 行動開始");
        
        float startTime = Time.time;
        float timeoutDuration = 10.0f;
        
        int actionCount = 0;
        bool shouldContinue = true;
        
        while (shouldContinue && actionCount < maxActionsPerTurn)
        {
            // 自分のターンでなくなったら即座に終了
            if (!IsMyActionPhase())
            {
                if (logDebug) Debug.Log($"[AI] {playerName}: 自分のアクションフェイズではないため中断");
                break;
            }
            
            // ポーズ中なら終了
            if (isPaused)
            {
                if (logDebug) Debug.Log($"[AI] {playerName}: ポーズ中のため行動を中断");
                break;
            }
            
            // タイムアウトチェック
            if (Time.time - startTime > timeoutDuration)
            {
                Debug.LogWarning($"[AI] {playerName}: 処理タイムアウト。ターンを強制終了します。");
                break;
            }
            
            actionCount++;
            
            yield return new WaitForSecondsRealtime(actionDelay);
            
            // 再度チェック
            if (!IsMyActionPhase() || isPaused)
            {
                break;
            }
            
            bool actionPerformed = false;
            
            try
            {
                if (!CheckFirstTurnRestriction() && TryOptimalAttacks())
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: 攻撃を実行しました");
                    actionPerformed = true;
                }
                else if (TrySummonStrongestCharacter())
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: 強力なキャラクターを召喚しました");
                    actionPerformed = true;
                }
                else if (TryUseOptimalSpell())
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: 最適なスペルを使用しました");
                    actionPerformed = true;
                }
                else if (TrySummonAnyCharacter())
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: キャラクターを召喚しました");
                    actionPerformed = true;
                }
                else if (TryUseFieldCard())
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: フィールドカードを使用しました");
                    actionPerformed = true;
                }
                else
                {
                    if (logDebug) Debug.Log($"[AI] {playerName}: これ以上の行動はありません");
                    shouldContinue = false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AI] {playerName}: アクション実行中にエラー: {ex.Message}");
                actionPerformed = false;
            }
            
            if (!actionPerformed)
            {
                shouldContinue = false;
            }
        }
        
        // ループ終了後の処理
        yield return new WaitForSecondsRealtime(actionDelay);
        
        isProcessingActions = false;
        
        // まだ自分のアクションフェイズなら終了フェイズへ
        if (IsMyActionPhase() && !isPaused)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: ターン終了処理を実行");
            GameManager.Instance.turnManager.GoToEndPhase();
        }
    }
    
    /// <summary>
    /// 先行1ターン目の攻撃制限をチェック
    /// </summary>
    private bool CheckFirstTurnRestriction()
    {
        if (GameManager.Instance != null && 
            GameManager.Instance.isFirstPlayerFirstTurn && 
            GameManager.Instance.firstPlayer == this)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: 先行1ターン目は攻撃できません");
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 現在の自分のアクションフェーズかをチェック
    /// </summary>
    private bool IsMyActionPhase()
    {
        return GameManager.Instance != null && 
               GameManager.Instance.turnManager != null && 
               GameManager.Instance.turnManager.currentPlayer == this &&
               GameManager.Instance.turnManager.currentPhase == TurnPhase.Action;
    }
    
    /// <summary>
    /// 最適な攻撃を行う
    /// </summary>
    private bool TryOptimalAttacks()
    {
        Debug.Log($"[AI] {playerName}: 攻撃処理を開始します");
        
        Player opponent = GetOpponent();
        if (opponent == null)
        {
            Debug.LogError("[AI] 対戦相手が見つかりません");
            return false;
        }
        
        if (GameManager.Instance != null && 
            GameManager.Instance.isFirstPlayerFirstTurn && 
            GameManager.Instance.firstPlayer == this)
        {
            Debug.Log($"[AI] {playerName}: 先行1ターン目は攻撃できません");
            return false;
        }
        
        List<CharacterCard> attackers = new List<CharacterCard>();
        foreach (CharacterCard card in characterField)
        {
            if (!card.hasAttackedThisTurn)
            {
                attackers.Add(card);
            }
        }
        
        if (attackers.Count == 0)
        {
            Debug.Log("[AI] 攻撃可能なキャラクターがありません");
            return false;
        }
        
        if (opponent.characterField.Count == 0)
        {
            CharacterCard attacker = attackers[0];
            string attackMessage = $"AIの「{attacker.cardName}」が直接攻撃します！";
            
            if (GameManager.Instance?.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowBattleNotification(attackMessage, 2.0f);
            }
            
            return DirectAttack(attacker);
        }
        
        CharacterCard firstAttacker = attackers[0];
        CharacterCard firstTarget = opponent.characterField[0];
        
        string message = $"AIの「{firstAttacker.cardName}」が「{firstTarget.cardName}」に攻撃します！";
        if (GameManager.Instance?.uiManager != null)
        {
            GameManager.Instance.uiManager.ShowBattleNotification(message, 2.0f);
        }
        
        return AttackWithCharacter(firstAttacker, firstTarget);
    }
    
    public override bool AttackWithCharacter(CharacterCard attacker, CharacterCard target)
    {
        if (attacker.hasAttackedThisTurn) return false;
        if (!characterField.Contains(attacker)) return false;
        
        Player opponent = GetOpponent();
        if (!opponent.characterField.Contains(target)) return false;
        
        if (GameManager.Instance != null && 
            GameManager.Instance.isFirstPlayerFirstTurn && 
            GameManager.Instance.firstPlayer == this)
        {
            return false;
        }
        
        if (logDebug) Debug.Log($"[AI] {playerName}: {attacker.cardName}で{target.cardName}に攻撃します");
        
        GameEventInfo attackEvent = new GameEventInfo(
            GameEventType.AttackDeclared,
            this,
            opponent,
            attacker,
            target
        );
        
        GameManager.Instance.TriggerGameEvent(attackEvent);
        BattleSystem.ResolveCharacterBattle(attacker, target);
        attacker.hasAttackedThisTurn = true;
        
        return true;
    }
    
    /// <summary>
    /// 最も強力なキャラクターを召喚する
    /// </summary>
    private bool TrySummonStrongestCharacter()
    {
        List<CharacterCard> playableCharacters = new List<CharacterCard>();
        
        foreach (Card card in hand)
        {
            if (card is CharacterCard characterCard && card.cost <= energy)
            {
                playableCharacters.Add(characterCard);
            }
        }
        
        if (playableCharacters.Count == 0 || characterField.Count >= MAX_CHARACTERS)
        {
            return false;
        }
        
        Dictionary<CharacterCard, int> cardScores = new Dictionary<CharacterCard, int>();
        
        foreach (CharacterCard card in playableCharacters)
        {
            int score = card.attackPower + card.defensePower / 2;
            
            if (card.cost > 0)
            {
                score = score * 10 / card.cost;
            }
            
            if (card.effects != null && card.effects.Count > 0)
            {
                score += 300;
            }
            
            cardScores[card] = score;
        }
        
        CharacterCard bestCard = null;
        int highestScore = -1;
        
        foreach (var kvp in cardScores)
        {
            if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                bestCard = kvp.Key;
            }
        }
        
        if (bestCard == null)
        {
            return false;
        }
        
        if (logDebug) Debug.Log($"[AI] {playerName}: 強力キャラクター {bestCard.cardName} を召喚します");
        
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string effectMessage = $"AIが「{bestCard.cardName}」を召喚します。";
            GameManager.Instance.uiManager.ShowNotification(effectMessage, 3.0f);
        }
        
        return PlayCard(bestCard);
    }
    
    /// <summary>
    /// まだ召喚していないキャラクターがあれば召喚する
    /// </summary>
    private bool TrySummonAnyCharacter()
    {
        List<CharacterCard> playableCharacters = new List<CharacterCard>();
        
        foreach (Card card in hand)
        {
            if (card is CharacterCard characterCard && card.cost <= energy)
            {
                playableCharacters.Add(characterCard);
            }
        }
        
        if (playableCharacters.Count == 0 || characterField.Count >= MAX_CHARACTERS)
        {
            return false;
        }
        
        playableCharacters.Sort((a, b) => b.cost.CompareTo(a.cost));
        
        CharacterCard cardToSummon = playableCharacters[0];
        
        if (logDebug) Debug.Log($"[AI] {playerName}: キャラクター {cardToSummon.cardName} を召喚します");
        
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string effectMessage = $"AIが「{cardToSummon.cardName}」を召喚します。";
            GameManager.Instance.uiManager.ShowNotification(effectMessage, 3.0f);
        }
        
        return PlayCard(cardToSummon);
    }
    
    /// <summary>
    /// 最適なスペルを使用する
    /// </summary>
    private bool TryUseOptimalSpell()
    {
        List<SpellCard> playableSpells = new List<SpellCard>();
        
        foreach (Card card in hand)
        {
            if (card is SpellCard spellCard && card.cost <= energy)
            {
                playableSpells.Add(spellCard);
            }
        }
        
        if (playableSpells.Count == 0)
        {
            return false;
        }
        
        Player opponent = GetOpponent();
        if (opponent == null) return false;
        
        SpellCard bestSpell = null;
        int highestPriority = -1;
        
        foreach (SpellCard spell in playableSpells)
        {
            int priority = EvaluateSpellPriority(spell, opponent);
            
            if (priority > highestPriority)
            {
                highestPriority = priority;
                bestSpell = spell;
            }
        }
        
        int priorityThreshold = 3;
        if (highestPriority < priorityThreshold)
        {
            return false;
        }
        
        if (bestSpell != null)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: スペルカード {bestSpell.cardName} を使用します");
            
            if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
            {
                string effectMessage = $"AIがスペルカード「{bestSpell.cardName}」を使用します。";
                GameManager.Instance.uiManager.ShowNotification(effectMessage, 3.0f);
            }
            
            return PlayCard(bestSpell);
        }
        
        return false;
    }
    
    /// <summary>
    /// スペルの優先度を評価
    /// </summary>
    private int EvaluateSpellPriority(SpellCard spell, Player opponent)
    {
        string cardText = (spell.cardName + " " + spell.description).ToLower();
        
        bool isDestructionSpell = cardText.Contains("破壊") || cardText.Contains("除去");
        if (isDestructionSpell)
        {
            if (opponent.characterField.Count == 0) return 1;
            
            int totalOpponentAttack = 0;
            foreach (CharacterCard card in opponent.characterField)
            {
                totalOpponentAttack += card.attackPower;
            }
            
            if (totalOpponentAttack > 3000) return 10;
            else if (totalOpponentAttack > 2000) return 8;
            else if (totalOpponentAttack > 1000) return 6;
            else return 4;
        }
        
        bool isHealingSpell = cardText.Contains("回復");
        if (isHealingSpell)
        {
            int initialLP = GameManager.Instance != null ? GameManager.Instance.initialLifePoints : 10000;
            float lifeRatio = initialLP > 0 ? (float)lifePoints / initialLP : 0.5f;
            
            if (lifeRatio < 0.3f) return 9;
            else if (lifeRatio < 0.5f) return 7;
            else if (lifeRatio < 0.7f) return 5;
            else return 2;
        }
        
        bool isDrawSpell = cardText.Contains("ドロー") || cardText.Contains("引く");
        if (isDrawSpell)
        {
            if (hand.Count <= 1) return 8;
            else if (hand.Count <= 3) return 6;
            else return 4;
        }
        
        bool isBuffSpell = cardText.Contains("強化") || cardText.Contains("上げる");
        if (isBuffSpell)
        {
            if (characterField.Count == 0) return 1;
            return 3 + characterField.Count;
        }
        
        bool isDebuffSpell = cardText.Contains("弱体") || cardText.Contains("下げる");
        if (isDebuffSpell)
        {
            if (opponent.characterField.Count == 0) return 1;
            return 3 + opponent.characterField.Count;
        }
        
        return spell.cost / 2 + 1;
    }
    
    /// <summary>
    /// フィールドカードの使用を試みる
    /// </summary>
    private bool TryUseFieldCard()
    {
        List<FieldCard> playableFieldCards = new List<FieldCard>();
        
        foreach (Card card in hand)
        {
            if (card is FieldCard fieldCard && card.cost <= energy)
            {
                playableFieldCards.Add(fieldCard);
            }
        }
        
        if (playableFieldCards.Count == 0)
        {
            return false;
        }
        
        int usedFieldSlots = 0;
        foreach (FieldCard card in fieldCards)
        {
            if (card != null) usedFieldSlots++;
        }
        
        if (usedFieldSlots >= fieldCards.Length)
        {
            return false;
        }
        
        System.Func<FieldCard, int> EvaluateFieldCard = (card) => {
            int score = card.cost * 10;
            
            if (card.modifiesStats)
            {
                score += 100;
                score += Mathf.Abs(card.attackModifier) * 5;
                score += Mathf.Abs(card.defenseModifier) * 5;
            }
            
            return score;
        };
        
        playableFieldCards.Sort((a, b) => EvaluateFieldCard(b).CompareTo(EvaluateFieldCard(a)));
        
        FieldCard bestCard = playableFieldCards[0];
        
        int position = -1;
        for (int i = 0; i < fieldCards.Length; i++)
        {
            if (fieldCards[i] == null)
            {
                position = i;
                break;
            }
        }
        
        if (position >= 0)
        {
            if (logDebug) Debug.Log($"[AI] {playerName}: フィールドカード {bestCard.cardName} を位置 {position} に配置します");
            
            if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
            {
                string effectMessage = $"AIがフィールドカード「{bestCard.cardName}」を配置します。";
                GameManager.Instance.uiManager.ShowNotification(effectMessage, 3.0f);
            }
            
            return PlayFieldCard(bestCard, position);
        }
        
        return false;
    }
}