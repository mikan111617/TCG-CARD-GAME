using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// カード詳細情報を表示するパネル
/// </summary>
public class CardDetailPanel : MonoBehaviour
{
    [Header("カード情報")]
    public Image cardImage;               // カード画像
    public TextMeshProUGUI cardNameText;  // カード名
    public TextMeshProUGUI cardTypeText;  // カードタイプ
    public TextMeshProUGUI cardDescriptionText;  // カード説明
    public TextMeshProUGUI cardEffectText;       // カード効果
    public TextMeshProUGUI cardStatsText;        // カードステータス
    public TextMeshProUGUI cardCountText;        // カード枚数
    
    [Header("ボタン")]
    public Button addToDeckButton;     // デッキに追加ボタン
    public Button removeFromDeckButton; // デッキから削除ボタン
    public Button closeButton;          // 閉じるボタン
    
    // 現在表示中のカード
    private Card currentCard;
    
    private void Start()
    {
        // ボタンにイベントを追加
        if (addToDeckButton != null)
        {
            addToDeckButton.onClick.AddListener(() => {
                DeckBuilderManager deckBuilder = UnityEngine.Object.FindFirstObjectByType<DeckBuilderManager>();
                if (deckBuilder != null && currentCard != null)
                {
                    deckBuilder.AddCardToDeck(currentCard);
                }
            });
        }
        
        if (removeFromDeckButton != null)
        {
            removeFromDeckButton.onClick.AddListener(() => {
                DeckBuilderManager deckBuilder = UnityEngine.Object.FindFirstObjectByType<DeckBuilderManager>();
                if (deckBuilder != null && currentCard != null)
                {
                    deckBuilder.RemoveCardFromDeck(currentCard);
                }
            });
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }
    
    /// <summary>
    /// カード詳細情報の設定
    /// </summary>
    public void SetupCardDetail(Card card)
    {
        this.currentCard = card;
        
        // カード基本情報
        if (cardNameText != null)
            cardNameText.text = card.cardName;
            
        if (cardDescriptionText != null)
            cardDescriptionText.text = card.description;
            
        // カードタイプ表示
        if (cardTypeText != null)
        {
            string typeText = "不明";
            switch (card.type)
            {
                case CardType.Character:
                    typeText = "キャラクター";
                    break;
                case CardType.Spell:
                    typeText = "スペル";
                    break;
                case CardType.Field:
                    typeText = "フィールド";
                    break;
            }
            cardTypeText.text = typeText;
        }
        
        // カードタイプによって表示を変更
        if (card is CharacterCard characterCard)
        {
            if (cardStatsText != null)
            {
                // カテゴリー情報を含めたステータス表示
                string categoryDisplay = characterCard.GetCategoryDisplayText();
                string statsText = $"ATK: {characterCard.attackPower} DEF: {characterCard.defensePower}";
                
                // カテゴリーがある場合は表示を追加
                if (!string.IsNullOrEmpty(categoryDisplay))
                {
                    statsText += $"\nカテゴリー: {categoryDisplay}";
                }
                
                cardStatsText.text = statsText;
            }
                
            if (cardEffectText != null)
            {
                string effectText = "カードの効果:\n\n";
                
                // カード効果のテキスト構築
                foreach (var effect in characterCard.effects)
                {
                    // GetDescriptionメソッドがあれば使用
                    if (effect != null)
                    {
                        try
                        {
                            // Reflectionを使用してGetDescriptionメソッドを呼び出す
                            System.Reflection.MethodInfo method = effect.GetType().GetMethod("GetDescription");
                            if (method != null)
                            {
                                string description = (string)method.Invoke(effect, null);
                                effectText += description + "\n";
                            }
                            else
                            {
                                effectText += "効果詳細不明\n";
                            }
                        }
                        catch
                        {
                            effectText += "効果詳細取得エラー\n";
                        }
                    }
                }
                
                cardEffectText.text = effectText;
            }
        }
        
        // 以下の部分は修正なし（スペルカードとフィールドカード）
        else if (card is SpellCard spellCard)
        {
            // 略: 変更なし
        }
        else if (card is FieldCard fieldCard)
        {
            // 略: 変更なし
        }
        
        // カード画像 - artworkが文字列になったため、Spriteをロード
        if (cardImage != null && !string.IsNullOrEmpty(card.artwork))
        {
            Sprite artworkSprite = LoadCardSprite(card);
            if (artworkSprite != null)
            {
                cardImage.sprite = artworkSprite;
                cardImage.color = Color.white;
            }
            else
            {
                // 画像が見つからない場合のフォールバック
                cardImage.sprite = null;
                cardImage.color = new Color(0.8f, 0.8f, 0.8f);
                Debug.LogWarning($"カード {card.cardName} のアートワーク {card.artwork} をロードできませんでした");
            }
        }
        else if (cardImage != null)
        {
            cardImage.sprite = null;
            cardImage.color = new Color(0.8f, 0.8f, 0.8f);
        }
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
        return null;
    }
    
    /// <summary>
    /// カード枚数表示更新
    /// </summary>
    public void UpdateCardCount(int current, int max)
    {
        if (cardCountText != null)
        {
            cardCountText.text = $"デッキ内: {current}/{max}";
            
            // 上限に達していたら赤色に
            if (current >= max)
            {
                cardCountText.color = Color.red;
            }
            else
            {
                cardCountText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// 追加ボタンの有効/無効設定
    /// </summary>
    public void SetAddButtonEnabled(bool enabled)
    {
        if (addToDeckButton != null)
        {
            addToDeckButton.interactable = enabled;
        }
    }
    
    /// <summary>
    /// 削除ボタンの有効/無効設定
    /// </summary>
    public void SetRemoveButtonEnabled(bool enabled)
    {
        if (removeFromDeckButton != null)
        {
            removeFromDeckButton.interactable = enabled;
        }
    }
    
    /// <summary>
    /// パネルを閉じる
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}