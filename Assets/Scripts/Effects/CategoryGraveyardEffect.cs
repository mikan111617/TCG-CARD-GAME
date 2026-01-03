using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// カテゴリーに基づいて捨て札からカードを手札に加える効果
/// </summary>
[CreateAssetMenu(fileName = "CategoryGraveyardEffect", menuName = "Card Game/Effects/CategoryGraveyard")]
public class CategoryGraveyardEffect : CardEffect
{
    [Tooltip("検索するカテゴリー。空の場合はカード自身のカテゴリーを使用")]
    public CardCategory targetCategory;
    
    [Tooltip("手札に加えるカードの最大枚数")]
    public int maxCards = 1;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        CharacterCard characterCard = null;
        
        // このエフェクトがキャラクターカードに付与されている場合
        if (owner.lastPlayedCard is CharacterCard)
        {
            characterCard = (CharacterCard)owner.lastPlayedCard;
        }
        
        // 検索対象のカテゴリーを決定
        List<CardCategory> categoriesToSearch = new List<CardCategory>();
        
        if (targetCategory != null)
        {
            categoriesToSearch.Add(targetCategory);
        }
        else if (characterCard != null && characterCard.categories.Count > 0)
        {
            categoriesToSearch.AddRange(characterCard.categories);
        }
        
        if (categoriesToSearch.Count == 0)
        {
            Debug.Log("検索するカテゴリーがありません");
            return;
        }
        
        // 捨て札からカードを検索
        List<Card> foundCards = new List<Card>();
        
        foreach (Card card in owner.graveyard)
        {
            if (foundCards.Count >= maxCards)
                break;
                
            if (card is CharacterCard charCard)
            {
                // カテゴリーの一致を確認
                foreach (CardCategory category in categoriesToSearch)
                {
                    if (charCard.HasCategory(category))
                    {
                        foundCards.Add(card);
                        break;
                    }
                }
            }
        }
        
        // 見つかったカードを手札に加える
        foreach (Card card in foundCards)
        {
            owner.graveyard.Remove(card);
            owner.hand.Add(card);
            Debug.Log($"{card.cardName}を捨て札から手札に加えました");
        }
        
        // UI更新
        if (foundCards.Count > 0 && GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdatePlayerHand(owner);
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
        
        return $"このカードがフィールドに出た時、捨て札から{categoryText}のカードを{maxCards}枚手札に加える。";
    }
}