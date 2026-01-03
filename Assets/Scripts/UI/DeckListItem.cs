using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// デッキリスト内の各デッキアイテムを表示するクラス
/// </summary>
public class DeckListItem : MonoBehaviour
{
    [Header("UI参照")]
    public TextMeshProUGUI deckNameText;     // デッキ名テキスト
    public TextMeshProUGUI cardCountText;    // カード枚数テキスト
    public Image deckCoverImage;             // デッキカバー画像
    public Image selectionIndicator;         // 選択中表示用インジケーター
    
    // このアイテムが表すデッキ情報
    private DeckData deckData;
    private bool isSelected = false;
    
    /// <summary>
    /// デッキアイテムの初期化
    /// </summary>
    public void SetupDeckItem(DeckData deck)
    {
        this.deckData = deck;
        
        // デッキ名表示
        if (deckNameText != null)
        {
            deckNameText.text = deck.deckName;
        }
        
        // カード枚数表示
        if (cardCountText != null)
        {
            int minDeckSize = DeckDataManager.Instance != null ? DeckDataManager.Instance.minDeckSize : 40;
            int maxDeckSize = DeckDataManager.Instance != null ? DeckDataManager.Instance.maxDeckSize : 60;
            
            cardCountText.text = $"{deck.cardIds.Count}/{maxDeckSize}";
            
            // 枚数が不足している場合は赤色に
            cardCountText.color = (deck.cardIds.Count < minDeckSize) ? Color.red : Color.white;
        }
        
        // デッキカバー画像の設定
        if (deckCoverImage != null && !string.IsNullOrEmpty(deck.coverCardId))
        {
            // カバーカードのIDからカードを取得
            CardDatabase database = Resources.Load<CardDatabase>("CardDatabase");
            if (database != null)
            {
                int cardId;
                if (int.TryParse(deck.coverCardId, out cardId))
                {
                    Card coverCard = database.GetCardById(cardId);
                    if (coverCard != null && !string.IsNullOrEmpty(coverCard.artwork))
                    {
                        // artworkが文字列(ファイル名)になったため、Spriteをロードする
                        Sprite artworkSprite = LoadCardSprite(coverCard);
                        if (artworkSprite != null)
                        {
                            deckCoverImage.sprite = artworkSprite;
                            deckCoverImage.color = Color.white;
                        }
                        else
                        {
                            Debug.LogWarning($"カバーカードのアートワーク {coverCard.artwork} をロードできませんでした");
                        }
                    }
                }
            }
        }
        
        // 選択状態を更新
        UpdateSelectionState();
    }
    
    /// <summary>
    /// カードスプライトをロードするヘルパーメソッド
    /// </summary>
    private Sprite LoadCardSprite(Card card)
    {
        if (card == null || string.IsNullOrEmpty(card.artwork)) return null;
        
        // 検索する候補パスのリスト
        string[] pathCandidates = new string[]
        {
            $"CardImages/character/{card.artwork}",
            $"CardImages/{card.artwork}", 
            $"CardImages/Cards/{card.artwork}",
            $"CardImages/All/{card.artwork}"
        };
        
        // 各候補パスを試す
        foreach (string path in pathCandidates)
        {
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
        }
        
        // 見つからなかった場合はnull
        Debug.LogWarning($"カード {card.cardName}（ID: {card.id}）の画像 {card.artwork} が見つかりませんでした");
        return null;
    }
    
    /// <summary>
    /// このデッキが選択状態かどうかを設定
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSelectionState();
    }
    
    /// <summary>
    /// 選択状態のUI表示を更新
    /// </summary>
    private void UpdateSelectionState()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.gameObject.SetActive(isSelected);
        }
        
        // 選択中は背景色を変更するなど
        Image backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? new Color(0.8f, 0.9f, 1f, 1f) : Color.white;
        }
    }
    
    /// <summary>
    /// このデッキのIDを取得
    /// </summary>
    public int GetDeckId()
    {
        return deckData != null ? deckData.deckId : -1;
    }
    
    /// <summary>
    /// このデッキのデータを取得
    /// </summary>
    public DeckData GetDeckData()
    {
        return deckData;
    }
    
    /// <summary>
    /// このデッキの名前を取得
    /// </summary>
    public string GetDeckName()
    {
        return deckData != null ? deckData.deckName : "";
    }
}