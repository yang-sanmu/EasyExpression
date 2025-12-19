# EasyExpression

[English](README.md) | [中文](README.zh-CN.md)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

一个轻量级、可扩展的 .NET 表达式引擎，支持变量、控制流和内置函数的脚本执行。

## ✨ 特性

- **轻量便携** - 基于 .NET Standard 2.0，兼容 .NET Framework 4.6.1+、.NET Core 2.0+ 和 .NET 5+
- **安全执行** - 内置超时、最大深度和节点数限制，防止无限循环和资源耗尽
- **丰富的表达式支持** - 算术、比较、逻辑运算符和字符串拼接
- **控制流** - 支持 `if`/`elseif`/`else`、`local` 块、`return`、`return_local` 和 `assert` 语句
- **内置函数** - 开箱即用的字符串、数学和日期时间函数
- **可扩展** - 通过 `IEngineContributor` 注册自定义函数和类型转换器
- **类型注解** - 可选的字段类型提示，如 `[fieldName:decimal]`
- **编译缓存** - 提升重复脚本执行的性能
- **详细的错误报告** - 错误信息包含行/列位置和代码片段

## 📦 安装

从 NuGet 安装：

```bash
dotnet add package EasyExpression.Core
```

或使用 NuGet 包管理器（Package Manager）：

```powershell
Install-Package EasyExpression.Core
```

或使用 `PackageReference`：

```xml
<ItemGroup>
  <PackageReference Include="EasyExpression.Core" Version="1.0.0" />
</ItemGroup>
```

### 从源码构建（可选）

```bash
git clone https://github.com/yang-sanmu/EasyExpression.git
cd EasyExpression
dotnet build
```

## 🚀 快速开始

### 基本用法

```csharp
using EasyExpression.Core.Engine;

// 创建引擎
var factory = new DefaultExpressionEngineFactory();
var engine = factory.Create();

// 定义输入字段
var inputs = new Dictionary<string, object?>
{
    { "price", 100m },
    { "quantity", 5 }
};

// 执行脚本
var script = @"
{
    set(total, [price:decimal] * [quantity:decimal])
    set(discount, [total] * 0.1)
    set(finalPrice, [total] - [discount])
}";

var result = engine.Execute(script, inputs);

// 访问结果
Console.WriteLine(result.Assignments["total"]);      // 500
Console.WriteLine(result.Assignments["discount"]);   // 50
Console.WriteLine(result.Assignments["finalPrice"]); // 450
```

### 脚本验证

```csharp
var validationResult = engine.Validate(script);
if (!validationResult.Success)
{
    Console.WriteLine($"第 {validationResult.ErrorLine} 行错误: {validationResult.ErrorMessage}");
}
```

## 📖 语言语法

### 数据类型

| 类型 | 示例 |
|------|------|
| 数字 (decimal) | `123`, `45.67`, `-10` |
| 字符串 | `'hello'`, `"world"` |
| 布尔值 | `true`, `false` |
| 日期时间 | `now` (当前时间) |
| 空值 | `null` |

### 运算符

| 类别 | 运算符 |
|------|--------|
| 算术 | `+`, `-`, `*`, `/`, `%` |
| 比较 | `==`, `!=`, `>`, `<`, `>=`, `<=` |
| 逻辑 | `&&`, `\|\|`, `!` |

### 字段引用

使用方括号访问输入字段：

```
[fieldName]              // 基本引用
[fieldName:decimal]      // 带类型注解
[fieldName:datetime]     // 日期时间类型
[fieldName:bool]         // 布尔类型
[fieldName:string]       // 字符串类型
```

### 语句

#### Set 语句
```
set(variableName, expression)
set(variableName:type, expression)  // 带类型注解
```

#### If/ElseIf/Else
```
if(condition) {
    // 语句
} elseif(condition) {
    // 语句
} else {
    // 语句
}
```

#### Local 块
```
local {
    // 隔离作用域
    return_local  // 仅退出此块
}
```

#### Assert
```
assert(condition, 'return', '错误信息', 'error')
assert(condition, 'continue', '警告信息', 'warn')
```

#### Message
```
msg('信息消息')
msg('警告消息', 'warn')
msg('错误消息', 'error')
```

#### Return
```
return        // 退出整个脚本
return_local  // 仅退出当前 local 块
```

### 注释

```
// 单行注释

/* 
   多行
   注释 
*/
```

## 🔧 内置函数

### 字符串函数

| 函数 | 描述 | 示例 |
|------|------|------|
| `ToString(value)` | 转换为字符串 | `ToString(123)` → `"123"` |
| `StartsWith(str, prefix, [ignoreCase])` | 检查前缀 | `StartsWith('Hello', 'He')` → `true` |
| `EndsWith(str, suffix, [ignoreCase])` | 检查后缀 | `EndsWith('Hello', 'lo')` → `true` |
| `Contains(str, sub, [ignoreCase])` | 检查包含 | `Contains('Hello', 'ell')` → `true` |
| `ToUpper(str)` | 转大写 | `ToUpper('hello')` → `"HELLO"` |
| `ToLower(str)` | 转小写 | `ToLower('HELLO')` → `"hello"` |
| `Trim(str)` | 去除空白 | `Trim('  hi  ')` → `"hi"` |
| `Len(str)` | 字符串长度 | `Len('hello')` → `5` |
| `Replace(str, old, new, [ignoreCase])` | 替换文本 | `Replace('hello', 'l', 'L')` → `"heLLo"` |
| `Substring(str, start, [length])` | 提取子串 | `Substring('hello', 1, 3)` → `"ell"` |
| `RegexMatch(str, pattern, [flags])` | 正则匹配 | `RegexMatch('test123', '\\d+')` → `true` |
| `Coalesce(a, b, ...)` | 第一个非空值 | `Coalesce(null, 'default')` → `"default"` |
| `Iif(cond, trueVal, falseVal)` | 内联条件 | `Iif(true, 'yes', 'no')` → `"yes"` |
| `FieldExists(name, ...)` | 检查字段存在 | `FieldExists('price')` → `true/false` |

### 数学函数

| 函数 | 描述 | 示例 |
|------|------|------|
| `ToDecimal(value)` | 转换为 decimal | `ToDecimal('123.45')` → `123.45` |
| `Max(a, b, ...)` | 最大值 | `Max(1, 5, 3)` → `5` |
| `Min(a, b, ...)` | 最小值 | `Min(1, 5, 3)` → `1` |
| `Sum(a, b, ...)` | 求和 | `Sum(1, 2, 3)` → `6` |
| `Average(a, b, ...)` | 平均值 | `Average(1, 2, 3)` → `2` |
| `Round(value, [digits])` | 四舍五入 | `Round(3.14159, 2)` → `3.14` |
| `Abs(value)` | 绝对值 | `Abs(-5)` → `5` |

### 日期时间函数

| 函数 | 描述 | 示例 |
|------|------|------|
| `ToDateTime(str)` | 解析日期时间 | `ToDateTime('2024-01-01 00:00:00')` |
| `FormatDateTime(dt, [format])` | 格式化日期时间 | `FormatDateTime(now, 'yyyy-MM-dd')` |
| `AddDays(dt, days)` | 添加天数 | `AddDays(now, 7)` |
| `AddHours(dt, hours)` | 添加小时 | `AddHours(now, 24)` |
| `AddMinutes(dt, minutes)` | 添加分钟 | `AddMinutes(now, 30)` |
| `AddSeconds(dt, seconds)` | 添加秒数 | `AddSeconds(now, 60)` |
| `TimeSpan(dt1, dt2, [unit])` | 时间差 | `TimeSpan(dt1, dt2, 'd')` (天) |

TimeSpan 单位：`ms` (毫秒)、`s` (秒)、`m` (分钟)、`h` (小时，默认)、`d` (天)

## ⚙️ 配置选项

```csharp
var engine = factory.Create(options =>
{
    // 执行限制
    options.MaxDepth = 64;                    // 最大嵌套深度
    options.MaxNodes = 2000;                  // 最大 AST 节点数
    options.MaxNodeVisits = 10000;            // 最大节点访问次数
    options.TimeoutMilliseconds = 2000;       // 执行超时时间
    
    // 字符串处理
    options.StringComparison = StringComparison.OrdinalIgnoreCase;
    options.CaseInsensitiveFieldNames = true;
    
    // 数字处理
    options.RoundingDigits = 2;
    options.MidpointRounding = MidpointRounding.AwayFromZero;
    
    // 日期时间
    options.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    options.NowUseLocalTime = true;
    
    // 空值处理
    options.TreatNullStringAsEmpty = true;
    options.TreatNullDecimalAsZero = false;
    options.TreatNullBoolAsFalse = false;
    
    // 相等比较模式
    options.EqualityCoercion = EqualityCoercionMode.Strict;
    
    // 字符串拼接行为
    options.StringConcat = StringConcatMode.PreferStringIfAnyString;
    
    // 其他
    options.EnableComments = true;
    options.EnableCompilationCache = true;
    options.RegexTimeoutMilliseconds = 0;     // 0 = 无超时
});
```

### StringConcat 模式

`StringConcat` 仅影响 `+` 运算符在“至少一侧为字符串”时的行为。

| 模式 | 行为 | 示例 |
|------|------|------|
| `PreferStringIfAnyString` | 只要任一操作数是字符串，就会把两边都转换为字符串（优先使用已注册的转换器）并进行拼接。 | `'1' + 2` → `"12"`，`ToDateTime('2024-01-01 00:00:00') + ' UTC'` → `"2024-01-01 00:00:00 UTC"` |
| `PreferNumericIfParsable` | 只要任一操作数是字符串，会先尝试将两边都解析为 `decimal`；如果两边都可解析则做数值相加，否则回退为字符串拼接。 | `'1' + '2'` → `3`，`'1' + 'b'` → `"1b"` |

### 相等比较模式

| 模式 | 行为 |
|------|------|
| `Strict` | 不进行类型转换；类型不匹配时抛出错误 |
| `NumberFriendly` | 涉及字符串时尝试数字比较 |
| `Permissive` | 不匹配时回退到字符串比较 |
| `MixedNumericOnly` | 仅对数字-字符串对进行数字转换 |

## 🔌 可扩展性

### 自定义函数

```csharp
public class MyFunction : IFunction
{
    public string Name => "MyFunc";
    
    public object? Invoke(object?[] args, InvocationContext ctx)
    {
        // 实现逻辑
        return args[0]?.ToString()?.ToUpperInvariant();
    }
}

// 通过 contributor 注册
public class MyContributor : IEngineContributor
{
    public void Configure(EngineServices services)
    {
        services.Functions.Register(new MyFunction());
    }
}

// 使用 contributor
var engine = factory.Create(contributors: new[] { new MyContributor() });
```

### 自定义类型转换器

```csharp
public class MyConverter : ITypeConverter
{
    public Type InputType => typeof(string);
    public Type OutputType => typeof(MyType);
    
    public bool TryConvert(object? value, out object? result)
    {
        // 转换逻辑
    }
}

// 在 contributor 中注册
services.Converters.Register(new MyConverter());
```

## 📊 执行结果

```csharp
var result = engine.Execute(script, inputs);

// 检查错误
if (result.HasError)
{
    Console.WriteLine($"错误: {result.ErrorMessage}");
    Console.WriteLine($"位置: 第 {result.ErrorLine} 行，第 {result.ErrorColumn} 列");
    Console.WriteLine($"代码: {result.ErrorSnippet}");
    Console.WriteLine($"错误码: {result.ErrorCode}");
}

// 访问赋值变量
foreach (var kvp in result.Assignments)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}

// 访问消息
foreach (var msg in result.Messages)
{
    Console.WriteLine($"[{msg.Level}] {msg.Text}");
}

// 执行时间
Console.WriteLine($"耗时: {result.Elapsed}");
```

## 🧪 运行测试

```bash
cd EasyExpression
dotnet test
```

## 📁 项目结构

```
EasyExpression/
├── EasyExpression.Core/
│   └── Engine/
│       ├── Ast/              # 抽象语法树节点
│       ├── Conversion/       # 类型转换器
│       ├── Functions/        # 内置和自定义函数
│       │   └── BuiltIns/     # 字符串、数学、日期时间函数
│       ├── Parsing/          # 词法分析器和解析器
│       ├── Runtime/          # 执行上下文和结果
│       ├── ExpressionEngine.cs
│       ├── ExpressionEngineFactory.cs
│       └── ExpressionEngineOptions.cs
└── EasyExpression.Core.Tests/    # 单元测试
```

## 📄 许可证

本项目基于 MIT 许可证开源 - 详见 [LICENSE](LICENSE) 文件。

## 🤝 贡献

欢迎贡献代码！请随时提交 Pull Request。
