using UnityEngine;

[CreateAssetMenu(fileName = "AttackTargetEffect", menuName = "Card Game/Effects/AttackTarget")]
public class AttackTargetEffect : CardEffect
{
    public override void ApplyEffect(Player owner, Player opponent)
    {
        // 実装（サンプルスクリーンショットのカード効果）
        Debug.Log("相手のキャラクターカードを奪う効果を発動");
    }
    
    public override string GetDescription()
    {
        return "1ターンに一度、\n相手のキャラクターカードを奪う。";
    }
}