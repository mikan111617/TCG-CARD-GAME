using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// カードマネージャークラス
public class CardManager : MonoBehaviour
{
    public CardDatabase cardDatabase;
    public List<int> defaultDeckIds; // デフォルトデッキのカードID

    public void LoadCardDatabase()
    {
        // カードデータベースのロード（ScriptableObjectから）
        cardDatabase = Resources.Load<CardDatabase>("CardDatabase");
    }

    public void PrepareDeck(Player player)
    {
        // デフォルトデッキを準備
        foreach (int cardId in defaultDeckIds)
        {
            Card cardCopy = Instantiate(cardDatabase.GetCardById(cardId));
            if (cardCopy != null)
            {
                player.deck.Add(cardCopy);
            }
        }
        
        // デッキをシャッフル
        ShuffleDeck(player.deck);
    }

    private void ShuffleDeck(List<Card> deck)
    {
        // Fisher-Yatesアルゴリズムでデッキをシャッフル
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
}