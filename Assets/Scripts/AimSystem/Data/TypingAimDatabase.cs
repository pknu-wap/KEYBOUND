using System.Collections.Generic;
using UnityEngine;

// ADDED: Editable word list; no randomly constructed strings.
[CreateAssetMenu(fileName = "TypingWords", menuName = "KeyBound/Aim/Typing Word Database")]
public class TypingAimDatabase : ScriptableObject
{
    [SerializeField] private List<string> words = new List<string>
    {
        "CAT", "DOG", "SUN", "KEY", "MAP", "BOX", "RUN", "RED", "TOP", "WIN",
        "GAME", "CODE", "TIME", "MOVE", "PLAY", "DATA", "SAVE", "OPEN", "EXIT", "TYPE",
        "ERROR", "INPUT", "STAGE", "SCORE", "START", "RESET", "LEVEL", "TIMER", "POWER", "CLICK",
        "SYSTEM", "PLAYER", "RANDOM", "SCREEN", "MEMORY", "TARGET", "ATTACK", "ESCAPE", "DELETE", "UPDATE"
    };
    public IReadOnlyList<string> Words => words;
}
