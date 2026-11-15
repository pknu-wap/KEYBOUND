using System;
using System.Collections.Generic;
using UnityEngine;

// ADDED: Operators are stored explicitly so fixed answers cannot silently disagree with their equations.
public enum CalculationOperator { Add, Subtract }
public enum CalculationGenerationMode { FixedOnly, RandomOnly, Both }

[Serializable]
public class FixedCalculationProblem
{
    public List<int> Numbers = new List<int> { 12, 7 };
    public List<CalculationOperator> Operators = new List<CalculationOperator> { CalculationOperator.Add };

    public bool TryEvaluate(out int answer, out string expression)
    {
        answer = 0;
        expression = "";
        if (Numbers == null || Operators == null || Numbers.Count < 2 ||
            Operators.Count != Numbers.Count - 1) return false;
        long result = Numbers[0];
        expression = Numbers[0].ToString();
        for (int i = 0; i < Operators.Count; i++)
        {
            bool add = Operators[i] == CalculationOperator.Add;
            result += add ? (long)Numbers[i + 1] : -(long)Numbers[i + 1];
            expression += (add ? " + " : " - ") + Numbers[i + 1];
        }
        if (result < 0 || result > int.MaxValue) return false;
        answer = (int)result;
        expression += " = ?";
        return true;
    }
}

[CreateAssetMenu(fileName = "CalculationProblems", menuName = "KeyBound/Aim/Calculation Database")]
public class CalculationAimDatabase : ScriptableObject
{
    [SerializeField] private List<FixedCalculationProblem> problems = new List<FixedCalculationProblem>();
    public IReadOnlyList<FixedCalculationProblem> Problems => problems;
}
