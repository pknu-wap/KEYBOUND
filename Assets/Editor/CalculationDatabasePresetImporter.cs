#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ADDED: Populate a selected CalculationAimDatabase with 30 validated preset problems.
public static class CalculationDatabasePresetImporter
{
    private static readonly string[] Expressions =
    {
        "2 + 3", "4 + 5", "7 - 3", "9 - 7", "6 + 2",
        "8 - 7", "3 + 4", "9 - 3", "5 - 2", "4 - 4",
        "12 + 7", "15 - 3", "8 + 9", "14 + 6", "18 - 5",
        "9 + 7", "11 + 7", "20 - 6", "8 + 7", "13 + 8",
        "17 + 8 + 3", "25 - 9 + 6", "12 + 15 - 8", "30 - 12 + 7",
        "18 + 9 - 5", "42 - 17 + 8", "15 + 12 + 9", "50 - 23 - 8",
        "27 + 14 - 6", "38 - 16 + 9"
    };

    [MenuItem("Tools/KEYBOUND/Calculation/Replace Selected Database With 30 Presets")]
    private static void Import()
    {
        CalculationAimDatabase database = Selection.activeObject as CalculationAimDatabase;
        if (database == null) return;
        if (!EditorUtility.DisplayDialog("KEYBOUND - Import 30 Problems",
            "선택한 CalculationAimDatabase의 기존 문제 목록을 30개 추천 문제로 교체합니다. 계속할까요?",
            "교체", "취소")) return;

        List<FixedCalculationProblem> items = new List<FixedCalculationProblem>();
        foreach (string expression in Expressions)
        {
            string[] tokens = expression.Split(' ');
            FixedCalculationProblem problem = new FixedCalculationProblem
            {
                Numbers = new List<int>(),
                Operators = new List<CalculationOperator>()
            };
            for (int i = 0; i < tokens.Length; i++)
            {
                if (i % 2 == 0) problem.Numbers.Add(int.Parse(tokens[i]));
                else problem.Operators.Add(tokens[i] == "+" ? CalculationOperator.Add : CalculationOperator.Subtract);
            }
            if (!problem.TryEvaluate(out int answer, out string _))
            {
                Debug.LogError("[KEYBOUND] Invalid preset: " + expression);
                return;
            }
            items.Add(problem);
        }

        Undo.RecordObject(database, "Import KEYBOUND Calculation Presets");
        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty problems = serializedDatabase.FindProperty("problems");
        if (problems == null)
        {
            Debug.LogError("[KEYBOUND] CalculationAimDatabase.problems field not found.");
            return;
        }
        problems.arraySize = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            SerializedProperty item = problems.GetArrayElementAtIndex(i);
            SerializedProperty numbers = item.FindPropertyRelative("Numbers");
            SerializedProperty operators = item.FindPropertyRelative("Operators");
            numbers.arraySize = items[i].Numbers.Count;
            operators.arraySize = items[i].Operators.Count;
            for (int j = 0; j < numbers.arraySize; j++)
                numbers.GetArrayElementAtIndex(j).intValue = items[i].Numbers[j];
            for (int j = 0; j < operators.arraySize; j++)
                operators.GetArrayElementAtIndex(j).enumValueIndex = (int)items[i].Operators[j];
        }
        serializedDatabase.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log("[KEYBOUND] Imported " + items.Count + " fixed calculation problems into " + database.name, database);
        EditorUtility.DisplayDialog("KEYBOUND", "30개 계산문제를 등록했습니다.", "확인");
    }

    [MenuItem("Tools/KEYBOUND/Calculation/Replace Selected Database With 30 Presets", true)]
    private static bool ValidateImport() => Selection.activeObject is CalculationAimDatabase;
}
#endif
