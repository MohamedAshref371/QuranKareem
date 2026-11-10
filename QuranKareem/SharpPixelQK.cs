using System;
using System.Drawing;
using System.Drawing.Imaging;

public class SharpPixelQK
{
    private readonly byte[] rgbValues;
    private BitmapData bmpData;
    private IntPtr bmpPtr;
    private bool locked = false;

    private int withOriginal = 0;
    private byte[] rgbValuesCopy;
    private byte[] rgbValuesCopy2;
    private readonly int[] boundaries = new int[4];
    
    public int Width { get; }
    public int Height { get; }
    public bool IsAlphaBitmap { get; }
    public Bitmap Bitmap { get; private set; }

    public SharpPixelQK(Bitmap bitmap, bool withOriginal = false)
    {
        if ((bitmap.PixelFormat & PixelFormat.Indexed) != 0) throw new Exception("Cannot lock an Indexed image.");

        Bitmap = bitmap;
        IsAlphaBitmap = (bitmap.PixelFormat & PixelFormat.Alpha) != 0;
        Width = bitmap.Width;
        Height = bitmap.Height;

        int bytes = (Width * Height) * (IsAlphaBitmap ? 4 : 3);
        rgbValues = new byte[bytes];

        this.withOriginal = withOriginal ? 1 : 0;
    }

    internal bool TryUseBitmap(Bitmap bitmap)
    {
        if (locked || bitmap.Width != Width || bitmap.Height != Height || bitmap.PixelFormat != Bitmap.PixelFormat)
            return false;

        Bitmap = bitmap;
        if (withOriginal == 2) withOriginal = 1; // Take a fresh original snapshot on the next lock.
        return true;
    }

    public void Lock()
    {
        if (locked) throw new Exception("Bitmap already locked.");

        Rectangle rect = new Rectangle(0, 0, Width, Height);
        bmpData = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
        bmpPtr = bmpData.Scan0;

        if (withOriginal != 2)
        {
            System.Runtime.InteropServices.Marshal.Copy(bmpPtr, rgbValues, 0, rgbValues.Length);
            Reset();
        }
            
        if (withOriginal == 1)
        {
            if (rgbValuesCopy == null) rgbValuesCopy = new byte[rgbValues.Length];
            if (rgbValuesCopy2 == null) rgbValuesCopy2 = new byte[rgbValues.Length];
            Array.Copy(rgbValues, rgbValuesCopy, rgbValues.Length);
            withOriginal = 2;
        }

        locked = true;
    }

    private void Reset()
    {
        boundaries[0] = int.MaxValue;
        boundaries[1] = int.MinValue;
        Reset2();
    }

    private void Reset2()
    {
        boundaries[2] = int.MaxValue;
        boundaries[3] = int.MinValue;
    }

    public void Unlock(bool setPixels, bool setAsOriginal = false)
    {
        if (!locked) throw new Exception("Bitmap not locked.");

        if (setPixels)
            SetPixels(setAsOriginal);
        else if (withOriginal == 2)
            withOriginal = 1;

        Bitmap.UnlockBits(bmpData);
        locked = false;
    }

    private void SetPixels(bool setAsOriginal)
    {
        if (boundaries[1] >= 0)
        {
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, boundaries[0], IntPtr.Add(bmpPtr, boundaries[0]), boundaries[1] - boundaries[0]);
            if (withOriginal == 2 && setAsOriginal)
            {
                Array.Copy(rgbValues, boundaries[0], rgbValuesCopy, boundaries[0], boundaries[1] - boundaries[0]);
                Reset();
            }
        }
    }

    public void SetOriginal(bool UndoCleraOnly = false)
    {
        if (!locked) throw new Exception("Bitmap not locked.");

        if (UndoCleraOnly && boundaries[3] >= 0)
        {
            Array.Copy(rgbValuesCopy2, boundaries[2], rgbValues, boundaries[2], boundaries[3] - boundaries[2]);
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, boundaries[2], IntPtr.Add(bmpPtr, boundaries[2]), boundaries[3] - boundaries[2]);
            Reset2();
        }
        else if (!UndoCleraOnly && withOriginal == 2 && boundaries[1] >= 0)
        {
            Array.Copy(rgbValuesCopy, boundaries[0], rgbValues, boundaries[0], boundaries[1] - boundaries[0]);
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, boundaries[0], IntPtr.Add(bmpPtr, boundaries[0]), boundaries[1] - boundaries[0]);
            Reset();
        }
    }

    public void Clear(Color color, int x0, int y0, int x1, int y1, Color Modifiedcolor, int delta = 0, bool withAlpha = true, bool skip0alpha = false, bool copy = false)
    {
        if (!locked) throw new Exception("Bitmap not locked.");

        int plus = IsAlphaBitmap ? 4 : 3;
        bool alpha = IsAlphaBitmap && withAlpha;
        bool skip = IsAlphaBitmap && skip0alpha;
        int index = (y0 * Width + x0) * plus;
        int finish = (y1 * Width + x1) * plus + 1;
        byte Alpha = color.A, Red = color.R, Green = color.G, Blue = color.B;
        byte RedM = Modifiedcolor.R, GreenM = Modifiedcolor.G, BlueM = Modifiedcolor.B;

        if (copy)
        {
            if (boundaries[3] < 0)
                Array.Copy(rgbValues, index, rgbValuesCopy2, index, finish - index);
            else
            {
                if (index < boundaries[2])
                    Array.Copy(rgbValues, index, rgbValuesCopy2, index, boundaries[2] - index - 1);
                if (finish > boundaries[3])
                    Array.Copy(rgbValues, boundaries[3], rgbValuesCopy2, boundaries[3], finish - boundaries[3]);
            }
            if (index < boundaries[2])
                boundaries[2] = index;
            if (finish > boundaries[3])
                boundaries[3] = finish;
        }
        else
            Reset2();
        
        int val;
        for (int j = y0; j <= y1; j++)
        {
            for (int i = x0; i <= x1; i++)
            {
                val = (j * Width + i) * plus;
                if ((!skip || rgbValues[val + 3] > 0) && (Modifiedcolor.IsEmpty || (Math.Abs(rgbValues[val + 2] - RedM) <= delta && Math.Abs(rgbValues[val + 1] - GreenM) <= delta && Math.Abs(rgbValues[val] - BlueM) <= delta)))
                {
                    rgbValues[val] = Blue;
                    rgbValues[val + 1] = Green;
                    rgbValues[val + 2] = Red;
                    if (alpha || color == Color.Transparent) rgbValues[val + 3] = Alpha;
                }
            }
        }

        if (index < boundaries[0])
            boundaries[0] = index;
        if (finish > boundaries[1])
            boundaries[1] = finish;
    }

    public void ReverseColors()
    {
        if (!locked) throw new Exception("Bitmap not locked.");

        int plus = IsAlphaBitmap ? 4 : 3;

        for (int index = 0; index < rgbValues.Length; index+= plus)
        {
            if (!IsAlphaBitmap || rgbValues[index + 3] > 0)
            {
                rgbValues[index] = (byte)(255 - rgbValues[index]);
                rgbValues[index + 1] = (byte)(255 - rgbValues[index + 1]);
                rgbValues[index + 2] = (byte)(255 - rgbValues[index + 2]);
            }
        }

        boundaries[0] = 0;
        boundaries[1] = rgbValues.Length;
    }
}

// I took the logic from https://www.codeproject.com/Articles/15192/FastPixel-A-much-faster-alternative-to-Bitmap-SetP
