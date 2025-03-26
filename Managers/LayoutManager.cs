using TFTController.Models;

namespace TFTController.Managers
{
    public static class LayoutManager
    {
        public static List<NavigableRegion> BuildNormalLayout()
        {
            var regions = new List<NavigableRegion>();

            // Build board hexes.
            var rowEdges = new (PointF Left, PointF Right)[]
            {
                ( new PointF(560f, 450f), new PointF(1250f, 450f) ),
                ( new PointF(610f, 520f), new PointF(1320f, 520f) ),
                ( new PointF(540f, 600f), new PointF(1280f, 600f) ),
                ( new PointF(590f, 675f), new PointF(1350f, 675f) )
            };
            float hexRadius = 45f;
            for (int row = 0; row < 4; row++)
            {
                var (rowLeft, rowRight) = rowEdges[row];
                for (int col = 0; col < 7; col++)
                {
                    float u = col / 6f;
                    PointF center = Lerp(rowLeft, rowRight, u);
                    var hexPolygon = CreateFlatTopHex(center, hexRadius);
                    regions.Add(new NavigableRegion
                    {
                        Category = RegionCategory.BoardHex,
                        Name = $"Hex({row},{col})",
                        Polygon = hexPolygon
                    });
                }
            }
            // Bench slots.
            float benchY = 740f, benchStartX = 380f, benchWidth = 90f, benchHeight = 80f, benchSpacing = 117f;
            for (int i = 0; i < 9; i++)
            {
                float x = benchStartX + i * benchSpacing;
                regions.Add(new NavigableRegion
                {
                    Category = RegionCategory.BenchSlot,
                    Name = $"BenchSlot({i})",
                    Rectangle = new RectangleF(x, benchY, benchWidth, benchHeight)
                });
            }
            // Shop slots.
            float shopY = 930f, shopStartX = 480f, shopSlotWidth = 190f, shopSlotHeight = 137f, shopSpacing = shopSlotWidth + 12;
            for (int i = 0; i < 5; i++)
            {
                float x = shopStartX + i * shopSpacing;
                regions.Add(new NavigableRegion
                {
                    Category = RegionCategory.ShopSlot,
                    Name = $"ShopSlot({i})",
                    Rectangle = new RectangleF(x, shopY, shopSlotWidth, shopSlotHeight)
                });
            }
            // Item panel.
            float itemPanelX = 10f, itemPanelY = 275f, itemPanelWidth = 40f, itemPanelHeight = 500f;
            float itemHeight = itemPanelHeight / 10f;
            for (int i = 0; i < 10; i++)
            {
                float y = itemPanelY + i * itemHeight;
                regions.Add(new NavigableRegion
                {
                    Category = RegionCategory.ItemArea,
                    Name = $"ItemSlot({i})",
                    Rectangle = new RectangleF(itemPanelX, y, itemPanelWidth, itemHeight)
                });
            }
            InitializeNormalAdjacency(regions);
            return regions;
        }

        public static List<NavigableRegion> BuildAugmentLayout(int formWidth)
        {
            var regions = new List<NavigableRegion>();

            // Augment layout using specified positions.
            int augmentSlotCount = 3;
            int centerX = formWidth / 2, centerY = 300;
            int rerollButtonY = centerY + 540;
            int columnWidth = 350;
            int mainButtonHeight = 500;
            int rerollButtonHeight = 50;
            int spacingBetweenColumns = 55;
            int totalWidth = augmentSlotCount * columnWidth + (augmentSlotCount - 1) * spacingBetweenColumns;
            int startX = centerX - totalWidth / 2;

            NavigableRegion[] mainRegions = new NavigableRegion[augmentSlotCount];
            NavigableRegion[] rerollRegions = new NavigableRegion[augmentSlotCount];

            for (int i = 0; i < augmentSlotCount; i++)
            {
                int x = startX + i * (columnWidth + spacingBetweenColumns);
                // Main augment region.
                RectangleF mainRect = new RectangleF(x, centerY, columnWidth, mainButtonHeight);
                mainRegions[i] = new NavigableRegion
                {
                    Category = RegionCategory.AugmentMain,
                    Name = $"AugmentMain({i})",
                    Rectangle = mainRect
                };
                regions.Add(mainRegions[i]);

                // Reroll button.
                RectangleF rerollRect = new RectangleF(x + 125, rerollButtonY, 100, rerollButtonHeight);
                rerollRegions[i] = new NavigableRegion
                {
                    Category = RegionCategory.AugmentReroll,
                    Name = $"AugmentReroll({i})",
                    Rectangle = rerollRect
                };
                regions.Add(rerollRegions[i]);

                // Vertical adjacency.
                mainRegions[i].Down = rerollRegions[i];
                rerollRegions[i].Up = mainRegions[i];
            }

            // "Hide augment" button.
            RectangleF hideRect = new RectangleF(850, 955, 220, 70);
            NavigableRegion hideRegion = new NavigableRegion
            {
                Category = RegionCategory.AugmentHide,
                Name = "AugmentHide",
                Rectangle = hideRect
            };
            regions.Add(hideRegion);

            // Horizontal adjacency.
            for (int i = 0; i < augmentSlotCount; i++)
            {
                if (i > 0)
                {
                    mainRegions[i].Left = mainRegions[i - 1];
                    rerollRegions[i].Left = rerollRegions[i - 1];
                }
                if (i < augmentSlotCount - 1)
                {
                    mainRegions[i].Right = mainRegions[i + 1];
                    rerollRegions[i].Right = rerollRegions[i + 1];
                }
                rerollRegions[i].Down = hideRegion;
            }
            if (augmentSlotCount > 0)
                hideRegion.Up = rerollRegions[augmentSlotCount / 2];

            return regions;
        }

        private static void InitializeNormalAdjacency(List<NavigableRegion> regions)
        {
            var boardHexes = regions.Where(r => r.Category == RegionCategory.BoardHex).ToList();
            var benchSlots = regions.Where(r => r.Category == RegionCategory.BenchSlot).ToList();
            var shopSlots = regions.Where(r => r.Category == RegionCategory.ShopSlot).ToList();
            var itemPanelSlots = regions.Where(r => r.Category == RegionCategory.ItemArea).ToList();

            NavigableRegion GetBoardHex(int row, int col) => boardHexes[row * 7 + col];

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var current = GetBoardHex(row, col);
                    current.Up = row > 0 ? GetBoardHex(row - 1, col) : itemPanelSlots.FirstOrDefault();
                    current.Down = row < 3 ? GetBoardHex(row + 1, col) : benchSlots[Math.Min(col, benchSlots.Count - 1)];
                    current.Left = col > 0 ? GetBoardHex(row, col - 1) : itemPanelSlots.FirstOrDefault();
                    current.Right = col < 6 ? GetBoardHex(row, col + 1) : itemPanelSlots.FirstOrDefault();
                }
            }
            for (int i = 0; i < benchSlots.Count; i++)
            {
                var bench = benchSlots[i];
                bench.Up = GetBoardHex(3, Math.Min(i, 6));
                bench.Down = shopSlots[Math.Min(i, shopSlots.Count - 1)];
                bench.Left = i > 0 ? benchSlots[i - 1] : itemPanelSlots.FirstOrDefault();
                bench.Right = i < benchSlots.Count - 1 ? benchSlots[i + 1] : itemPanelSlots.FirstOrDefault();
            }
            for (int i = 0; i < shopSlots.Count; i++)
            {
                var shop = shopSlots[i];
                shop.Up = benchSlots[Math.Min(i, benchSlots.Count - 1)];
                shop.Left = i > 0 ? shopSlots[i - 1] : itemPanelSlots.FirstOrDefault();
                shop.Right = i < shopSlots.Count - 1 ? shopSlots[i + 1] : itemPanelSlots.FirstOrDefault();
            }
            for (int i = 0; i < itemPanelSlots.Count; i++)
            {
                var item = itemPanelSlots[i];
                item.Up = i > 0 ? itemPanelSlots[i - 1] : boardHexes.FirstOrDefault();
                item.Down = i < itemPanelSlots.Count - 1 ? itemPanelSlots[i + 1] : shopSlots.FirstOrDefault();
                item.Left = null;
                item.Right = boardHexes.FirstOrDefault();
            }
        }

        private static PointF Lerp(PointF A, PointF B, float t)
        {
            return new PointF(A.X + (B.X - A.X) * t, A.Y + (B.Y - A.Y) * t);
        }

        private static PointF[] CreateFlatTopHex(PointF center, float radius)
        {
            PointF[] points = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                float angleDeg = -30f + 60f * i;
                float angleRad = (float)(Math.PI / 180.0 * angleDeg);
                float x = center.X + radius * (float)Math.Cos(angleRad);
                float y = center.Y + radius * (float)Math.Sin(angleRad);
                points[i] = new PointF(x, y);
            }
            return points;
        }
    }
}
