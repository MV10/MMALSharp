// <copyright file="IDelegatedProcessorMetadata.cs" company="Techyian">
// Copyright (c) Ian Auty and contributors. All rights reserved.
// Licensed under the MIT License. Please see LICENSE.txt for License info.
// </copyright>

namespace MMALSharp.Processors.Effects
{
    /// <summary>
    /// The basic metadata passed to a custom image processor delegate. A custom processor can
    /// implement a structure with additional metadata. The implementation copies these properties
    /// to fields with the same names in lowercase (the language doesn't allow an interface to
    /// declare field members) which should be used by the processor for performance reasons, and
    /// custom implementations should likewise prefer the use of fields over properties.
    /// </summary>
    public interface IDelegatedProcessorMetadata
    {
        /// <summary>
        /// The width of the image. Read the field named "width" instead of this property.
        /// </summary>
        int Width { set; }

        /// <summary>
        /// The height of the image. Read the field named "height" instead of this property.
        /// </summary>
        int Height { set; }
    }
}
