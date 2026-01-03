using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DestructionEffect : CardEffect
{
    public override void ApplyEffect(Player owner, Player opponent)
    {
        // 破壊効果の実装
        // 例: カードドロー、ダメージ、特殊効果など
    }
    
    public override string GetDescription()
    {
        return "このカードが破壊された時、特殊効果が発動します。";
    }
}