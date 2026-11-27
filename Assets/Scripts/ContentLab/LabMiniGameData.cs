namespace Keybound.ContentLab
{
    public enum LabMiniGameKind
    {
        VirusChase, CacheCollapse, PacketSorting, LeakSeal, ZoneShift
    }

    public sealed class LabMiniGameData : MiniGameData
    {
        public LabMiniGameKind Kind;
        public override int ConcurrentMissionLimit => StageCount;
    }
}
