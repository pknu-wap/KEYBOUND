using System.Linq;

namespace Keybound.ContentLab
{
    internal static class LabCatalog
    {
        internal static readonly CatalogEntry[] Missions = MissionPathRules.Entries
            .Concat(MissionSignalRules.Entries)
            .Concat(MissionDataRules.Entries)
            .Concat(MissionSenseRules.Entries)
            .Concat(ReferenceMissionA.Entries)
            .Concat(ReferenceMissionB.Entries)
            .ToArray();

        internal static readonly CatalogEntry[] MiniGames = MiniActionRules.Entries
            .Concat(MiniBoardRules.Entries)
            .Concat(MiniSystemRules.Entries)
            .Concat(MiniMatchRules.Entries)
            .Concat(ReferenceMiniA.Entries)
            .Concat(ReferenceMiniB.Entries)
            .ToArray();

        internal static int ReferenceMissionCount => ReferenceMissionA.Entries.Length + ReferenceMissionB.Entries.Length;
        internal static int ReferenceMiniGameCount => ReferenceMiniA.Entries.Length + ReferenceMiniB.Entries.Length;
    }
}
