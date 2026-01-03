using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CardDatabaseData
{
    public List<CharacterCard> characterCards = new List<CharacterCard>();
    public List<SpellCard> spellCards = new List<SpellCard>();
    public List<FieldCard> fieldCards = new List<FieldCard>();
}