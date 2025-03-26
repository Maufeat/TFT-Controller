using TFTController.Models;

namespace TFTController.Renderers
{
    public static class OverlayRenderer
    {
        public static void Draw(Graphics g,
                                  IEnumerable<NavigableRegion> regions,
                                  NavigableRegion selectedRegion,
                                  Point cursorPos)
        {
            foreach (var region in regions)
            {
                bool isSelected = region == selectedRegion;
                if (region.Polygon != null)
                {
                    if (isSelected)
                        g.DrawPolygon(new Pen(Color.Lime, 3), region.Polygon);
                    //else
                        //g.DrawPolygon(new Pen(Color.FromArgb(178, 255, 255, 255), 1), region.Polygon);

                }
                else if (region.Rectangle.HasValue)
                {
                    RectangleF rect = region.Rectangle.Value;
                    if (isSelected)
                        g.DrawRectangle(new Pen(Color.Lime, 3), rect.X, rect.Y, rect.Width, rect.Height);
                    //else
                        //g.DrawRectangle(new Pen(Color.FromArgb(178, 255, 255, 255), 1), rect.X, rect.Y, rect.Width, rect.Height);
                }
            }
            // Draw the cursor.
            int cursorSize = 20;
            g.FillEllipse(Brushes.Red, cursorPos.X - cursorSize / 2, cursorPos.Y - cursorSize / 2, cursorSize, cursorSize);
        }
    }
}
