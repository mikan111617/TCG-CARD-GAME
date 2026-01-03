using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public partial class SpellCard : Card
{
    // スペル発動可能なタイミング
    [SerializeField]
    private List<GameEventType> activationTiming = new List<GameEventType>();

    public SpellType spellType;
    public int effectValue;
    public List<CardEffect> effects = new List<CardEffect>();

    // このスペルが特定のフィールド位置を必要とするかどうか
    public bool requiresPosition { get; set; } = false;
    
    public override void OnPlay(Player owner, Player opponent)
    {
        Debug.Log($"[SpellCard] {cardName} を {owner.playerName} が発動しました");
     
        try
        {
            if (effects != null && effects.Count > 0)
            {
                foreach (CardEffect effect in effects)
                {
                    if (effect != null)
                    {
                        Debug.Log($"[SpellCard] {cardName} の効果 {effect.effectName} を適用します");
                        effect.ApplyEffect(owner, opponent);
                    }
                }
                
                if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
                {
                    string effectMessage = $"スペルカード「{cardName}」の効果が発動しました。\n{description}";
                    GameManager.Instance.uiManager.ShowSpellNotification(effectMessage, 2.5f);
                }
            }
            else
            {
                Debug.Log($"[SpellCard] {cardName} に設定された効果がありません");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SpellCard] {cardName} の効果発動中にエラー: {ex.Message}\n{ex.StackTrace}");
        }
        
        // スペルカードは使用後に墓地へ
        owner.graveyard.Add(this);
    }

    // このスペルが発動可能なタイミングか判定
    public bool CanActivateOn(GameEventType eventType)
    {
        return activationTiming.Contains(eventType);
    }
    
    // 相手ターンに発動可能か
    public bool canActivateOnOpponentTurn = false;
    
}

public enum SpellType
{
    Draw,
    Buff,
    Debuff,
    LifeDamage,
    LifeHeal,
    Resurrection,
    // DeckDestruction, // 削除
    CardDestruction,
    HandDestruction
}