﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bachelor_Project_Hydrogen_Compression_WinForms.UserControls.Device
{
    public class CompressorComponent : IContainImage
    {
        public string Name;

        public enum ComponentType { Valve, CounterTrigger, OpticalSensor, Reservoir, Pump }
        public enum ComponentOrientation { Horizontal, Vertical }
        public enum ComponentStatus { Disabled, Active, Inactive }

        public ComponentOrientation Orientation;
        public ComponentType Type;
        public ComponentStatus Status;

        public Dictionary<string, Image> ComponentImages;

        private float _fillAmount;
        public float FillAmount
        {
            get
            {
                return _fillAmount;
            }
            set
            {
                _fillAmount = value > 1f ? 1f : (value < 0f ? 0f : value);
            }
        }

        public CompressorComponent(string name, ComponentType type, ComponentOrientation orientation,
             ComponentStatus status = ComponentStatus.Disabled, float fillAmount = 1)
        {
            this.Name = name;
            this.Orientation = orientation;
            this.Type = type;
            this.Status = status;
            this.ComponentImages = CompressorDeviceRules.GetComponentImages(type);
            this._fillAmount = fillAmount;

        }

        Image IContainImage.GetImage()
        {
            Image img = (Image)ComponentImages[Orientation.ToString() + ComponentStatus.Disabled.ToString()].Clone();

            string key = Orientation.ToString() + Status.ToString();
            Image fillOverlay = ComponentImages[key];

            Graphics g = Graphics.FromImage(img);

            int x, y;
            Rectangle srcActiveRect;
            Rectangle destRect = default(Rectangle), sourceRect = default(Rectangle);

            if (Orientation == ComponentOrientation.Horizontal)
            {
                x = 0;
                y = 0;

                srcActiveRect = new Rectangle(x, y, (int)(fillOverlay.Width * FillAmount), fillOverlay.Height);

                fillOverlay = ((Bitmap)(fillOverlay)).Clone(srcActiveRect, fillOverlay.PixelFormat);

                destRect = new Rectangle(x, y, (int)(img.Width * FillAmount), img.Height);
                sourceRect = new Rectangle(0, 0, fillOverlay.Width, fillOverlay.Height);
            }
            else if (Orientation == ComponentOrientation.Vertical)
            {
                x = 0;
                y = (int)(img.Height * (1f - FillAmount));

                srcActiveRect = new Rectangle(x, y, fillOverlay.Width, (int)(fillOverlay.Height * FillAmount));

                fillOverlay = ((Bitmap)(fillOverlay)).Clone(srcActiveRect, fillOverlay.PixelFormat);

                destRect = new Rectangle(x, y, img.Width, img.Height - y);
                sourceRect = new Rectangle(0, 0, fillOverlay.Width, fillOverlay.Height);

                //Console.WriteLine(destRect);
                //Console.WriteLine(sourceRect);
            }

            if (!(destRect.Width == 0 || destRect.Height == 0 || sourceRect.Width == 0 || sourceRect.Height == 0))
            {
                g.DrawImage(fillOverlay, destRect, sourceRect, GraphicsUnit.Pixel);
            }

            g.Dispose();

            fillOverlay.Dispose();

            return img;
        }
    }
}
