using System.Collections.Generic;
using UnityEngine;

public partial class Player : MonoBehaviour
{
    [Header("プレイヤー情報")]
    public string playerName;
    public int lifePoints;
    public int energy = 0;
    
    [Header("カード管理")]
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> graveyard = new List<Card>();
    
    [Header("フィールド管理")]
    public List<CharacterCard> characterField = new List<CharacterCard>();
    public List<SpellCard> spellField = new List<SpellCard>();
    public FieldCard fieldCard;
    
    // 最大フィールド数
    public const int MAX_CHARACTERS = 5;
    public const int MAX_SPELLS = 3;

    // 手札の上限定数を追加
    public const int MAX_HAND_SIZE = 7;
    
    // ドロー関連フラグ
    private bool cannotDrawCard = false;
    
    // イベント
    public delegate void PlayerEvent(Player player);
    public delegate void CardEvent(Player player, Card card);
    
    public event PlayerEvent OnLifePointsChanged;
    public event CardEvent OnCardDrawn;

    public FieldCard[] fieldCards = new FieldCard[3];
    
    // プレイヤー初期化
    public void InitializePlayer(int startingLifePoints)
    {
        lifePoints = startingLifePoints;
        energy = 0;
        
        // コレクションをクリア
        deck.Clear();
        hand.Clear();
        graveyard.Clear();
        characterField.Clear();
        spellField.Clear();
        fieldCard = null;
        
        // フラグリセット
        cannotDrawCard = false;
    }
    
    // ライフポイント変更
    public void ChangeLifePoints(int amount)
    {
        int newLifePoints = lifePoints + amount;
        
        // ライフポイントは0未満にならないよう制限
        newLifePoints = Mathf.Max(0, newLifePoints);
        
        // 変更があった場合のみ処理
        if (newLifePoints != lifePoints)
        {
            // デバッグログ
            Debug.Log($"[Player] {playerName}のライフポイント変更: {lifePoints} -> {newLifePoints} (変化量: {amount})");
            
            // ライフポイント更新
            lifePoints = newLifePoints;
            
            // ステータス更新
            OnLifePointsChanged?.Invoke(this);
            
            // UI更新
            GameManager.Instance.uiManager?.UpdateLifePoints(this);
            
            // ライフが0になった場合、すぐに勝利条件チェック
            if (lifePoints <= 0)
            {
                Debug.Log($"[Player] {playerName}のライフポイントが0になりました。勝利条件チェック実行");
                // 非同期で実行して無限ループを防止
                GameManager.Instance.CheckVictoryConditions();
            }
        }
    }
        
    // DrawCardメソッドを修正
    public bool DrawCard()
    {
        if (deck.Count <= 0 || cannotDrawCard)
        {
            cannotDrawCard = true;
            GameManager.Instance.CheckVictoryConditions();
            return false;
        }
        
        // デッキから手札へ
        Card drawnCard = deck[0];
        deck.RemoveAt(0);

        // いったん手札に追加してから枚数を確認する（最大 8 枚になる可能性あり）
        hand.Add(drawnCard);
        OnCardDrawn?.Invoke(this, drawnCard);

        // 上限オーバーならランダムで 1 枚墓地へ
        if (hand.Count > MAX_HAND_SIZE)
        {
            int index = Random.Range(0, hand.Count);
            Card discarded = hand[index];
            hand.RemoveAt(index);
            graveyard.Add(discarded);

            // 画面通知
            GameManager.Instance.uiManager?.ShowNotification(
                $"{playerName} の手札が 7 枚を超えたため\n「{discarded.cardName}」を捨て札にしました。",
                2.0f,
                UIManager.NotificationType.System);
        }
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateHand(this);
        GameManager.Instance.uiManager?.UpdateDeckCount(this);
        
        return true;
    }

    // ランダムにカードを捨てる（AIプレイヤー用）
    private void DiscardRandomCard(Card newCard)
    {
        // 既存の手札から捨てるカードをランダムに選択
        int randomIndex = UnityEngine.Random.Range(0, hand.Count);
        Card cardToDiscard = hand[randomIndex];
        
        // 捨てるカードを墓地に移動
        hand.RemoveAt(randomIndex);
        graveyard.Add(cardToDiscard);
        
        // 新しく引いたカードを手札に追加
        hand.Add(newCard);
        
        // イベント発火
        OnCardDrawn?.Invoke(this, newCard);
        
        // ログ出力
        Debug.Log($"[Player] {playerName} が手札上限により {cardToDiscard.cardName} を捨て、{newCard.cardName} を引きました");
    }

    // 特定のカードを捨てる（UI選択用）
    public void DiscardSpecificCard(Card cardToDiscard, Card newCard = null)
    {
        // 手札から指定されたカードを捨てる
        if (hand.Contains(cardToDiscard))
        {
            hand.Remove(cardToDiscard);
            graveyard.Add(cardToDiscard);
            
            // 新しいカードがある場合は手札に追加
            if (newCard != null)
            {
                hand.Add(newCard);
                OnCardDrawn?.Invoke(this, newCard);
            }
            
            // UI更新
            GameManager.Instance.uiManager?.UpdateHand(this);
            GameManager.Instance.uiManager?.UpdateGraveyard(this);
            
            Debug.Log($"[Player] {playerName} がカード {cardToDiscard.cardName} を捨てました");
        }
        else
        {
            Debug.LogWarning($"[Player] 捨てようとしたカード {cardToDiscard.cardName} は手札にありません");
        }
    }
    
    // 修正: カードをプレイするときにイベントを発火
    public bool PlayCard(Card card, int? fieldPosition = null)
    {
        if (card == null)
        {
            Debug.LogError("PlayCard: カードがnullです");
            return false;
        }
        
        // 手札にカードが存在するか確認
        if (!hand.Contains(card))
        {
            Debug.LogWarning($"PlayCard: カード {card.cardName} は手札にありません");
            return false;
        }
        
        // エナジーコストチェック
        int costToPlay = card is CharacterCard charCard ? charCard.effectiveCost : card.cost;
        if (energy < costToPlay)
        {
            Debug.LogWarning($"PlayCard: エナジーが足りません（必要: {costToPlay}, 所持: {energy}）");
            return false;
        }
        
        // エナジーを消費
        ChangeEnergy(-costToPlay);
        
        // 手札からカードを削除
        hand.Remove(card);
        
        // カードタイプに応じた処理
        Player opponent = GameManager.Instance.GetOpponent(this);
        if (opponent == null)
        {
            Debug.LogError("PlayCard: 対戦相手が見つかりません");
            return false;
        }
        
        // 最後にプレイしたカードを記録
        SetLastPlayedCard(card);
        
        // カードの効果を発動
        card.OnPlay(this, opponent);
        
        // カードタイプに応じた処理
        if (card is CharacterCard characterCard)
        {
            // キャラクターカードをフィールドに追加
            characterField.Add(characterCard);
            
            // イベント発火
            GameEventInfo eventInfo = new GameEventInfo(
                GameEventType.CharacterSummoned,
                this,
                opponent,
                characterCard
            );
            GameManager.Instance.TriggerGameEvent(eventInfo);
        }
        else if (card is SpellCard spellCard)
        {
            // スペルカードは直接墓地へ
            graveyard.Add(spellCard);
            
            // イベント発火
            GameEventInfo eventInfo = new GameEventInfo(
                GameEventType.SpellActivated,
                this,
                opponent,
                spellCard
            );
            GameManager.Instance.TriggerGameEvent(eventInfo);
        }
        else if (card is FieldCard fieldCard)
        {
            // フィールドカードをフィールドに追加
            activeFieldCards.Add(fieldCard);
            
            // イベント発火
            GameEventInfo eventInfo = new GameEventInfo(
                GameEventType.CardPlayed,
                this,
                opponent,
                fieldCard
            );
            GameManager.Instance.TriggerGameEvent(eventInfo);
        }
        
        // UI更新
        if (GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.UpdatePlayerHand(this);
            GameManager.Instance.uiManager.UpdateEnergy(this);
            
            if (card is CharacterCard)
            {
                GameManager.Instance.uiManager.UpdateCharacterField(this);
            }
            else if (card is SpellCard)
            {
                // スペルは墓地に直行するので墓地のみ更新
                GameManager.Instance.uiManager.UpdateGraveyard(this);
            }
            else if (card is FieldCard)
            {
                GameManager.Instance.uiManager.UpdateFieldCard(this);
            }
        }
        
        // プレイされたイベントを発行
        OnCardPlayed(card);
        
        return true;
    }

    // キャラクターカードをプレイ
    private bool PlayCharacterCard(CharacterCard card, int position)
    {
        // フィールドが満杯か確認
        if (characterField.Count >= MAX_CHARACTERS) return false;
        
        // 位置が有効か確認
        if (position < 0 || position >= MAX_CHARACTERS) 
        {
            // 位置指定がない場合は次の空きスロットに配置
            position = characterField.Count;
        }
        
        // キャラクターをフィールドに追加
        characterField.Add(card);
        
        // カード効果を発動
        card.OnPlay(this, GameManager.Instance.GetOpponent(this));
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateCharacterField(this);
        
        return true;
    }
    
    // スペルカードをプレイ
    private bool PlaySpellCard(SpellCard card, int position)
    {

        // 使い捨てスペルカードの場合は効果発動後に墓地へ
        card.OnPlay(this, GameManager.Instance.GetOpponent(this));
        graveyard.Add(card);
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateGraveyard(this);
        return true;
    }
    
    // フィールドカードを場に出す処理
    public bool PlayFieldCard(FieldCard card, int position)
    {
        // コストチェック
        if (energy <= card.cost)
            return false;
            
        // 位置の有効性確認
        if (position < 0 || position >= fieldCards.Length)
        {
            Debug.LogWarning($"[Player] 無効なフィールド位置: {position}");
            return false;
        }
        
        // フィールドカードは1枚だけを保証するチェック
        // フィールドカードが1枚でも存在するなら古いのを捨て札へ
        FieldCard oldCard = null;
        for (int i = 0; i < fieldCards.Length; i++)
        {
            if (fieldCards[i] != null)
            {
                oldCard = fieldCards[i];
                // 古いカードを墓地へ移動
                fieldCards[i] = null;
                graveyard.Add(oldCard);
                
                // 除去イベント発火
                oldCard.OnRemove();
                
                // 既存カード除去メッセージ
                if (this == GameManager.Instance.player1 && GameManager.Instance.uiManager != null)
                {
                    string removeMessage = $"既存のフィールドカード「{oldCard.cardName}」を破棄し、\n新しいフィールドカードを配置します。";
                    GameManager.Instance.uiManager.ShowNotification(removeMessage, 2.0f);
                }
            }
        }
        
        // 新しいカードを配置
        fieldCards[position] = card;
        
        // コスト支払い
        energy -= card.cost;
        
        // 手札から除去
        hand.Remove(card);
        
        // フィールドカード配置メッセージ（プレイヤーの場合のみ）
        if (this == GameManager.Instance.player1 && GameManager.Instance.uiManager != null)
        {
            string fieldCardMessage = $"フィールドカード「{card.cardName}」を配置しました。\n{card.description}";
            GameManager.Instance.uiManager.ShowNotification(fieldCardMessage, 2.5f);
        }
        
        // カードの効果を発動
        card.OnPlay(this, GameManager.Instance.GetOpponent(this));
        
        // フィールド効果を再適用
        RefreshFieldEffects();
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateFieldCard(this);
        GameManager.Instance.uiManager?.UpdateEnergy(this);
        GameManager.Instance.uiManager?.UpdateHand(this);
        
        // プレイされたイベントを発行
        OnCardPlayed(card);
            
        return true;
    }

     // カードがプレイされた時のイベント処理
    public void OnCardPlayed(Card card)
    {
        // カードプレイ時の共通処理
        Debug.Log($"プレイヤー {playerName} が {card.cardName} をプレイしました");
        
        // ゲーム状態の更新
        if (GameManager.Instance != null)
        {
            GameManager.Instance.uiManager?.UpdateUI();
        }
        
        // カードタイプに応じた追加処理とメッセージ表示
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string effectMessage = "";
            
            if (card is CharacterCard characterCard)
            {
                Debug.Log($"キャラクターカード {characterCard.cardName} をフィールドに配置");
                effectMessage = $"キャラクターカード「{characterCard.cardName}」をフィールドに配置しました。\n攻撃力: {characterCard.attackPower}、防御力: {characterCard.defensePower}";
                
                // 効果の詳細を追加
                if (!string.IsNullOrEmpty(card.description))
                {
                    effectMessage += $"\n効果: {card.description}";
                }
            }
            else if (card is SpellCard spellCard)
            {
                Debug.Log($"スペルカード {spellCard.cardName} の効果を発動");
                effectMessage = $"スペルカード「{spellCard.cardName}」の効果を発動しました。\n{card.description}";
            }
            else if (card is FieldCard fieldCard)
            {
                Debug.Log($"フィールドカード {fieldCard.cardName} をフィールドに配置");
                effectMessage = $"フィールドカード「{fieldCard.cardName}」をフィールドに配置しました。\n{card.description}";
            }
            
            // 効果メッセージを表示
            if (!string.IsNullOrEmpty(effectMessage))
            {
                GameManager.Instance.uiManager.ShowNotification(effectMessage, 3.0f);
            }
        }
    }
    

    // すべてのフィールド効果を再適用
    public void RefreshFieldEffects()
    {
        // フィールドカードの効果を再適用
        foreach (FieldCard card in activeFieldCards)
        {
            card.ReapplyEffects();
        }
    }

    // フィールドカードを取り除く
    public void RemoveFieldCard(FieldCard card)
    {
        for (int i = 0; i < fieldCards.Length; i++)
        {
            if (fieldCards[i] == card)
            {
                // OnRemove の引数を削除
                card.OnRemove();
                
                // フィールドから墓地へ
                fieldCards[i] = null;
                graveyard.Add(card);
                break;
            }
        }
        
        // UI更新
        GameManager.Instance.uiManager?.UpdateFieldCard(this);
    }

    // アクティブなフィールドカードを取得（nullでないフィールドカード）
    public List<FieldCard> activeFieldCards
    {
        get
        {
            List<FieldCard> active = new List<FieldCard>();
            foreach (FieldCard card in fieldCards)
            {
                if (card != null)
                    active.Add(card);
            }
            return active;
        }
    }

    // 修正: キャラクターで攻撃するときにイベントを発火
    // AttackWithCharacterメソッドを修正
    // Player.cs の AttackWithCharacter メソッドに以下の修正を加えます
    public virtual bool AttackWithCharacter(CharacterCard attacker, CharacterCard defender)
    {
        // 攻撃条件チェック
        if (!characterField.Contains(attacker))
        {
            Debug.LogWarning($"[Player] {attacker.cardName}はフィールド上にありません");
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowNotification($"{attacker.cardName}はフィールド上にありません", 1.5f, UIManager.NotificationType.Battle);
            }
            return false;
        }
        
        if (attacker.hasAttackedThisTurn)
        {
            Debug.LogWarning($"[Player] {attacker.cardName}は既に攻撃済みです");
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowNotification($"{attacker.cardName}は既に攻撃済みです", 1.5f, UIManager.NotificationType.Battle);
            }
            return false;
        }
        
        // 先行1ターン目の攻撃制限チェック
        if (GameManager.Instance.isFirstPlayerFirstTurn && GameManager.Instance.firstPlayer == this)
        {
            Debug.LogWarning("[Player] 先行1ターン目は攻撃できません");
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowNotification("先行1ターン目は攻撃できません", 2.0f, UIManager.NotificationType.Battle);
            }
            return false;
        }
        
        // 相手のキャラクターでなければ攻撃不可
        // Card の owner プロパティを使用
        if (defender.owner == this)
        {
            Debug.LogWarning($"[Player] {defender.cardName}は自分のカードです。攻撃できません");
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowNotification("自分のカードには攻撃できません", 1.5f, UIManager.NotificationType.Battle);
            }
            return false;
        }
        
        // 戦闘処理をバトルシステムに委譲
        BattleSystem.ResolveCharacterBattle(attacker, defender);
        return true;
    }
        
    // DirectAttackメソッドを修正
    public virtual bool DirectAttack(CharacterCard attacker)
    {
        // 既存の条件チェック
        if (attacker.hasAttackedThisTurn) return false;
        if (!characterField.Contains(attacker)) return false;
        
        Player opponent = GetOpponent();
        if (opponent.characterField.Count > 0) return false;
        
        // 先行1ターン目の攻撃制限チェック
        if (GameManager.Instance != null && 
            GameManager.Instance.isFirstPlayerFirstTurn && 
            GameManager.Instance.firstPlayer == this)
        {
            Debug.Log($"[Player] 先行1ターン目の攻撃は制限されています");
            return false;
        }
        
        // 直接攻撃メッセージを表示（プレイヤーの場合のみ）
        if (this == GameManager.Instance.player1 && GameManager.Instance.uiManager != null)
        {
            string attackMessage = $"「{attacker.cardName}」（ATK:{attacker.attackPower}）で\n{opponent.playerName}に直接攻撃します！";
            GameManager.Instance.uiManager.ShowNotification(attackMessage, 2.0f);
        }
        
        // 直接攻撃イベントを発火
        GameEventInfo directAttackEvent = new GameEventInfo(
            GameEventType.DirectAttack,
            this,
            opponent,
            attacker
        );
        
        GameManager.Instance.TriggerGameEvent(directAttackEvent);
        
        // 攻撃処理
        BattleSystem.ResolveDirectAttack(attacker, opponent);
        
        // 攻撃済みフラグを立てる
        attacker.hasAttackedThisTurn = true;
        
        return true;
    }
    
    // ドローが可能か確認
    public bool CanDrawCard()
    {
        return !cannotDrawCard && deck.Count > 0;
    }
    
    // 新しいターン開始時の処理
    public void StartNewTurn()
    {
        // エナジー増加
        ChangeEnergy(GameManager.Instance.turnEnergyGain);
        
        // キャラクターの攻撃フラグをリセット
        foreach (CharacterCard card in characterField)
        {
            card.hasAttackedThisTurn = false;
        }
    }

    public Player GetOpponent()
    {
        if (this == GameManager.Instance.player1)
            return GameManager.Instance.player2;
        else
            return GameManager.Instance.player1;
    }

    // エナジーの増減
public void ChangeEnergy(int amount)
{
    energy += amount;
    
    // 最小値を0に設定
    if (energy < 0)
        energy = 0;
        
    // UI更新
    if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
    {
        GameManager.Instance.uiManager.UpdatePlayerEnergy(this);
    }
}

    // 最後にプレイしたカードの記録（効果発動時に使用）
    [SerializeField]
    private Card _lastPlayedCard;

    public Card lastPlayedCard 
    {
        get { return _lastPlayedCard; }
    }

    public void SetLastPlayedCard(Card card)
    {
        _lastPlayedCard = card;
    }

    // 特殊勝利条件やフィールド効果のチェック
    public void CheckFieldEffects(Player opponent)
    {
        // 特殊勝利条件のチェック
        foreach (CharacterCard card in characterField)
        {
            foreach (CardEffect effect in card.effects)
            {
                if (effect is SpecialVictoryEffect victoryEffect)
                {
                    victoryEffect.CheckVictoryCondition(this, opponent);
                }
            }
        }
        
        // コスト削減効果の適用
        // 手札のカードのコスト削減をリセット
        foreach (Card card in hand)
        {
            if (card is CharacterCard charCard)
            {
                charCard.temporaryCostReduction = 0;
            }
        }
        
        // フィールド上のカードの効果を適用
        foreach (CharacterCard card in characterField)
        {
            foreach (CardEffect effect in card.effects)
            {
                if (effect is CategoryCostReductionEffect costEffect)
                {
                    costEffect.ApplyTurnEffect(card, this);
                }
            }
        }
    }
}