using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace WebScrapper.scrapper;

public class HtmlDoc {
    private static readonly HttpClient Client = new() {
        Timeout = TimeSpan.FromSeconds(10)
    };
    private readonly string html;
    private readonly int len;
    private char concatChar;
    private bool delimitTags = true;
    private bool brToNewline = false;

    public HtmlDoc(string contents) {
        html = contents;
        len = html.Length;
        concatChar = '\n';
    }

    public void SetConcatenatingChar(char given) {
        concatChar = given;
    }

    //<br> -> '\n'
    public void ReplaceLineBreakWithNewLine(bool enabled) {
        brToNewline = enabled;
    }

    /// <summary>During extraction - raw text can be found in both tags and subtags. The text will be concatenated
    /// using the <c>concatChar</c> (a delimiter)</summary>
    /// <param name="enabled"> whether to use a delimiter when concatenating text extracted from two or more tags</param>
    /// <returns>The raw Tag object or null if not found</returns>
    public void DelimitTags(bool enabled) {
        delimitTags = enabled;
    }

    /// <summary> Find a tag by name matching at least given attributes.
    /// The tag returned will have at least the number of attributes specified.
    /// The order in which attributes are provided does not matter </summary>
    /// <param name="tag"> The tag name to find</param>
    /// <param name="attributes"> The attributes to match against with accordance to rules</param>
    /// <returns>The raw Tag object or null if not found</returns>
    public Tag? Find(string tag, params (string, string, Compare)[] attributes) {
        return FindFrom(tag, 0, attributes);
    }

    /// <summary>Find a tag by name that may or may not contain attributes</summary>
    /// <param name="tag"> the tag name to find</param> 
    /// <returns>The raw Tag object or null if not found</returns>
    public Tag? Find(string tag) {
        return FindFrom(tag, 0);
    }
    
    /// <summary>Find tags by name and attributes starting at a given index until the end of document</summary>
    /// <param name="tag"> the tag name to find</param> 
    /// <param name="from"> index where to begin </param> 
    /// <param name="attributes"> predicates to match against </param> 
    /// <returns>The raw Tag object or null if not found</returns>
    public List<Tag> FindAllFrom(string tag, int from, params (string, string, Compare)[] attributes) {
        List<Tag> tags = new List<Tag>();
        int cursor = from;
        while (cursor < len) {
            Tag? traverserTag = FindFrom(tag, cursor, attributes);
            if (traverserTag == null) {
                break;
            }

            tags.Add(traverserTag);
            if (traverserTag.EndOffset == -1) {
                cursor = traverserTag.StartOffset + 1;
            }
            else {
                cursor = traverserTag.EndOffset + 1;
            }
        }

        return tags;
    }

    public List<Tag> FindAll(string tag, params (string, string, Compare)[] attributes) {
        return FindAllFrom(tag, 0, attributes);
    }
    public List<Tag> FindAll(string tag) {
        return FindAllFrom(tag, 0);
    }

    /// <summary>Linearly searches for a tag in a given HtmlDoc returning the first Tag that matches predicates</summary>
    /// <param name="tag"> the tag name to find</param> 
    /// <param name="from"> the index to search from</param> 
    /// <param name="attributes"> (key, value, _) pairs of strings representing tag attributes to match against </param> 
    /// <returns>The <c>Tag</c> object or <c>null</c> if not found</returns>
    public Tag? FindFrom(string tag, int from, params (string, string, Compare)[] attributes) {
        for (int i = from; i < len; i++) {
            char chr = html[i];
            switch (chr) {
                case '<':
                    if (isCommentAhead(i)) {
                        i = skipComment(i+1);
                        continue;
                    }
                    bool closing = i + 1 < len && html[i + 1] == '/';
                    if (closing) {
                        // skip to the end of it
                        int tagNameSt = i + 2;
                        i = IndexOf(">", tagNameSt);
                        continue;
                    }

                    bool hasAttributes = false;
                    // parse tag
                    int j = i + 1;
                    for (; j < len; j++) {
                        if (isHtmlWhiteSpace(html[j])) {
                            hasAttributes = true;
                            break;
                        }

                        if (html[j] == '>') {
                            break;
                        }
                    }

                    string tagName = html[(i + 1)..j];
                    Tag parsedTag = new Tag(tagName);
                    int end = -1;
                    if (hasAttributes) {
                        end = parseAttributes(parsedTag, j + 1);
                    }

                    if (parsedTag.Name == tag && parsedTag.CompareAttributes(attributes)) {
                        parsedTag.StartOffset = i;
                        return parsedTag;
                    }

                    if (hasAttributes) {
                        i = end;
                    }

                    break;
            }
        }

        return null;
    }

    // https://www.w3.org/TR/html4/struct/text.html#h-9.1
    private static bool isHtmlWhiteSpace(char c) {
        return c == ' ' || c == '\r' || c == '\n' || c == '\t' || c == '\f';
    }
    
    // returns index of '>' where tag ends or an index at EOF
    private int parseAttributes(Tag parsedTag, int from) {
        //from cursor should be placed after tag name
        StringBuilder name = new StringBuilder();
        StringBuilder value = new StringBuilder();
        bool afterEqual = false, inQuoteVal = false;
        for (int i = from; i < len; i++) {
            char chr = html[i];
            switch (chr) {
                case '=':
                    afterEqual = true;
                    break;
                case ' ':
                case '\n':
                case '\r':
                case '\t':
                case '\f':
                    if (!afterEqual) {
                        break;
                    }
                    if (inQuoteVal) {
                        value.Append(chr);
                    }
                    else if (value.Length > 0) {
                        parsedTag.Attributes.Add((name.ToString(), value.ToString()));
                        afterEqual = false;
                        name.Clear();
                        value.Clear();
                    }
                    break;
                case '"':
                    if (!afterEqual) {
                        // keys shouldn't contain quotes but if it does it'll be appended
                        name.Append('"');
                        break;
                    }

                    if (inQuoteVal) {
                        // triggered on the closing "
                        parsedTag.Attributes.Add((name.ToString(), value.ToString()));
                        afterEqual = false;
                        name.Clear();
                        value.Clear();
                    }
                    inQuoteVal = !inQuoteVal;

                    break;
                case '/':
                    if (!inQuoteVal) {
                        //assume it's a self-closing tag
                        int endOffset = i + 1;
                        for (; endOffset < len; endOffset++) {
                            if (html[endOffset] != '>')
                                continue;
                            
                            break;
                        }

                        if (name.Length != 0 && value.Length != 0) {
                            parsedTag.Attributes.Add((name.ToString(), value.ToString()));
                        }
                        return endOffset;
                    }
                    
                    if (afterEqual) {
                        value.Append(chr);
                    }
                    else {
                        name.Append(chr);
                    }

                    break;
                case '>':
                    if (!inQuoteVal) {
                        if (name.Length > 0 && value.Length > 0) {
                            parsedTag.Attributes.Add((name.ToString(), value.ToString()));
                        }

                        return i;
                    }

                    if (afterEqual) {
                        value.Append(chr);
                    }
                    else {
                        name.Append(chr);
                    }

                    break;
                default:
                    if (afterEqual) {
                        value.Append(chr);
                    }
                    else {
                        name.Append(chr);
                    }

                    break;
            }
        }

        return -1;
    }
    
    /// <summary> Searches for all tags matching predicates within given tag </summary>
    /// <param name="tag"> the tag within which to search</param>
    /// <param name="target"> the tag name to match against </param>
    /// <returns>The <c>Tag</c> object or <c>null</c> if not found</returns>
    public List<Tag> ExtractTags(Tag tag, string target) {
        if (tag.StartOffset < 0 || tag.StartOffset >= len) {
            return new List<Tag>();
        }
        var extractedTags = new List<Tag>();
        int from = tag.EndOffset == -1 ? tag.EstimateEndOffset() : tag.EndOffset;
        Stack<string> tagStack = new Stack<string>();
        tagStack.Push(tag.Name);
        for (int i = from; i < len; i++) {
            char chr = html[i];
            switch (chr) {
                case '<':
                    if (isCommentAhead(i)) {
                        i = skipComment(i+1);
                        continue;
                    }
                    bool closing = i + 1 < len && html[i + 1] == '/';
                    if (closing) {
                        int tagNameSt = i + 2;
                        i = IndexOf(">", tagNameSt);
                        string closedTagName = html[tagNameSt..i].Trim();
                        // Pop all void tags
                        while (tagStack.Count > 0 && closedTagName != tagStack.Pop()) { }
                        if (tagStack.Count == 0) {
                            return extractedTags;
                        }
                        continue;
                    }

                    bool hasAttributes = false;
                    // parse tag
                    int j = i + 1;
                    for (; j < len; j++) {
                        if (isHtmlWhiteSpace(html[j])) {
                            hasAttributes = true;
                            break;
                        }

                        if (html[j] == '>') {
                            break;
                        }
                    }

                    string tagName = html[(i + 1)..j];
                    Tag parsedTag = new Tag(tagName);
                    int end = -1;
                    if (hasAttributes) {
                        end = parseAttributes(parsedTag, j + 1);
                    }

                    tagStack.Push(parsedTag.Name);
                    if (end != -1 && html[end - 1] == '/') {
                        tagStack.Pop();
                    }
                    // It could be that the tag given is a void tag in which case it has no tags
                    if (tagStack.Count == 0) {
                        return extractedTags;
                    }

                    if (parsedTag.Name == target) {
                        parsedTag.StartOffset = i;
                        extractedTags.Add(parsedTag);
                    }

                    if (hasAttributes) {
                        i = end;
                    }

                    break;
            }
        }

        return extractedTags;
    }

    private int skipComment(int from) {
        return IndexOf("-->", from);
    }
    private bool isCommentAhead(int lt) {
        return lt + 1 < len && html[lt + 1] == '!' 
            && lt + 2 < len && html[lt + 2] == '-'
            && lt + 3 < len && html[lt + 3] == '-';
    }

    /// <summary>
    /// Extracts text from given tag and all its sub-tags beginning at <c>tag.StartOffset</c>. The extraction will begin
    /// at the top level tag and will continue until either the tag is closed or the end of document is reached.
    /// Text extracted from sub-tags will be concatenated using the specified delimiter that can be set by calling
    /// <c>SetConcatenatingChar()</c>. The <c>EndOffset</c> field of the given tag will be set to the index where parsing
    /// finished if its value was -1. </summary>
    /// <param name="tag">the tag to extract text from</param> 
    /// <returns>The raw extracted html</returns>
    public string ExtractText(Tag tag) {
        if (tag.StartOffset < 0 || tag.StartOffset >= len) {
            return "";
        }

        bool append = false, inQuotes = false;
        bool concatenate = false;
        Stack<string> stack = new Stack<string>();
        StringBuilder text = new StringBuilder();
        for (int i = tag.StartOffset; i < len; i++) {
            char chr = html[i];
            switch (chr) {
                case ' ':
                    if (append) {
                        // based on default case
                        if (delimitTags && concatenate && text.Length > 0) {
                            text.Append(concatChar);
                        }

                        text.Append(' ');
                        concatenate = false;
                    }

                    break;
                case '"':
                    if (append) {
                        text.Append('"');
                    }
                    else {
                        inQuotes = !inQuotes;
                    }

                    break;
                // cannot exist in text in this form, must be a character code
                case '<':
                    if (isCommentAhead(i)) {
                        i = skipComment(i+1);
                        continue;
                    }
                    bool closing = i + 1 < len && html[i + 1] == '/';
                    if (closing) {
                        i++;
                    }

                    bool hasAttributes = false;
                    int tagEnd = i + 1;
                    for (int j = tagEnd; j < len; j++) {
                        char c = html[j];
                        switch (c) {
                            case ' ':
                                if (closing) {
                                    // handle closing tags with a whitespace </div >, they shouldn't have attributes
                                    continue;
                                }

                                hasAttributes = true;
                                tagEnd = j;
                                goto exitLoop;
                            case '>':
                                tagEnd = j;
                                goto exitLoop;
                        }
                    }

                    exitLoop:

                    string anyTag = html[(i + 1)..tagEnd];
                    if (brToNewline && anyTag.StartsWith("br")) {
                        text.Append('\n');
                    }

                    // move cursor to '>' or ' ' before attributes
                    i = tagEnd;
                    bool voidTag = anyTag[^1] == '/'; //last char
                    if (closing) {
                        // while stack is not exhausted
                        while (stack.Count > 0 && stack.Pop() != anyTag) { }

                        if (stack.Count == 0) {
                            if (tag.EndOffset == -1)
                                tag.EndOffset = i;
                            return text.ToString();
                        }
                    }
                    else if (!voidTag) {
                        stack.Push(anyTag);
                    }

                    append = !hasAttributes;
                    concatenate = true;
                    break;
                case '>':
                    concatenate = true;
                    append = true;
                    break;
                default:
                    if (append) {
                        if (delimitTags && concatenate && text.Length > 0) {
                            text.Append(concatChar);
                        }

                        concatenate = false;
                        text.Append(chr);
                    }
                    break;
            }
        }

        // if unclosed should exit due to length here
        if (tag.EndOffset == -1)
            tag.EndOffset = len;
        return text.ToString();
    }

    // Always returns indices within bounds
    private int IndexOf(string text, int from) {
        if (from >= len) {
            return len - 1;
        }
        int index = html.IndexOf(text, from, StringComparison.InvariantCulture);
        if (index == -1) {
            return len - 1;
        }
        return index;
    }

    public static string fetchHtml(string url) {
        var getRequest = new HttpRequestMessage {
            RequestUri = new Uri(url),
            Method = HttpMethod.Get,
        };
        getRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 Gecko/20100101");
        getRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        getRequest.Headers.AcceptLanguage.ParseAdd("en-US;q=0.7");
        getRequest.Headers.Add("Set-GPC", "1");
        var response = Client.Send(getRequest);
        if (response.StatusCode == HttpStatusCode.OK) {
            return response.Content.ReadAsStringAsync().Result;
        }

        Console.WriteLine("Response Code: " + response.StatusCode);
        return response.Content.ReadAsStringAsync().Result;
    }
}