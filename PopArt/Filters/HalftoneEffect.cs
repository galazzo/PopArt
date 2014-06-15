using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.WindowsRuntime;
using Nokia.Graphics;

namespace Nokiadeveloper
{
    /*public class NativeEffect : DelegatingEffect
    {

    };*/

    public class HalftoneEffect : CustomEffectBase
    {
        private double[][,] tone;

        public enum Shape
        {
            Circle,
            Diamond,
            Square
        }

        private uint m_cell_size = 20;
        public uint CellSize {
            get
            {
                return m_cell_size;
            }

            set
            {
                m_cell_size = value;
                tone = null;
                tone = new double[10][,];
                uint radius = m_cell_size;
                uint range = (uint)Math.Floor((double)(m_cell_size / 10));

                for (int i = 0; i < 10; i++)
                {
                    tone[i] = new double[m_cell_size, m_cell_size];
                    CreateDot(ref tone[i], m_cell_size, radius);
                    radius -= range;
                }
            }
        }

        public Shape CellShape { get; set; }

        public HalftoneEffect(IImageProvider source)
            : base(source)
        {            
            tone = new double[10][,];
            uint radius = CellSize;
            uint range = (uint) Math.Floor((double) (CellSize / 10) );

            for(int i = 0 ; i < 10; i++)
            {
                tone[i] = new double[CellSize, CellSize];
                CreateDot(ref tone[i], CellSize, radius);
                radius -= range;
            }
        }

        protected override void OnProcess(PixelRegion sourcePixelRegion, PixelRegion targetPixelRegion)
        {
            /*System.Diagnostics.Debug.WriteLine("[OnProcess][ImageSize]: " + sourcePixelRegion.ImageSize);            
            System.Diagnostics.Debug.WriteLine("[OnProcess][line]: " + sourcePixelRegion.StartIndex);
            System.Diagnostics.Debug.WriteLine("[OnProcess][sourcePixelRegion.Pitch]: " + sourcePixelRegion.Pitch);
            System.Diagnostics.Debug.WriteLine("[OnProcess][sourcePixelRegion.Bounds]: " + sourcePixelRegion.Bounds);*/

            int m_width = (int) sourcePixelRegion.ImageSize.Width;
            int m_height = (int) sourcePixelRegion.ImageSize.Height;
            uint luma = 0;
            uint toneIndex = 0;
            
            for (uint y = 0; y < m_height - CellSize; y += CellSize)
            {
                for (uint x = 0; x < m_width - CellSize; x += CellSize)
                {
                    luma = 0;
                    for (uint m_y = y; m_y < (y+CellSize); m_y++)
                    {
                        for (uint m_x = x; m_x < (x + CellSize); m_x++)
                        {
                            uint color = sourcePixelRegion.ImagePixels[m_x + (m_y * m_width)];
                            var r = (byte)((color >> 16) & 255);
                            var g = (byte)((color >> 8) & 255);
                            var b = (byte)((color) & 255);

                            luma += (uint)( (double)(r+g+b) / 3.0 );
                        }
                    }
                    luma /= (CellSize * CellSize);

                    toneIndex = (uint)Math.Floor((float)luma / 25);
                    if (toneIndex >= 10) toneIndex = 9;
                    
                    var offset = 8;
                    var m_xxx = 0;
                    
                    for (uint m_y = y, m_yy = 0; m_y < (y+CellSize); m_y++, m_yy++)
                    {
                        for (uint m_x = x, m_xx = 0; m_x < (x + CellSize); m_x++, m_xx++)
                        {
                            m_xxx = (int)((m_xx + ((((y/CellSize) % 2) == 1) ? offset : 0)) % CellSize);
                            
                            uint color = sourcePixelRegion.ImagePixels[m_x + (m_y * m_width)];
                            var r = (byte)((color >> 16) & 255);
                            var g = (byte)((color >> 8) & 255);
                            var b = (byte)((color) & 255);

                            r = (byte) ((double) r * tone[toneIndex][m_yy, m_xxx]);
                            g = (byte) ((double) g * tone[toneIndex][m_yy, m_xxx]);
                            b = (byte) ((double) b * tone[toneIndex][m_yy, m_xxx]);

                           targetPixelRegion.ImagePixels[m_x + ((int)(m_y * m_width))] = (uint)(b | (g << 8) | (r << 16) | (0xFF << 24));
                        }
                    }

                }
            }
        }

        private static void CreateDot(ref double[,] dot, uint outersize, uint innersize)
        {
            uint cx = outersize / 2;
            uint cy = outersize / 2;

            double _x = 0;
            double _y = 0;

            double distance = 0;
                       
            innersize /= 2;

            for (int y = 0; y < outersize; y++)
            {
                for (int x = 0; x < outersize; x++)
                {
                    _x = x - cx;
                    _y = y - cy;

                    distance = Math.Sqrt(_x * _x + _y * _y);

                    if (distance <= innersize)
                    {
                        dot[y, x] = 0;
                    }
                    else
                    {
                        dot[y, x] = 1;
                    }
                }
            }
        }
    }
}
