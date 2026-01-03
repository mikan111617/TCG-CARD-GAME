using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 同じカテゴリーを持つフィールド上のカードの攻撃力・守備力を上げる効果
/// </summary>
[CreateAssetMenu(fileName = "CategoryBoostEffect", menuName = "Card Game/Effects/CategoryBoost")]
public class CategoryBoostEffect : CardEffect
{
    [Tooltip("強化するカテゴリー。空の場合はカード自身のカテゴリーを使用")]
    public CardCategory targetCategory;
    
    [Tooltip("攻撃力の上昇値")]
    public int attackBoost = 500;
    
    [Tooltip("守備力の上昇値")]
    public int defenseBoost = 500;
    
    // 強化したカードを追跡するためのリスト（フィールドを離れる時に効果を解除するため）
    private List<CharacterCard> boostedCards = new List<CharacterCard>();
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        CharacterCard sourceCard = null;
        
        // このエフェクトがキャラクターカードに付与されている場合
        if (owner.lastPlayedCard is CharacterCard)
        {
            sourceCard = (CharacterCard)owner.lastPlayedCard;
        }
        
        // 強化対象のカテゴリーを決定
        List<CardCategory> categoriesToBoost = new List<CardCategory>();
        
        if (targetCategory != null)
        {
            categoriesToBoost.Add(targetCategory);
        }
        else if (sourceCard != null && sourceCard.categories.Count > 0)
        {
            categoriesToBoost.AddRange(sourceCard.categories);
        }
        
        if (categoriesToBoost.Count == 0)
        {
            Debug.Log("強化するカテゴリーがありません");
            return;
        }
        
        // フィールド上の対象カテゴリーのカードを強化
        foreach (CharacterCard card in owner.characterField)
        {
            // 自分自身は除外（オプション）
            if (card == sourceCard)
                continue;
                
            // カテゴリーの一致を確認
            bool shouldBoost = false;
            foreach (CardCategory category in categoriesToBoost)
            {
                if (card.HasCategory(category))
                {
                    shouldBoost = true;
                    break;
                }
            }
            
            if (shouldBoost)
            {
                card.ApplyStatBonus(attackBoost, defenseBoost);
                boostedCards.Add(card);
                Debug.Log($"{card.cardName}のステータスを強化: 攻撃力+{attackBoost}, 守備力+{defenseBoost}");
            }
        }
    }
    
    // カードがフィールドから離れる時に効果を解除
    public void RemoveEffect(Player owner)
    {
        foreach (CharacterCard card in boostedCards)
        {
            if (owner.characterField.Contains(card))
            {
                card.ApplyStatBonus(-attackBoost, -defenseBoost);
                Debug.Log($"{card.cardName}の強化効果を解除");
            }
        }
        
        boostedCards.Clear();
    }
    
    public override string GetDescription()
    {
        string categoryText;
        
        if (targetCategory != null)
        {
            categoryText = targetCategory.categoryName;
        }
        else
        {
            categoryText = "このカードと同じカテゴリー";
        }
        
        return $"フィールド上の{categoryText}を持つすべてのキャラクターの攻撃力を{attackBoost}、守備力を{defenseBoost}上げる。このカードがフィールドを離れるまで持続。";
    }
}