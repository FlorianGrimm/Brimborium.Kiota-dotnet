// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Kiota.Abstractions.Serialization;

/// <summary>
/// Set of helper methods for JSON serialization
/// </summary>
public static partial class KiotaJsonSerializer {
    private const string _JsonContentType = "application/json";

    /// <summary>
    /// Deserializes the given stream into an object.
    /// </summary>
    /// <param name="parsableFactory">The factory to create the object.</param>
    /// <param name="serializedRepresentation">The serialized representation of the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<T?> DeserializeAsync<T>(string serializedRepresentation, ParsableFactory<T> parsableFactory, CancellationToken cancellationToken = default) where T : IParsable
    => KiotaSerializer.DeserializeAsync(_JsonContentType, serializedRepresentation, parsableFactory, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into an object.
    /// </summary>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="parsableFactory">The factory to create the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<T?> DeserializeAsync<T>(Stream stream, ParsableFactory<T> parsableFactory, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeAsync(_JsonContentType, stream, parsableFactory, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into an object.
    /// </summary>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<T?> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Stream stream, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeAsync<T>(_JsonContentType, stream, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into an object.
    /// </summary>
    /// <param name="serializedRepresentation">The serialized representation of the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<T?> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string serializedRepresentation, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeAsync<T>(_JsonContentType, serializedRepresentation, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="parsableFactory">The factory to create the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<IEnumerable<T>> DeserializeCollectionAsync<T>(Stream stream, ParsableFactory<T> parsableFactory, CancellationToken cancellationToken = default) where T : IParsable
    => KiotaSerializer.DeserializeCollectionAsync(_JsonContentType, stream, parsableFactory, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="serializedRepresentation">The serialized representation of the objects.</param>
    /// <param name="parsableFactory">The factory to create the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<IEnumerable<T>> DeserializeCollectionAsync<T>(string serializedRepresentation, ParsableFactory<T> parsableFactory, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeCollectionAsync(_JsonContentType, serializedRepresentation, parsableFactory, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<IEnumerable<T>> DeserializeCollectionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Stream stream, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeCollectionAsync<T>(_JsonContentType, stream, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="serializedRepresentation">The serialized representation of the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    public static Task<IEnumerable<T>> DeserializeCollectionAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(string serializedRepresentation, CancellationToken cancellationToken = default) where T : IParsable
        => KiotaSerializer.DeserializeCollectionAsync<T>(_JsonContentType, serializedRepresentation, cancellationToken);
}