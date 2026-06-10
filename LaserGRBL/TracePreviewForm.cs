using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LaserGRBL
{
    internal sealed class TracePreviewSegment
    {
        public TracePreviewSegment(PointF start, PointF end, bool laserOn)
        {
            Start = start;
            End = end;
            LaserOn = laserOn;
        }

        public PointF Start { get; private set; }
        public PointF End { get; private set; }
        public bool LaserOn { get; private set; }
    }

    internal static class TracePreviewParser
    {
        public static List<TracePreviewSegment> Parse(string gcode)
        {
            List<TracePreviewSegment> segments = new List<TracePreviewSegment>();
            GrblCommand.StatePositionBuilder state = new GrblCommand.StatePositionBuilder();
            string[] lines = (gcode ?? string.Empty).Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                using (GrblCommand command = new GrblCommand(line))
                {
                    command.BuildHelper();
                    state.AnalyzeCommand(command, false);
                    if (command.IsMovement && state.TrueMovement())
                    {
                        PointF start = new PointF((float)state.X.Previous, (float)state.Y.Previous);
                        PointF end = new PointF((float)state.X.Number, (float)state.Y.Number);
                        segments.Add(new TracePreviewSegment(start, end, state.LaserBurning));
                    }
                }
            }

            return segments;
        }
    }

    internal sealed class TracePreviewForm : Form
    {
        private readonly TracePreviewCanvas mCanvas;
        private readonly Timer mTimer;
        private readonly Button mPlayPause;

        public TracePreviewForm(string gcode)
        {
            List<TracePreviewSegment> segments = TracePreviewParser.Parse(gcode);

            Text = "Trace Path Simulator";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(640, 480);
            Size = new Size(900, 650);

            Label safetyLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 42,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "SIMULATION ONLY - no commands are sent to the laser.",
                ForeColor = Color.DarkGreen,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold)
            };

            mCanvas = new TracePreviewCanvas(segments) { Dock = DockStyle.Fill };

            FlowLayoutPanel controls = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8)
            };

            mPlayPause = new Button { Text = "Pause", Width = 90 };
            Button restart = new Button { Text = "Restart", Width = 90 };
            Button close = new Button { Text = "Close", Width = 90 };
            Label legend = new Label
            {
                AutoSize = true,
                Padding = new Padding(16, 7, 0, 0),
                Text = "Red: laser on    Gray dashed: travel    Blue dot: machine position"
            };

            mPlayPause.Click += PlayPause_Click;
            restart.Click += Restart_Click;
            close.Click += delegate { Close(); };

            controls.Controls.Add(mPlayPause);
            controls.Controls.Add(restart);
            controls.Controls.Add(close);
            controls.Controls.Add(legend);

            Controls.Add(mCanvas);
            Controls.Add(safetyLabel);
            Controls.Add(controls);

            mTimer = new Timer { Interval = 120 };
            mTimer.Tick += Timer_Tick;
            mTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            mTimer.Stop();
            mTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!mCanvas.Advance())
            {
                mTimer.Stop();
                mPlayPause.Text = "Play";
            }
        }

        private void PlayPause_Click(object sender, EventArgs e)
        {
            if (mTimer.Enabled)
            {
                mTimer.Stop();
                mPlayPause.Text = "Play";
            }
            else
            {
                if (mCanvas.IsComplete)
                    mCanvas.Restart();

                mTimer.Start();
                mPlayPause.Text = "Pause";
            }
        }

        private void Restart_Click(object sender, EventArgs e)
        {
            mCanvas.Restart();
            mTimer.Start();
            mPlayPause.Text = "Pause";
        }
    }

    internal sealed class TracePreviewCanvas : Panel
    {
        private readonly List<TracePreviewSegment> mSegments;
        private readonly RectangleF mBounds;
        private int mCompletedSegments;

        public TracePreviewCanvas(List<TracePreviewSegment> segments)
        {
            mSegments = segments ?? new List<TracePreviewSegment>();
            mBounds = CalculateBounds(mSegments);
            DoubleBuffered = true;
            BackColor = Color.White;
        }

        public bool IsComplete
        {
            get { return mCompletedSegments >= mSegments.Count; }
        }

        public bool Advance()
        {
            if (IsComplete)
                return false;

            mCompletedSegments++;
            Invalidate();
            return !IsComplete;
        }

        public void Restart()
        {
            mCompletedSegments = 0;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (mSegments.Count == 0)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    "Load a file containing laser-on movement before opening the trace preview.",
                    Font,
                    ClientRectangle,
                    Color.DimGray,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            using (Pen plannedLaser = new Pen(Color.FromArgb(90, Color.IndianRed), 2))
            using (Pen plannedTravel = new Pen(Color.LightGray, 1))
            using (Pen completedLaser = new Pen(Color.Red, 3))
            using (Pen completedTravel = new Pen(Color.Gray, 2))
            using (Brush head = new SolidBrush(Color.RoyalBlue))
            {
                plannedTravel.DashStyle = DashStyle.Dash;
                completedTravel.DashStyle = DashStyle.Dash;

                for (int i = 0; i < mSegments.Count; i++)
                {
                    TracePreviewSegment segment = mSegments[i];
                    PointF start = ToScreen(segment.Start);
                    PointF end = ToScreen(segment.End);
                    Pen pen;

                    if (i < mCompletedSegments)
                        pen = segment.LaserOn ? completedLaser : completedTravel;
                    else
                        pen = segment.LaserOn ? plannedLaser : plannedTravel;

                    e.Graphics.DrawLine(pen, start, end);
                }

                PointF position = mCompletedSegments == 0
                    ? ToScreen(mSegments[0].Start)
                    : ToScreen(mSegments[mCompletedSegments - 1].End);
                e.Graphics.FillEllipse(head, position.X - 6, position.Y - 6, 12, 12);
            }
        }

        private PointF ToScreen(PointF point)
        {
            const float padding = 35f;
            float width = Math.Max(1f, mBounds.Width);
            float height = Math.Max(1f, mBounds.Height);
            float scale = Math.Min(
                Math.Max(1f, ClientSize.Width - padding * 2) / width,
                Math.Max(1f, ClientSize.Height - padding * 2) / height);
            float drawingWidth = width * scale;
            float drawingHeight = height * scale;
            float left = (ClientSize.Width - drawingWidth) / 2f;
            float top = (ClientSize.Height - drawingHeight) / 2f;

            return new PointF(
                left + (point.X - mBounds.Left) * scale,
                top + drawingHeight - (point.Y - mBounds.Top) * scale);
        }

        private static RectangleF CalculateBounds(List<TracePreviewSegment> segments)
        {
            if (segments.Count == 0)
                return new RectangleF(0, 0, 1, 1);

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            foreach (TracePreviewSegment segment in segments)
            {
                minX = Math.Min(minX, Math.Min(segment.Start.X, segment.End.X));
                minY = Math.Min(minY, Math.Min(segment.Start.Y, segment.End.Y));
                maxX = Math.Max(maxX, Math.Max(segment.Start.X, segment.End.X));
                maxY = Math.Max(maxY, Math.Max(segment.Start.Y, segment.End.Y));
            }

            return new RectangleF(minX, minY, Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY));
        }
    }
}
