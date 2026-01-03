using UnityEngine;

public static partial class BattleSystem
{
    /// <summary>
    /// キャラクター同士の戦闘処理 - 修正版
    /// </summary>
    public static void ResolveCharacterBattle(CharacterCard attacker, CharacterCard defender)
    {
        // 攻撃可能チェック
        if (!CanAttack(attacker))
        {
            Debug.LogWarning("[BattleSystem] 攻撃条件を満たしていません");
            return;
        }
        
        Debug.Log($"[BattleSystem] 戦闘開始: {attacker.cardName} vs {defender.cardName}");
        
        // 攻撃・防御力の取得
        int attackPower = CalculateAttackPower(attacker);
        int defensePower = CalculateDefensePower(defender);
        
        Debug.Log($"[BattleSystem] 攻撃力: {attackPower}, 防御力: {defensePower}");
        
        // 戦闘結果通知メッセージを準備
        string battleResultMessage = "";
        
        // 攻撃側が勝った場合のみ守備側にダメージを与える
        bool defenderDestroyed = false;
        if (attackPower > defensePower)
        {
            // 戦闘ダメージ計算
            int damageToDefender = attackPower - defensePower;
            
            // ダメージを確実に適用するように修正
            defenderDestroyed = true; // 攻撃力が防御力を上回った場合は破壊とする
            Debug.Log($"[BattleSystem] {defender.cardName}に{damageToDefender}ダメージ。破壊: {defenderDestroyed}");
            
            // 戦闘結果のメッセージを追加
            battleResultMessage += $"「{attacker.cardName}」の攻撃が成功！\n「{defender.cardName}」に{damageToDefender}ダメージを与え、破壊しました。";
            
            // プレイヤーへのダメージ（貫通ダメージ）
            Player defenderOwner = GetCardOwner(defender);
            if (defender.allowPiercingDamage)
            {
                defenderOwner.ChangeLifePoints(-damageToDefender);
                Debug.Log($"[BattleSystem] {defender.cardName}を通して{defenderOwner.playerName}に{damageToDefender}ダメージ");
                
                // 貫通ダメージのメッセージを追加
                battleResultMessage += $"\n貫通効果により{defenderOwner.playerName}に{damageToDefender}ダメージ！";
            }
        }
        else
        {
            // 攻撃が失敗した場合のメッセージ
            battleResultMessage += $"「{attacker.cardName}」の攻撃は「{defender.cardName}」の防御を突破できませんでした。";
        }
        
        // 反撃処理 - 守備側が破壊されていない場合のみ
        bool attackerDestroyed = false;
        if (!defenderDestroyed && defender.canCounterAttack)
        {
            int counterAttackPower = CalculateCounterAttackPower(defender);
            int attackerDefensePower = CalculateDefensePower(attacker);
            
            Debug.Log($"[BattleSystem] 反撃: 攻撃力{counterAttackPower} vs 防御力{attackerDefensePower}");
            
            // 反撃が防御を上回る場合のみダメージを与える
            if (counterAttackPower > attackerDefensePower)
            {
                int damageToAttacker = counterAttackPower - attackerDefensePower;
                attackerDestroyed = true; // 反撃で攻撃力が防御力を上回った場合は破壊
                Debug.Log($"[BattleSystem] {defender.cardName}の反撃！{attacker.cardName}に{damageToAttacker}ダメージ。破壊: {attackerDestroyed}");
                
                // 反撃成功のメッセージを追加
                battleResultMessage += $"\n「{defender.cardName}」の反撃が成功！\n「{attacker.cardName}」に{damageToAttacker}ダメージを与え、破壊しました。";
                
                // 貫通ダメージ
                if (attacker.allowPiercingDamage)
                {
                    Player attackerOwner = GetCardOwner(attacker);
                    attackerOwner.ChangeLifePoints(-damageToAttacker);
                    Debug.Log($"[BattleSystem] {attacker.cardName}を通して{attackerOwner.playerName}に{damageToAttacker}ダメージ");
                    
                    // 貫通ダメージのメッセージを追加
                    battleResultMessage += $"\n貫通効果により{attackerOwner.playerName}に{damageToAttacker}ダメージ！";
                }
            }
            else
            {
                // 反撃が失敗した場合のメッセージ
                battleResultMessage += $"\n「{defender.cardName}」の反撃は「{attacker.cardName}」の防御を突破できませんでした。";
            }
        }
        
        // 戦闘結果を通知
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.ShowNotification(battleResultMessage, 3.5f);
        }
        
        // 破壊処理
        if (defenderDestroyed)
        {
            ProcessCardDestruction(defender);
        }
        
        if (attackerDestroyed)
        {
            ProcessCardDestruction(attacker);
        }
        
        // 攻撃済みフラグを設定
        attacker.hasAttackedThisTurn = true;
        
        // 勝利条件チェック
        GameManager.Instance.CheckVictoryConditions();
    }

    /// <summary>
    /// カード破壊処理 - 捨て札への移動を確実に行う
    /// </summary>
    private static void ProcessCardDestruction(CharacterCard card)
    {
        if (card == null) return;
        
        Debug.Log($"[BattleSystem] {card.cardName}を破壊処理");
        
        // カードの所有者を取得
        Player owner = GetCardOwner(card);
        if (owner == null)
        {
            Debug.LogError($"[BattleSystem] {card.cardName}の所有者が見つかりません");
            return;
        }
        
        // 破壊時効果の発動
        card.OnDestruction();
        
        // フィールドから削除
        if (owner.characterField.Contains(card))
        {
            owner.characterField.Remove(card);
            Debug.Log($"[BattleSystem] {card.cardName}をフィールドから削除");
        }
        else
        {
            Debug.LogWarning($"[BattleSystem] {card.cardName}はフィールド上にありません");
        }
        
        // 墓地に追加
        owner.graveyard.Add(card);
        Debug.Log($"[BattleSystem] {card.cardName}を墓地に追加。墓地カード数: {owner.graveyard.Count}");
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateCharacterField(owner);
        GameManager.Instance.uiManager?.UpdateGraveyard(owner);
        
        // バトル関連の通知として表示
        if (GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.ShowBattleNotification($"{card.cardName}が破壊されました", 1.5f);
        }
    }

    // キャラクター同士の戦闘
    public static void ResolveCharacterAttack(CharacterCard attacker, CharacterCard defender)
    {
        // プレイヤーへのダメージ計算
        int damageToDefenderPlayer = 0;
        
        // 守備力より攻撃力が高い場合、プレイヤーに貫通ダメージ
        if (attacker.attackPower > defender.defensePower && defender.allowPiercingDamage)
        {
            damageToDefenderPlayer = attacker.attackPower - defender.defensePower;
            defender.opponent.lifePoints -= damageToDefenderPlayer;
        }
        
        // 攻撃側カードの破壊判定
        bool attackerDestroyed = false;
        
        // 守備側カードが反撃可能な場合
        if (defender.canCounterAttack)
        {
            // 反撃で攻撃側が破壊されるかチェック
            attackerDestroyed = defender.attackPower > attacker.defensePower;
            
            // 反撃による貫通ダメージ
            if (defender.attackPower > attacker.defensePower && attacker.allowPiercingDamage)
            {
                int damageToAttackerPlayer = defender.attackPower - attacker.defensePower;
                attacker.opponent.lifePoints -= damageToAttackerPlayer;
            }
        }
        
        // 守備側カードの破壊判定
        bool defenderDestroyed = attacker.attackPower > defender.defensePower;
        
        // 破壊処理
        if (defenderDestroyed)
        {
            // 守備側カード破壊時の効果発動 - 引数なしバージョンを使用
            defender.OnDestruction();
            
            // フィールドから墓地へ移動（characterFieldに修正）
            defender.owner.characterField.Remove(defender);
            defender.owner.graveyard.Add(defender);
        }
        
        if (attackerDestroyed)
        {
            // 攻撃側カード破壊時の効果発動 - 引数なしバージョンを使用
            attacker.OnDestruction();
            
            // フィールドから墓地へ移動（characterFieldに修正）
            attacker.owner.characterField.Remove(attacker);
            attacker.owner.graveyard.Add(attacker);
        }
        
        // 攻撃済みフラグを設定
        attacker.hasAttackedThisTurn = true;
        
        // 勝利条件チェック
        GameManager.Instance.CheckVictoryConditions();
    }
    
    /// <summary>
    /// プレイヤーへの直接攻撃
    /// </summary>
    public static void ResolveDirectAttack(CharacterCard attacker, Player target)
    {
        // 攻撃可能チェック
        if (!CanAttack(attacker))
        {
            Debug.LogWarning("[BattleSystem] 攻撃条件を満たしていません");
            return;
        }
        
        Debug.Log($"[BattleSystem] 直接攻撃: {attacker.cardName} が {target.playerName} に攻撃");
        
        // 攻撃力の取得
        int attackPower = CalculateAttackPower(attacker);
        Debug.Log($"[BattleSystem] 直接攻撃の攻撃力: {attackPower}");
        
        // ダメージ適用
        target.ChangeLifePoints(-attackPower);
        Debug.Log($"[BattleSystem] {target.playerName}に{attackPower}ダメージ");
        
        // 直接攻撃の結果を通知
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string directAttackMessage = $"「{attacker.cardName}」の直接攻撃！\n{target.playerName}に{attackPower}ダメージを与えました。";
            GameManager.Instance.uiManager.ShowNotification(directAttackMessage, 2.5f);
        }
        
        // 攻撃済みフラグを設定
        attacker.hasAttackedThisTurn = true;
        
        // 勝利条件チェック
        GameManager.Instance.CheckVictoryConditions();
    }
        
    // 攻撃力計算（バフなどの効果を含む）
    private static int CalculateAttackPower(CharacterCard card)
    {
        int basePower = card.attackPower;
        
        // バフ効果計算（フィールドカードなど）
        Player owner = GetCardOwner(card);
        
        // フィールドカードの効果を適用（配列に修正）
        for (int i = 0; i < owner.fieldCards.Length; i++)
        {
            FieldCard fieldCard = owner.fieldCards[i];
            if (fieldCard != null && fieldCard.modifiesStats) // affectsAttack → modifiesStats
            {
                basePower = fieldCard.ModifyAttack(card, basePower); // ModifyAttackPower → ModifyAttack
            }
        }
        
        // カードの特殊効果による変化
        foreach (var effect in card.effects)
        {
            if (effect is StatModifierEffect statEffect)
            {
                basePower = statEffect.ModifyAttack(basePower);
            }
        }
        
        return Mathf.Max(0, basePower);
    }
    
    // 防御力計算（バフなどの効果を含む）
    private static int CalculateDefensePower(CharacterCard card)
    {
        int basePower = card.defensePower;
        
        // バフ効果計算（フィールドカードなど）
        Player owner = GetCardOwner(card);
        
        // フィールドカードの効果を適用（配列に修正）
        for (int i = 0; i < owner.fieldCards.Length; i++)
        {
            FieldCard fieldCard = owner.fieldCards[i];
            if (fieldCard != null && fieldCard.modifiesStats) // affectsDefense → modifiesStats
            {
                basePower = fieldCard.ModifyDefense(card, basePower); // ModifyDefensePower → ModifyDefense
            }
        }
        
        // カードの特殊効果による変化
        foreach (var effect in card.effects)
        {
            if (effect is StatModifierEffect statEffect)
            {
                basePower = statEffect.ModifyDefense(basePower);
            }
        }
        
        return Mathf.Max(0, basePower);
    }
    
    // 反撃時の攻撃力計算
    private static int CalculateCounterAttackPower(CharacterCard card)
    {
        // 反撃能力がない場合は0を返す
        if (!card.canCounterAttack) return 0;
        
        return CalculateAttackPower(card);
    }
    
    // カードの破壊判定と処理
    private static void CheckCardDestruction(CharacterCard card)
    {
        // HP概念を排除し、破壊判定をTakeDamageで行うように修正
        // 呼び出し元で既にTakeDamageの結果を使って破壊処理をしているため、
        // このメソッドは基本的に使われない想定
        
        Debug.Log($"{card.cardName}が破壊された");
        
        // カードの所有者を取得
        Player owner = GetCardOwner(card);
        
        // 破壊時効果の発動 - 引数なしバージョンに修正
        card.OnDestruction();
        
        // フィールドから墓地へ移動（characterFieldに修正）
        owner.characterField.Remove(card);
        owner.graveyard.Add(card);
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateCharacterField(owner);
        GameManager.Instance.uiManager?.UpdateGraveyard(owner);
    }
    
    // カードの所有者を取得
    private static Player GetCardOwner(Card card)
    {
        // プレイヤー1のフィールドか手札にあるか確認
        Player player1 = GameManager.Instance.player1;
        
        if (card is CharacterCard charCard)
        {
            if (player1.characterField.Contains(charCard)) return player1;
        }
        else if (card is SpellCard spellCard)
        {
            if (player1.spellField.Contains(spellCard)) return player1;
        }
        else
        {
            // フィールドカードの所有者チェック（配列に修正）
            bool inPlayer1Field = false;
            for (int i = 0; i < player1.fieldCards.Length; i++)
            {
                if (player1.fieldCards[i] == card)
                {
                    inPlayer1Field = true;
                    break;
                }
            }
            
            if (inPlayer1Field || player1.hand.Contains(card) || player1.deck.Contains(card) || player1.graveyard.Contains(card))
            {
                return player1;
            }
        }
        
        // 該当しない場合はプレイヤー2のカード
        return GameManager.Instance.player2;
    }

    // このメソッドを修正（characterFieldに変更、HPチェックを撤廃）
    private static void CheckCharacterDestruction(CharacterCard card)
    {
        // 攻撃力と防御力を比較する判定に変更
        // ここでは直接破壊処理を実行
        
        // 破壊時効果の発動 - 引数なしバージョンを使用
        card.OnDestruction();
        
        // フィールドから墓地へ移動（characterFieldに修正）
        card.owner.characterField.Remove(card);
        card.owner.graveyard.Add(card);
    }


    // 特定カテゴリーに攻撃/防御ボーナスを適用
    public static void ApplyCategoryBonus(Player player, string categoryName, int attackBonus, int defenseBonus)
    {
        if (player == null)
            return;
            
        Debug.Log($"{categoryName}カテゴリーのカードに攻撃+{attackBonus}/防御+{defenseBonus}のボーナスを適用");
        
        foreach (CharacterCard card in player.characterField)
        {
            // 新しいカテゴリーシステムで検索するように修正
            if (card.HasCategoryWithName(categoryName))
            {
                card.ApplyStatBonus(attackBonus, defenseBonus);
                Debug.Log($"{card.cardName}にボーナスを適用: 攻撃{card.GetEffectiveAttackPower()}/防御{card.GetEffectiveDefensePower()}");
            }
        }
    }

    public static bool CanAttack(CharacterCard attacker)
    {
        // 攻撃を行えるかどうかのチェック
        if (attacker == null) return false;
        
        // 先行1ターン目チェック
        if (GameManager.Instance != null && 
            GameManager.Instance.isFirstPlayerFirstTurn && 
            GameManager.Instance.turnManager != null &&
            GameManager.Instance.turnManager.currentPlayer == GameManager.Instance.firstPlayer)
        {
            Debug.Log("[BattleSystem] 先行1ターン目は攻撃できません");
            return false;
        }
        
        // 攻撃済みチェック
        if (attacker.hasAttackedThisTurn)
        {
            return false;
        }
        
        return true;
    }
}