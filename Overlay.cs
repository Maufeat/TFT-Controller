using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TFTController.Managers;
using TFTController.Models;
using TFTController.Utilities;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using TFTController.Renderers;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Timer = System.Windows.Forms.Timer;

namespace TFTController
{
    public partial class Overlay : Form
    {
        private Timer timer;
        private Point cursorPos;
        private System.Collections.Generic.List<NavigableRegion> regions;
        private NavigableRegion selectedRegion;
        private System.Collections.Generic.List<NavigableRegion> savedNormalRegions;
        private NavigableRegion savedSelectedRegion;
        private bool isAugmentMode = false;

        // For cursor animation.
        private bool isAnimating = false;
        private Point animationStartPos;
        private Point animationTargetPos;
        private DateTime animationStartTime;
        private TimeSpan animationDuration = TimeSpan.FromMilliseconds(200);

        // XInput fields.
        private ushort previousButtons = 0;
        private bool isHolding = false;
        private bool isRightHolding = false;
        private byte previousRightTrigger = 0;

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        private static extern int XInputGetState(int dwUserIndex, out XInputState pState);
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [StructLayout(LayoutKind.Sequential)]
        private struct XInputState
        {
            public uint dwPacketNumber;
            public XInputGamepad Gamepad;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct XInputGamepad
        {
            public ushort wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        public Overlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.Green;
            TransparencyKey = Color.Green;
            AllowTransparency = true;
            DoubleBuffered = true;
            cursorPos = new Point(100, 100);
            // Register a hotkey to exit.
            InputSimulator.RegisterHotKey(Handle, 1, 0, (uint)Keys.Escape);

            regions = LayoutManager.BuildNormalLayout();
            selectedRegion = regions.FirstOrDefault(r => r.Category == RegionCategory.BoardHex);
            CenterCursorOnSelected();

            timer = new Timer { Interval = 16 };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw regions and cursor.
            // For better visuals, you could pass selectedRegion to the renderer.
            OverlayRenderer.Draw(e.Graphics, regions, selectedRegion, cursorPos);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            InputSimulator.UnregisterHotKey(Handle, 1);
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == 1)
                Application.Exit();
            base.WndProc(ref m);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            bool augmentActive = IsAugmentScreenActive();
            if (augmentActive && !isAugmentMode)
            {
                savedNormalRegions = regions;
                savedSelectedRegion = selectedRegion;
                regions = LayoutManager.BuildAugmentLayout(Width);
                isAugmentMode = true;
                CenterCursorOnSelected();
            }
            // Process XInput.
            XInputState state;
            int result = XInputGetState(0, out state);
            if (result == InputSimulator.ERROR_SUCCESS)
            {
                var gamepad = state.Gamepad;
                HandleDPadInput(gamepad);
                HandleThumbstick(gamepad);
                HandleAButton(gamepad);
                HandleRightTrigger(gamepad);
                HandleButtonMapping(gamepad);
                previousButtons = gamepad.wButtons;
            }
            AnimateCursor();
            UpdateHoverRegion();
            Invalidate();
        }

        #region Input Handling

        private void HandleDPadInput(XInputGamepad gamepad)
        {
            ushort pressed = (ushort)(gamepad.wButtons & ~previousButtons);
            if ((pressed & 0x0001) != 0 && selectedRegion.Up != null)
            {
                selectedRegion = selectedRegion.Up;
                CenterCursorOnSelected();
            }
            if ((pressed & 0x0002) != 0 && selectedRegion.Down != null)
            {
                selectedRegion = selectedRegion.Down;
                CenterCursorOnSelected();
            }
            if ((pressed & 0x0004) != 0 && selectedRegion.Left != null)
            {
                selectedRegion = selectedRegion.Left;
                CenterCursorOnSelected();
            }
            if ((pressed & 0x0008) != 0 && selectedRegion.Right != null)
            {
                selectedRegion = selectedRegion.Right;
                CenterCursorOnSelected();
            }
        }

        private void HandleAButton(XInputGamepad gamepad)
        {
            ushort aPressed = (ushort)(gamepad.wButtons & ~previousButtons & 0x1000);
            if (aPressed != 0)
            {
                if (isAugmentMode)
                {
                    InputSimulator.SimulateMouseClick(cursorPos);
                    RestoreNormalLayout();
                }
                else if (selectedRegion != null &&
                         (selectedRegion.Category == RegionCategory.BoardHex ||
                          selectedRegion.Category == RegionCategory.BenchSlot ||
                          selectedRegion.Category == RegionCategory.ItemArea))
                {
                    isHolding = !isHolding;
                    if (isHolding)
                        InputSimulator.SimulateMouseDown(cursorPos);
                    else
                        InputSimulator.SimulateMouseUp(cursorPos);
                }
                else
                {
                    InputSimulator.SimulateMouseClick(cursorPos);
                }
            }
        }

        private void HandleRightTrigger(XInputGamepad gamepad)
        {
            if (isAugmentMode)
            {
                previousRightTrigger = gamepad.bRightTrigger;
                return;
            }
            byte threshold = 30;
            if (gamepad.bRightTrigger > threshold && previousRightTrigger <= threshold)
            {
                if (selectedRegion != null &&
                    (selectedRegion.Category == RegionCategory.BoardHex ||
                     selectedRegion.Category == RegionCategory.BenchSlot ||
                     selectedRegion.Category == RegionCategory.ItemArea))
                {
                    isRightHolding = !isRightHolding;
                    if (isRightHolding)
                        InputSimulator.SimulateRightMouseDown(cursorPos);
                    else
                        InputSimulator.SimulateRightMouseUp(cursorPos);
                }
                else
                {
                    InputSimulator.SimulateRightMouseClick(cursorPos);
                }
            }
            previousRightTrigger = gamepad.bRightTrigger;
        }

        private void HandleButtonMapping(XInputGamepad gamepad)
        {
            if ((gamepad.wButtons & InputSimulator.XINPUT_GAMEPAD_X) != 0 && (previousButtons & InputSimulator.XINPUT_GAMEPAD_X) == 0)
                InputSimulator.SimulateKeyPress(0x44);
            if ((gamepad.wButtons & InputSimulator.XINPUT_GAMEPAD_Y) != 0 && (previousButtons & InputSimulator.XINPUT_GAMEPAD_Y) == 0)
                InputSimulator.SimulateKeyPress(0x46);
            if ((gamepad.wButtons & InputSimulator.XINPUT_GAMEPAD_LEFT_SHOULDER) != 0 && (previousButtons & InputSimulator.XINPUT_GAMEPAD_LEFT_SHOULDER) == 0)
                InputSimulator.SimulateKeyPress(0x45);
        }

        private void HandleThumbstick(XInputGamepad gamepad)
        {
            float normLX = Math.Max(-1, (float)gamepad.sThumbLX / 32767);
            float normLY = Math.Max(-1, (float)gamepad.sThumbLY / 32767);
            if (Math.Abs(normLX) < 0.2f) normLX = 0;
            if (Math.Abs(normLY) < 0.2f) normLY = 0;
            if (normLX != 0 || normLY != 0)
            {
                if (isHolding) { InputSimulator.SimulateMouseUp(cursorPos); isHolding = false; }
                if (isRightHolding) { InputSimulator.SimulateRightMouseUp(cursorPos); isRightHolding = false; }
                isAnimating = false;
            }
            int moveSpeed = 10;
            cursorPos.X += (int)(normLX * moveSpeed);
            cursorPos.Y -= (int)(normLY * moveSpeed);
            cursorPos.X = Math.Max(0, Math.Min(Width, cursorPos.X));
            cursorPos.Y = Math.Max(0, Math.Min(Height, cursorPos.Y));
            SetCursorPos(cursorPos.X, cursorPos.Y);
        }

        #endregion

        #region Cursor Animation

        private void CenterCursorOnSelected()
        {
            if (selectedRegion == null) return;
            PointF center = selectedRegion.GetCenter();
            animationStartPos = cursorPos;
            animationTargetPos = new Point((int)center.X, (int)center.Y);
            animationStartTime = DateTime.Now;
            isAnimating = true;
        }

        private void AnimateCursor()
        {
            if (isAnimating)
            {
                double t = (DateTime.Now - animationStartTime).TotalMilliseconds / animationDuration.TotalMilliseconds;
                if (t >= 1.0)
                {
                    t = 1.0;
                    isAnimating = false;
                }
                int newX = (int)(animationStartPos.X + t * (animationTargetPos.X - animationStartPos.X));
                int newY = (int)(animationStartPos.Y + t * (animationTargetPos.Y - animationStartPos.Y));
                cursorPos = new Point(newX, newY);
                SetCursorPos(cursorPos.X, cursorPos.Y);
            }
        }

        #endregion

        #region OCR and Layout Restoration

        private bool IsAugmentScreenActive()
        {
            // Capture a region where the augment UI is expected.
            Rectangle captureRegion = new Rectangle(1920 / 2 - 99, 967, 200, 50);
            try
            {
                using (Bitmap bmp = new Bitmap(captureRegion.Width, captureRegion.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(captureRegion.Location, Point.Empty, captureRegion.Size);
                    }
                    Mat mat = ImageHelper.BitmapToMat(bmp);
                    using (Image<Bgr, byte> source = mat.ToImage<Bgr, byte>())
                    {
                        using (Image<Bgr, byte> template = new Image<Bgr, byte>("OCR/augment_template.png"))
                        {
                            using (Image<Gray, float> result = source.MatchTemplate(template, TemplateMatchingType.CcorrNormed))
                            {
                                double[] minValues, maxValues;
                                Point[] minLocations, maxLocations;
                                result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                                return maxValues[0] > 0.9;
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void RestoreNormalLayout()
        {
            if (savedNormalRegions != null)
            {
                regions = savedNormalRegions;
                selectedRegion = savedSelectedRegion;
                isAugmentMode = false;
                CenterCursorOnSelected();
            }
        }

        #endregion

        #region Miscellaneous

        private void UpdateHoverRegion()
        {
            Point p = cursorPos;
            var hovered = regions.FirstOrDefault(r => r.Contains(p));
        }

        #endregion
    }
}
