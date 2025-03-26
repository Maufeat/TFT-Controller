namespace TFTController.Models
{
    public enum RegionCategory
    {
        BoardHex,
        BenchSlot,
        ShopSlot,
        ItemArea,
        AugmentMain,
        AugmentReroll,
        AugmentHide
    }

    public class NavigableRegion
    {
        public RegionCategory Category { get; set; }
        public string Name { get; set; }
        public PointF[] Polygon { get; set; }
        public RectangleF? Rectangle { get; set; }
        public NavigableRegion Up { get; set; }
        public NavigableRegion Down { get; set; }
        public NavigableRegion Left { get; set; }
        public NavigableRegion Right { get; set; }

        public bool Contains(Point p)
        {
            if (Polygon != null)
                return IsPointInPolygon(Polygon, p);
            else if (Rectangle.HasValue)
                return Rectangle.Value.Contains(p);
            return false;
        }

        public PointF GetCenter()
        {
            if (Polygon != null)
            {
                float sumX = 0, sumY = 0;
                foreach (var pt in Polygon)
                {
                    sumX += pt.X;
                    sumY += pt.Y;
                }
                return new PointF(sumX / Polygon.Length, sumY / Polygon.Length);
            }
            else if (Rectangle.HasValue)
            {
                var r = Rectangle.Value;
                return new PointF(r.X + r.Width / 2, r.Y + r.Height / 2);
            }
            return new PointF(0, 0);
        }

        private bool IsPointInPolygon(PointF[] poly, PointF test)
        {
            bool result = false;
            int j = poly.Length - 1;
            for (int i = 0; i < poly.Length; i++)
            {
                if ((poly[i].Y < test.Y && poly[j].Y >= test.Y || poly[j].Y < test.Y && poly[i].Y >= test.Y) &&
                    (poly[i].X + (test.Y - poly[i].Y) / (poly[j].Y - poly[i].Y) * (poly[j].X - poly[i].X) < test.X))
                {
                    result = !result;
                }
                j = i;
            }
            return result;
        }
    }
}
