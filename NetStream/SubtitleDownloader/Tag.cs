using System.Diagnostics;

namespace WebScrapper.scrapper;

/// <summary>
/// Represents an HTML tag which may include attributes in the form of:
/// <code>&lt;tagname prop1="val1" prop2="val2"&gt; </code>
/// </summary>
public class Tag {
    public string Name { get; }
    public int StartOffset { get; internal set; } //offset at which the given tag begins
    public int EndOffset { get; internal set; } = -1;
    public List<(string, string)> Attributes { get; }

    /// <summary>
    /// Searches for attribute by key, if another pair with the same key exists the first pair's value is returned
    /// <returns> string being the pair's value or null if no match is found </returns>
    /// </summary>
    public string? GetAttribute(string key) {
        if (Attributes.Count == 0) {
            return null;
        }

        foreach (var key_value in Attributes) {
            if (key_value.Item1 == key) {
                return key_value.Item2;
            }
        }

        return null;
    }

    public bool CompareAttributes(params (string, string, Compare)[] pairs) {
        if (Attributes.Count < pairs.Length) {
            return false;
        }
        
        // Attributes can be provided in a varying order
        foreach (var predicate in pairs) {
            if (!AttributesMatchPredicate(predicate)) {
                return false;
            }
        }

        return true;
    }

    // Determines if Attributes match given predicate
    private bool AttributesMatchPredicate((string, string, Compare) predicate) {
        foreach (var pair in Attributes) {
            switch (predicate.Item3) {
                case Compare.EXACT:
                    if (pair.Item1 == predicate.Item1 && pair.Item2 == predicate.Item2) {
                        return true;
                    }
                    break;
                case Compare.KEY_ONLY:
                    if (pair.Item1 == predicate.Item1) {
                        return true;
                    }
                    break;
                case Compare.VALUE_STARTS_WITH:
                    if (pair.Item2.StartsWith(predicate.Item2)) {
                        return true;
                    }
                    break;
                default:
                    throw new UnreachableException("Compare enum has 3 values");
            }
        }

        return false;
    }

    public Tag(string name) {
        StartOffset = 0;
        Name = name;
        Attributes = new List<(string, string)>();
    }

    public Tag(string name, List<(string, string)> attributes) {
        StartOffset = 0;
        Name = name;
        Attributes = attributes;
    }

    public int EstimateEndOffset() {
        int end = StartOffset + 1 + Name.Length;
        foreach (var attribute in Attributes) {
            end += 1 + attribute.Item1.Length + attribute.Item2.Length + 3;
        }
        return end;
    }
    
    public override string ToString() {
        return "Tag{name=" + Name + ", index=" + StartOffset + "}";
    }
}

/// <summary>
/// Comparison policy for each pair of attributes.
/// It's meant to give more control when navigating the page.
/// Allows retrieving auto-generated tags or tags whose attribute values have no significance
/// </summary>
public enum Compare {
    ///<summary> Both key and value must match </summary>
    EXACT,

    ///<summary> Only the key must match whereas value is irrelevant </summary>
    KEY_ONLY,

    ///<summary> Key must match fully but value must only start with given string </summary>
    VALUE_STARTS_WITH,
}