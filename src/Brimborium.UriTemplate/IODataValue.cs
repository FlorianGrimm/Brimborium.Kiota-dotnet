namespace Brimborium.UriTemplate;

/// <summary>
/// allows a value to formatted and append to the target.
/// </summary>
public interface IODataValue {
    /// <summary>
    /// add the value to the target.Output
    /// </summary>
    /// <param name="target">target for url</param>
    /// <returns>true if successfull</returns>
    bool AppendValue(in UriTemplateTarget target);
}
