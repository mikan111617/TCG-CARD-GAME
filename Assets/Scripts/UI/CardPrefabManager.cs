using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// カードプレファブを管理するクラス
/// </summary>
public class CardPrefabManager : MonoBehaviour
{
    // シングルトンパターン
    public static CardPrefabManager Instance { get; private set; }
    
    [Header("カードプレファブ")]
    public GameObject characterCardPrefab;  // キャラクターカードプレファブ
    public GameObject spellCardPrefab;      // スペルカードプレファブ
    public GameObject fieldCardPrefab;      // フィールドカードプレファブ
    public GameObject defaultCardPrefab;    // デフォルトのカードプレファブ
    
    private void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// カードタイプに合わせたプレファブを取得
    /// </summary>
    public GameObject GetCardPrefab(Card card)
    {
        if (card == null) return defaultCardPrefab;
        
        // カードタイプに応じてプレファブを返す
        if (card is CharacterCard && characterCardPrefab != null)
        {
            return characterCardPrefab;
        }
        else if (card is SpellCard && spellCardPrefab != null)
        {
            return spellCardPrefab;
        }
        else if (card is FieldCard && fieldCardPrefab != null)
        {
            return fieldCardPrefab;
        }
        
        // デフォルトプレファブを返す
        return defaultCardPrefab;
    }
}