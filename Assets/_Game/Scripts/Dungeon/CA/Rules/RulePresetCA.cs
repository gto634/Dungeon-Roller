using System;
using System.Collections.Generic;
using UnityEngine;

public enum RuleEvaluationCA { FirstMatch, Sequential }

[CreateAssetMenu(fileName = "RulePresetCA", menuName = "Dungeon/CA/Rules/Rule Preset")]
public class RulePresetCA : ScriptableObject
{
    public List<RuleCA> rules = new List<RuleCA>();

    [SerializeField] RuleEvaluationCA evaluation;
    public RuleEvaluationCA Evaluation => evaluation;
    public bool ChangesStates => rules != null && rules.Exists(rule => rule != null && rule.ChangesStates);
    [Min(0)] public int defaultState;

    public int ApplyRules(int currentState, int livingNeighbors)
    {
        if (rules != null)
        {
            foreach (RuleCA rule in rules)
            {
                if (rule is OperatorRule binary && binary.TryApplyRule(currentState, livingNeighbors, out int nextState))
                    return nextState;
            }
        }

        return defaultState;
    }

    public bool ValidateFor(Type cellType, out string error)
    {
        if (!ValidateRules(out error)) return false;
        foreach (RuleCA rule in rules)
            if (!rule.CellType.IsAssignableFrom(cellType))
            { error = $"Rule {rule.name} expects {rule.CellType.Name}, not {cellType.Name}."; return false; }
        if (cellType == typeof(BinaryCellCA) && defaultState > 1)
        { error = "Binary cells require a default state of 0 or 1."; return false; }
        return true;
    }

    public bool ValidateRules(out string error)
    {
        if (defaultState < 0)
        {
            error = "The default state must be non-negative.";
            return false;
        }

        if (rules == null || rules.Count == 0)
        {
            error = "A rule preset must contain at least one rule.";
            return false;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            if (rules[i] == null)
            {
                error = $"Rule {i} is missing.";
                return false;
            }

            if (!rules[i].ValidateRule(out string ruleError))
            {
                error = $"Rule {i} ({rules[i].name}): {ruleError}";
                return false;
            }
        }

        if (evaluation != RuleEvaluationCA.FirstMatch && evaluation != RuleEvaluationCA.Sequential)
        { error = "Unknown rule evaluation mode."; return false; }
        if (evaluation == RuleEvaluationCA.FirstMatch && rules.Exists(rule => !rule.SupportsFirstMatch))
        { error = "First Match requires cell condition rules. Use Sequential for placement or progression."; return false; }
        error = null;
        return true;
    }
}
