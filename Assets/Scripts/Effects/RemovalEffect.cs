using UnityEngine;

/// <summary>
/// フィールドカードが除去された時に発動する効果
/// </summary>
[CreateAssetMenu(fileName = "RemovalEffect", menuName = "Card Game/Effects/RemovalEffect")]
public class RemovalEffect : CardEffect
{
    public EffectType effectType = EffectType.None;
    public int effectValue = 0;
    
    public enum EffectType
    {
        None,
        DrawCard,
        DamageOpponent,
        RecoverLife,
        DiscardOpponentCard,
        ReturnCardFromGraveyard
    }
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        switch (effectType)
        {
            case EffectType.DrawCard:
                // カードをドロー
                for (int i = 0; i < effectValue; i++)
                {
                    owner.DrawCard();
                }
                Debug.Log($"{owner.playerName}はカードを{effectValue}枚引きました");
                break;
                
            case EffectType.DamageOpponent:
                // 相手にダメージ
                opponent.ChangeLifePoints(-effectValue);
                Debug.Log($"{opponent.playerName}に{effectValue}ダメージ");
                break;
                
            case EffectType.RecoverLife:
                // ライフ回復
                owner.ChangeLifePoints(effectValue);
                Debug.Log($"{owner.playerName}は{effectValue}ライフを回復した");
                break;
                
            case EffectType.DiscardOpponentCard:
                // 相手のカードを破棄
                if (opponent.hand.Count > 0 && effectValue > 0)
                {
                    int discardCount = Mathf.Min(effectValue, opponent.hand.Count);
                    for (int i = 0; i < discardCount; i++)
                    {
                        // ランダムに選ぶ
                        int randomIndex = Random.Range(0, opponent.hand.Count);
                        Card discardedCard = opponent.hand[randomIndex];
                        opponent.hand.RemoveAt(randomIndex);
                        opponent.graveyard.Add(discardedCard);
                    }
                    Debug.Log($"{opponent.playerName}のカードを{discardCount}枚捨てさせた");
                    
                    // UI更新
                    GameManager.Instance.uiManager?.UpdatePlayerHand(opponent);
                }
                break;
                
            case EffectType.ReturnCardFromGraveyard:
                // 墓地からカードを手札に戻す
                if (owner.graveyard.Count > 0 && effectValue > 0)
                {
                    // 実装: 墓地からカードを選ぶUIを表示
                    Debug.Log($"{owner.playerName}は墓地からカードを最大{effectValue}枚手札に戻せる");
                    
                    // 仮実装：ランダムに戻す
                    int returnCount = Mathf.Min(effectValue, owner.graveyard.Count);
                    for (int i = 0; i < returnCount; i++)
                    {
                        int randomIndex = Random.Range(0, owner.graveyard.Count);
                        Card returnedCard = owner.graveyard[randomIndex];
                        owner.graveyard.RemoveAt(randomIndex);
                        owner.hand.Add(returnedCard);
                    }
                    
                    // UI更新
                    GameManager.Instance.uiManager?.UpdatePlayerHand(owner);
                }
                break;
                
            case EffectType.None:
            default:
                // 効果なし
                break;
        }
    }
    
    public override string GetDescription()
    {
        switch (effectType)
        {
            case EffectType.DrawCard:
                return $"このカードが除去された時、カードを{effectValue}枚引く。";
                
            case EffectType.DamageOpponent:
                return $"このカードが除去された時、相手に{effectValue}ダメージを与える。";
                
            case EffectType.RecoverLife:
                return $"このカードが除去された時、ライフを{effectValue}回復する。";
                
            case EffectType.DiscardOpponentCard:
                return $"このカードが除去された時、相手は手札を{effectValue}枚捨てる。";
                
            case EffectType.ReturnCardFromGraveyard:
                return $"このカードが除去された時、墓地からカードを最大{effectValue}枚手札に加える。";
                
            case EffectType.None:
            default:
                return "このカードが除去された時の効果はない。";
        }
    }
}