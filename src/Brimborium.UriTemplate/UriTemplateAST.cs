using System.Collections;
using System.Diagnostics.Tracing;
using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Represents a parsed URI template as an ordered sequence of constants and
/// template operations.
/// </summary>
/// <param name="ListChild">The constant and operation nodes in template order.</param>
public sealed record class UriTemplateASTSequence(
    params UriTemplateASTSequenceChild[] ListChild
) {

    /// <summary>
    /// Expands the sequence using the supplied variable substitutions.
    /// </summary>
    /// <param name="substitutions">Values supplied for variables in the template.</param>
    /// <param name="target">The target to which the expanded template is appended.</param>
    /// <returns>
    /// <see langword="true"/> if at least one variable value was expanded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        UriTemplateTarget target) {
        bool result = false;
        foreach (var item in this.ListChild) {
            var subResult = item.Expand(substitutions, target);
            result |= subResult;
        }
        return result;
    }
}

/// <summary>
/// Defines a node in a parsed URI template sequence.
/// </summary>
public abstract record class UriTemplateASTSequenceChild() {
    /// <summary>
    /// Expands this node using the supplied variable substitutions.
    /// </summary>
    /// <param name="substitutions">Values supplied for variables in the template.</param>
    /// <param name="target">The target to which this node's output is appended.</param>
    /// <returns>
    /// <see langword="true"/> if a variable value was expanded;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public abstract bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        );
}

/// <summary>
/// Represents literal text in a parsed URI template.
/// </summary>
/// <param name="Value">The literal text to append during expansion.</param>
public sealed record class UriTemplateASTConstant(
        string Value
    ) : UriTemplateASTSequenceChild() {
    /// <inheritdoc/>
    public override bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        ) {
        _ = target.Append(this.Value);
        return false;
    }
}

/// <summary>
/// Identifies an expansion operator that controls the prefix, separators, and
/// escaping used for a URI-template expression.
/// </summary>
public enum UriTemplateASTOperator {
    /// <summary>Uses the default expansion behavior.</summary>
    Noop,
    /// <summary>Uses reserved-character expansion without an operator prefix.</summary>
    Plus,
    /// <summary>Uses reserved-character expansion with a fragment prefix.</summary>
    Hash,
    /// <summary>Prefixes values with dots and separates them with dots.</summary>
    Dot,
    /// <summary>Prefixes values with slashes and separates them with slashes.</summary>
    Slash,
    /// <summary>Prefixes values with semicolons and separates them with semicolons.</summary>
    Semicolon,
    /// <summary>Prefixes the expression with a question mark and emits query parameters.</summary>
    Questionmark,
    /// <summary>Emits additional query parameters separated by ampersands.</summary>
    Ampersand
}

/// <summary>
/// Represents a URI-template expression, including its operator and variables.
/// </summary>
/// <param name="Operator">The operator controlling this expression's expansion, if any.</param>
/// <param name="ListPlaceholder">The variables contained in the expression.</param>
public sealed record class UriTemplateASTOperation(
        UriTemplateASTOperator? Operator,
        params UriTemplateASTPlaceholder[] ListPlaceholder
        ) : UriTemplateASTSequenceChild() {
    /// <summary>
    /// Appends the operator's initial prefix, if it defines one.
    /// </summary>
    /// <param name="output">The builder receiving the prefix.</param>
    public void AddPrefix(StringBuilder output) {
        if (this.Operator is { } op) {
            switch (op) {
                case UriTemplateASTOperator.Hash:
                    _ = output.Append('#');
                    break;
                case UriTemplateASTOperator.Dot:
                    _ = output.Append('.');
                    break;
                case UriTemplateASTOperator.Slash:
                    _ = output.Append('/');
                    break;
                case UriTemplateASTOperator.Semicolon:
                    _ = output.Append(';');
                    break;
                case UriTemplateASTOperator.Questionmark:
                    _ = output.Append('?');
                    break;
                case UriTemplateASTOperator.Ampersand:
                    _ = output.Append('&');
                    break;
                default:
                    return;
            }
        }
    }

    /// <summary>
    /// Appends the separator used between values for this operator.
    /// </summary>
    /// <param name="output">The builder receiving the separator.</param>
    public void AddSeparator(StringBuilder output) {
        if (this.Operator is { } op) {
            switch (op) {
                case UriTemplateASTOperator.Dot:
                    _ = output.Append('.');
                    return;
                case UriTemplateASTOperator.Slash:
                    _ = output.Append('/');
                    return;
                case UriTemplateASTOperator.Semicolon:
                    _ = output.Append(';');
                    return;
                case UriTemplateASTOperator.Questionmark:
                case UriTemplateASTOperator.Ampersand:
                    _ = output.Append('&');
                    return;
                default:
                    _ = output.Append(',');
                    return;
            }
        }
    }

    /// <inheritdoc/>
    public override bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        ) {
        bool first = true;
        bool result = false;
        foreach (var placeholder in this.ListPlaceholder) {
            var subResult = placeholder.Expand(this, first, substitutions, target);
            if (subResult) {
                result = true;
                first = false;
            }
        }
        return result;
    }
}

/// <summary>
/// Represents a variable within a URI-template expression and its expansion
/// modifiers.
/// </summary>
/// <param name="Name">The variable name used to look up a substitution.</param>
/// <param name="Composite">
/// Whether list or dictionary values are expanded as composite values.
/// </param>
/// <param name="MaxChar">
/// Maximum number of Unicode code points emitted for a scalar value, or
/// <c>-1</c> for no limit.
/// </param>
public sealed record class UriTemplateASTPlaceholder(
        string Name,
        bool Composite,
        int MaxChar
        ) {

    /// <summary>
    /// Expands this variable when a usable substitution is available.
    /// </summary>
    /// <param name="astOperator">The enclosing expression's expansion operator.</param>
    /// <param name="first">
    /// Whether this is the first variable in the expression that produces output.
    /// </param>
    /// <param name="substitutions">Values supplied for variables in the template.</param>
    /// <param name="target">The target to which the expanded value is appended.</param>
    /// <returns>
    /// <see langword="true"/> if a value was expanded; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    public bool Expand(
        UriTemplateASTOperation astOperator,
        bool first,
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        ) {

        if (!substitutions.TryGetValue(this.Name, out var value)) {
            return false;
        } else {
            if (value is null) {
                return false;
            }
            SubstitutionType substType;
            {
                if (value is IUriTemplateValue uriTemplateValue) {
                    if (uriTemplateValue.TryGetValue(substitutions, out var nextValue)) {
                        substType = SubstitutionType.Value;
                        value = nextValue;
                        if (value is null) {
                            return false;
                        }
                    } else {
                        return false;
                    }
                }
                if (value is ODataValue oDataValue) {
                    if (oDataValue.Value is IList or IDictionary) {
                        value = oDataValue.Value;
                    }
                }

                if (value is string) {
                    substType = SubstitutionType.String;
                } else if (UriTemplateTarget.IsNativeType(value)) {
                    substType = SubstitutionType.String;
                } else if (value is IList list) {
                    if (0 == list.Count) {
                        return false;
                    } else {
                        substType = SubstitutionType.List;
                    }
                } else if (value is IDictionary dictionary) {
                    if (0 == dictionary.Count) {
                        return false;
                    } else {
                        substType = SubstitutionType.Dictionary;
                    }
                } else if (value is IODataValue iODataValue) {
                    substType = SubstitutionType.Value;
                } else {
                    throw new ArgumentException($"Illegal class passed as substitution, found {value.GetType()} at name:{this.Name}");
                }
            }

            if (first) {
                astOperator.AddPrefix(target.Output);
            } else {
                astOperator.AddSeparator(target.Output);
            }

            switch (substType) {
                case SubstitutionType.String:
                    target.AddScalarValue(astOperator, this.Name, value, this.MaxChar);
                    break;
                case SubstitutionType.Value:
                    target.AddScalarValue(astOperator, this.Name, value, this.MaxChar);
                    break;
                case SubstitutionType.List:
                    _ = target.AddListValue(astOperator, this.Name, (IList)value, this.MaxChar, this.Composite);
                    break;
                case SubstitutionType.Dictionary:
                    _ = target.AddDictionaryValue(astOperator, this.Name, ((IDictionary)value), this.MaxChar, this.Composite);
                    break;
            }
            return true;
        }
    }

    private enum SubstitutionType {
        Empty,
        String,
        Value,
        List,
        Dictionary
    }

}
