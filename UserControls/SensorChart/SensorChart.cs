﻿using Bachelor_Project.Extensions;
using Bachelor_Project.Forms.Options_Forms;
using Bachelor_Project.Handlers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace Bachelor_Project.UserControls
{
    public partial class SensorChart : UserControl
    {

        private Sensor _sensor;
        public Sensor Sensor 
        { 
            get { return _sensor; } 
            set 
            { 
                _sensor = value;
                this.chart1.Palette = _colorPalettes[_sensor.Type];
                this.chart1.Series[0].Name = _sensorSeries[_sensor.Type];
                RefreshChart(); 
            } 
        }

        private Dictionary<Sensor.SensorType, ChartColorPalette> _colorPalettes = new Dictionary<Sensor.SensorType, ChartColorPalette>()
        { 
            [Sensor.SensorType.Temperature] = ChartColorPalette.Fire,
            [Sensor.SensorType.Pressure] = ChartColorPalette.SeaGreen,
            [Sensor.SensorType.Volume] = ChartColorPalette.BrightPastel,
            [Sensor.SensorType.Unknown] = ChartColorPalette.SemiTransparent
        };

        private Dictionary<Sensor.SensorType, string> _sensorSeries = new Dictionary<Sensor.SensorType, string>()
        {
            [Sensor.SensorType.Temperature] = "T/dt",
            [Sensor.SensorType.Pressure] = "P/dt",
            [Sensor.SensorType.Volume] = "V/dt",
            [Sensor.SensorType.Unknown] = "Unknown"
        };

        public SensorChart(Sensor sensor)
        {
            InitializeComponent();

            this.Sensor = sensor;

            this.chart1.Titles[0].Text = $"{Sensor.Type.ToString()} Sensor Readings ({Sensor.Name})";

            AppearanceOptionsForm.OnColorPaletteChange += SetColorPaletteForControls;

            if(AppearanceOptionsForm.SelectedColorPalette != null)
            SetColorPaletteForControls(AppearanceOptionsForm.SelectedColorPalette);
        }


        public void SensorChartUpdate()
        {
            //foreach(string sensorName in SensorDataRef.Keys.ToList())
            //{
            //    if (!this.comboBox_SensorList.Items.Contains(sensorName))
            //    {
            //        this.comboBox_SensorList.Items.Add(sensorName);
            //    }
            //}

            //foreach(object obj in this.comboBox_SensorList.Items)
            //{
            //    if(!SensorDataRef.ContainsKey(obj.ToString()))
            //    {
            //        this.comboBox_SensorList.Items.Remove(obj);
            //    }
            //}

            RefreshChart();
        }

        public void RefreshChart()
        {
            if (AppPreferences.ApplicationIsTerminating) return;

            try
            {
                if (Sensor != null && this.chart1 != null && this.chart1.Series != null && this.chart1.Series.Count > 0 /*&& SensorDataRef.TryGetValue(SelectedSensor, out List<float> values)*/)
                {
                    List<float> values = Sensor.Readings;

                    int i2 = 0;
                    for (int i = 0; this.chart1.Series != null && i < this.chart1.Series[0].Points.Count; i++)
                    {
                        if (this.chart1.Series[0].Points[i].YValues[0] != values[i2])
                        {
                            this.chart1.Series[0].Points.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            if(i2 < values.Count - 1)
                            {
                                i2++;
                            }
                        }
                    }

                    if(this.chart1.Series != null)
                    {
                        for (int i = this.chart1.Series[0].Points.Count; i < values.Count; i++)
                        {
                            this.chart1.Series[0].Points.Add(values[i]);
                        }
                    }
                }
                else
                {
                    if (this.chart1.Series != null)
                    {
                        if (this.chart1.Series[0].Points.Count != 0)
                        {
                            this.chart1.Series[0].Points.Clear();
                        }
                    }
                }
            }
            catch(Exception ex) // May arise when disposing control
            {
                // Skip...
            }
        }

        //public string SelectedSensor
        //{
        //    get 
        //    { 
        //        return this.comboBox_SensorList.SelectedItem != null ? 
        //            this.comboBox_SensorList.SelectedItem.ToString() : null; 
        //    }
        //    set 
        //    {
        //        if (value != null && this.comboBox_SensorList.Items.Contains(value))
        //        {
        //            this.comboBox_SensorList.SelectedItem = value;
        //        }
        //    }
        //}

        private void comboBox_CyclogramList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RefreshChart();
        }

        public void SetColorPaletteForControls(Dictionary<FormColorVariant, Color> colorPalette)
        {
            this.BackColor = colorPalette[FormColorVariant.DarkFirst];
            this.chart1.BackColor = colorPalette[FormColorVariant.NormalSecond];

            this.chart1.ChartAreas[0].BackColor = colorPalette[FormColorVariant.Outline];

        }
    }
}
