#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Collections;
using System.Diagnostics.Tracing;
using System.Text;

namespace Brimborium.UriTemplate;

public sealed record class UriTemplateASTSequence(
    params UriTemplateASTSequenceChild[] ListChild
) {

    //public void Expand(
    //    IReadOnlyDictionary<string, object?> substitutions,
    //    StringBuilder output
    //    ) {
    //    UriTemplateTarget target = new(output);
    //    _ = this.Expand(substitutions, target);
    //}

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

public abstract record class UriTemplateASTSequenceChild() {
    public virtual bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        ) => false;
}

public sealed record class UriTemplateASTConstant(
        string Value
    ) : UriTemplateASTSequenceChild() {
    public override bool Expand(
        IReadOnlyDictionary<string, object?> substitutions,
        in UriTemplateTarget target
        ) {
        _ = target.Append(this.Value);
        return false;
    }
}

public enum UriTemplateASTOperator {
    Noop,
    Plus,
    Hash,
    Dot,
    Slash,
    Semicolon,
    Questionmark,
    Ampersand
}

public sealed record class UriTemplateASTOperation(
        UriTemplateASTOperator? Operator,
        params UriTemplateASTPlaceholder[] ListPlaceholder
        ) : UriTemplateASTSequenceChild() {
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

public sealed record class UriTemplateASTPlaceholder(
        string Name,
        bool Composite,
        int MaxChar
        ) {

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
                if (value is string stringValue) {
                    if (string.IsNullOrEmpty(stringValue)) {
                        return false;
                    } else { 
                        substType = SubstitutionType.String;
                    }
                } else if (UriTemplateTarget.IsNativeType(value)) {
                    substType = SubstitutionType.String;
                } else if (value is IUriTemplateValue uriTemplateValue) {
                    if (uriTemplateValue.TryGetValue(substitutions, out var nextValue)) {
                        substType = SubstitutionType.Value;
                        value = nextValue;
                    } else {
                        return false;
                    }
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
