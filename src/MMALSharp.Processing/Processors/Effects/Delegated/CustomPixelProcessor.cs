// <copyright file="CustomPixelProcessor.cs" company="Techyian">
// Copyright (c) Ian Auty and contributors. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

using MMALSharp.Common;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace MMALSharp.Processors.Effects
{
    /// <summary>
    /// An effects processor which invokes a delegate to parallel-process each pixel in the image.
    /// </summary>
    /// <typeparam name="T">Thread-safe metadata to pass to the delegate derived from <see cref="PixelProcessorMetadata"/></typeparam>
    public class CustomPixelProcessor<T> : ParallelCellProcessorBase, IFrameProcessor
        where T : struct, IPixelProcessorMetadata
    {
        private readonly Func<T, (int r, int g, int b)> _pixelFunction;

        private T _customMetadata;

        /// <summary>
        /// This constructor uses the default parallel processing cell count based on the image
        /// resolution and the recommended values defined by the <see cref="FrameAnalyser"/>. 
        /// Requires use of one of the standard camera image resolutions.
        /// </summary>
        /// <param name="pixelFunction">The delegate to process each pixel's data. The return value is an (r,g,b) tuple.</param>
        /// <param name="customMetadata">Additional metadata passed to the effect processor delegate.</param>
        public CustomPixelProcessor(Func<T, (int r, int g, int b)> pixelFunction, T customMetadata = default)
            : base()
        {
            _pixelFunction = pixelFunction;
            _customMetadata = customMetadata;
        }

        /// <summary>
        /// This constructor accepts custom parallel processing cell counts. You must use this
        /// constructor if you are processing non-standard image resolutions.
        /// </summary>
        /// <param name="horizontalCellCount">The number of columns to divide the image into.</param>
        /// <param name="verticalCellCount">The number of rows to divide the image into.</param>
        /// <param name="pixelFunction">The delegate to process each pixel's data. The return value is an (r,g,b) tuple.</param>
        /// <param name="customMetadata">Additional metadata passed to the effect processor delegate.</param>
        public CustomPixelProcessor(int horizontalCellCount, int verticalCellCount, Func<T, (int r, int g, int b)> pixelFunction, T customMetadata = default)
            : base(horizontalCellCount, verticalCellCount)
        {
            _pixelFunction = pixelFunction;
            _customMetadata = customMetadata;
        }

        // TODO fix the base summary text (references convolutions)
        /// <inheritdoc />
        public void Apply(ImageContext context)
        {
            PrepareProcessingContext(context);

            _customMetadata.Width = this.FrameMetadata.Width;
            _customMetadata.Height = this.FrameMetadata.Height;

            Parallel.ForEach(this.CellRect, (cell)
                => ProcessCell(cell, this.ProcessingContext.Data, this.FrameMetadata, _customMetadata, this.SwapRawRGBtoBGR, this.RedOffset, this.BlueOffset));

            PostProcessContext();
        }

        private void ProcessCell(Rectangle rect, byte[] image, FrameAnalysisMetadata frameMetadata, T pixelMetadata, bool swapRedBlue, int redOffset, int blueOffset)
        {
            // Rectangle and FrameAnalysisMetadata are structures; they are by-value copies and all fields are value-types which makes them thread safe

            int x2 = rect.X + rect.Width;
            int y2 = rect.Y + rect.Height;

            int index;

            int storeRedOffset = swapRedBlue ? blueOffset : redOffset;
            int storeBlueOffset = swapRedBlue ? redOffset : blueOffset;

            for (var x = rect.X; x < x2; x++)
            {
                for (var y = rect.Y; y < y2; y++)
                {
                    index = (x * frameMetadata.Bpp) + (y * frameMetadata.Stride);

                    pixelMetadata.X = x;
                    pixelMetadata.Y = y;

                    pixelMetadata.R = image[index + redOffset];
                    pixelMetadata.G = image[index + 1];
                    pixelMetadata.B = image[index + blueOffset];

                    var (r, g, b) = _pixelFunction.Invoke(pixelMetadata);

                    image[index + storeRedOffset] = (byte)r;
                    image[index + 1] = (byte)g;
                    image[index + storeBlueOffset] = (byte)b;
                }
            }
        }
    }
}
