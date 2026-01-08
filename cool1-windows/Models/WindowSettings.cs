namespace Cool1Windows.Models
{
    public class WindowSettings
    {
        public double Width { get; set; } = 500;
        public double Height { get; set; } = 700;
        public double Left { get; set; } = -1;
        public double Top { get; set; } = -1;
        public bool IsMaximized { get; set; } = false;
        public string SortMode { get; set; } = "手动排序";
    }
}
