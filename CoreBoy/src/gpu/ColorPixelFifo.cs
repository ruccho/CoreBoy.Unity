using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreBoy.gpu
{
    public class ColorPixelFifo : IPixelFifo
    {
        /*
        private readonly IntQueue _pixels = new IntQueue(16);
        private readonly IntQueue _palettes = new IntQueue(16);
        private readonly IntQueue _priorities = new IntQueue(16);
        */

        private readonly Lcdc _lcdc;
        private readonly IDisplay _display;
        private readonly ColorPalette _bgPalette;
        private readonly ColorPalette _oamPalette;

        private readonly Fifo<PixelInfo> pixels = new Fifo<PixelInfo>(16);

        public ColorPixelFifo(Lcdc lcdc, IDisplay display, ColorPalette bgPalette, ColorPalette oamPalette)
        {
            _lcdc = lcdc;
            _display = display;
            _bgPalette = bgPalette;
            _oamPalette = oamPalette;
        }

        public int GetLength() => pixels.Count; // _pixels.Size();
        public void PutPixelToScreen() => _display.PutColorPixel(DequeuePixel());

        private int DequeuePixel()
        {
            var p = pixels.Dequeue();
            return
                GetColor(p.priority, p.palette,
                    p.pixel); //  _priorities.Dequeue(), _palettes.Dequeue(), _pixels.Dequeue());
        }

        public void DropPixel() => DequeuePixel();

        private PixelInfo[] tempPixelInfo = default;

        public void Enqueue8Pixels(int[] pixelLine, TileAttributes tileAttributes)
        {
            int palette = tileAttributes.GetColorPaletteIndex();
            int priority = tileAttributes.IsPriority() ? 100 : -1;
            /*
            foreach (var p in pixelLine)
            {
                /*
                _pixels.Enqueue(p);
                _palettes.Enqueue(tileAttributes.GetColorPaletteIndex());
                _priorities.Enqueue(tileAttributes.IsPriority() ? 100 : -1);
                */ /*
                pixels.Enqueue(new PixelInfo()
                {
                    palette = palette,
                    pixel = p,
                    priority = priority
                });
            }
        */

            if (tempPixelInfo == null || tempPixelInfo.Length < pixelLine.Length)
                tempPixelInfo = new PixelInfo[pixelLine.Length];

            for (int i = 0; i < pixelLine.Length; i++)
            {
                tempPixelInfo[i] = new PixelInfo()
                {
                    palette = palette,
                    pixel = pixelLine[i],
                    priority = priority
                };
            }

            pixels.Enqueue(tempPixelInfo.AsSpan(0, pixelLine.Length));
        }

        /*
        lcdc.0
    
        when 0 => sprites are always displayed on top of the bg
    
        bg tile attribute.7
    
        when 0 => use oam priority bit
        when 1 => bg priority
    
        sprite attribute.7
    
        when 0 => sprite above bg
        when 1 => sprite above bg color 0
         */

        public void SetOverlay(int[] pixelLine, int offset, TileAttributes spriteAttr, int oamIndex)
        {
            for (var j = offset; j < pixelLine.Length; j++)
            {
                var p = pixelLine[j];
                var i = j - offset;
                if (p == 0)
                {
                    continue; // color 0 is always transparent
                }

                var pixelInfo = pixels.Get(i);
                var oldPriority = pixelInfo.priority; // _priorities.Get(i);

                var put = false;
                if ((oldPriority == -1 || oldPriority == 100) && !_lcdc.IsBgAndWindowDisplay())
                {
                    // this one takes precedence
                    put = true;
                }
                else if (oldPriority == 100)
                {
                    // bg with priority
                    put = pixelInfo.pixel == 0; // _pixels.Get(i) == 0;
                }
                else if (oldPriority == -1 && !spriteAttr.IsPriority())
                {
                    // bg without priority
                    put = true;
                }
                else if (oldPriority == -1 && spriteAttr.IsPriority() && pixelInfo.pixel == 0) // _pixels.Get(i) == 0)
                {
                    // bg without priority
                    put = true;
                }
                else if (oldPriority >= 0 && oldPriority < 10)
                {
                    // other sprite
                    put = oldPriority > oamIndex;
                }

                if (put)
                {
                    /*
                    _pixels.Set(i, p);
                    _palettes.Set(i, spriteAttr.GetColorPaletteIndex());
                    _priorities.Set(i, oamIndex);
                    */

                    pixels.Set(i, new PixelInfo()
                    {
                        pixel = p,
                        palette = spriteAttr.GetColorPaletteIndex(),
                        priority = oamIndex
                    });
                }
            }
        }

        public void Clear()
        {
            /*
            _pixels.Clear();
            _palettes.Clear();
            _priorities.Clear();
            */

            pixels.Clear();
        }


        private int GetColor(int priority, int palette, int color)
        {
            if (priority >= 0 && priority < 10)
                return _oamPalette.GetPalette(palette, color);
            else
                return _bgPalette.GetPalette(palette, color);
        }


        struct PixelInfo : IEquatable<PixelInfo>
        {
            private sealed class PixelPalettePriorityEqualityComparer : IEqualityComparer<PixelInfo>
            {
                public bool Equals(PixelInfo x, PixelInfo y)
                {
                    return x.pixel == y.pixel && x.palette == y.palette && x.priority == y.priority;
                }

                public int GetHashCode(PixelInfo obj)
                {
                    return HashCode.Combine(obj.pixel, obj.palette, obj.priority);
                }
            }

            public static IEqualityComparer<PixelInfo> PixelPalettePriorityComparer { get; } =
                new PixelPalettePriorityEqualityComparer();

            public bool Equals(PixelInfo other)
            {
                return pixel == other.pixel && palette == other.palette && priority == other.priority;
            }

            public override bool Equals(object obj)
            {
                return obj is PixelInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(pixel, palette, priority);
            }

            public static bool operator ==(PixelInfo left, PixelInfo right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(PixelInfo left, PixelInfo right)
            {
                return !left.Equals(right);
            }

            public int pixel;
            public int palette;
            public int priority;
        }
    }
}