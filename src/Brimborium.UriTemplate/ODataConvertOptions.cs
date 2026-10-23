namespace Brimborium.UriTemplate;

/// <summary>
/// Options
/// </summary>
/// <param name="ODataDateTimeLiteralStyle">Quoted or not</param>
public sealed record class ODataConvertOptions(
    ODataDateTimeLiteralStyle ODataDateTimeLiteralStyle
    ) {
    private static ODataConvertOptions _Default = new ODataConvertOptions(
            ODataDateTimeLiteralStyle: ODataDateTimeLiteralStyle.Quoted
        );

    /// <summary>
    /// Gets or sets the default URI template options.
    /// Set this at the start of your app.
    /// </summary>
    /// <remarks>
    /// If not explicitly set, the default options use the quoted OData date-time literal style.
    /// A <see cref="UriTemplateCache"/> created without explicit options captures this value when it is
    /// constructed; this includes <see cref="UriTemplateCache.Default"/>, which is created on the first
    /// call to <see cref="UriTemplate.Expand"/>. Changing this property afterwards does not affect
    /// existing caches.
    /// </remarks>
    public static ODataConvertOptions Default { get => _Default; set => _Default = value; }

    private static ODataConvertOptions _Quoted = new ODataConvertOptions(
            ODataDateTimeLiteralStyle: ODataDateTimeLiteralStyle.Quoted
        );

    /// <summary>
    /// Quoted
    /// </summary>
    public static ODataConvertOptions Quoted => _Quoted;

    private static ODataConvertOptions _Unquoted = new ODataConvertOptions(
            ODataDateTimeLiteralStyle: ODataDateTimeLiteralStyle.Unquoted
        );

    /// <summary>
    /// Unquoted
    /// </summary>
    public static ODataConvertOptions Unquoted => _Unquoted;
}

/// <summary>
/// Defines how <see cref="ODataValue"/> writes date and time literals
/// (<see cref="DateTimeOffset"/>, <see cref="DateTime"/>, <see cref="DateOnly"/>, <see cref="TimeOnly"/>).
/// </summary>
public enum ODataDateTimeLiteralStyle {
    /// <summary>
    /// Enclosed in single quotes, e.g. <c>'2024-01-02'</c>.
    /// </summary>
    Quoted = 0,

    /// <summary>
    /// Without quotes as defined by the OData v4 ABNF, e.g. <c>2024-01-02</c>.
    /// A <see cref="DateTime"/> with <see cref="DateTimeKind.Unspecified"/> is written as UTC.
    /// </summary>
    Unquoted = 1
}
