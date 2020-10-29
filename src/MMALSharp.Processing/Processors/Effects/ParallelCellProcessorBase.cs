// <copyright file="ParallelCellProcessorBase.cs" company="Techyian">
// Copyright (c) Ian Auty and contributors. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using MMALSharp.Common;
using MMALSharp.Common.Utility;

namespace MMALSharp.Processors.Effects
{
    /// <summary>
    /// Base class for image processors using cell-based parallel processing and
    /// possibly needing conversions between raw and formatted ImageContext data.
    /// </summary>
    public abstract class ParallelCellProcessorBase
    {
        // Some members are fields rather than properties for parallel processing performance reasons.
        // Array-based fields are threadsafe as long as multiple threads access unique array indices.

        /// <summary>
        /// The number cells the image is divided into horizonally.
        /// </summary>
        protected internal int HorizontalCellCount { get; private set; }

        /// <summary>
        /// The number cells the image is divided into vertically.
        /// </summary>
        protected internal int VerticalCellCount { get; private set; }

        /// <summary>
        /// The original image data before effects are applied.
        /// </summary>
        protected internal ImageContext SourceContext;

        /// <summary>
        /// The context against which to apply the processing algorithm. Guaranteed to
        /// be in RAW format, but refer to <see cref="RedOffset"/>, <see cref="BlueOffset"/>,
        /// and <see cref="SwapRawRGBtoBGR"/> to correctl interpret red/blue channel data.
        /// </summary>
        protected internal ImageContext ProcessingContext;

        /// <summary>
        /// Zero when <see cref="ProcessingContext"/> is RGB, or 2 when it is BGR. Based on
        /// whether the <see cref="SourceContext"/> was originally raw or encoded.
        /// </summary>
        protected internal int RedOffset;

        /// <summary>
        /// Zero when <see cref="ProcessingContext"/> is BGR, or 0 when it is RGB. Based on
        /// whether the <see cref="SourceContext"/> was originally raw or encoded.
        /// </summary>
        protected internal int BlueOffset;

        /// <summary>
        /// When true, the raw <see cref="ProcessingContext"/> buffer is RGB and must be
        /// swapped to BGR because <see cref="SourceContext"/> specifies an encoded format.
        /// This fixes a known System.Drawing Bitmap.Save bug which ignores RGB formats and
        /// requires BGR layout.
        /// </summary>
        protected internal bool SwapRawRGBtoBGR;

        /// <summary>
        /// The parallel processing cell coordinates calculated by <see cref="FrameAnalyser"/>.
        /// </summary>
        protected internal Rectangle[] CellRect;

        /// <summary>
        /// The frame metadata calculated by <see cref="FrameAnalyser"/>
        /// </summary>
        protected internal FrameAnalysisMetadata FrameMetadata;

        /// <summary>
        /// This constructor uses the default parallel processing cell count based on the image
        /// resolution and the recommended values defined by the <see cref="FrameAnalyser"/>. 
        /// Requires use of one of the standard camera image resolutions.
        /// </summary>
        public ParallelCellProcessorBase()
        {
            HorizontalCellCount = 0;
            VerticalCellCount = 0;
        }

        /// <summary>
        /// This constructor accepts custom parallel processing cell counts. You must use this
        /// constructor if you are processing non-standard image resolutions.
        /// </summary>
        /// <param name="horizontalCellCount">The number of columns to divide the image into.</param>
        /// <param name="verticalCellCount">The number of rows to divide the image into.</param>
        public ParallelCellProcessorBase(int horizontalCellCount, int verticalCellCount)
        {
            HorizontalCellCount = horizontalCellCount;
            VerticalCellCount = verticalCellCount;
        }

        /// <summary>
        /// Derived classes should call this, then apply image processing effects against
        /// <see cref="ProcessingContext"/>. After processing, call <see cref="PostProcessContext"/>.
        /// </summary>
        /// <param name="context">An image context providing additional metadata on the data passed in.</param>
        protected internal void PrepareProcessingContext(ImageContext context)
        {
            this.SourceContext = context;
            this.ProcessingContext = context.Raw ? context : CloneToRawBitmap(context);

            // When the original ImageContext was in raw format, pixel data is RGB. When it
            // was in an encoded format, Bitmap extraction stores pixel data as BGR.
            this.RedOffset = context.Raw ? 0 : 2;
            this.BlueOffset = context.Raw ? 2 : 0;

            // When the original ImageContext was in raw format, and the end result will
            // be stored to an encoded format, Bitmap.Save requires us to swap to BGR.
            this.SwapRawRGBtoBGR = context.Raw && context.StoreFormat != null;

            var analyser = new FrameAnalyser
            {
                HorizonalCellCount = HorizontalCellCount,
                VerticalCellCount = VerticalCellCount,
            };
            analyser.Apply(this.ProcessingContext);

            this.CellRect = analyser.CellRect;
            this.FrameMetadata = analyser.Metadata;
        }

        /// <summary>
        /// Derived classes should call this after applying image processing effects. The
        /// original context argument will be updated. <see cref="ProcessingContext"/> will be
        /// released (set to null) by this method.
        /// </summary>
        protected internal void PostProcessContext()
        {
            if (this.SourceContext.StoreFormat != null)
            {
                FormatRawBitmap(this.ProcessingContext, this.SourceContext);
                this.SourceContext.Raw = false; // context is never raw after formatting
            }
            else
            {
                if (!this.SourceContext.Raw)
                {
                    // TakePicture doesn't set the Resolution, copy it from the cloned version which stored it from Bitmap
                    this.SourceContext.Resolution = new Resolution(this.ProcessingContext.Resolution.Width, this.ProcessingContext.Resolution.Height);

                    this.SourceContext.Data = new byte[this.ProcessingContext.Data.Length];
                    Array.Copy(this.ProcessingContext.Data, this.SourceContext.Data, this.SourceContext.Data.Length);
                    this.SourceContext.Raw = true; // we just copied raw data to the source context
                }
            }

            this.ProcessingContext = null;
        }

        /// <summary>
        /// Converts a raw RGB cell to a BGR layout so that Bitmap.Save will work properly when
        /// the final image is to be output to an encoded format. This is a known issue with the
        /// System.Drawing Bitmap.Save method.
        /// </summary>
        /// <param name="rect">The coordinates of the cell being parallel-processed.</param>
        /// <param name="image">The image buffer to process.</param>
        /// <param name="metadata">Details about the image buffer.</param>
        protected internal void SwapRedBlueChannels(Rectangle rect, byte[] image, FrameAnalysisMetadata metadata)
        {
            int x2 = rect.X + rect.Width;
            int y2 = rect.Y + rect.Height;
            int index;
            for (var x = rect.X; x < x2; x++)
            {
                for (var y = rect.Y; y < y2; y++)
                {
                    index = (x * metadata.Bpp) + (y * metadata.Stride);
                    byte swap = image[index];
                    image[index] = image[index + 2];
                    image[index + 2] = swap;
                }
            }
        }

        private ImageContext CloneToRawBitmap(ImageContext sourceContext)
        {
            var newContext = new ImageContext
            {
                Raw = true,
                Eos = sourceContext.Eos,
                IFrame = sourceContext.IFrame,
                Encoding = sourceContext.Encoding,
                Pts = sourceContext.Pts,
                StoreFormat = sourceContext.StoreFormat
            };

            using (var ms = new MemoryStream(sourceContext.Data))
            {
                using (var sourceBmp = new Bitmap(ms))
                {
                    // sourceContext.Resolution isn't set by TakePicture (width,height is 0,0)
                    newContext.Resolution = new Resolution(sourceBmp.Width, sourceBmp.Height);

                    // If the source bitmap has a raw-compatible format, use it, otherwise default to RGBA
                    newContext.PixelFormat = PixelFormatToMMALEncoding(sourceBmp.PixelFormat, MMALEncoding.RGBA);
                    var bmpTargetFormat = MMALEncodingToPixelFormat(newContext.PixelFormat);
                    var rect = new Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);

                    using (var newBmp = sourceBmp.Clone(rect, bmpTargetFormat))
                    {
                        BitmapData bmpData = null;
                        try
                        {
                            bmpData = newBmp.LockBits(rect, ImageLockMode.ReadOnly, bmpTargetFormat);
                            var ptr = bmpData.Scan0;
                            int size = bmpData.Stride * newBmp.Height;
                            newContext.Data = new byte[size];
                            newContext.Stride = bmpData.Stride;
                            Marshal.Copy(ptr, newContext.Data, 0, size);
                        }
                        finally
                        {
                            newBmp.UnlockBits(bmpData);
                        }
                    }
                }
            }

            return newContext;
        }

        private void FormatRawBitmap(ImageContext sourceContext, ImageContext targetContext)
        {
            var pixfmt = MMALEncodingToPixelFormat(sourceContext.PixelFormat);

            using (var bitmap = new Bitmap(sourceContext.Resolution.Width, sourceContext.Resolution.Height, pixfmt))
            {
                BitmapData bmpData = null;
                try
                {
                    bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
                    var ptr = bmpData.Scan0;
                    int size = bmpData.Stride * bitmap.Height;
                    var data = sourceContext.Data;
                    Marshal.Copy(data, 0, ptr, size);
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, targetContext.StoreFormat);
                    targetContext.Data = new byte[ms.Length];
                    Array.Copy(ms.ToArray(), 0, targetContext.Data, 0, ms.Length);
                }
            }
        }

        private PixelFormat MMALEncodingToPixelFormat(MMALEncoding encoding)
        {
            if (encoding == MMALEncoding.RGB24)
            {
                return PixelFormat.Format24bppRgb;
            }

            if (encoding == MMALEncoding.RGB32)
            {
                return PixelFormat.Format32bppRgb;
            }

            if (encoding == MMALEncoding.RGBA)
            {
                return PixelFormat.Format32bppArgb;
            }

            throw new Exception($"Unsupported pixel format: {encoding}");
        }

        private MMALEncoding PixelFormatToMMALEncoding(PixelFormat format, MMALEncoding defaultEncoding)
        {
            if (format == PixelFormat.Format24bppRgb)
            {
                return MMALEncoding.RGB24;
            }

            if (format == PixelFormat.Format32bppRgb)
            {
                return MMALEncoding.RGB32;
            }

            if (format == PixelFormat.Format32bppArgb)
            {
                return MMALEncoding.RGBA;
            }

            return defaultEncoding;
        }
    }
}
