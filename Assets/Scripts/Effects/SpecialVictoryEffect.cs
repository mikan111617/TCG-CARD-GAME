using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 特定のカードが5枚フィールドに揃ったときに勝利する効果
/// </summary>
[CreateAssetMenu(fileName = "SpecialVictoryEffect", menuName = "Card Game/Effects/SpecialVictory")]
public class SpecialVictoryEffect : CardEffect
{
    [Tooltip("勝利条件のカード枚数")]
    public int requiredCards = 5;
    
    [Tooltip("必要な特定の効果名")]
    public string requiredEffectName = "特殊勝利効果";
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        CheckVictoryCondition(owner, opponent);
    }
    
    // ターン開始時などに実行する勝利条件チェック
    public void CheckVictoryCondition(Player owner, Player opponent)
    {
        // 特定効果を持つカードの数をチェック
        int count = 0;
        
        foreach (CharacterCard card in owner.characterField)
        {
            foreach (CardEffect effect in card.effects)
            {
                if (effect is SpecialVictoryEffect && effect.effectName == requiredEffectName)
                {
                    count++;
                    break;
                }
            }
        }
        
        // 勝利条件を満たしているかチェック
        if (count >= requiredCards)
        {
            Debug.Log($"特殊勝利条件達成！{owner.playerName}の勝利！");
            
            // ゲーム終了処理
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EndGame(owner, $"特殊勝利条件「{requiredEffectName}」達成");
            }
        }
    }
    
    public override string GetDescription()
    {
        return $"「{requiredEffectName}」効果を持つカードが{requiredCards}枚フィールドに揃うと、ゲームに勝利する。";
    }
}