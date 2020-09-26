namespace RTSCamera
{
    public struct SelectionItem
    {
        public bool IsLocalizationId;
        public string Data;
        public string Variation;

        public SelectionItem(bool isLocalizationId, string data, string variation = null)
        {
            this.IsLocalizationId = isLocalizationId;
            this.Data = data;
            this.Variation = variation;
        }
    }
}
