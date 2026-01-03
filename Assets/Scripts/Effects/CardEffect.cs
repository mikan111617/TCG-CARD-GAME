using UnityEngine;

// カード効果の基底クラス
public abstract class CardEffect : ScriptableObject
{
    public string effectName;
    public string effectDescription;
    
    // 効果発動
    public abstract void ApplyEffect(Player owner, Player opponent);
    
    // 効果の説明テキスト取得
    public virtual string GetDescription()
    {
        return effectDescription;
    }
}

// ドロー効果
[CreateAssetMenu(fileName = "DrawEffect", menuName = "Card Game/Effects/Draw")]
public class DrawEffect : CardEffect
{
    public int drawCount = 1;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        for (int i = 0; i < drawCount; i++)
        {
            owner.DrawCard();
        }
    }
    
    public override string GetDescription()
    {
        return $"{drawCount}枚のカードをドローする。";
    }
}

// ダメージ効果
[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Game/Effects/Damage")]
public class DamageEffect : CardEffect
{
    public int damageAmount = 500;
    public bool targetOpponent = true;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        if (targetOpponent)
        {
            opponent.ChangeLifePoints(-damageAmount);
        }
        else
        {
            owner.ChangeLifePoints(-damageAmount);
        }
    }
    
    public override string GetDescription()
    {
        string target = targetOpponent ? "相手" : "自分";
        return $"{target}プレイヤーに{damageAmount}ダメージを与える。";
    }
}

// 回復効果
[CreateAssetMenu(fileName = "HealEffect", menuName = "Card Game/Effects/Heal")]
public class HealEffect : CardEffect
{
    public int healAmount = 500;
    public bool targetSelf = true;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        if (targetSelf)
        {
            owner.ChangeLifePoints(healAmount);
        }
        else
        {
            opponent.ChangeLifePoints(healAmount);
        }
    }
    
    public override string GetDescription()
    {
        string target = targetSelf ? "自分" : "相手";
        return $"{target}プレイヤーのライフを{healAmount}回復する。";
    }
}

// ステータス変更効果
[CreateAssetMenu(fileName = "StatModifierEffect", menuName = "Card Game/Effects/StatModifier")]
public class StatModifierEffect : CardEffect
{
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public string targetCategory = "";
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        foreach (CharacterCard card in owner.characterField)
        {
            // category プロパティを HasCategoryWithName メソッドに置き換え
            if (string.IsNullOrEmpty(targetCategory) || card.HasCategoryWithName(targetCategory))
            {
                card.ApplyStatBonus(attackBonus, defenseBonus);
            }
        }
    }
    
    public int ModifyAttack(int baseAttack)
    {
        return baseAttack + attackBonus;
    }
    
    public int ModifyDefense(int baseDefense)
    {
        return baseDefense + defenseBonus;
    }
    
    public override string GetDescription()
    {
        string target = string.IsNullOrEmpty(targetCategory) ? "すべての" : targetCategory + "カテゴリーの";
        string effect = "";
        
        if (attackBonus != 0 && defenseBonus != 0)
        {
            effect = $"攻撃力を{attackBonus:+#;-#;0}、守備力を{defenseBonus:+#;-#;0}する";
        }
        else if (attackBonus != 0)
        {
            effect = $"攻撃力を{attackBonus:+#;-#;0}する";
        }
        else if (defenseBonus != 0)
        {
            effect = $"守備力を{defenseBonus:+#;-#;0}する";
        }
        
        return $"{target}キャラクターの{effect}。";
    }
}
// カードを奪う効果（スクリーンショットにあった効果）
[CreateAssetMenu(fileName = "StealCardEffect", menuName = "Card Game/Effects/StealCard")]
public class StealCardEffect : CardEffect
{
    public bool oncePerTurn = true;
    private bool usedThisTurn = false;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        if (oncePerTurn && usedThisTurn) return;
        
        // 相手のキャラクターフィールドを確認
        if (opponent.characterField.Count > 0)
        {
            // 最もコストの高いキャラクターを奪う（例としてシンプルな実装）
            CharacterCard targetCard = null;
            int highestCost = -1;
            
            foreach (CharacterCard card in opponent.characterField)
            {
                if (card.cost > highestCost)
                {
                    targetCard = card;
                    highestCost = card.cost;
                }
            }
            
            if (targetCard != null && owner.characterField.Count < Player.MAX_CHARACTERS)
            {
                // カードを移動
                opponent.characterField.Remove(targetCard);
                owner.characterField.Add(targetCard);
                
                Debug.Log($"{owner.playerName}が{opponent.playerName}の{targetCard.cardName}を奪いました！");
                
                // UIを更新
                GameManager.Instance.uiManager?.UpdateCharacterField(owner);
                GameManager.Instance.uiManager?.UpdateCharacterField(opponent);
                
                // 使用済みフラグを立てる
                usedThisTurn = true;
            }
        }
    }
    
    // ターン終了時にリセット
    public void ResetTurnUsage()
    {
        usedThisTurn = false;
    }
    
    public override string GetDescription()
    {
        return "1ターンに一度、相手のキャラクターカードを奪う。";
    }
}