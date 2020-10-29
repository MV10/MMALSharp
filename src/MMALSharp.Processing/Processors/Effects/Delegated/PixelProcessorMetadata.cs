// <copyright file="PixelProcessorMetadata.cs" company="Techyian">
// Copyright (c) Ian Auty and contributors. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

namespace MMALSharp.Processors.Effects
{
    /// <summary>
    /// A default implementation of <see cref="IPixelProcessorMetadata"/> for
    /// delegated image processors which do not need custom input data.
    /// </summary>
    public struct PixelProcessorMetadata : IPixelProcessorMetadata
    {
        /// <summary>
        /// The width of the image.
        /// </summary>
        public int width;

        /// <summary>
        /// The height of the image.
        /// </summary>
        public int height;

        /// <summary>
        /// The X coordinate of the pixel to process.
        /// </summary>
        public int x;

        /// <summary>
        /// The Y coordinte of the pixel to process.
        /// </summary>
        public int y;

        /// <summary>
        /// The red channel of the pixel to process.
        /// </summary>
        public int r;

        /// <summary>
        /// The green channel of the pixel to process.
        /// </summary>
        public int g;

        /// <summary>
        /// The blue channel of the pixel to process.
        /// </summary>
        public int b;

        /// <inheritdoc />
        public int Width { set => width = value; }

        /// <inheritdoc />
        public int Height { set => height = value; }

        /// <inheritdoc />
        public int X { set => x = value; }

        /// <inheritdoc />
        public int Y { set => y = value; }

        /// <inheritdoc />
        public int R { set => r = value; }

        /// <inheritdoc />
        public int G { set => g = value; }

        /// <inheritdoc />
        public int B { set => b = value; }
    }
}
