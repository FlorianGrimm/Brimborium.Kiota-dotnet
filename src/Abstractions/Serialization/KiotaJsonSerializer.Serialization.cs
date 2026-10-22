// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.ComponentModel;

namespace Microsoft.Kiota.Abstractions.Serialization;

public static partial class KiotaJsonSerializer {

    /// <summary>
    /// Serializes the given object into a string based on the content type.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="serializeOnlyChangedValues">By default, you'll only get the changed properties.</param>
    /// <returns>The serialized representation as a stream.</returns>
    public static Stream SerializeAsStream<T>(T value, bool serializeOnlyChangedValues = true) where T : IParsable
        => KiotaSerializer.SerializeAsStream(_JsonContentType, value, serializeOnlyChangedValues);

    /// <summary>
    /// Serializes the given object into a string based on the content type.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="serializeOnlyChangedValues">By default, you'll only get the changed properties.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The serialized representation as a string.</returns>
    public static Task<string> SerializeAsStringAsync<T>(T value, bool serializeOnlyChangedValues = true, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.SerializeAsStringAsync(_JsonContentType, value, serializeOnlyChangedValues, cancellationToken);

    /// <summary>
    /// Serializes the given object into a string based on the content type.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="serializeOnlyChangedValues">By default, you'll only get the changed properties.</param>
    /// <returns>The serialized representation as a stream.</returns>
    public static Stream SerializeAsStream<T>(IEnumerable<T> value, bool serializeOnlyChangedValues = true) where T : IParsable
        => KiotaSerializer.SerializeAsStream(_JsonContentType, value, serializeOnlyChangedValues);

    /// <summary>
    /// Serializes the given object into a string based on the content type.
    /// </summary>
    /// <typeparam name="T">Type of the object to serialize</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <param name="serializeOnlyChangedValues">By default, you'll only get the changed properties.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The serialized representation as a string.</returns>
    public static Task<string> SerializeAsStringAsync<T>(IEnumerable<T> value, bool serializeOnlyChangedValues = true, CancellationToken cancellationToken = default) where T : IParsable =>
        KiotaSerializer.SerializeAsStringAsync(_JsonContentType, value, serializeOnlyChangedValues, cancellationToken);
}
