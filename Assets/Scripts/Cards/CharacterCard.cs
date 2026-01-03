using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static UIManager;

[System.Serializable]
public class CharacterCard : Card
{
    [Header("キャラクター基本情報")]
    public ElementType element;
    public int attackPower;
    public int defensePower;
    public List<CardEffect> effects = new List<CardEffect>();
    // 進化機能を削除
    // public int? evolvesFromId; // 削除
    
    [Header("カテゴリー設定")]
    public List<CardCategory> categories = new List<CardCategory>(); // 最大3つまでのカテゴリー
    
    [Header("戦闘状態")]
    [System.NonSerialized] public bool hasAttackedThisTurn;
    
    // 現在の実効値を表すフィールド
    [System.NonSerialized] private int currentAttackPower;
    [System.NonSerialized] private int currentDefensePower;
    [System.NonSerialized] private int currentHp;
    
    [System.NonSerialized] public bool allowPiercingDamage = false;
    [System.NonSerialized] public bool canCounterAttack = true;

    // コスト削減効果のためのプロパティ
    [System.NonSerialized] 
    public int temporaryCostReduction = 0;
    
    // 効果説明の生成
    public override string GetEffectDescription()
    {
        string description = "";
        
        // カテゴリー情報
        if (categories.Count > 0)
        {
            List<string> categoryNames = new List<string>();
            foreach (var category in categories)
            {
                categoryNames.Add(category.categoryName);
            }
            description += $"【カテゴリー: {string.Join(", ", categoryNames)}】\n";
        }
        
        // 効果説明の追加
        foreach (var effect in effects)
        {
            description += effect.GetDescription() + "\n";
        }
        
        return description.TrimEnd('\n');
    }
    
    // カテゴリー名のリストを取得するヘルパーメソッド
    public List<string> GetCategoryNames()
    {
        List<string> names = new List<string>();
        foreach (var category in categories)
        {
            if (category != null)
                names.Add(category.categoryName);
        }
        return names;
    }
    
    // カテゴリー名を文字列として結合して取得するヘルパーメソッド
    public string GetCategoryDisplayText()
    {
        if (categories.Count == 0)
            return "";
            
        return string.Join(", ", GetCategoryNames());
    }
    
    // フィールドへの配置時処理
    public override void OnPlay(Player owner, Player opponent)
    {
        Debug.Log($"[CharacterCard] {cardName} を {owner.playerName} がプレイしました");
        
        try
        {
            if (effects != null && effects.Count > 0)
            {
                foreach (CardEffect effect in effects)
                {
                    if (effect != null)
                    {
                        // サーチ系効果は除外
                        if (effect is CategorySearchEffect || effect is CategoryGraveyardEffect)
                        {
                            Debug.Log($"[CharacterCard] サーチ効果 {effect.effectName} はスキップされました");
                            continue;
                        }
                        
                        Debug.Log($"[CharacterCard] {cardName} の効果 {effect.effectName} を適用します");
                        effect.ApplyEffect(owner, opponent);
                    }
                }
                
                if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
                {
                    string effectMessage = $"キャラクターカード「{cardName}」の効果が発動しました。";
                    GameManager.Instance.uiManager.ShowNotification(effectMessage, 2.0f, NotificationType.CardAction);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterCard] {cardName} の効果発動中にエラー: {ex.Message}\n{ex.StackTrace}");
        }
    }
    
    // 初期値にリセット
    public void ResetStats()
    {
        currentAttackPower = attackPower;
        currentDefensePower = defensePower;
        currentHp = defensePower;
    }
    
    // 実効攻撃力の取得
    public int GetEffectiveAttackPower()
    {
        int effectiveAttack = currentAttackPower;
        
        if (owner != null && owner.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in owner.activeFieldCards)
            {
                if (fieldCard.modifiesStats)
                {
                    effectiveAttack = fieldCard.ModifyAttack(this, effectiveAttack);
                }
            }
        }
        
        if (opponent != null && opponent.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in opponent.activeFieldCards)
            {
                if (fieldCard.modifiesStats && fieldCard.affectsOpponentField)
                {
                    effectiveAttack = fieldCard.ModifyAttack(this, effectiveAttack);
                }
            }
        }
        
        return Mathf.Max(0, effectiveAttack);
    }
    
    // 実効防御力の取得
    public int GetEffectiveDefensePower()
    {
        int effectiveDefense = currentDefensePower;
        
        if (owner != null && owner.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in owner.activeFieldCards)
            {
                if (fieldCard.modifiesStats)
                {
                    effectiveDefense = fieldCard.ModifyDefense(this, effectiveDefense);
                }
            }
        }
        
        if (opponent != null && opponent.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in opponent.activeFieldCards)
            {
                if (fieldCard.modifiesStats && fieldCard.affectsOpponentField)
                {
                    effectiveDefense = fieldCard.ModifyDefense(this, effectiveDefense);
                }
            }
        }
        
        return Mathf.Max(0, effectiveDefense);
    }
    
    // 他のキャラクターへの攻撃処理
    public void Attack(CharacterCard target)
    {
        if (hasAttackedThisTurn)
            return;
            
        BattleSystem.ResolveCharacterAttack(this, target);
        hasAttackedThisTurn = true;
    }
    
    // プレイヤーへの直接攻撃処理
    public void AttackPlayer(Player target)
    {
        if (hasAttackedThisTurn)
            return;
            
        BattleSystem.ResolveDirectAttack(this, opponent);
        hasAttackedThisTurn = true;
    }
    
    // ダメージを受ける処理
    public bool TakeDamage(int amount, bool isFromBattle = true, bool isFromSpell = false)
    {
        if (isFromBattle && owner != null && owner.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in owner.activeFieldCards)
            {
                if (fieldCard.preventBattleDestruction)
                {
                    Debug.Log($"{this.cardName}は戦闘破壊から保護されています");
                    currentHp -= amount;
                    return false;
                }
            }
        }
        
        if (isFromSpell && owner != null && owner.activeFieldCards != null)
        {
            foreach (FieldCard fieldCard in owner.activeFieldCards)
            {
                if (fieldCard.preventSpellDestruction)
                {
                    Debug.Log($"{this.cardName}はスペル効果による破壊から保護されています");
                    return false;
                }
            }
        }
        
        Debug.Log($"{this.cardName}が{amount}のダメージを受けた");
        currentHp -= amount;
        
        return currentHp <= 0;
    }
    
    // ステータスボーナスを適用するメソッド
    public void ApplyStatBonus(int attackBonus, int defenseBonus)
    {
        currentAttackPower += attackBonus;
        currentDefensePower += defenseBonus;
        
        if (defenseBonus > 0)
        {
            currentHp += defenseBonus;
        }
        
        currentAttackPower = Mathf.Max(0, currentAttackPower);
        currentDefensePower = Mathf.Max(0, currentDefensePower);
        currentHp = Mathf.Max(0, currentHp);
    }
    
    // カード破壊時の効果発動メソッド
    public void OnDestruction()
    {
        if (owner == null || opponent == null)
            return;
            
        Debug.Log($"{this.cardName}が破壊されました");
        
        foreach (var effect in effects)
        {
            if (effect is DestructionEffect destructionEffect)
            {
                destructionEffect.ApplyEffect(owner, opponent);
            }
        }
    }
    
    // 特定のカテゴリーを持っているか確認
    public bool HasCategory(CardCategory category)
    {
        return categories.Contains(category);
    }
    
    // カテゴリー名で検索
    public bool HasCategoryWithName(string categoryName)
    {
        foreach (var category in categories)
        {
            if (category != null && category.categoryName == categoryName)
                return true;
        }
        
        return false;
    }
    
    // カテゴリーの追加（最大3つまで）
    public bool AddCategory(CardCategory category)
    {
        if (categories.Count >= 3 || categories.Contains(category))
            return false;
            
        categories.Add(category);
        return true;
    }
    
    // カテゴリーの削除
    public bool RemoveCategory(CardCategory category)
    {
        return categories.Remove(category);
    }

    // 実効コストを計算するプロパティ
    public int effectiveCost 
    {
        get { return Mathf.Max(0, cost - temporaryCostReduction); }
    }

    // フィールドから離れる時の処理
    public void OnRemoveFromField()
    {
        foreach (CardEffect effect in effects)
        {
            if (effect is CategoryBoostEffect boostEffect)
            {
                boostEffect.RemoveEffect(owner);
            }
            else if (effect is CategoryCostReductionEffect costEffect)
            {
                costEffect.RemoveEffect(this, owner);
            }
        }
        
        Debug.Log($"{this.cardName}がフィールドから離れました");
    }

    // 効果数チェックメソッド（最大2つまでの制限）
    public bool CanAddEffect()
    {
        return effects.Count < 2;
    }

    // 効果を追加するメソッド（最大数チェック付き）
    public bool AddEffect(CardEffect effect)
    {
        if (!CanAddEffect())
            return false;
        
        // サーチ系効果は追加不可
        if (effect is CategorySearchEffect || effect is CategoryGraveyardEffect)
        {
            Debug.LogWarning("サーチ系効果は使用できません");
            return false;
        }
            
        effects.Add(effect);
        return true;
    }
}

public enum ElementType
{
    Fire,
    Water,
    Earth,
    Wind,
    Light,
    Dark,
    Neutral
}