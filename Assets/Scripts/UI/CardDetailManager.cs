using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// カード詳細表示マネージャー
public class CardDetailManager : MonoBehaviour
{
    [Header("詳細表示UI")]
    public GameObject detailPanel;           // 詳細表示パネル
    public Transform cardDisplayPosition;    // カード表示位置
    public Button closeButton;               // 閉じるボタン
    
    [Header("カード設定")]
    public GameObject cardPrefab;            // カードプレハブ
    public float cardScale = 2.0f;           // 表示スケール
    
    private GameObject currentDisplayCard;   // 現在表示中のカード
    private Card currentCardData;            // 現在表示中のカードデータ
    
    // 初期化
    private void Start()
    {
        // イベントリスナー設定
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideCardDetail);
        }
        
        // 初期状態は非表示
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }
    
    // カード詳細表示
    public void ShowCardDetail(Card card, bool isPlayerCard)
    {
        if (card == null || detailPanel == null)
        {
            return;
        }
        
        // 現在表示中のカードがあれば削除
        DestroyCurrentDisplayCard();
        
        // パネルを表示
        detailPanel.SetActive(true);
        
        // カード情報を保存
        currentCardData = card;
        
        // カードのプレハブを生成
        if (cardPrefab != null && cardDisplayPosition != null)
        {
            currentDisplayCard = Instantiate(cardPrefab, cardDisplayPosition);
            
            // カード情報を設定
            CardUI cardUI = currentDisplayCard.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                
                // サイズを大きくする
                RectTransform rectTransform = currentDisplayCard.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one * cardScale;
                }
                
                // ボタンコンポーネントを無効化（クリックされないように）
                Button button = currentDisplayCard.GetComponent<Button>();
                if (button != null)
                {
                    button.enabled = false;
                }
            }
        }
        
        // 詳細情報を表示
        DisplayCardDetails(card, isPlayerCard);
    }
    
    // 詳細情報表示
    private void DisplayCardDetails(Card card, bool isPlayerCard)
    {
        // カードタイプごとの詳細情報を表示
        // （実装されているテキスト要素に応じて表示）
        
        if (card is CharacterCard characterCard)
        {
            DisplayCharacterDetails(characterCard, isPlayerCard);
        }
        else if (card is SpellCard spellCard)
        {
            DisplaySpellDetails(spellCard, isPlayerCard);
        }
        else if (card is FieldCard fieldCard)
        {
            DisplayFieldDetails(fieldCard, isPlayerCard);
        }
    }
    
    // キャラクターカード詳細表示
    private void DisplayCharacterDetails(CharacterCard card, bool isPlayerCard)
    {
        // キャラクターカード特有の表示
        // 例：効果リストの表示など
        
        // 効果リストを表示するテキストがあれば、ここで更新
    }
    
    // スペルカード詳細表示
    private void DisplaySpellDetails(SpellCard card, bool isPlayerCard)
    {
        // スペルカード特有の表示
        // 例：呪文タイプの表示など
    }
    
    // フィールドカード詳細表示
    private void DisplayFieldDetails(FieldCard card, bool isPlayerCard)
    {
        // フィールドカード特有の表示
        // 例：効果対象カードの表示など
    }
    
    // 詳細表示を閉じる
    public void HideCardDetail()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        
        DestroyCurrentDisplayCard();
    }
    
    // 現在表示中のカードを削除
    private void DestroyCurrentDisplayCard()
    {
        if (currentDisplayCard != null)
        {
            Destroy(currentDisplayCard);
            currentDisplayCard = null;
        }
    }
}