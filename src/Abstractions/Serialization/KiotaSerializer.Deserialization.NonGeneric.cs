// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------



using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Kiota.Abstractions.Serialization;

internal interface IKiotaDeserializationWrapper {
    Task<IParsable?> DeserializeAsync(string contentType, Stream stream, CancellationToken cancellationToken);
    Task<IParsable?> DeserializeAsync(string contentType, string serializedRepresentation, CancellationToken cancellationToken);
    Task<IEnumerable<IParsable>> DeserializeCollectionAsync(string contentType, Stream stream, CancellationToken cancellationToken);
    Task<IEnumerable<IParsable>> DeserializeCollectionAsync(string contentType, string serializedRepresentation, CancellationToken cancellationToken);
}

internal class KiotaDeserializationWrapper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T> : IKiotaDeserializationWrapper where T : IParsable {
    public async Task<IParsable?> DeserializeAsync(string contentType, Stream stream, CancellationToken cancellationToken) => await KiotaSerializer.DeserializeAsync<T>(contentType, stream, cancellationToken).ConfigureAwait(false);
    public async Task<IParsable?> DeserializeAsync(string contentType, string serializedRepresentation, CancellationToken cancellationToken) => await KiotaSerializer.DeserializeAsync<T>(contentType, serializedRepresentation, cancellationToken).ConfigureAwait(false);
    public async Task<IEnumerable<IParsable>> DeserializeCollectionAsync(string contentType, Stream stream, CancellationToken cancellationToken) => (await KiotaSerializer.DeserializeCollectionAsync<T>(contentType, stream, cancellationToken).ConfigureAwait(false)).OfType<IParsable>();
    public async Task<IEnumerable<IParsable>> DeserializeCollectionAsync(string contentType, string serializedRepresentation, CancellationToken cancellationToken) => (await KiotaSerializer.DeserializeCollectionAsync<T>(contentType, serializedRepresentation, cancellationToken).ConfigureAwait(false)).OfType<IParsable>();
}

internal static class KiotaDeserializationWrapperFactory {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsIParsable(Type type) => typeof(IParsable).IsAssignableFrom(type);
    private static readonly ConcurrentDictionary<Type, IKiotaDeserializationWrapper> _Deserializers = new ConcurrentDictionary<Type, IKiotaDeserializationWrapper>();

    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "CreateInternal is invoked with the same type that is validated as IParsable at the call site; DynamicallyAccessedMembers requirements are satisfied by callers.")]
    static internal IKiotaDeserializationWrapper Create(Type type) => IsIParsable(type) ? _Deserializers.GetOrAdd(type, CreateInternal) : throw new ArgumentException("The given Type is not of IParsable", nameof(type));

    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    private static IKiotaDeserializationWrapper CreateInternal([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType) {
        if (Activator.CreateInstance(typeof(KiotaDeserializationWrapper<>).MakeGenericType(targetType)) is IKiotaDeserializationWrapper deserializer) {
            return deserializer;
        } else {
            throw new InvalidOperationException($"Unable to create deserializer for type {targetType}");
        }
    }
}

public static partial class KiotaSerializer {

    /// <summary>
    /// Deserializes the given string into a collection of objects based on the content type.
    /// </summary>
    /// <param name="type">The target type to deserialize</param>
    /// <param name="contentType">The content type of the stream.</param>
    /// <param name="serializedRepresentation">The serialized representation of the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    /// <returns></returns>

    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    public static Task<IParsable?> DeserializeAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string contentType, string serializedRepresentation, CancellationToken cancellationToken = default)
        => KiotaDeserializationWrapperFactory.Create(type).DeserializeAsync(contentType, serializedRepresentation, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="type">The target type to deserialize</param>
    /// <param name="contentType">The content type of the stream.</param>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    /// <returns></returns>
    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    public static Task<IParsable?> DeserializeAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string contentType, Stream stream, CancellationToken cancellationToken = default)
         => KiotaDeserializationWrapperFactory.Create(type).DeserializeAsync(contentType, stream, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="type">The target type to deserialize</param>
    /// <param name="contentType">The content type of the stream.</param>
    /// <param name="stream">The stream to deserialize.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    public static Task<IEnumerable<IParsable>> DeserializeCollectionAsync(Type type, string contentType, Stream stream, CancellationToken cancellationToken = default)
         => KiotaDeserializationWrapperFactory.Create(type).DeserializeCollectionAsync(contentType, stream, cancellationToken);

    /// <summary>
    /// Deserializes the given stream into a collection of objects based on the content type.
    /// </summary>
    /// <param name="type">The target type to deserialize</param>
    /// <param name="contentType">The content type of the stream.</param>
    /// <param name="serializedRepresentation">The serialized representation of the object.</param>
    /// <param name="cancellationToken">The cancellation token for the task</param>
    [RequiresDynamicCode("Activator creates an instance of a generic class with the Target Type as the generic type argument.")]
    public static Task<IEnumerable<IParsable>> DeserializeCollectionAsync(Type type, string contentType, string serializedRepresentation, CancellationToken cancellationToken = default)
         => KiotaDeserializationWrapperFactory.Create(type).DeserializeCollectionAsync(contentType, serializedRepresentation, cancellationToken);
}