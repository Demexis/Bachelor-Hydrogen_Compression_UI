﻿using Bachelor_Project.Forms.Options_Forms;
using Bachelor_Project.Miscellaneous;
using Bachelor_Project.UserControls.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bachelor_Project
{
    public partial class Cyclogram : UserControl
    {
        #region Enums

        public enum CyclogramPlayMode { Single, Loop };

        #endregion


        #region CyclogramPreferences

        [Category("Behavior"), Description("Determines when the timestamp of the cyclogram starts to change depending on the ticks of the timer.")]
        public bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                _active = value;
            }
        }
        private bool _active;

        [Category("Appearance"), Description("Name of the cyclogram.")]
        public string CyclogramName { get; set; }


        [Category("Behavior"), Description("Play mode defines if the cyclogram's execution will stop after reaching the end.")]
        public CyclogramPlayMode PlayMode { get { return _playMode; } set { _playMode = value; } }

        private CyclogramPlayMode _playMode;

        [Category("Appearance"), Description("Specifies the part of the cyclogram in width, which will be assigned to the column with the names of the components and their statuses.")]
        public float TitleWidthRatio { get; set; } = 0.2f;

        [Category("Appearance"), Description("Determines the part of the cyclogram in height, which will be assigned to one row with the name of the cyclogram and with the names of the steps.")]
        public float CyclogramRulerHeightRatio { get; set; } = 0.1f;

        [Category("Appearance"), Description("Specifies the number of displayed components and their statuses.")]
        public int MaxSimultaneousRecords { get; set; } = 20;

        [Category("Appearance"), Description("Specifies the time interval that will be visible horizontally on the cyclogram.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public int HorizontalVisionRange // Milliseconds
        {
            get
            {
                return _horizontalVisionRange;
            }
            set
            {
                _horizontalVisionRange = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Bindable(false)]
        [Browsable(false)]
        public int HorizontalVisionRangeClamped // Milliseconds
        {
            get
            {
                if (_horizontalVisionRange == 0)
                {
                    _horizontalVisionRange = _defaultHorizontalVisionRange;
                }

                return Mathf.Clamp(_horizontalVisionRange, 0, GetTotalLengthInMilliseconds);
            }
            set
            {
                _horizontalVisionRange = value;
            }
        }
        private int _horizontalVisionRange = _defaultHorizontalVisionRange;
        private const int _defaultHorizontalVisionRange = 15000;

        [Category("Behavior"), Description("The time after which the cyclogram will focus on the current position of the timestamp.")]
        public int FollowSleepTime { get; set; } = 2000;


        #endregion


        #region CyclogramScrollers

        public bool VerticalScrollerFollowMouse;
        private Point _verticalMouseStartLocation;
        private int _verticalPosWhenMouseStarted;
        public bool HorizontalScrollerFollowMouse;
        private Point _horizontalMouseStartLocation;
        private int _horizontalPosWhenMouseStarted;

        public int VerticalScrollerPos = 0;

        public int ScrollerSize = 20;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int HorizontalVisionPos { get; set; } = 0;
        public int _followStopTime = 0;


        private float _verticalPosScroll = 0.1f; // [0 - 1]

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float HorizontalPosScrollCoeff
        {
            get { return _verticalPosScroll; }
            set { _verticalPosScroll = Mathf.Clamp(value, 0, 1); }
        }

        private float _timeStampFollowPoint = 0.5f; // [0 - 1]

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public float TimeStampFollowPoint {
            get { return _timeStampFollowPoint; }
            set { _timeStampFollowPoint = Mathf.Clamp(value, 0, 1); }
        }

        private bool _shiftKeyPressed;

        #endregion


        #region Actions

        public Action<CyclogramComponentElement, CyclogramStatusElement> OnComponentStatusChange;
        public Action<CyclogramStepElement> OnStepChange;
        public Action OnSingleExecutionEnd;


        #endregion


        #region Collections

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public List<CyclogramComponentElement> Components { get { return _components; } set { _components = value; } }
        private List<CyclogramComponentElement> _components = new List<CyclogramComponentElement>();

        //[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        //public List<CyclogramStatusElement> Statuses { get { return _statuses; } set { _statuses = value; } }
        //private List<CyclogramStatusElement> _statuses = new List<CyclogramStatusElement>();

        public int GetStatusesCount()
        {
            int count = 0;

            _components.ForEach(x => count += x.Statuses.Count);

            return count;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public List<CyclogramStepElement> Steps { get { return _steps; } set { _steps = value; } }
        private List<CyclogramStepElement> _steps = new List<CyclogramStepElement>();

        #endregion


        #region TimeStamp

        public int CurrentTimeStamp { get; set; }

        public void SetCurrentTimeStamp(int valueMilliseconds, bool checkSteps = true) 
        { 
            CurrentTimeStamp = valueMilliseconds;
            UpdateVisionPos();
            if(checkSteps) CheckSteps();
            this.Refresh(); 
        }

        public int GetTotalLengthInMilliseconds 
        { 
            get 
            {
                int length = 0;

                if(Steps != null)
                {
                    foreach (CyclogramStepElement step in Steps)
                    {
                        length += step.LengthMilliseconds;
                    }
                }

                return length;
            } 
        }

        #endregion


        #region Rectangles

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetCyclogramTitleRect => new Rectangle(
            0,
            0,
            (int)(this.Width * TitleWidthRatio),
            (int)(this.Height * CyclogramRulerHeightRatio)
        );

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetStepsRect => new Rectangle(
            (int)(this.Width * TitleWidthRatio),
            0,
            (int)(this.Width * (1 - TitleWidthRatio)) - ScrollerSize,
            (int)(this.Height * CyclogramRulerHeightRatio)
        );

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetTitlesRect => new Rectangle(
            0,
            (int)(this.Height * CyclogramRulerHeightRatio),
            (int)(this.Width * TitleWidthRatio),
            (int)(this.Height - (int)(this.Height * CyclogramRulerHeightRatio)) - ScrollerSize
        );

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetSequencesRect => new Rectangle(
            (int)(this.Width * TitleWidthRatio),
            (int)(this.Height * CyclogramRulerHeightRatio),
            (int)(this.Width * (1 - TitleWidthRatio)) - ScrollerSize,
            (int)(this.Height - (int)(this.Height * CyclogramRulerHeightRatio)) - ScrollerSize
        );

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetVerticalScrollerRect => new Rectangle(
            this.Width - ScrollerSize,
            0,
            ScrollerSize,
            this.Height            
        );

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Rectangle GetHorizontalScrollerRect => new Rectangle(
            0,
            this.Height - ScrollerSize,
            this.Width,
            ScrollerSize
        );

        #endregion


        #region Colors

        public Color OutlineColor = Color.FromArgb(32, 30, 55);
        public Color CategoryFillColor = Color.FromArgb(42, 40, 65);
        public Color TitleFillColor = Color.FromArgb(52, 50, 75);
        public Color CyclogramTitleColor = Color.FromArgb(93, 91, 122);

        public Color BackgroundColor = Color.FromArgb(32, 30, 45);
        public Color SecondBackgroundColor = Color.FromArgb(23, 21, 32);

        public Color DefaultTextColor = Color.FromArgb(180, 160, 220);
        public Color BrightTextColor = Color.FromArgb(250, 250, 250);

        public Color ActiveOutlineColor = AppPreferences.TimeStampColor;

        private void SetColorPalette(Dictionary<FormColorVariant, Color> colorPalette)
        {
            OutlineColor = colorPalette[FormColorVariant.Outline];
            CategoryFillColor = colorPalette[FormColorVariant.NormalFirst];
            TitleFillColor = colorPalette[FormColorVariant.NormalSecond];
            CyclogramTitleColor = colorPalette[FormColorVariant.BrightSecond];
            BackgroundColor = colorPalette[FormColorVariant.DarkFirst];
            SecondBackgroundColor = colorPalette[FormColorVariant.DarkSecond];
            DefaultTextColor = colorPalette[FormColorVariant.TextColorSecond];
            BrightTextColor = colorPalette[FormColorVariant.BrightFirst];
        }

        #endregion

        public Cyclogram()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            this.MouseWheel += MouseWheelScroll;

            AppearanceOptionsForm.OnColorPaletteChange += SetColorPalette;
        }

        #region Governance

        public void Play(bool value)
        {
            this.Active = value;
            this.Refresh();
        }

        public void Stop()
        {
            this.Active = false;
            this.CurrentTimeStamp = 0;
            OnSingleExecutionEnd?.Invoke();
        }

        public void StepForward()
        {
            bool process = false;

            for (int i = 0; i < this.Steps.Count; i++)
            {
                int traveledLength = i > 0 ? Steps.Take(i).Sum((x) => x.LengthMilliseconds) : 0;

                if (process)
                {
                    this.CurrentTimeStamp = traveledLength;
                    this.CheckSteps();
                    break;
                }

                bool isActive = Mathf.Between(this.CurrentTimeStamp, traveledLength, 
                                                  traveledLength + this.Steps[i].LengthMilliseconds - 1);

                if(isActive)
                {
                    process = true;
                    continue;
                }
            }

            UpdateVisionPos();

            this.Refresh();
        }

        public void StepBackward()
        {
            for (int i = 0; i < this.Steps.Count; i++)
            {
                int traveledLength = i > 0 ? Steps.Take(i).Sum((x) => x.LengthMilliseconds) : 0;

                bool isActive = Mathf.Between(this.CurrentTimeStamp, traveledLength,
                                                  traveledLength + this.Steps[i].LengthMilliseconds - 1);

                if (isActive)
                {
                    traveledLength = i - 1 > 0 ? Steps.Take(i - 1).Sum((x) => x.LengthMilliseconds) : 0;

                    this.CurrentTimeStamp = traveledLength;
                    this.CheckSteps();
                    break;
                }
            }

            UpdateVisionPos();

            this.Refresh();
        }

        public void SetRightEnd()
        {
            int traveledLength = this.Steps.Count - 1 > 0 ? Steps.Take(this.Steps.Count - 1).Sum((x) => x.LengthMilliseconds) : 0;

            this.CurrentTimeStamp = traveledLength;
            this.CheckSteps();

            UpdateVisionPos();

            this.Refresh();
        }

        public void SetLeftEnd()
        {
            this.CurrentTimeStamp = 0;
            this.CheckSteps();

            UpdateVisionPos();

            this.Refresh();
        }

        public void Clear()
        {
            Components.Clear();
            //Statuses.Clear();
            Steps.Clear();
        }

        #endregion

        private void IncrementTimeStamp()
        {
            if (this.CurrentTimeStamp >= this.GetTotalLengthInMilliseconds - 1)
            {
                if (this.PlayMode == Cyclogram.CyclogramPlayMode.Single)
                {
                    return;
                }
                else if (this.PlayMode == Cyclogram.CyclogramPlayMode.Loop)
                {
                    this.CurrentTimeStamp = 0;

                    Console.WriteLine("Cyclogram reached the end. Starting a new cycle.");
                }
            }

            if (this.CurrentTimeStamp + timer_main.Interval >= this.GetTotalLengthInMilliseconds - 1)
            {
                this.CurrentTimeStamp = this.GetTotalLengthInMilliseconds - 1;
            }
            else
            {
                this.CurrentTimeStamp += timer_main.Interval;
            }

            if (this.CurrentTimeStamp >= this.GetTotalLengthInMilliseconds - 1 &&
                this.PlayMode == Cyclogram.CyclogramPlayMode.Single)
            {
                Stop();

                Console.WriteLine("Cyclogram reached the end.");
            }

            CheckSteps();
        }

        private void CheckSteps()
        {
            for (int i = 0; i < this.Steps.Count; i++)
            {
                int traveledLength = 0;

                for (int j = 0; j < i; j++)
                {
                    traveledLength += this.Steps[j].LengthMilliseconds;
                }

                bool shouldBeActive = this.CurrentTimeStamp >= traveledLength
                    && this.CurrentTimeStamp < traveledLength + this.Steps[i].LengthMilliseconds;

                bool stepChanged = false;

                foreach (CyclogramSequenceElement sequence in this.Steps[i].Sequences)
                {
                    if (sequence.Active ^ shouldBeActive) // ^ stands for XOR - means (1 && 0 or 0 && 1)
                    {
                        sequence.Active = !sequence.Active;

                        //Console.WriteLine($"Sequence [{sequence.SequenceID}] has been {(sequence.Active ? "activated" : "deactivated")}");

                        if(sequence.Active)
                        {
                            try
                            {
                                CyclogramComponentElement component = Components.First(x => x.Name == sequence.ComponentName);
                                CyclogramStatusElement status = component.Statuses.First(x => x.Name == sequence.StatusName);

                                OnComponentStatusChange?.Invoke(component, status);

                                stepChanged = true;
                            }
                            catch(Exception ex)
                            {
                                // Skip...
                            }
                        }

                    }
                }

                if(stepChanged)
                {
                    OnStepChange?.Invoke(this.Steps[i]);
                }

            }
        }

        private void UncheckSteps()
        {
            foreach(CyclogramStepElement step in this.Steps)
            {
                foreach (CyclogramSequenceElement sequence in step.Sequences)
                {
                    sequence.Active = false;
                }
            }
        }



        #region Events


        private void DrawCyclogram(PaintEventArgs e)
        {
            if (Steps.Count == 0) return;

            Graphics g = e.Graphics;

            Rectangle MainTitleRect = GetCyclogramTitleRect;
            Rectangle StepsRect = GetStepsRect;
            Rectangle TitlesRect = GetTitlesRect;
            Rectangle SequencesRect = GetSequencesRect;


            // Drawing Background
            g.FillRectangle(new SolidBrush(BackgroundColor), new Rectangle(default(Point), this.Size));

            // Drawing Cyclogram Name
            {
                g.FillRectangle(new SolidBrush(CyclogramTitleColor), new RectangleF(MainTitleRect.Location, MainTitleRect.Size));
                g.DrawRectangle(new Pen(OutlineColor, 2), new Rectangle(MainTitleRect.Location, MainTitleRect.Size));
                g.DrawString(" " + CyclogramName, Font, new SolidBrush(BrightTextColor), MainTitleRect.Location);
            }

            // Setting vision range
            
            if(_followStopTime <= 0 && this.Active)
            {
                UpdateVisionPos();
            }

            int visionStartPos = Mathf.Clamp(HorizontalVisionPos + HorizontalVisionRangeClamped, 0, GetTotalLengthInMilliseconds) - HorizontalVisionRangeClamped;
            int visionEndPos = Mathf.Clamp(visionStartPos + HorizontalVisionRangeClamped, 0, GetTotalLengthInMilliseconds);

            // Drawing Steps

            Dictionary<CyclogramStepElement, Point> stepLines = new Dictionary<CyclogramStepElement, Point>();
            {
                Point stepsLocation = StepsRect.Location;

                //int lineWidth = StepsRect.Width;


                for (int i = 0; i < Steps.Count; i++)
                {
                    int localPos = 0;

                    for (int j = 0; j < i; j++)
                    {
                        localPos += Steps[j].LengthMilliseconds;
                    }

                    // If step is within the range
                    if (Mathf.LineSegmentsIntersect(localPos, localPos + Steps[i].LengthMilliseconds, visionStartPos, visionEndPos))
                    {
                        (int x, int y) = Mathf.LineSegmentsConjunction(localPos, localPos + Steps[i].LengthMilliseconds, visionStartPos, visionEndPos);

                        int operationWidth = (int)((y - x) / (float)(visionEndPos - visionStartPos) * StepsRect.Width);

                        int operationX = StepsRect.X + (int)((x - visionStartPos) / (float)(visionEndPos - visionStartPos) * StepsRect.Width);

                        g.FillRectangle(new SolidBrush(CategoryFillColor), new RectangleF(operationX, stepsLocation.Y, operationWidth, StepsRect.Height));
                        g.DrawRectangle(new Pen(OutlineColor, 2), new Rectangle(operationX, stepsLocation.Y, operationWidth, StepsRect.Height));
                        g.DrawString(" " + Steps[i].Name, Font, new SolidBrush(BrightTextColor), new Point(operationX, stepsLocation.Y));

                        stepLines.Add(Steps[i], new Point(operationX, operationWidth));
                    }
                }
            }

            // Drawing roads
            {
                int height = TitlesRect.Height;

                int segments = 4 * Steps.Count;

                Point location = SequencesRect.Location;

                for (int i = 0; i < MaxSimultaneousRecords; i++)
                {
                    int lineWidth = SequencesRect.Width;

                    int cellHeight = height / (MaxSimultaneousRecords - i);
                    height -= cellHeight;

                    for (int j = 0; j < segments; j++)
                    {
                        int segmentWidth = lineWidth / (segments - j);
                        lineWidth -= segmentWidth;

                        if (j % 2 == 0)
                            g.FillRectangle(new SolidBrush(SecondBackgroundColor), new Rectangle(location, new Size(segmentWidth, cellHeight)));

                        g.DrawRectangle(new Pen(OutlineColor), new Rectangle(location, new Size(segmentWidth, cellHeight)));
                        location.X += segmentWidth;
                    }
                    location.X = SequencesRect.Left;
                    location.Y += cellHeight;
                }
            }

            int countOfRecords = 0;
            int countOfSkips = VerticalScrollerPos;

            // Drawing Categories and Titles
            {
                Point location = TitlesRect.Location;

                int width = TitlesRect.Width;
                int height = TitlesRect.Height;

                for (int i = 0; i < Components.Count; i++)
                {
                    CyclogramComponentElement component = Components[i];

                    if (countOfSkips == 0)
                    {
                        if (countOfRecords >= MaxSimultaneousRecords) break;

                        countOfRecords++;

                        int cellHeight = height / (MaxSimultaneousRecords - countOfRecords + 1);
                        height -= cellHeight;

                        g.FillRectangle(new SolidBrush(CategoryFillColor), new RectangleF(location.X, location.Y, width, cellHeight));
                        g.DrawRectangle(new Pen(OutlineColor, 2), new Rectangle(location.X, location.Y, width, cellHeight));
                        g.DrawString(" " + component.Name, Font, new SolidBrush(BrightTextColor), location + new Size(0, cellHeight / 4));

                        int rectSide = width > cellHeight ? cellHeight / 5 : width / 5;

                        Point p = new Point(location.X + width - rectSide, location.Y);
                        g.DrawRectangle(new Pen(OutlineColor), new Rectangle(p, new Size(rectSide, cellHeight)));
                        g.FillRectangle(new SolidBrush(CyclogramTitleColor), new Rectangle(p, new Size(rectSide, cellHeight)));

                        location.Y += cellHeight;
                    }
                    else
                    {
                        countOfSkips--;
                    }

                    

                    foreach (CyclogramStatusElement title in component.Statuses)
                    {
                        if(countOfSkips > 0)
                        {
                            countOfSkips--;
                            continue;
                        }

                        if (countOfRecords >= MaxSimultaneousRecords) break;

                        countOfRecords++;

                        int cellHeight = height / (MaxSimultaneousRecords - countOfRecords + 1);
                        height -= cellHeight;

                        g.FillRectangle(new SolidBrush(TitleFillColor), new RectangleF(location.X, location.Y, width, cellHeight));
                        g.DrawRectangle(new Pen(OutlineColor, 2), new Rectangle(location.X, location.Y, width, cellHeight));
                        g.DrawString("   " + title.Name, Font, new SolidBrush(DefaultTextColor), location + new Size(0, cellHeight / 4));

                        // Drawing sequences

                        for (int k = 0; k < Steps.Count; k++)
                        {
                            if(stepLines.TryGetValue(Steps[k], out Point stepLine))
                            {
                                int currentCellPos = stepLine.X;
                                int currentCellWidth = stepLine.Y;

                                for (int k3 = 0; k3 < Steps[k].Sequences.Count; k3++)
                                {
                                    CyclogramSequenceElement sequence = Steps[k].Sequences[k3];

                                    if (!sequence.ComponentName.Equals(component.Name) || !sequence.StatusName.Equals(title.Name))
                                        continue;
                                    //if (!Steps[k].Sequences[k3].TitleID.Equals(title.TitleID)) continue;

                                    LinearGradientBrush linGrBrush = new LinearGradientBrush(
                                       location + new Size(currentCellPos, 0),
                                       location + new Size(currentCellPos, 0) + new Size(currentCellWidth, cellHeight),
                                       CyclogramTitleColor,
                                       DefaultTextColor);

                                    g.DrawRectangle(new Pen(OutlineColor, 2), new Rectangle(location + new Size(currentCellPos, 0), new Size(currentCellWidth, cellHeight)));
                                    g.FillRectangle(linGrBrush, new Rectangle(location + new Size(currentCellPos, 0), new Size(currentCellWidth, cellHeight)));


                                    if (Steps[k].Sequences[k3].Active)
                                    {
                                        g.DrawRectangle(new Pen(ActiveOutlineColor, 2), new Rectangle(location + new Size(currentCellPos, 0), new Size(currentCellWidth, cellHeight)));
                                    }
                                }
                            }
                        }

                        location.Y += cellHeight;
                    }
                }
            }

            // Drawing Time Stamp line

            Point timeStampLocation = new Point(SequencesRect.Left, SequencesRect.Top);

            if (GetTotalLengthInMilliseconds != 0)
            {
                if (Mathf.Between(CurrentTimeStamp, visionStartPos, visionEndPos))
                {
                    if (visionStartPos != visionEndPos)
                        timeStampLocation = new Point(SequencesRect.Left + (int)(SequencesRect.Width * Mathf.NormalizedRelationBetween(CurrentTimeStamp, visionStartPos, visionEndPos)), SequencesRect.Top);
                    else
                        Console.WriteLine($"{HorizontalVisionRangeClamped} : {visionStartPos} : {visionEndPos} : {GetTotalLengthInMilliseconds}");

                    g.DrawRectangle(new Pen(ActiveOutlineColor, 2), new Rectangle(timeStampLocation, new Size(1, SequencesRect.Height)));
                }
            }
            else
            {
                timeStampLocation = new Point(SequencesRect.Left, SequencesRect.Top);
                g.DrawRectangle(new Pen(ActiveOutlineColor, 2), new Rectangle(timeStampLocation, new Size(1, SequencesRect.Height)));
            }


            // Drawing Vertical Scroller
            g.DrawRectangle(new Pen(OutlineColor), GetVerticalScrollerRect);
            g.FillRectangle(new SolidBrush(OutlineColor), GetVerticalScrollerRect);

            if(Components.Count + GetStatusesCount() > MaxSimultaneousRecords)
            {
                // Draw up button
                Rectangle upButtonRect = new Rectangle(this.Width - ScrollerSize, 0, ScrollerSize, ScrollerSize);
                g.DrawRectangle(new Pen(OutlineColor), upButtonRect);
                g.FillRectangle(new SolidBrush(TitleFillColor), upButtonRect);

                // Draw down button
                Rectangle downButtonRect = new Rectangle(
                    this.Width - ScrollerSize, 
                    this.Height - ScrollerSize, 
                    ScrollerSize, 
                    ScrollerSize
                );

                g.DrawRectangle(new Pen(OutlineColor), downButtonRect);
                g.FillRectangle(new SolidBrush(TitleFillColor), downButtonRect);

                // Draw scroller part

                int scrollerPartHeight = (int)((this.Height - 2 * ScrollerSize) * ((float)MaxSimultaneousRecords / (Components.Count + GetStatusesCount())));

                Rectangle scrollerPartRect = new Rectangle(
                    this.Width - ScrollerSize, 
                    ScrollerSize + (int)(VerticalScrollerPos * (this.Height - 2 * ScrollerSize - scrollerPartHeight) / ((float)(Components.Count + GetStatusesCount() - MaxSimultaneousRecords) ) ), 
                    ScrollerSize,
                    scrollerPartHeight
                );
                g.DrawRectangle(new Pen(OutlineColor), scrollerPartRect);
                g.FillRectangle(new SolidBrush(CyclogramTitleColor), scrollerPartRect);
            }


            // Drawing Horizontal Scroller
            g.DrawRectangle(new Pen(OutlineColor), GetHorizontalScrollerRect);
            g.FillRectangle(new SolidBrush(OutlineColor), GetHorizontalScrollerRect);

            if (GetTotalLengthInMilliseconds > HorizontalVisionRangeClamped)
            {
                // Draw left button
                Rectangle upButtonRect = new Rectangle(0, this.Height - ScrollerSize, ScrollerSize, ScrollerSize);
                g.DrawRectangle(new Pen(OutlineColor), upButtonRect);
                g.FillRectangle(new SolidBrush(TitleFillColor), upButtonRect);

                // Draw right button
                Rectangle downButtonRect = new Rectangle(
                    this.Width - ScrollerSize,
                    this.Height - ScrollerSize,
                    ScrollerSize,
                    ScrollerSize
                );

                g.DrawRectangle(new Pen(OutlineColor), downButtonRect);
                g.FillRectangle(new SolidBrush(TitleFillColor), downButtonRect);

                // Draw scroller part

                int scrollerPartWidth = (int)((this.Width - 2 * ScrollerSize) * (HorizontalVisionRangeClamped / (float)(GetTotalLengthInMilliseconds)));

                Rectangle scrollerPartRect = new Rectangle(
                    ScrollerSize + (int)(visionStartPos / (float)(GetTotalLengthInMilliseconds - HorizontalVisionRangeClamped) * (this.Width - 2 * ScrollerSize - scrollerPartWidth)),
                    this.Height - ScrollerSize,
                    scrollerPartWidth,
                    ScrollerSize
                );
                g.DrawRectangle(new Pen(OutlineColor), scrollerPartRect);
                g.FillRectangle(new SolidBrush(CyclogramTitleColor), scrollerPartRect);
            }
        }

        private void Cyclogram_Paint(object sender, PaintEventArgs e)
        {
            DrawCyclogram(e);
        }

        public void UpdateVisionPos()
        {
            if (CurrentTimeStamp > HorizontalVisionPos + (int)(HorizontalVisionRangeClamped * TimeStampFollowPoint))
            {
                HorizontalVisionPos = CurrentTimeStamp - (int)(HorizontalVisionRangeClamped * TimeStampFollowPoint);
            }

            if (HorizontalVisionPos > CurrentTimeStamp)
            {
                HorizontalVisionPos = CurrentTimeStamp;
            }
        }

        private void Cyclogram_Resize(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private void timer_main_Tick(object sender, EventArgs e)
        {
            if(!HorizontalScrollerFollowMouse)
                _followStopTime -= timer_main.Interval;

            if (this.Active)
            {
                IncrementTimeStamp();

                this.Refresh();
            }
        }

        private void MouseWheelScroll(object sender, MouseEventArgs e)
        {
            this.Focus();

            if(e.Delta > 0)
            {
                if(_shiftKeyPressed)
                {
                    HorizontalVisionPos = Mathf.Clamp(HorizontalVisionPos - (int)(HorizontalVisionRangeClamped * HorizontalPosScrollCoeff), 0, GetTotalLengthInMilliseconds - HorizontalVisionRangeClamped);

                    _followStopTime = FollowSleepTime;

                    this.Refresh();

                    //if (HorizontalScrollerPos > 0)
                    //{
                    //    HorizontalScrollerPos--;

                    //    this.Refresh();
                    //}
                }
                else
                {
                    if (VerticalScrollerPos > 0)
                    {
                        VerticalScrollerPos--;

                        if (_followStopTime > 0) _followStopTime = FollowSleepTime;

                        this.Refresh();
                    }
                }
            }
            else
            {
                if (_shiftKeyPressed)
                {
                    HorizontalVisionPos = Mathf.Clamp(HorizontalVisionPos + (int)(HorizontalVisionRangeClamped * _verticalPosScroll), 0, GetTotalLengthInMilliseconds - HorizontalVisionRangeClamped);

                    _followStopTime = FollowSleepTime;

                    this.Refresh();

                    //if (HorizontalScrollerPos < Steps.Count - MaxSimultaneousSteps)
                    //{
                    //    HorizontalScrollerPos++;

                    //    this.Refresh();
                    //}
                }
                else
                {
                    if (VerticalScrollerPos < Components.Count + GetStatusesCount() - MaxSimultaneousRecords)
                    {
                        VerticalScrollerPos++;

                        if (_followStopTime > 0) _followStopTime = FollowSleepTime;

                        this.Refresh();
                    }
                }
            }
        }

        private void Cyclogram_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey || e.KeyCode == Keys.Shift)
            {
                _shiftKeyPressed = true;
            }
        }

        private void Cyclogram_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey || e.KeyCode == Keys.Shift)
            {
                _shiftKeyPressed = false;
            }
        }

        private void Cyclogram_MouseDown(object sender, MouseEventArgs e)
        {
            // Vertical Scroller
            int scrollerPartHeight = (int)((this.Height - 2 * ScrollerSize) * ((float)MaxSimultaneousRecords / (Components.Count + GetStatusesCount())));

            Rectangle verticalScrollerPartRect = new Rectangle(
                    this.Width - ScrollerSize,
                    ScrollerSize + (int)(VerticalScrollerPos * (this.Height - 2 * ScrollerSize - scrollerPartHeight) / ((float)(Components.Count + GetStatusesCount() - MaxSimultaneousRecords))),
                    ScrollerSize,
                    scrollerPartHeight
                );

            if (verticalScrollerPartRect.Contains(e.Location) && Components.Count + GetStatusesCount() > MaxSimultaneousRecords)
            {
                VerticalScrollerFollowMouse = true;
                _verticalMouseStartLocation = e.Location;
                _verticalPosWhenMouseStarted = VerticalScrollerPos;
            }

            // Horizontal Scroller
            int visionStartPos = Mathf.Clamp(HorizontalVisionPos + HorizontalVisionRangeClamped, 0, GetTotalLengthInMilliseconds) - HorizontalVisionRangeClamped;
            int scrollerPartWidth = (int)((this.Width - 2 * ScrollerSize) * (HorizontalVisionRangeClamped / (float)(GetTotalLengthInMilliseconds)));

            Rectangle horizontalScrollerPartRect = new Rectangle(
                ScrollerSize + (int)(visionStartPos / (float)(GetTotalLengthInMilliseconds - HorizontalVisionRangeClamped) * (this.Width - 2 * ScrollerSize - scrollerPartWidth)),
                this.Height - ScrollerSize,
                scrollerPartWidth,
                ScrollerSize
            );

            if (horizontalScrollerPartRect.Contains(e.Location))
            {
                HorizontalScrollerFollowMouse = true;
                _horizontalMouseStartLocation = e.Location;
                _horizontalPosWhenMouseStarted = HorizontalVisionPos;

                _followStopTime = FollowSleepTime;
            }
        }

        private void Cyclogram_MouseUp(object sender, MouseEventArgs e)
        {
            VerticalScrollerFollowMouse = false;
            HorizontalScrollerFollowMouse = false;
        }

        private void Cyclogram_MouseMove(object sender, MouseEventArgs e)
        {
            if(VerticalScrollerFollowMouse)
            {
                int scrollerPartHeight = (int)((this.Height - 2 * ScrollerSize) * ((float)MaxSimultaneousRecords / (Components.Count + GetStatusesCount())));
                int step = scrollerPartHeight / MaxSimultaneousRecords;

                int y = _verticalPosWhenMouseStarted + (e.Location.Y - _verticalMouseStartLocation.Y) / step;

                VerticalScrollerPos = Mathf.Clamp(y, 0, Components.Count + GetStatusesCount() - MaxSimultaneousRecords);

                //Task.Run(() => this.Refresh());
                this.Refresh();
            }

            if(HorizontalScrollerFollowMouse)
            {
                int scrollerPartWidth = (int)((this.Width - 2 * ScrollerSize) * (HorizontalVisionRangeClamped / (float)(GetTotalLengthInMilliseconds)));
                float step = (float)scrollerPartWidth / HorizontalVisionRangeClamped;

                int x = (int)(_horizontalPosWhenMouseStarted + (e.Location.X - _horizontalMouseStartLocation.X) / step);

                HorizontalVisionPos = Mathf.Clamp(x, 0, GetTotalLengthInMilliseconds - HorizontalVisionRangeClamped);

                //Task.Run(() => this.Refresh());
                this.Refresh();
            }
        }

        #endregion
    }
}
