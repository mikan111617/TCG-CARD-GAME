using UnityEngine;

[CreateAssetMenu(fileName = "DrawEffect", menuName = "Card Game/Effects/Draw")]
public class DrawCardEffect : CardEffect
{
    public int drawCount;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        for (int i = 0; i < drawCount; i++)
        {
            owner.DrawCard();
        }
    }
    
    public override string GetDescription()
    {
        return drawCount + "枚のカードをドローする。";
    }
}