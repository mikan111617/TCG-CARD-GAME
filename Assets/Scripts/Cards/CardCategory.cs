using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// カードのカテゴリーを表すScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "New Category", menuName = "Card Game/Card Category")]
public class CardCategory : ScriptableObject
{
    [Header("基本情報")]
    public string categoryId; // 内部管理用ID
    public string categoryName; // 表示名
    public string description; // カテゴリーの説明
    public Color categoryColor = Color.white; // カテゴリーの色
    public Sprite categoryIcon; // カテゴリーアイコン
    
    [Header("関連カテゴリー")]
    public List<CardCategory> relatedCategories; // 関連するカテゴリー
    
    // カテゴリーの一意性を保つためのID生成
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(categoryId))
        {
            categoryId = System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
    
    // 文字列表現
    public override string ToString()
    {
        return categoryName;
    }
}

/// <summary>
/// カテゴリーデータをまとめて管理するScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "CategoryDatabase", menuName = "Card Game/Category Database")]
public class CategoryDatabase : ScriptableObject
{
    [SerializeField]
    private List<CardCategory> categories = new List<CardCategory>();
    
    // すべてのカテゴリーを取得
    public List<CardCategory> GetAllCategories()
    {
        return categories;
    }
    
    // カテゴリー追加
    public void AddCategory(CardCategory category)
    {
        if (!categories.Contains(category))
        {
            categories.Add(category);
        }
    }
    
    // カテゴリー削除
    public void RemoveCategory(CardCategory category)
    {
        categories.Remove(category);
    }
    
    // IDによるカテゴリー検索
    public CardCategory GetCategoryById(string categoryId)
    {
        return categories.Find(c => c.categoryId == categoryId);
    }
    
    // 名前によるカテゴリー検索
    public CardCategory GetCategoryByName(string categoryName)
    {
        return categories.Find(c => c.categoryName == categoryName);
    }
}