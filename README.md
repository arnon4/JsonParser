# JsonParser - A Dynamic Json Parser for C\#

## What is JsonParser

A library for dynamic JSON parsing in C#. JsonParser provides the ability to parse JSON dynamically at runtime while still having the ability to inspect value types, and without the need to create a class for serialization.

## Quickstart

Using the following JSON object as an example, stored in `example.json`:

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
JsonParser parser = new(lines);
var jsonArray = parser.Get<JsonArray>(); // we can retrieve either a JsonArray or a JsonObject

var firstItem = jsonArray.Get<string>(0)!; // "1"
var thirdItem = jsonArray.Get<double>(2); // 3.2
var fifthItem = jsonArray.Get<bool>(4); // false
var nestedNestedCity = jsonArray.Get<JsonObject>(5)!.Get<JsonObject>("address")!.Get<string>("city")!; // "New York"
var nullValue = jsonArray.Get<JsonArray>(6)!.Get<object?>(2); // null
Console.WriteLine(jsonArray); //["1", "2", 3.2, false, null, {"name": "Alex", "age": 30, "isStudent": false, "address": {"city": "New York", "country": "USA"}, "hobbies": ["reading", "music", "movies"]}, ["1", 2, false, null]]
```
