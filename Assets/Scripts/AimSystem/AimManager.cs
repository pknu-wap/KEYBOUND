
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AimConditionGenerator generator;
    [SerializeField] private AimConditionProcessor processor;
    // ADDED: Dedicated generators for new aim types.
    [SerializeField] private TypingAimGenerator typingGenerator;
    [SerializeField] private CalculationAimGenerator calculationGenerator;

    // ADDED: ScriptableObject 설정 에셋 목록
    [Header("Aim Generation Settings")]
    [SerializeField]
    private List<AimGenerationSettings> aimSettings =
        new List<AimGenerationSettings>();

    public AimData CurrentAim =>
        processor != null ? processor.CurrentAim : null;

    public bool StartRandomAim()
    {
        return StartRandomAim(0);
    }

    internal bool StartRandomAim(int campaignStage)
    {
        if (processor == null)
        {
            Debug.LogError(
                "[AimManager] Generator or Processor missing.",
                this
            );

            return false;
        }

        // ADDED: 활성화된 ScriptableObject 설정만 선택
        List<AimGenerationSettings> enabledSettings =
            aimSettings.FindAll(
                settings => settings != null && settings.Enabled
            );

        if (enabledSettings.Count == 0)
        {
            Debug.LogError(
                "[AimManager] No enabled aim settings.",
                this
            );

            return false;
        }

        if (campaignStage > 0)
        {
            var availableSettings = enabledSettings.FindAll(
                settings => CampaignMissionBalance.IsAvailable(settings.AimType, campaignStage));
            if (availableSettings.Count > 0)
                enabledSettings = availableSettings;
        }

        AimGenerationSettings selected = enabledSettings[Random.Range(0, enabledSettings.Count)];

        Debug.Log(
            $"[AimManager] Selected aim type: {selected.AimType}",
            this
        );

        return StartAim(selected, campaignStage);
    }

    public bool StartAim(AimGenerationSettings selected) => StartAim(selected, 0);

    private bool StartAim(AimGenerationSettings selected, int campaignStage)
    {
        if (processor == null || selected == null || !selected.Enabled)
            return false;

        Key origin = Key.None;
        if (campaignStage > 0)
            processor.TryGetPlayerKey(out origin);

        // ADDED: Route each aim type to its responsible generator.
        AimData generatedAim;
        switch (selected.AimType)
        {
            case AimType.Typing:
                generatedAim = typingGenerator != null ? typingGenerator.Generate(selected, campaignStage) : null;
                break;
            case AimType.Calculation:
                generatedAim = calculationGenerator != null ? calculationGenerator.Generate(selected, campaignStage) : null;
                break;
            default:
                generatedAim = generator != null ? generator.Generate(selected, campaignStage, origin) : null;
                break;
        }
        if (generatedAim == null)
            Debug.LogError($"[AimManager] Generation failed: {selected.AimType}. Check generator references/settings.", this);

        if (generatedAim == null)
        {
            return false;
        }

        if (campaignStage > 0 && generatedAim.Type != AimType.Sequence)
            generatedAim.TravelSteps = CampaignMissionRoute.TravelSteps(origin, generatedAim);

        processor.StartAim(generatedAim);

        return processor.IsAimActive;
    }
}
