// <copyright file="CustomCellProcessor.cs" company="Techyian">
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
    /// An effects processor which invokes a delegate to parallel-process each cell in the image.
    /// </summary>
    public class CustomCellProcessor : ParallelCellProcessorBase, IFrameProcessor
    {
        /// <summary>
        /// This constructor uses the default parallel processing cell count based on the image
        /// resolution and the recommended values defined by the <see cref="FrameAnalyser"/>. 
        /// Requires use of one of the standard camera image resolutions.
        /// </summary>
        public CustomCellProcessor()
            : base()
        { }

        /// <summary>
        /// This constructor accepts custom parallel processing cell counts. You must use this
        /// constructor if you are processing non-standard image resolutions.
        /// </summary>
        /// <param name="horizontalCellCount">The number of columns to divide the image into.</param>
        /// <param name="verticalCellCount">The number of rows to divide the image into.</param>
        public CustomCellProcessor(int horizontalCellCount, int verticalCellCount)
            : base(horizontalCellCount, verticalCellCount)
        { }

        public void Apply(ImageContext context)
        {
        }
    }
}
