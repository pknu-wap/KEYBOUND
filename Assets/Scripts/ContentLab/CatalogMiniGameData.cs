namespace Keybound.ContentLab
{
    public sealed class CatalogMiniGameData : MiniGameData
    {
        internal CatalogEntry Entry { get; private set; }
        internal string Feedback { get; set; }
        public override MiniGameStageMode StageMode => Entry.StageMode;

        internal static CatalogMiniGameData Create(CatalogEntry entry, MiniGameBase controller)
        {
            var data = CreateInstance<CatalogMiniGameData>();
            data.Entry = entry;
            data.Id = entry.Name;
            data.DisplayName = entry.Name;
            data.ControllerPrefab = controller;
            return data;
        }
    }
}
