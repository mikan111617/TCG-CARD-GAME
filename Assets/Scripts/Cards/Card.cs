using UnityEngine;

[System.Serializable]
public abstract class Card : ScriptableObject
{
    [Header("カード基本情報")]
    public int id;
    public string cardName;
    public string description;
    
    // アートワークはファイル名を文字列として保存
    public string artwork;
    
    public int cost;
    public CardType type;

    // プロパティをフィールドに変更
    [System.NonSerialized] 
    public Player owner;
    
    [System.NonSerialized] 
    public Player opponent;

    // カード効果の説明文を生成する仮想メソッド
    public virtual string GetEffectDescription()
    {
        return description;
    }
    
    // カードがプレイされたときの処理
    public virtual void OnPlay(Player owner, Player opponent)
    {
        this.owner = owner;
        this.opponent = opponent;
        
        Debug.Log($"{cardName}がプレイされました");
    }
}

public enum CardType
{
    Character,
    Spell,
    Field
}