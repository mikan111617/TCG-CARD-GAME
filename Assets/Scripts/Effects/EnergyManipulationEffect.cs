using UnityEngine;

/// <summary>
/// エナジーを増減させる効果
/// </summary>
[CreateAssetMenu(fileName = "EnergyManipulationEffect", menuName = "Card Game/Effects/EnergyManipulation")]
public class EnergyManipulationEffect : CardEffect
{
    [Tooltip("エナジー変動量（正：増加、負：減少）")]
    public int energyChange = 1;
    
    [Tooltip("自分のエナジーを変動させる")]
    public bool affectSelf = true;
    
    [Tooltip("相手のエナジーを変動させる")]
    public bool affectOpponent = false;
    
    public override void ApplyEffect(Player owner, Player opponent)
    {
        if (affectSelf)
        {
            owner.ChangeEnergy(energyChange);
            Debug.Log($"{owner.playerName}のエナジーを{(energyChange > 0 ? "+" : "")}{energyChange}変更");
        }
        
        if (affectOpponent)
        {
            opponent.ChangeEnergy(-energyChange);
            Debug.Log($"{opponent.playerName}のエナジーを{(energyChange > 0 ? "-" : "+")}{energyChange}変更");
        }
    }
    
    public override string GetDescription()
    {
        string description = "";
        
        if (affectSelf)
        {
            description += $"自分のエナジーを{(energyChange > 0 ? "+" : "")}{energyChange}する。";
        }
        
        if (affectOpponent)
        {
            if (!string.IsNullOrEmpty(description))
                description += " ";
                
            description += $"相手のエナジーを{(energyChange > 0 ? "-" : "+")}{energyChange}する。";
        }
        
        return description;
    }
}