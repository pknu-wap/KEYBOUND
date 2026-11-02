
using System.Collections.Generic;
using UnityEngine;

public class AimManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AimConditionGenerator generator;
    [SerializeField] private AimConditionProcessor processor;

    [Header("Aim Generation Settings")]
    [SerializeField]
    private List<AimGenerationSettings> aimSettings =
        new List<AimGenerationSettings>();

    public AimData CurrentAim => processor != null
        ? processor.CurrentAim
        : null;

    public bool StartRandomAim()
    {
        if (generator == null || processor == null)
        {
            Debug.LogError(
                "[AimManager] Generator or Processor missing.",
                this
            );
            return false;
        }

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

        int index = Random.Range(0, enabledSettings.Count);

        AimGenerationSettings selected = enabledSettings[index];

        Debug.Log(
            $"[AimManager] Selected aim type: {selected.AimType}",
            this
        );

        AimData generatedAim = generator.Generate(selected);

        if (generatedAim == null)
        {
            Debug.LogError(
                "[AimManager] Aim generation failed.",
                this
            );
            return false;
        }

        processor.StartAim(generatedAim);

        return processor.IsAimActive;
    }
}
