// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using Brimborium.Kiota.Abstractions.Serialization;
namespace Brimborium.Kiota.Serialization.Multipart;
/// <summary>
/// Factory to create multipart serialization writers.
/// </summary>
public class MultipartSerializationWriterFactory : ISerializationWriterFactory
{
    /// <inheritdoc/>
    public string ValidContentType => "multipart/form-data";
    /// <inheritdoc/>
    public ISerializationWriter GetSerializationWriter(string contentType)
    {
        if(string.IsNullOrEmpty(contentType)) {
            throw new ArgumentNullException(nameof(contentType));
        }
        else if(!ValidContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentOutOfRangeException($"expected a {ValidContentType} content type");
        }

        return new MultipartSerializationWriter();
    }
}
