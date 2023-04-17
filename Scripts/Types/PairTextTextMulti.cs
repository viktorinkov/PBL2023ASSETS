using System;
using UnityEngine;

namespace MatchingGameTemplate.Types
{
    [Serializable]
    public class PairTextTextMulti
    {
        [Tooltip("The first text of the pair")]
        public string firstText;

        [Tooltip("The second text group of the pair")]
        public string[] multiText;

        [Tooltip("The correct index of the answer, 0 is the first answer from the list, 1 is the second answer, etc")]
        public int correctAnswer = 0;
    }
}

