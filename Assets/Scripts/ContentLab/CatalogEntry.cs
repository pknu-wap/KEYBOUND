using System;

namespace Keybound.ContentLab
{
    internal sealed class CatalogEntry
    {
        internal readonly string Name;
        internal readonly string Instructions;
        internal readonly MiniGameStageMode StageMode;
        internal readonly Action<CatalogRun> Configure;

        internal CatalogEntry(string name, string instructions, Action<CatalogRun> configure,
            MiniGameStageMode stageMode = MiniGameStageMode.OneStageAlone)
        {
            Name = name;
            Instructions = instructions;
            Configure = configure;
            StageMode = stageMode;
        }
    }
}
