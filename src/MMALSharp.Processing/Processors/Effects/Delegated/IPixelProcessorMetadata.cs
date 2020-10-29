// <copyright file="IPixelProcessorMetadata.cs" company="Techyian">
// Copyright (c) Ian Auty and contributors. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

namespace MMALSharp.Processors.Effects
{
    /// <summary>
    /// Metadata for delegates invoked by <see cref="CustomPixelProcessor{TMetaData}"/>.
    /// </summary>
    public interface IPixelProcessorMetadata : IDelegatedProcessorMetadata
    {
        /// <summary>
        /// The X coordinate of the pixel to process. Read the field named "x" instead of this property.
        /// </summary>
        int X { set; }

        /// <summary>
        /// The Y coordinate of the pixel to process. Read the field named "y" instead of this property.
        /// </summary>
        int Y { set; }

        /// <summary>
        /// The red channel value of the pixel to process. Read the field named "r" instead of this property.
        /// </summary>
        int R { set; }

        /// <summary>
        /// The green channel value of the pixel to process. Read the field named "g" instead of this property.
        /// </summary>
        int G { set; }

        /// <summary>
        /// The blue channel value of the pixel to process. Read the field named "b" instead of this property.
        /// </summary>
        int B { set; }
    }
}
