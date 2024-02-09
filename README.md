# JsonParser - A Dynamic Json Parser for C\#

## Why JsonParser?

I wanted to be able to dynamically parse JSON in C# without having to create a class for every JSON object I wanted to parse.
I also wanted to be able to parse JSON objects that I didn't know the structure of at compile time.

I find this useful for parsing configuration files which can grow quite large, and because of this are annoying to maintain as C# classes.

## QuickstartA

Using the following JSON object as an example, stored in example.json:

```JSON
[
    "1",
    "2",
    3.2,
    null,
    false,
    {
        "name": "Alex",
        "age": 30,
        "address": {
            "city": "New York",
            "country": "USA"
        },
        "isStudent": false,
        "hobbies": [
            "reading",
            "music",
            "movies"
        ]
    },
    [
        "1",
        2,
        null,
        false
    ]
]
```

You can parse it like this:

```C#
using JsonParser;

var lines = File.ReadAllLines("example.json");
parser.Parse();
JsonArray jsonArray = parser.GetParsed<JsonArray>(); // we can retrieve either a JsonArray or a JsonObject

string firstItem = jsonArray.Get<string>(0)!; // "1"
double thirdItem = jsonArray.Get<double>(2); // 3.2
bool fifthItem = jsonArray.Get<bool>(4); // false
string nestedNestedCity = jsonArray.Get<JsonObject>(5)!.Get<JsonObject>("address")!.Get<string>("city")!; // "New York"
object? nullValue = jsonArray.Get<JsonArray>(6)!.Get<object?>(2); // null
Console.WriteLine(jsonArray); //["1", "2", 3.2, false, null, {"name": "Alex", "age": 30, "isStudent": false, "address": {"city": "New York", "country": "USA"}, "hobbies": ["reading", "music", "movies"]}, ["1", 2, false, null]]
```
