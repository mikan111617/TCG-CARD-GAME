using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲームイベントタイプ - スペルカード発動タイミングの定義
public enum GameEventType
{
    CardPlayed,        // カードがプレイされた
    CharacterSummoned, // キャラクターが召喚された
    SpellActivated,    // スペルが発動された
    AttackDeclared,    // 攻撃が宣言された
    DirectAttack,      // 直接攻撃
    PhaseChanged,      // フェーズが変更された
    TurnChanged        // ターンプレイヤーが変更された
}

// ゲームイベント情報クラス
public class GameEventInfo
{
    public GameEventType eventType;
    public Player activePlayer;   // イベントを起こしたプレイヤー
    public Player targetPlayer;   // 対象となるプレイヤー（存在する場合）
    public Card sourceCard;       // イベントの発生源カード（存在する場合）
    public Card targetCard;       // 対象となるカード（存在する場合）

    // コンストラクタ
    public GameEventInfo(GameEventType type, Player active, Player target = null, Card source = null, Card target2 = null)
    {
        eventType = type;
        activePlayer = active;
        targetPlayer = target;
        sourceCard = source;
        targetCard = target2;
    }
}