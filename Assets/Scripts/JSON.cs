using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using JetBrains.Annotations;

public class JsonObject : Dictionary<string, object>
{
    public JsonObject() { }
    public JsonObject(IDictionary<string, object> _dictionary) : base(_dictionary) { }


    public object this[string _key, object _default ]
    {
        get { return this.Get(_key, _default); }
    }

    public void Set(string _key, object _value)
    {
        this.Replace(_key, _value);
    }

    public string ToJson()
    {
        return ToJson(this);
    }

    public static string ToJson(JsonObject _object)
    {
        return MiniJson.Serialize(_object);
    }

    public static JsonObject FromJson(string _jsonString)
    {
        return MiniJson.Deserialize(_jsonString) as JsonObject;
    }

    public override string ToString()
    {
        return JsonFormatter.prettyPrint(ToJson(this)) ?? string.Empty;
    }
}

public class JsonArray : List<object>
{
    public JsonArray() { }
    public JsonArray(IEnumerable<object> collection) : base(collection) { }
    public JsonArray(int capacity) : base(capacity) { }

    public string ToJson()
    {
        return ToJson(this);
    }

    public static string ToJson(JsonArray _object)
    {
        return MiniJson.Serialize(_object);
    }

    public static JsonArray FromJson(string _jsonString)
    {
        return MiniJson.Deserialize(_jsonString) as JsonArray;
    }
}

public static class JSONHelper
{
    /// <summary>
    /// Возвращает значение из Json-объекта, преобразуя его в указанный тип.
    /// Если значение отсутствует или имеет неверный формат - возвращает значение по-умолчанию.
    /// </summary>
    /// <typeparam name="TResult">Тип результата</typeparam>
    /// <param name="_obj">Json-объект с данными</param>
    /// <param name="_key">Ключ</param>
    /// <param name="_default">Значение по-умолчанию, возвращаемое в случае ошибки</param>
    /// <returns>Результат</returns>
    public static TResult Get<TResult>(this JsonObject _obj, string _key, TResult _default = default(TResult))
    {
        if (_obj == null || !_obj.ContainsKey(_key))
            return _default;
        return _obj[_key].Cast(_default);
    }

    /// <summary>
    /// Получает значение из Json-объекта, преобразуя его в указанный тип.
    /// Если значение найдено - записывает его в переменную.
    /// </summary>
    /// <typeparam name="TResult">Тип результата</typeparam>
    /// <param name="_obj">Json-объект с данными</param>
    /// <param name="_key">Ключ</param>
    /// <param name="_value">Значение для записи</param>
    public static void Get<TResult>(this JsonObject _obj, string _key, ref TResult _value)
    {
        if (_obj == null || !_obj.ContainsKey(_key))
            return;
        _value = _obj[_key].Cast(_value);
    }

    /// <summary>
    /// Возвращает значение из Json-объекта, преобразуя его в указанный тип.
    /// Если значение отсутствует или имеет неверный формат - создает новое значение указанного типа. 
    /// </summary>
    /// <typeparam name="TResult">Тип результата</typeparam>
    /// <param name="_obj">Json-объект с данными</param>
    /// <param name="_key">Ключ</param>
    /// <returns>Результат</returns>
    [NotNull]
    public static TResult GetForced<TResult>(this JsonObject _obj, string _key)
        where TResult : new()
    {
        if (_obj == null || !_obj.ContainsKey(_key))
            return new TResult();
        return _obj[_key].CastNew<TResult>();
    }
}

    public class MiniJson
    {
        // interpret all numbers as if they are english US formatted numbers
        private static NumberFormatInfo numberFormat = (new CultureInfo("en-US")).NumberFormat;

        /// <summary>
        /// Parses the string json into a value
        /// </summary>
        /// <param name="json">A JSON string.</param>
        /// <returns>An List&lt;object&gt;, a Dictionary&lt;string, object&gt;, a double, an integer,a string, null, true, or false</returns>
        public static object Deserialize(string json)
        {
            // save the string for debug information
            if (json == null)
            {
                return null;
            }

            return Parser.Parse(json);
        }

        /// <summary>
        /// Converts a IDictionary / IList object or a simple type (string, int, etc.) into a JSON string
        /// </summary>
        /// <param name="json">A Dictionary&lt;string, object&gt; / List&lt;object&gt;</param>
        /// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
        public static string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private sealed class Parser : IDisposable
        {
            private const string WhiteSpace = " \t\n\r";
            private const string WordBreak = " \t\n\r{}[],:\"";

            private StringReader json;

            private Parser(string jsonString)
            {
                json = new StringReader(jsonString);
            }

            private enum TOKEN
            {
                NONE,
                CURLY_OPEN,
                CURLY_CLOSE,
                SQUARED_OPEN,
                SQUARED_CLOSE,
                COLON,
                COMMA,
                STRING,
                NUMBER,
                TRUE,
                FALSE,
                NULL
            }

            private char PeekChar
            {
                get
                {
                    return Convert.ToChar(json.Peek());
                }
            }

            private char NextChar
            {
                get
                {
                    return Convert.ToChar(json.Read());
                }
            }

            private string NextWord
            {
                get
                {
                    StringBuilder word = new StringBuilder();

                    while (WordBreak.IndexOf(PeekChar) == -1)
                    {
                        word.Append(NextChar);

                        if (json.Peek() == -1)
                        {
                            break;
                        }
                    }

                    return word.ToString();
                }
            }

            private TOKEN NextToken
            {
                get
                {
                    EatWhitespace();

                    if (json.Peek() == -1)
                    {
                        return TOKEN.NONE;
                    }

                    char c = PeekChar;
                    switch (c)
                    {
                        case '{':
                            return TOKEN.CURLY_OPEN;
                        case '}':
                            json.Read();
                            return TOKEN.CURLY_CLOSE;
                        case '[':
                            return TOKEN.SQUARED_OPEN;
                        case ']':
                            json.Read();
                            return TOKEN.SQUARED_CLOSE;
                        case ',':
                            json.Read();
                            return TOKEN.COMMA;
                        case '"':
                            return TOKEN.STRING;
                        case ':':
                            return TOKEN.COLON;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                        case '-':
                            return TOKEN.NUMBER;
                    }

                    string word = NextWord;

                    switch (word)
                    {
                        case "false":
                            return TOKEN.FALSE;
                        case "true":
                            return TOKEN.TRUE;
                        case "null":
                            return TOKEN.NULL;
                    }

                    return TOKEN.NONE;
                }
            }

            public static object Parse(string jsonString)
            {
                using (var instance = new Parser(jsonString))
                {
                    return instance.ParseValue();
                }
            }

            public void Dispose()
            {
                json.Dispose();
                json = null;
            }

            private JsonObject ParseObject()
            {
                JsonObject table = new JsonObject();

                // ditch opening brace
                json.Read();

                // {
                while (true)
                {
                    switch (NextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.CURLY_CLOSE:
                            return table;
                        default:
                            // name
                            string name = ParseString();
                            if (name == null)
                            {
                                return null;
                            }

                            // :
                            if (NextToken != TOKEN.COLON)
                            {
                                return null;
                            }

                            // ditch the colon
                            json.Read();

                            // value
                            table[name] = ParseValue();
                            break;
                    }
                }
            }

            private JsonArray ParseArray()
            {
                JsonArray array = new JsonArray();

                // ditch opening bracket
                json.Read();

                // [
                var parsing = true;
                while (parsing)
                {
                    TOKEN nextToken = NextToken;

                    switch (nextToken)
                    {
                        case TOKEN.NONE:
                            return null;
                        case TOKEN.COMMA:
                            continue;
                        case TOKEN.SQUARED_CLOSE:
                            parsing = false;
                            break;
                        default:
                            object value = ParseByToken(nextToken);

                            array.Add(value);
                            break;
                    }
                }

                return array;
            }

            private object ParseValue()
            {
                TOKEN nextToken = NextToken;
                return ParseByToken(nextToken);
            }

            private object ParseByToken(TOKEN token)
            {
                switch (token)
                {
                    case TOKEN.STRING:
                        return ParseString();
                    case TOKEN.NUMBER:
                        return ParseNumber();
                    case TOKEN.CURLY_OPEN:
                        return ParseObject();
                    case TOKEN.SQUARED_OPEN:
                        return ParseArray();
                    case TOKEN.TRUE:
                        return true;
                    case TOKEN.FALSE:
                        return false;
                    case TOKEN.NULL:
                        return null;
                    default:
                        return null;
                }
            }

            private string ParseString()
            {
                StringBuilder s = new StringBuilder();
                char c;

                // ditch opening quote
                json.Read();

                bool parsing = true;
                while (parsing)
                {
                    if (json.Peek() == -1)
                    {
                        parsing = false;
                        break;
                    }

                    c = NextChar;
                    switch (c)
                    {
                        case '"':
                            parsing = false;
                            break;
                        case '\\':
                            if (json.Peek() == -1)
                            {
                                parsing = false;
                                break;
                            }

                            c = NextChar;
                            switch (c)
                            {
                                case '"':
                                case '\\':
                                case '/':
                                    s.Append(c);
                                    break;
                                case 'b':
                                    s.Append('\b');
                                    break;
                                case 'f':
                                    s.Append('\f');
                                    break;
                                case 'n':
                                    s.Append('\n');
                                    break;
                                case 'r':
                                    s.Append('\r');
                                    break;
                                case 't':
                                    s.Append('\t');
                                    break;
                                case 'u':
                                    var hex = new StringBuilder();

                                    for (int i = 0; i < 4; i++)
                                    {
                                        hex.Append(NextChar);
                                    }

                                    s.Append((char)Convert.ToInt32(hex.ToString(), 16));
                                    break;
                            }

                            break;
                        default:
                            s.Append(c);
                            break;
                    }
                }

                return s.ToString();
            }

            private object ParseNumber()
            {
                string number = NextWord;

                if (number.IndexOf('.') == -1)
                {
                    return long.Parse(number, numberFormat);
                }

                return double.Parse(number, numberFormat);
            }

            private void EatWhitespace()
            {
                while (WhiteSpace.IndexOf(PeekChar) != -1)
                {
                    json.Read();

                    if (json.Peek() == -1)
                    {
                        break;
                    }
                }
            }
        }

        private sealed class Serializer
        {
            private StringBuilder builder;

            private Serializer()
            {
                builder = new StringBuilder();
            }

            public static string Serialize(object obj)
            {
                var instance = new Serializer();

                instance.SerializeValue(obj);

                return instance.builder.ToString();
            }

            private void SerializeValue(object value)
            {
                IList asList;
                IDictionary asDict;
                string asStr;

                if (value == null)
                {
                    builder.Append("null");
                }
                else if ((asStr = value as string) != null)
                {
                    SerializeString(asStr);
                }
                else if (value is bool)
                {
                    builder.Append(value.ToString().ToLower());
                }
                else if ((asList = value as IList) != null)
                {
                    SerializeArray(asList);
                }
                else if ((asDict = value as IDictionary) != null)
                {
                    SerializeObject(asDict);
                }
                else if (value is char)
                {
                    SerializeString(value.ToString());
                }
                else
                {
                    SerializeOther(value);
                }
            }

            private void SerializeObject(IDictionary obj)
            {
                bool first = true;

                builder.Append('{');

                foreach (object e in obj.Keys)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    SerializeString(e.ToString());
                    builder.Append(':');

                    SerializeValue(obj[e]);

                    first = false;
                }

                builder.Append('}');
            }

            private void SerializeArray(IList array)
            {
                builder.Append('[');

                bool first = true;

                foreach (object obj in array)
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    SerializeValue(obj);

                    first = false;
                }

                builder.Append(']');
            }

            private void SerializeString(string str)
            {
                builder.Append('\"');

                char[] charArray = str.ToCharArray();
                foreach (var c in charArray)
                {
                    switch (c)
                    {
                        case '"':
                            builder.Append("\\\"");
                            break;
                        case '\\':
                            builder.Append("\\\\");
                            break;
                        case '\b':
                            builder.Append("\\b");
                            break;
                        case '\f':
                            builder.Append("\\f");
                            break;
                        case '\n':
                            builder.Append("\\n");
                            break;
                        case '\r':
                            builder.Append("\\r");
                            break;
                        case '\t':
                            builder.Append("\\t");
                            break;
                        default:
                            int codepoint = Convert.ToInt32(c);
                            if ((codepoint >= 32) && (codepoint <= 126))
                            {
                                builder.Append(c);
                            }
                            else
                            {
                                builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
                            }

                            break;
                    }
                }

                builder.Append('\"');
            }

            private void SerializeOther(object value)
            {
                if (value is float
                    || value is int
                    || value is uint
                    || value is long
                    || value is double
                    || value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is ulong
                    || value is decimal)
                {
                    builder.Append(value.ToString());
                }
                else
                {
                    SerializeString(value.ToString());
                }
            }
        }
    }

    public class JsonFormatter
    {
        private bool inDoubleString = false;
        private bool inSingleString = false;
        private bool inVariableAssignment = false;
        private char prevChar = char.MinValue;
        private Stack<JsonContextType> context = new Stack<JsonContextType>();
        private const int defaultIndent = 0;
        private const string indent = "\t";
        private const string space = " ";

        public static string prettyPrint(string input)
        {
            try
            {
                return new JsonFormatter().print(input);
            }
            catch
            {
                return (string)null;
            }
        }

        private static void buildIndents(int indents, StringBuilder output)
        {
            for (; indents > 0; --indents)
                output.Append("\t");
        }

        private bool inString()
        {
            if (!inDoubleString)
                return inSingleString;
            return true;
        }

        public string print(string input)
        {
            StringBuilder output = new StringBuilder(input.Length * 2);
            for (int index = 0; index < input.Length; ++index)
            {
                char ch = input[index];
                switch (ch)
                {
                    case ' ':
                        if (inString())
                        {
                            output.Append(ch);
                            break;
                        }
                        break;
                    case '"':
                        if (!inSingleString && (int)prevChar != 92)
                            inDoubleString = !inDoubleString;
                        output.Append(ch);
                        break;
                    default:
                        switch (ch)
                        {
                            case ':':
                                if (!inString())
                                {
                                    inVariableAssignment = true;
                                    output.Append(ch);
                                    output.Append(" ");
                                    break;
                                }
                                output.Append(ch);
                                break;
                            case '=':
                                output.Append(ch);
                                break;
                            default:
                                switch (ch)    
                                {
                                    case '[':
                                        output.Append(ch);
                                        if (!inString())
                                        {
                                            context.Push(JsonContextType.Array);
                                            break;
                                        }
                                        break;
                                    case ']':
                                        if (!inString())
                                        {
                                            output.Append(ch);
                                            context.Pop();
                                            break;
                                        }
                                        output.Append(ch);
                                        break;
                                    default:
                                        switch (ch)
                                        {
                                            case '{':
                                                if (!inString())
                                                {
                                                    if (inVariableAssignment || context.Count > 0 && context.Peek() != JsonContextType.Array)
                                                    {
                                                        output.Append(Environment.NewLine);
                                                        buildIndents(context.Count, output);
                                                    }
                                                    output.Append(ch);
                                                    context.Push(JsonContextType.Object);
                                                    output.Append(Environment.NewLine);
                                                    buildIndents(context.Count, output);
                                                    break;
                                                }
                                                output.Append(ch);
                                                break;
                                            case '}':
                                                if (!inString())
                                                {
                                                    output.Append(Environment.NewLine);
                                                    context.Pop();
                                                    buildIndents(context.Count, output);
                                                    output.Append(ch);
                                                    break;
                                                }
                                                output.Append(ch);
                                                break;
                                            default:
                                                if ((int)ch != 39)
                                                {
                                                    if ((int)ch == 44)
                                                    {
                                                        output.Append(ch);
                                                        if (!inString())
                                                            output.Append(" ");
                                                        if (!inString() && context.Peek() != JsonContextType.Array)
                                                        {
                                                            buildIndents(context.Count, output);
                                                            output.Append(Environment.NewLine);
                                                            buildIndents(context.Count, output);
                                                            inVariableAssignment = false;
                                                            break;
                                                        }
                                                        break;
                                                    }
                                                    output.Append(ch);
                                                    break;
                                                }
                                                if (!inDoubleString && (int)prevChar != 92)
                                                    inSingleString = !inSingleString;
                                                output.Append(ch);
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                }
                prevChar = ch;
            }
            return output.ToString();
        }

        private enum JsonContextType
        {
            Object,
            Array,
        }
    }
