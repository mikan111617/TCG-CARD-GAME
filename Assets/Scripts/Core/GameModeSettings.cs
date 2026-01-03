using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ゲームモード設定クラス
[System.Serializable]
public class GameModeSettings
{
    public GameMode gameMode;          // ゲームモード（AI対戦/ネットワーク対戦）
    public int player1DeckId;          // プレイヤー1のデッキID
    public int aiProfileId;            // AIプロファイルID（以前のaiDifficultyを置き換え）
}

// ゲームモード列挙型
public enum GameMode
{
    VsAI,
    VsNetwork
}

// AI難易度列挙型
public enum AIDifficulty
{
    Easy,
    Normal,
    Hard
}