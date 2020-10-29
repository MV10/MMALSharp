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
    /// <typeparam name="TMetaData">Thread-safe metadata to pass to the delegate derived from <see cref="PixelProcessorMetadata"/></typeparam>
    public class CustomPixelProcessor<TMetaData> : ParallelCellProcessorBase, IFrameProcessor
        where TMetaData : struct, IPixelProcessorMetadata
    {
        private readonly Func<TMetaData, (int r, int g, int b)> _pixelFunction;

        /// <summary>
        /// This constructor uses the default parallel processing cell count based on the image
        /// resolution and the recommended values defined by the <see cref="FrameAnalyser"/>. 
        /// Requires use of one of the standard camera image resolutions.
        /// </summary>
        /// <param name="pixelFunction">The delegate to process each pixel's data. The return value is an (r,g,b) tuple.</param>
        public CustomPixelProcessor(Func<TMetaData, (int r, int g, int b)> pixelFunction)
            : base()
        {
            _pixelFunction = pixelFunction;
        }

        /// <summary>
        /// This constructor accepts custom parallel processing cell counts. You must use this
        /// constructor if you are processing non-standard image resolutions.
        /// </summary>
        /// <param name="pixelFunction">The delegate to process each pixel's data. The return value is an (r,g,b) tuple.</param>
        /// <param name="horizontalCellCount">The number of columns to divide the image into.</param>
        /// <param name="verticalCellCount">The number of rows to divide the image into.</param>
        public CustomPixelProcessor(Func<TMetaData, (int r, int g, int b)> pixelFunction, int horizontalCellCount, int verticalCellCount)
            : base(horizontalCellCount, verticalCellCount)
        {
            _pixelFunction = pixelFunction;
        }

        // TODO fix the base summary text (references convolutions)
        /// <inheritdoc />
        public void Apply(ImageContext context)
            => Apply(context, default);

        /// <summary>
        /// Applies the effect to the image.
        /// </summary>
        /// <param name="context">The image's metadata.</param>
        /// <param name="customMetadata">Custom data to be passed to the processing delegate.</param>
        public void Apply(ImageContext context, TMetaData customMetadata)
        {
            PrepareProcessingContext(context);

            customMetadata.Width = this.FrameMetadata.Width;
            customMetadata.Height = this.FrameMetadata.Height;

            Parallel.ForEach(this.CellRect, (cell)
                => ProcessCell(cell, this.ProcessingContext.Data, this.FrameMetadata, customMetadata, this.SwapRawRGBtoBGR, this.RedOffset, this.BlueOffset));

            PostProcessContext();
        }

        private void ProcessCell(Rectangle rect, byte[] image, FrameAnalysisMetadata frameMetadata, TMetaData pixelMetadata, bool swapRedBlue, int redOffset, int blueOffset)
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
