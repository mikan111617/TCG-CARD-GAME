using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// カードデータベースクラス - ScriptableObjectを継承
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Card Game/Card Database")]
public class CardDatabase : ScriptableObject
{
    // シリアライズ可能な各カードタイプのリスト
    [SerializeField] private List<CharacterCard> characterCards = new List<CharacterCard>();
    [SerializeField] private List<SpellCard> spellCards = new List<SpellCard>();
    [SerializeField] private List<FieldCard> fieldCards = new List<FieldCard>();
    
    // 全カードのリストを取得
    public List<Card> GetAllCards()
    {
        List<Card> allCards = new List<Card>();
        
        // 各カードタイプを結合
        allCards.AddRange(characterCards.Cast<Card>());
        allCards.AddRange(spellCards.Cast<Card>());
        allCards.AddRange(fieldCards.Cast<Card>());
        
        return allCards;
    }
    
    // IDによるカード検索
    public Card GetCardById(int id)
    {
        // キャラクターカードを検索
        foreach (CharacterCard card in characterCards)
        {
            if (card.id == id) return card;
        }
        
        // スペルカードを検索
        foreach (SpellCard card in spellCards)
        {
            if (card.id == id) return card;
        }
        
        // フィールドカードを検索
        foreach (FieldCard card in fieldCards)
        {
            if (card.id == id) return card;
        }
        
        Debug.LogWarning("ID: " + id + " のカードが見つかりません");
        return null;
    }
    
    // 名前によるカード検索
    public Card GetCardByName(string name)
    {
        // キャラクターカードを検索
        foreach (CharacterCard card in characterCards)
        {
            if (card.cardName == name) return card;
        }
        
        // スペルカードを検索
        foreach (SpellCard card in spellCards)
        {
            if (card.cardName == name) return card;
        }
        
        // フィールドカードを検索
        foreach (FieldCard card in fieldCards)
        {
            if (card.cardName == name) return card;
        }
        
        Debug.LogWarning("名前: " + name + " のカードが見つかりません");
        return null;
    }
    
    // 属性によるキャラクターカード検索
    public List<CharacterCard> GetCharacterCardsByElement(ElementType element)
    {
        List<CharacterCard> result = new List<CharacterCard>();
        
        foreach (CharacterCard card in characterCards)
        {
            if (card.element == element)
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // スペルタイプによるスペルカード検索
    public List<SpellCard> GetSpellCardsByType(SpellType spellType)
    {
        List<SpellCard> result = new List<SpellCard>();
        
        foreach (SpellCard card in spellCards)
        {
            if (card.spellType == spellType)
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // カテゴリーによるキャラクターカード検索 - 修正済み
    public List<CharacterCard> GetCharacterCardsByCategory(string categoryName)
    {
        List<CharacterCard> result = new List<CharacterCard>();
        
        foreach (CharacterCard card in characterCards)
        {
            // category プロパティの代わりに HasCategoryWithName メソッドを使用
            if (card.HasCategoryWithName(categoryName))
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // コスト範囲によるカード検索
    public List<Card> GetCardsByCostRange(int minCost, int maxCost)
    {
        List<Card> result = new List<Card>();
        
        foreach (Card card in GetAllCards())
        {
            if (card.cost >= minCost && card.cost <= maxCost)
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // 攻撃力範囲によるキャラクターカード検索
    public List<CharacterCard> GetCharacterCardsByAttackRange(int minAttack, int maxAttack)
    {
        List<CharacterCard> result = new List<CharacterCard>();
        
        foreach (CharacterCard card in characterCards)
        {
            if (card.attackPower >= minAttack && card.attackPower <= maxAttack)
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // 防御力範囲によるキャラクターカード検索
    public List<CharacterCard> GetCharacterCardsByDefenseRange(int minDefense, int maxDefense)
    {
        List<CharacterCard> result = new List<CharacterCard>();
        
        foreach (CharacterCard card in characterCards)
        {
            if (card.defensePower >= minDefense && card.defensePower <= maxDefense)
            {
                result.Add(card);
            }
        }
        
        return result;
    }
    
    // カードデータベースにカードを追加
    public void AddCard(Card card)
    {
        if (card is CharacterCard characterCard)
        {
            characterCards.Add(characterCard);
        }
        else if (card is SpellCard spellCard)
        {
            spellCards.Add(spellCard);
        }
        else if (card is FieldCard fieldCard)
        {
            fieldCards.Add(fieldCard);
        }
    }
    
    // JSONからカードデータをロード
    public void LoadFromJson(string json)
    {
        // JSON形式のデータをデシリアライズする処理
        CardDatabaseData data = JsonUtility.FromJson<CardDatabaseData>(json);
        
        if (data != null)
        {
            // JSONから読み込んだデータでリストを初期化
            characterCards = data.characterCards;
            spellCards = data.spellCards;
            fieldCards = data.fieldCards;
        }
    }
    
    // カードデータをJSONに変換
    public string SaveToJson()
    {
        // データベース内容をシリアライズ可能な形式に変換
        CardDatabaseData data = new CardDatabaseData
        {
            characterCards = characterCards,
            spellCards = spellCards,
            fieldCards = fieldCards
        };
        
        // JSON形式にシリアライズ
        return JsonUtility.ToJson(data, true);
    }

    // CardDatabaseクラスに追加するメソッド
    public void RemoveCard(Card card)
    {
        if (card is CharacterCard characterCard)
        {
            characterCards.Remove(characterCard);
        }
        else if (card is SpellCard spellCard)
        {
            spellCards.Remove(spellCard);
        }
        else if (card is FieldCard fieldCard)
        {
            fieldCards.Remove(fieldCard);
        }
    }
}