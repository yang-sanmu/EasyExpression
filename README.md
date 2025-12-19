# EasyExpression

[English](README.md) | [中文](README.zh-CN.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

A lightweight, extensible expression engine for .NET that supports scripting with variables, control flow, and built-in functions.

## ✨ Features

- **Lightweight & Portable** - Targets .NET Standard 2.0, compatible with .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+
- **Safe Execution** - Built-in timeout, max depth, and node limits to prevent infinite loops and resource exhaustion
- **Rich Expression Support** - Arithmetic, comparison, logical operators, and string concatenation
- **Control Flow** - `if`/`elseif`/`else`, `local` blocks, `return`, `return_local`, and `assert` statements
- **Built-in Functions** - String, Math, and DateTime functions out of the box
- **Extensible** - Register custom functions and type converters via `IEngineContributor`
- **Type Annotations** - Optional type hints for field references like `[fieldName:decimal]`
- **Compilation Cache** - Improved performance for repeated script execution
- **Detailed Error Reporting** - Line/column positions and code snippets in error messages

## 📦 Installation

Install from NuGet:

```bash
dotnet add package EasyExpression.Core
```

Or via Package Manager:

```powershell
Install-Package EasyExpression.Core
```

Or via `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="EasyExpression.Core" Version="1.0.0" />
</ItemGroup>
```

### Build from source (optional)

```bash
git clone https://github.com/yang-sanmu/EasyExpression.git
cd EasyExpression
dotnet build
```

## 🚀 Quick Start

### Basic Usage

```csharp
using EasyExpression.Core.Engine;

// Create engine
var factory = new DefaultExpressionEngineFactory();
var engine = factory.Create();

// Define input fields
var inputs = new Dictionary<string, object?>
{
    { "price", 100m },
    { "quantity", 5 }
};

// Execute script
var script = @"
{
    set(total, [price:decimal] * [quantity:decimal])
    set(discount, [total] * 0.1)
    set(finalPrice, [total] - [discount])
}";

var result = engine.Execute(script, inputs);

// Access results
Console.WriteLine(result.Assignments["total"]);      // 500
Console.WriteLine(result.Assignments["discount"]);   // 50
Console.WriteLine(result.Assignments["finalPrice"]); // 450
```

### Script Validation

```csharp
var validationResult = engine.Validate(script);
if (!validationResult.Success)
{
    Console.WriteLine($"Error at line {validationResult.ErrorLine}: {validationResult.ErrorMessage}");
}
```

## 📖 Language Syntax

### Data Types

| Type | Example |
|------|---------|
| Number (decimal) | `123`, `45.67`, `-10` |
| String | `'hello'`, `"world"` |
| Boolean | `true`, `false` |
| DateTime | `now` (current time) |
| Null | `null` |

### Operators

| Category | Operators |
|----------|-----------|
| Arithmetic | `+`, `-`, `*`, `/`, `%` |
| Comparison | `==`, `!=`, `>`, `<`, `>=`, `<=` |
| Logical | `&&`, `\|\|`, `!` |

### Field References

Access input fields using square brackets:

```
[fieldName]              // Basic reference
[fieldName:decimal]      // With type annotation
[fieldName:datetime]     // DateTime type
[fieldName:bool]         // Boolean type
[fieldName:string]       // String type
```

### Statements

#### Set Statement
```
set(variableName, expression)
set(variableName:type, expression)  // With type annotation
```

#### If/ElseIf/Else
```
if(condition) {
    // statements
} elseif(condition) {
    // statements
} else {
    // statements
}
```

#### Local Block
```
local {
    // Isolated scope
    return_local  // Exit only this block
}
```

#### Assert
```
assert(condition, 'return', 'Error message', 'error')
assert(condition, 'continue', 'Warning message', 'warn')
```

#### Message
```
msg('Information message')
msg('Warning message', 'warn')
msg('Error message', 'error')
```

#### Return
```
return        // Exit entire script
return_local  // Exit current local block only
```

### Comments

```
// Single-line comment

/* 
   Multi-line 
   comment 
*/
```

## 🔧 Built-in Functions

### String Functions

| Function | Description | Example |
|----------|-------------|---------|
| `ToString(value)` | Convert to string | `ToString(123)` → `"123"` |
| `StartsWith(str, prefix, [ignoreCase])` | Check prefix | `StartsWith('Hello', 'He')` → `true` |
| `EndsWith(str, suffix, [ignoreCase])` | Check suffix | `EndsWith('Hello', 'lo')` → `true` |
| `Contains(str, sub, [ignoreCase])` | Check contains | `Contains('Hello', 'ell')` → `true` |
| `ToUpper(str)` | Uppercase | `ToUpper('hello')` → `"HELLO"` |
| `ToLower(str)` | Lowercase | `ToLower('HELLO')` → `"hello"` |
| `Trim(str)` | Remove whitespace | `Trim('  hi  ')` → `"hi"` |
| `Len(str)` | String length | `Len('hello')` → `5` |
| `Replace(str, old, new, [ignoreCase])` | Replace text | `Replace('hello', 'l', 'L')` → `"heLLo"` |
| `Substring(str, start, [length])` | Extract substring | `Substring('hello', 1, 3)` → `"ell"` |
| `RegexMatch(str, pattern, [flags])` | Regex matching | `RegexMatch('test123', '\\d+')` → `true` |
| `Coalesce(a, b, ...)` | First non-null | `Coalesce(null, 'default')` → `"default"` |
| `Iif(cond, trueVal, falseVal)` | Inline if | `Iif(true, 'yes', 'no')` → `"yes"` |
| `FieldExists(name, ...)` | Check field exists | `FieldExists('price')` → `true/false` |

### Math Functions

| Function | Description | Example |
|----------|-------------|---------|
| `ToDecimal(value)` | Convert to decimal | `ToDecimal('123.45')` → `123.45` |
| `Max(a, b, ...)` | Maximum value | `Max(1, 5, 3)` → `5` |
| `Min(a, b, ...)` | Minimum value | `Min(1, 5, 3)` → `1` |
| `Sum(a, b, ...)` | Sum of values | `Sum(1, 2, 3)` → `6` |
| `Average(a, b, ...)` | Average value | `Average(1, 2, 3)` → `2` |
| `Round(value, [digits])` | Round number | `Round(3.14159, 2)` → `3.14` |
| `Abs(value)` | Absolute value | `Abs(-5)` → `5` |

### DateTime Functions

| Function | Description | Example |
|----------|-------------|---------|
| `ToDateTime(str)` | Parse datetime | `ToDateTime('2024-01-01 00:00:00')` |
| `FormatDateTime(dt, [format])` | Format datetime | `FormatDateTime(now, 'yyyy-MM-dd')` |
| `AddDays(dt, days)` | Add days | `AddDays(now, 7)` |
| `AddHours(dt, hours)` | Add hours | `AddHours(now, 24)` |
| `AddMinutes(dt, minutes)` | Add minutes | `AddMinutes(now, 30)` |
| `AddSeconds(dt, seconds)` | Add seconds | `AddSeconds(now, 60)` |
| `TimeSpan(dt1, dt2, [unit])` | Time difference | `TimeSpan(dt1, dt2, 'd')` (days) |

TimeSpan units: `ms` (milliseconds), `s` (seconds), `m` (minutes), `h` (hours, default), `d` (days)

## ⚙️ Configuration Options

```csharp
var engine = factory.Create(options =>
{
    // Execution limits
    options.MaxDepth = 64;                    // Max nesting depth
    options.MaxNodes = 2000;                  // Max AST nodes
    options.MaxNodeVisits = 10000;            // Max node visits
    options.TimeoutMilliseconds = 2000;       // Execution timeout
    
    // String handling
    options.StringComparison = StringComparison.OrdinalIgnoreCase;
    options.CaseInsensitiveFieldNames = true;
    
    // Number handling
    options.RoundingDigits = 2;
    options.MidpointRounding = MidpointRounding.AwayFromZero;
    
    // DateTime
    options.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    options.NowUseLocalTime = true;
    
    // Null handling
    options.TreatNullStringAsEmpty = true;
    options.TreatNullDecimalAsZero = false;
    options.TreatNullBoolAsFalse = false;
    
    // Equality comparison mode
    options.EqualityCoercion = EqualityCoercionMode.Strict;
    
    // String concatenation behavior
    options.StringConcat = StringConcatMode.PreferStringIfAnyString;
    
    // Other
    options.EnableComments = true;
    options.EnableCompilationCache = true;
    options.RegexTimeoutMilliseconds = 0;     // 0 = no timeout
});
```

### StringConcat Modes

`StringConcat` only affects the `+` operator when at least one side is a string.

| Mode | Behavior | Examples |
|------|----------|----------|
| `PreferStringIfAnyString` | If either operand is a string, always convert both sides to string (via converters when available) and concatenate. | `'1' + 2` → `"12"`, `ToDateTime('2024-01-01 00:00:00') + ' UTC'` → `"2024-01-01 00:00:00 UTC"` |
| `PreferNumericIfParsable` | If either operand is a string, first try parsing both sides as `decimal`. If both are parsable, do numeric addition; otherwise fall back to string concatenation. | `'1' + '2'` → `3`, `'1' + 'b'` → `"1b"` |

### Equality Coercion Modes

| Mode | Behavior |
|------|----------|
| `Strict` | No type coercion; type mismatch throws error |
| `NumberFriendly` | Try numeric comparison when strings involved |
| `Permissive` | Fall back to string comparison on mismatch |
| `MixedNumericOnly` | Numeric coercion only for number-string pairs |

## 🔌 Extensibility

### Custom Functions

```csharp
public class MyFunction : IFunction
{
    public string Name => "MyFunc";
    
    public object? Invoke(object?[] args, InvocationContext ctx)
    {
        // Implementation
        return args[0]?.ToString()?.ToUpperInvariant();
    }
}

// Register via contributor
public class MyContributor : IEngineContributor
{
    public void Configure(EngineServices services)
    {
        services.Functions.Register(new MyFunction());
    }
}

// Use contributor
var engine = factory.Create(contributors: new[] { new MyContributor() });
```

### Custom Type Converters

```csharp
public class MyConverter : ITypeConverter
{
    public Type InputType => typeof(string);
    public Type OutputType => typeof(MyType);
    
    public bool TryConvert(object? value, out object? result)
    {
        // Conversion logic
    }
}

// Register in contributor
services.Converters.Register(new MyConverter());
```

## 📊 Execution Result

```csharp
var result = engine.Execute(script, inputs);

// Check for errors
if (result.HasError)
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
    Console.WriteLine($"Location: Line {result.ErrorLine}, Column {result.ErrorColumn}");
    Console.WriteLine($"Code: {result.ErrorSnippet}");
    Console.WriteLine($"Error Code: {result.ErrorCode}");
}

// Access assigned variables
foreach (var kvp in result.Assignments)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// Access messages
foreach (var msg in result.Messages)
{
    Console.WriteLine($"[{msg.Level}] {msg.Text}");
}

// Execution time
Console.WriteLine($"Elapsed: {result.Elapsed}");
```

## 🧪 Running Tests

```bash
cd EasyExpression
dotnet test
```

## 📁 Project Structure

```
EasyExpression/
├── EasyExpression.Core/
│   └── Engine/
│       ├── Ast/              # Abstract Syntax Tree nodes
│       ├── Conversion/       # Type converters
│       ├── Functions/        # Built-in and custom functions
│       │   └── BuiltIns/     # String, Math, DateTime functions
│       ├── Parsing/          # Lexer and Parser
│       ├── Runtime/          # Execution context and results
│       ├── ExpressionEngine.cs
│       ├── ExpressionEngineFactory.cs
│       └── ExpressionEngineOptions.cs
└── EasyExpression.Core.Tests/    # Unit tests
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.