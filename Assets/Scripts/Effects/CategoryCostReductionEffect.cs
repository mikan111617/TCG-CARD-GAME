using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 手札にある同じカテゴリーのキャラクターカードのコストを減らす効果
/// </summary>
[CreateAssetMenu(fileName = "CategoryCostReductionEffect", menuName = "Card Game/Effects/CategoryCostReduction")]
public class CategoryCostReductionEffect : CardEffect
{
    [Tooltip("コスト削減するカテゴリー。空の場合はカード自身のカテゴリーを使用")]
    public CardCategory targetCategory;
    
    [Tooltip("削減するコスト")]
    public int costReduction = 1;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        // 直接効果を適用しない
        // 永続効果なので、カードがフィールドに残っている間は
        // ターン開始時やハンド更新時にApplyTurnEffectが呼ばれる必要がある
    }
    
    // ターン開始時やハンド更新時に呼び出される効果
    public void ApplyTurnEffect(CharacterCard sourceCard, Player owner)
    {
        if (sourceCard == null || owner == null)
            return;
            
        // コスト削減対象のカテゴリーを決定
        List<CardCategory> categoriesToReduce = new List<CardCategory>();
        
        if (targetCategory != null)
        {
            categoriesToReduce.Add(targetCategory);
        }
        else if (sourceCard.categories.Count > 0)
        {
            categoriesToReduce.AddRange(sourceCard.categories);
        }
        
        if (categoriesToReduce.Count == 0)
            return;
            
        // 手札の対象カテゴリーのカードのコストを削減
        foreach (Card card in owner.hand)
        {
            if (card is CharacterCard charCard)
            {
                // カテゴリーの一致を確認
                foreach (CardCategory category in categoriesToReduce)
                {
                    if (charCard.HasCategory(category))
                    {
                        // 一時的なコスト削減（実際のコストは変更せず、表示や使用時のコストチェックで使用）
                        charCard.temporaryCostReduction = Mathf.Max(charCard.temporaryCostReduction, costReduction);
                        break;
                    }
                }
            }
        }
    }
    
    // 効果の解除（ソースカードがフィールドを離れる時）
    public void RemoveEffect(CharacterCard sourceCard, Player owner)
    {
        if (owner == null)
            return;
            
        // すべての手札のカードのコスト削減をリセット
        // 注意: 他のカードからの効果もリセットされるので、実際には全カードの効果を再適用する必要がある
        foreach (Card card in owner.hand)
        {
            if (card is CharacterCard charCard)
            {
                charCard.temporaryCostReduction = 0;
            }
        }
        
        // 他のすべてのコスト削減効果を再適用
        foreach (CharacterCard fieldCard in owner.characterField)
        {
            if (fieldCard == sourceCard)
                continue;
                
            foreach (CardEffect effect in fieldCard.effects)
            {
                if (effect is CategoryCostReductionEffect costEffect)
                {
                    costEffect.ApplyTurnEffect(fieldCard, owner);
                }
            }
        }
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
        
        return $"手札にある{categoryText}のキャラクターカードのコストを{costReduction}減らす。このカードがフィールドにいる限り有効。";
    }
}