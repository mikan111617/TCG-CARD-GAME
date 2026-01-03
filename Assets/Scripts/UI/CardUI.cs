using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// カードUIの表示・制御を行うコンポーネント - 修正版
/// </summary>
public class CardUI : MonoBehaviour
{
    [Header("カード要素")]
    public Image cardFrame;               // カードフレーム
    public Image cardArtwork;             // カードアートワーク
    public Image attributeIcon;           // 属性アイコン
    public Image cardTypeIcon;            // カードタイプアイコン
    public GameObject characterStatsPanel; // キャラクターステータスパネル
    
    [Header("テキスト要素")]
    public TextMeshProUGUI cardNameText;   // カード名
    public TextMeshProUGUI costText;       // コスト
    public TextMeshProUGUI descriptionText; // 説明文
    public TextMeshProUGUI attackText;     // 攻撃力
    public TextMeshProUGUI defenseText;    // 防御力
    public TextMeshProUGUI categoryText;   // カテゴリー
    
    [Header("フレーム素材")]
    public Sprite characterCardFrame;     // キャラクターカードフレーム
    public Sprite spellCardFrame;         // スペルカードフレーム
    public Sprite fieldCardFrame;         // フィールドカードフレーム
    
    [Header("属性アイコン")]
    public Sprite fireIcon;               // 火属性アイコン
    public Sprite waterIcon;              // 水属性アイコン
    public Sprite earthIcon;              // 地属性アイコン
    public Sprite windIcon;               // 風属性アイコン
    public Sprite lightIcon;              // 光属性アイコン
    public Sprite darkIcon;               // 闇属性アイコン
    public Sprite neutralIcon;            // 無属性アイコン
    
    [Header("カードタイプアイコン")]
    public Sprite characterIcon;          // キャラクターアイコン
    public Sprite spellIcon;              // スペルアイコン
    public Sprite fieldIcon;              // フィールドアイコン
    
    [Header("カードの裏面")]
    public GameObject cardBack;           // カード裏面オブジェクト
    public Image cardBackImage;           // カード裏面画像
    public Sprite characterCardBack;      // キャラクターカード裏面
    public Sprite spellCardBack;          // スペルカード裏面
    public Sprite fieldCardBack;          // フィールドカード裏面
    
    // 現在表示中のカード
    private Card currentCard;
    
    // インタラクティブ状態
    private bool isInteractable = true;
        
    /// <summary>
    /// キャラクターカードの設定
    /// </summary>
    private void SetupCharacterCard(CharacterCard card)
    {
        if (card == null) return;
        
        // フレームの設定
        if (cardFrame != null && characterCardFrame != null)
            cardFrame.sprite = characterCardFrame;
            
        // カードタイプアイコンの設定
        if (cardTypeIcon != null && characterIcon != null)
        {
            cardTypeIcon.sprite = characterIcon;
            cardTypeIcon.gameObject.SetActive(true);
        }
        
        // ステータス表示パネルを有効化
        if (characterStatsPanel != null)
            characterStatsPanel.SetActive(true);
            
        // ステータスの設定
        if (attackText != null)
            attackText.text = card.attackPower.ToString();
            
        if (defenseText != null)
            defenseText.text = card.defensePower.ToString();
            
        // カテゴリーテキスト表示の修正
        if (categoryText != null) {
            // カテゴリーリストから表示テキストを生成
            categoryText.text = card.GetCategoryDisplayText();
        }
            
        // 属性アイコンの設定
        if (attributeIcon != null)
        {
            attributeIcon.gameObject.SetActive(true);
            
            switch (card.element)
            {
                case ElementType.Fire:
                    attributeIcon.sprite = fireIcon;
                    attributeIcon.color = new Color(1.0f, 0.3f, 0.3f);
                    if (costText != null) costText.color = new Color(1.0f, 0.3f, 0.3f);
                    break;
                    
                case ElementType.Water:
                    attributeIcon.sprite = waterIcon;
                    attributeIcon.color = new Color(0.3f, 0.5f, 1.0f);
                    if (costText != null) costText.color = new Color(0.3f, 0.5f, 1.0f);
                    break;
                    
                case ElementType.Earth:
                    attributeIcon.sprite = earthIcon;
                    attributeIcon.color = new Color(0.6f, 0.4f, 0.2f);
                    if (costText != null) costText.color = new Color(0.6f, 0.4f, 0.2f);
                    break;
                    
                case ElementType.Wind:
                    attributeIcon.sprite = windIcon;
                    attributeIcon.color = new Color(0.5f, 0.8f, 0.5f);
                    if (costText != null) costText.color = new Color(0.5f, 0.8f, 0.5f);
                    break;
                    
                case ElementType.Light:
                    attributeIcon.sprite = lightIcon;
                    attributeIcon.color = new Color(1.0f, 0.9f, 0.5f);
                    if (costText != null) costText.color = new Color(1.0f, 0.9f, 0.5f);
                    break;
                    
                case ElementType.Dark:
                    attributeIcon.sprite = darkIcon;
                    attributeIcon.color = new Color(0.5f, 0.3f, 0.7f);
                    if (costText != null) costText.color = new Color(0.5f, 0.3f, 0.7f);
                    break;
                    
                case ElementType.Neutral:
                default:
                    attributeIcon.sprite = neutralIcon;
                    attributeIcon.color = new Color(0.8f, 0.8f, 0.8f);
                    if (costText != null) costText.color = new Color(0.8f, 0.8f, 0.8f);
                    break;
            }
        }
    }
        
    /// <summary>
    /// スペルカードの設定
    /// </summary>
    private void SetupSpellCard(SpellCard card)
    {
        if (card == null) return;
        
        // フレームの設定
        if (cardFrame != null && spellCardFrame != null)
            cardFrame.sprite = spellCardFrame;
            
        // カードタイプアイコンの設定
        if (cardTypeIcon != null && spellIcon != null)
        {
            cardTypeIcon.sprite = spellIcon;
            cardTypeIcon.gameObject.SetActive(true);
        }
        
        // ステータス表示パネルを無効化
        if (characterStatsPanel != null)
            characterStatsPanel.SetActive(false);
            
        // 属性アイコンを非表示
        if (attributeIcon != null)
            attributeIcon.gameObject.SetActive(false);
            
        // コストテキストの色をデフォルトに
        if (costText != null)
            costText.color = Color.white;
    }
    
    /// <summary>
    /// フィールドカードの設定
    /// </summary>
    private void SetupFieldCard(FieldCard card)
    {
        if (card == null) return;
        
        // フレームの設定
        if (cardFrame != null && fieldCardFrame != null)
            cardFrame.sprite = fieldCardFrame;
            
        // カードタイプアイコンの設定
        if (cardTypeIcon != null && fieldIcon != null)
        {
            cardTypeIcon.sprite = fieldIcon;
            cardTypeIcon.gameObject.SetActive(true);
        }
        
        // ステータス表示パネルを無効化
        if (characterStatsPanel != null)
            characterStatsPanel.SetActive(false);
            
        // 属性アイコンを非表示
        if (attributeIcon != null)
            attributeIcon.gameObject.SetActive(false);
            
        // コストテキストの色をデフォルトに
        if (costText != null)
            costText.color = Color.white;
    }
    
    /// <summary>
    /// カードの裏面を表示
    /// </summary>
    public void ShowCardBack()
    {
        if (cardBack == null) return;
        
        cardBack.SetActive(true);
        
        // カードタイプに応じた裏面を設定
        if (cardBackImage != null && currentCard != null)
        {
            switch (currentCard.type)
            {
                case CardType.Character:
                    cardBackImage.sprite = characterCardBack;
                    break;
                    
                case CardType.Spell:
                    cardBackImage.sprite = spellCardBack;
                    break;
                    
                case CardType.Field:
                    cardBackImage.sprite = fieldCardBack;
                    break;
            }
        }
    }
    
    /// <summary>
    /// カードの表面を表示
    /// </summary>
    public void ShowCardFront()
    {
        if (cardBack == null) return;
        
        cardBack.SetActive(false);
    }
    
    /// <summary>
    /// カードの操作可能状態を設定
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
        
        // カードの見た目を調整
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        canvasGroup.alpha = interactable ? 1.0f : 0.5f;
        
        // ボタンの操作可能状態も設定
        Button button = GetComponent<Button>();
        if (button != null)
            button.interactable = interactable;
    }
    
    /// <summary>
    /// カードの操作可能状態を取得
    /// </summary>
    public bool IsInteractable()
    {
        return isInteractable;
    }
    
    /// <summary>
    /// 現在表示しているカードを取得
    /// </summary>
    public Card GetCard()
    {
        return currentCard;
    }

    /// <summary>
    /// 相手の手札カードとしてセットアップ
    /// </summary>
    public void SetupAsOpponentHandCard(bool isFieldCard)
    {
        // すべての表側要素を非表示
        if (cardFrame) cardFrame.gameObject.SetActive(false);
        if (cardArtwork) cardArtwork.gameObject.SetActive(false);
        if (attributeIcon) attributeIcon.gameObject.SetActive(false);
        if (cardTypeIcon) cardTypeIcon.gameObject.SetActive(false);
        if (characterStatsPanel) characterStatsPanel.SetActive(false);
        if (cardNameText) cardNameText.gameObject.SetActive(false);
        if (costText) costText.gameObject.SetActive(false);
        if (descriptionText) descriptionText.gameObject.SetActive(false);
        
        // 裏面を表示
        if (cardBack)
        {
            cardBack.SetActive(true);
            
            // カードタイプに応じた裏面を表示
            if (cardBackImage)
            {
                cardBackImage.sprite = isFieldCard ? fieldCardBack : characterCardBack;
            }
        }
    }

    /// <summary>
    /// カードのアートワークを設定する - ファイル名から直接ロードするように修正
    /// </summary>
    public void SetCardArtwork(Sprite artwork)
    {
        if (cardArtwork != null && artwork != null)
        {
            cardArtwork.sprite = artwork;
            cardArtwork.color = Color.white;
            cardArtwork.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// カードのセットアップ（メインメソッド）
    /// </summary>
    public void SetupCard(Card card)
    {
        if (card == null)
        {
            Debug.LogError("SetupCard: カードがnullです");
            return;
        }
        
        // カード参照を保存
        currentCard = card;
        
        // 基本情報の設定
        if (cardNameText != null)
            cardNameText.text = card.cardName;
            
        if (costText != null)
            costText.text = card.cost.ToString();
            
        if (descriptionText != null)
            descriptionText.text = card.description;
        
        // カードタイプに基づいてセットアップ
        if (card is CharacterCard characterCard)
        {
            SetupCharacterCard(characterCard);
        }
        else if (card is SpellCard spellCard)
        {
            SetupSpellCard(spellCard);
        }
        else if (card is FieldCard fieldCard)
        {
            SetupFieldCard(fieldCard);
        }
        
        // アートワークをロード (文字列からロード)
        if (!string.IsNullOrEmpty(card.artwork) && cardArtwork != null)
        {
            LoadAndSetArtwork(card.artwork);
        }
    }
    
    /// <summary>
    /// カードアートワークをファイル名から読み込んで設定
    /// </summary>
    private void LoadAndSetArtwork(string artworkFileName)
    {
        // 検索する候補パスのリスト
        string[] pathCandidates = new string[]
        {
            $"CardImages/Characters/{artworkFileName}",
            $"CardImages/Spells{artworkFileName}",
            $"CardImages/Fields/{artworkFileName}",
        };
        
        Sprite loadedSprite = null;
        
        // 各候補パスを試す
        foreach (string path in pathCandidates)
        {
            loadedSprite = Resources.Load<Sprite>(path);
            if (loadedSprite != null)
            {
                break;
            }
        }
        
        if (loadedSprite != null)
        {
            cardArtwork.sprite = loadedSprite;
            cardArtwork.color = Color.white;
            cardArtwork.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"アートワーク {artworkFileName} をロードできませんでした");
            cardArtwork.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool selected)
    {
        // 選択状態の視覚的な表現
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        if (selected)
        {
            // 選択された状態
            canvasGroup.alpha = 1.0f;
            transform.localScale = transform.localScale * 1.1f; // 少し拡大
        }
        else
        {
            // 非選択状態
            canvasGroup.alpha = 0.7f;
            transform.localScale = transform.localScale / 1.1f; // 元のサイズに戻す
        }
    }

    // CardUIクラスに追加するメソッド（ラベル表示用）
    public void AddLabel(string labelText)
    {
        // ラベル表示用のGameObjectを作成
        GameObject labelObj = new GameObject("CardLabel");
        labelObj.transform.SetParent(transform, false);
        
        // テキストコンポーネント追加
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = 12;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        
        // 背景パネル
        GameObject bgPanel = new GameObject("LabelBG");
        bgPanel.transform.SetParent(labelObj.transform, false);
        Image bg = bgPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        // 位置調整 - カードの上部に配置
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.pivot = new Vector2(0.5f, 1);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(0, 20);
        }
        
        // 背景パネルのサイズ調整
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        if (bgRect != null)
        {
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
        }
        
        // レイヤー順の調整
        labelObj.transform.SetAsLastSibling();
    }

    /// <summary>
    /// カード情報のデバッグログを出力
    /// </summary>
    public void LogCardInfo(Card card)
    {
        if (card == null) return;
        
        // 基本情報
        string cardInfo = $"[カード詳細] ID: {card.id}, 名前: {card.cardName}, コスト: {card.cost}\n";
        cardInfo += $"説明: {card.description}\n";
        cardInfo += $"タイプ: {card.GetType().Name}\n";
        
        // カードタイプごとの追加情報
        if (card is CharacterCard charCard)
        {
            cardInfo += $"攻撃力: {charCard.attackPower}, 防御力: {charCard.defensePower}\n";
            
            // カテゴリー情報
            if (charCard.categories != null && charCard.categories.Count > 0)
            {
                cardInfo += "カテゴリー: ";
                foreach (var category in charCard.categories)
                {
                    if (category != null)
                        cardInfo += $"{category.categoryName}, ";
                }
                cardInfo = cardInfo.TrimEnd(',', ' ') + "\n";
            }
            
            // 効果情報
            if (charCard.effects != null && charCard.effects.Count > 0)
            {
                cardInfo += "効果:\n";
                foreach (var effect in charCard.effects)
                {
                    if (effect != null)
                        cardInfo += $"- {effect.GetType().Name}: {effect.GetDescription()}\n";
                }
            }
        }
        else if (card is SpellCard spellCard)
        {
            cardInfo += $"相手ターンに発動可能: {(spellCard.canActivateOnOpponentTurn ? "はい" : "いいえ")}\n";
            
            // 効果情報
            if (spellCard.effects != null && spellCard.effects.Count > 0)
            {
                cardInfo += "効果:\n";
                foreach (var effect in spellCard.effects)
                {
                    if (effect != null)
                        cardInfo += $"- {effect.GetType().Name}: {effect.GetDescription()}\n";
                }
            }
        }
        else if (card is FieldCard fieldCard)
        {
            cardInfo += $"自分のフィールドに影響: {(fieldCard.affectsOwnField ? "はい" : "いいえ")}\n";
            cardInfo += $"相手のフィールドに影響: {(fieldCard.affectsOpponentField ? "はい" : "いいえ")}\n";
            
            if (fieldCard.modifiesStats)
            {
                cardInfo += $"ステータス変更: 攻撃力{(fieldCard.attackModifier >= 0 ? "+" : "")}{fieldCard.attackModifier}, ";
                cardInfo += $"防御力{(fieldCard.defenseModifier >= 0 ? "+" : "")}{fieldCard.defenseModifier}\n";
            }
            
            // 効果情報
            if (fieldCard.effects != null && fieldCard.effects.Count > 0)
            {
                cardInfo += "効果:\n";
                foreach (var effect in fieldCard.effects)
                {
                    if (effect != null)
                        cardInfo += $"- {effect.GetType().Name}: {effect.GetDescription()}\n";
                }
            }
        }
        
        // ログ出力
        Debug.Log(cardInfo);
    }
}