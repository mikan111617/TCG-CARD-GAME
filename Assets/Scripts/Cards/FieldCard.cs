using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class FieldCard : Card
{
    [Header("フィールドカード基本設定")]
    public List<CardEffect> effects = new List<CardEffect>(); // 最大3つまで効果を持てる
    public List<CardCategory> affectedCategories = new List<CardCategory>(); // 影響を与えるカテゴリー
    public List<ElementType> affectedElements = new List<ElementType>(); // 影響を与える属性

    [Header("効果適用範囲")]
    public bool affectsOwnField = true; // 自分のフィールドに影響するか
    public bool affectsOpponentField = false; // 相手のフィールドに影響するか

    [Header("能力修正設定（バフ/デバフ）")]
    public bool modifiesStats = false; // ステータス修正を行うか
    public int attackModifier = 0; // 攻撃力修正値（正負両方可能）
    public int defenseModifier = 0; // 防御力修正値（正負両方可能）

    // 以下の効果は削除
    // [Header("カード移動効果")]
    // public bool allowsDeckSearch = false;
    // public bool allowsGraveyardRecovery = false;
    // public bool oncePerTurn = true;
    // private bool usedThisTurn = false;
    
    // [Header("視覚効果")]
    // public bool revealOpponentHand = false;
    
    // [Header("保護効果")]
    // public bool preventBattleDestruction = false;
    // public bool preventSpellDestruction = false;
    
    // [Header("ライフ効果")]
    // public bool providesLifeRecovery = false;
    // public int lifeRecoveryAmount = 0;

    // 互換性のために残すが、常にfalseを返す
    [System.NonSerialized]
    public bool preventBattleDestruction = false;
    [System.NonSerialized]
    public bool preventSpellDestruction = false;
    
    // このフィールドカードの効果説明を生成
    public override string GetEffectDescription()
    {
        string description = "";
        
        // ステータス修正効果（バフ/デバフ）のみ表示
        if (modifiesStats)
        {
            string targets = GetTargetDescription();
            
            if (attackModifier != 0 && defenseModifier != 0)
            {
                description += $"{targets}の攻撃力を{FormatModifier(attackModifier)}、防御力を{FormatModifier(defenseModifier)}する。\n";
            }
            else if (attackModifier != 0)
            {
                description += $"{targets}の攻撃力を{FormatModifier(attackModifier)}する。\n";
            }
            else if (defenseModifier != 0)
            {
                description += $"{targets}の防御力を{FormatModifier(defenseModifier)}する。\n";
            }
        }
        
        // 登録されている効果の説明を追加（バフ/デバフ系のみ）
        foreach (var effect in effects)
        {
            if (effect is StatModifierEffect || effect is CategoryBoostEffect)
            {
                description += effect.GetDescription() + "\n";
            }
        }
        
        return description.TrimEnd('\n');
    }
    
    // 修正値の表示形式を整える（+500、-300など）
    private string FormatModifier(int modifier)
    {
        return modifier > 0 ? $"+{modifier}" : modifier.ToString();
    }
    
    // 対象の説明を生成（カテゴリーと属性の組み合わせ）
    private string GetTargetDescription()
    {
        List<string> targetParts = new List<string>();
        
        if (affectedCategories.Count > 0)
        {
            List<string> categoryNames = new List<string>();
            foreach (var category in affectedCategories)
            {
                categoryNames.Add(category.categoryName);
            }
            targetParts.Add(string.Join("/", categoryNames) + "カテゴリー");
        }
        
        if (affectedElements.Count > 0)
        {
            List<string> elementNames = new List<string>();
            foreach (var element in affectedElements)
            {
                elementNames.Add(element.ToString());
            }
            targetParts.Add(string.Join("/", elementNames) + "属性");
        }
        
        if (targetParts.Count == 0)
        {
            return "すべて";
        }
        
        return string.Join("または", targetParts) + "の";
    }
    
    // 攻撃力を修正
    public int ModifyAttack(CharacterCard character, int currentAttack)
    {
        if (!ShouldAffectCard(character))
        {
            return currentAttack;
        }
        
        int modifiedAttack = currentAttack + attackModifier;
        return Mathf.Max(0, modifiedAttack);
    }
    
    // 防御力を修正
    public int ModifyDefense(CharacterCard character, int currentDefense)
    {
        if (!ShouldAffectCard(character))
        {
            return currentDefense;
        }
        
        int modifiedDefense = currentDefense + defenseModifier;
        return Mathf.Max(0, modifiedDefense);
    }
    
    // カードに影響を与えるべきか判定
    private bool ShouldAffectCard(CharacterCard character)
    {
        bool isOwnCard = character.owner == this.owner;
        
        if (isOwnCard && !affectsOwnField)
            return false;
            
        if (!isOwnCard && !affectsOpponentField)
            return false;
        
        // カテゴリーチェック
        if (affectedCategories.Count > 0)
        {
            bool hasMatchingCategory = false;
            
            foreach (var category in affectedCategories)
            {
                if (character.HasCategory(category))
                {
                    hasMatchingCategory = true;
                    break;
                }
            }
            
            if (!hasMatchingCategory)
                return false;
        }
        
        // 属性チェック
        if (affectedElements.Count > 0 && !affectedElements.Contains(character.element))
        {
            return false;
        }
        
        return true;
    }
    
    // ターン終了時のリセット（互換性のため残す）
    public void ResetTurnUsage()
    {
        // バフ/デバフ効果のみのため、特に処理なし
    }
    
    public override void OnPlay(Player owner, Player opponent)
    {
        Debug.Log($"[FieldCard] {cardName} を {owner.playerName} が配置しました");
        
        try
        {
            if (effects != null && effects.Count > 0)
            {
                foreach (CardEffect effect in effects)
                {
                    if (effect != null)
                    {
                        // バフ/デバフ系効果のみ発動
                        if (effect is StatModifierEffect || effect is CategoryBoostEffect)
                        {
                            Debug.Log($"[FieldCard] {cardName} の効果 {effect.effectName} を適用します");
                            effect.ApplyEffect(owner, opponent);
                        }
                        else
                        {
                            Debug.Log($"[FieldCard] {cardName} の効果 {effect.effectName} はバフ/デバフではないためスキップ");
                        }
                    }
                }
                
                if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
                {
                    string effectMessage = $"フィールドカード「{cardName}」の効果が発動しました。\n{description}";
                    GameManager.Instance.uiManager.ShowFieldCardNotification(effectMessage, 2.5f);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FieldCard] {cardName} の効果発動中にエラー: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // カード効果を再適用
    public void ReapplyEffects()
    {
        if (owner == null || opponent == null)
            return;
            
        foreach (var effect in effects)
        {
            if (effect is StatModifierEffect || effect is CategoryBoostEffect)
            {
                effect.ApplyEffect(owner, opponent);
            }
        }
    }
    
    // カード除去時の処理
    public void OnRemove()
    {
        if (owner == null || opponent == null)
            return;

        foreach (var effect in effects)
        {
            if (effect is RemovalEffect removalEffect)
            {
                removalEffect.ApplyEffect(owner, opponent);
            }
        }
    }
}