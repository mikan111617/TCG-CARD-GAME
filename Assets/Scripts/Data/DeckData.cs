using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;

public class CardDatabaseCreator
{
   [MenuItem("Tools/Card Game/Create Empty Card Database")]
    public static void CreateEmptyCardDatabase()
    {
        // Resourcesフォルダの存在確認
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        
        // パスの設定
        string path = "Assets/Resources/CardDatabase.asset";
        
        // 既存のデータベースをチェック
        CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(path);
        
        if (database == null)
        {
            // 新しい空のデータベースを作成
            database = ScriptableObject.CreateInstance<CardDatabase>();
            
            // アセットとして保存
            AssetDatabase.CreateAsset(database, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("空のカードデータベースを作成しました: " + path);
        }
        else
        {
            Debug.Log("カードデータベースは既に存在します: " + path);
        }
        
        // エディタでアセットを選択
        Selection.activeObject = database;
    }


    // カテゴリーデータベース作成
    private static void EnsureCategoryDatabase()
    {
        string categoryPath = "Assets/Resources/CategoryDatabase.asset";
        CategoryDatabase categoryDB = AssetDatabase.LoadAssetAtPath<CategoryDatabase>(categoryPath);
        
        if (categoryDB == null)
        {
            categoryDB = ScriptableObject.CreateInstance<CategoryDatabase>();
            AssetDatabase.CreateAsset(categoryDB, categoryPath);
            Debug.Log("カテゴリーデータベースを作成しました: " + categoryPath);
        }
    }
    
    // デフォルトカードの作成
    private static void CreateDefaultCards(CardDatabase database)
    {
        // キャラクターカードのサンプル作成
        CreateSampleCharacterCards(database);
        
        // スペルカードのサンプル作成
        CreateSampleSpellCards(database);
        
        // フィールドカードのサンプル作成
        CreateSampleFieldCards(database);
    }
    
    // サンプルキャラクターカード作成
    private static void CreateSampleCharacterCards(CardDatabase database)
    {
        // カテゴリーデータベースの取得
        CategoryDatabase categoryDB = Resources.Load<CategoryDatabase>("CategoryDatabase");
        CardCategory studentCategory = null;
        
        // 学生カテゴリーが存在するか確認
        if (categoryDB != null)
        {
            studentCategory = categoryDB.GetCategoryByName("学生");
            
            // 学生カテゴリーが存在しない場合は作成
            if (studentCategory == null)
            {
                studentCategory = ScriptableObject.CreateInstance<CardCategory>();
                studentCategory.categoryName = "学生";
                studentCategory.description = "学校に通う生徒";
                studentCategory.categoryColor = Color.blue;
                studentCategory.categoryId = System.Guid.NewGuid().ToString().Substring(0, 8);
                
                // アセットとして追加
                if (categoryDB != null)
                {
                    AssetDatabase.AddObjectToAsset(studentCategory, AssetDatabase.GetAssetPath(categoryDB));
                    categoryDB.AddCategory(studentCategory);
                    EditorUtility.SetDirty(categoryDB);
                }
            }
        }
        
        // 知的少女レイン
        CharacterCard rain = ScriptableObject.CreateInstance<CharacterCard>();
        rain.id = 10100;
        rain.cardName = "知的少女レイン";
        rain.description = "理性的で賢い少女。IQ180。";
        rain.cost = 3;
        rain.type = CardType.Character;
        rain.element = ElementType.Light;
        rain.attackPower = 1400;
        rain.defensePower = 1400;
        rain.categories = new List<CardCategory>();
        
        // 学生カテゴリーの追加
        if (studentCategory != null)
        {
            rain.categories.Add(studentCategory);
        }
        
        // 効果オブジェクトの作成と追加
        AttackTargetEffect attackEffect = ScriptableObject.CreateInstance<AttackTargetEffect>();
        rain.effects = new List<CardEffect>() { attackEffect };
        
        // サブアセットとしてカードデータベースに追加
        AssetDatabase.AddObjectToAsset(rain, AssetDatabase.GetAssetPath(database));
        AssetDatabase.AddObjectToAsset(attackEffect, AssetDatabase.GetAssetPath(database));
        
        // データベースに追加
        database.AddCard(rain);
    }
    
    // サンプルスペルカード作成
    private static void CreateSampleSpellCards(CardDatabase database)
    {
        // ドローカード
        SpellCard draw = ScriptableObject.CreateInstance<SpellCard>();
        draw.id = 20100;
        draw.cardName = "知識の探求";
        draw.description = "カードを2枚ドローする。";
        draw.cost = 1;
        draw.type = CardType.Spell;
        draw.spellType = SpellType.Draw;
        DrawCardEffect drawEffect = ScriptableObject.CreateInstance<DrawCardEffect>();
        drawEffect.drawCount = 2;
        draw.effects = new List<CardEffect>() { drawEffect };
        AssetDatabase.AddObjectToAsset(draw, database);
        AssetDatabase.AddObjectToAsset(drawEffect, database);
        database.AddCard(draw);
        
        // その他のスペルカードを追加
        // ...
    }
    
    // サンプルフィールドカード作成
    private static void CreateSampleFieldCards(CardDatabase database)
    {
        // カテゴリーデータベースの取得
        CategoryDatabase categoryDB = Resources.Load<CategoryDatabase>("CategoryDatabase");
        CardCategory studentCategory = null;
        
        // 学生カテゴリーの検索
        if (categoryDB != null)
        {
            studentCategory = categoryDB.GetCategoryByName("学生");
        }
        
        // フィールドカード
        FieldCard field = ScriptableObject.CreateInstance<FieldCard>();
        field.id = 30100;
        field.cardName = "学園の図書館";
        field.description = "学生カテゴリーのキャラクターの攻撃力と防御力を500ポイント上昇させる。";
        field.cost = 2;
        field.type = CardType.Field;
        field.effects = new List<CardEffect>();
        field.affectedCategories = new List<CardCategory>();
        
        // 学生カテゴリーを対象に追加
        if (studentCategory != null)
        {
            field.affectedCategories.Add(studentCategory);
        }
        
        // ステータス修正の設定
        field.modifiesStats = true;
        field.attackModifier = 500;
        field.defenseModifier = 500;
        
        // アセットとデータベースに追加
        AssetDatabase.AddObjectToAsset(field, database);
        database.AddCard(field);
        
        // その他のフィールドカードを追加
        // ...
    }
}
#endif