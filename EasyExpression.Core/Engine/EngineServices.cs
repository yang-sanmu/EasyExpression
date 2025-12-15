using System;
using EasyExpression.Core.Engine.Conversion;
using EasyExpression.Core.Engine.Functions;
using EasyExpression.Core.Engine.Functions.BuiltIns;

namespace EasyExpression.Core.Engine
{
    /// <summary>
    /// Engine services aggregation: includes options, function registry, and converter registry.
    /// </summary>
    public sealed class EngineServices
    {
        public ExpressionEngineOptions Options { get; }
        public FunctionRegistry Functions { get; }
        public TypeConversionRegistry Converters { get; }
        private readonly System.Collections.Generic.List<Functions.IEngineContributor> _contributors = new System.Collections.Generic.List<Functions.IEngineContributor>();

        public EngineServices(ExpressionEngineOptions? options = null)
        {
            Options = options ?? new ExpressionEngineOptions();
            Functions = new FunctionRegistry(caseInsensitive: true);
            Converters = new TypeConversionRegistry();

            RegisterBuiltInFunctions();
            RegisterBuiltInConverters();

            // Allow external contributors to extend (explicitly add via UseContributor)
        }

        public EngineServices UseContributor(Functions.IEngineContributor contributor)
        {
            if (contributor == null) throw new ArgumentNullException(nameof(contributor));
            _contributors.Add(contributor);
            contributor.Configure(this);
            return this;
        }

        private void RegisterBuiltInFunctions()
        {
            Functions.Register(new ToStringFunction());
            Functions.Register(new StartsWithFunction());
            Functions.Register(new EndsWithFunction());
            Functions.Register(new ContainsFunction());
            Functions.Register(new ToUpperFunction());
            Functions.Register(new ToLowerFunction());
            Functions.Register(new RegexMatchFunction());
            Functions.Register(new SubstringFunction());
            Functions.Register(new TrimFunction());
            Functions.Register(new LenFunction());
            Functions.Register(new ReplaceFunction());
            Functions.Register(new CoalesceFunction());
            Functions.Register(new IifFunction());
            Functions.Register(new ToDecimalFunction());
            Functions.Register(new MaxFunction());
            Functions.Register(new MinFunction());
            Functions.Register(new AverageFunction());
            Functions.Register(new SumFunction());
            Functions.Register(new RoundFunction());
            Functions.Register(new AbsFunction());
            Functions.Register(new ToDateTimeFunction());
            Functions.Register(new FormatDateTimeFunction());
            Functions.Register(new AddDayFunction());
            Functions.Register(new AddDaysFunction());
            Functions.Register(new AddHoursFunction());
            Functions.Register(new AddMinutesFunction());
            Functions.Register(new AddSecondsFunction());
            Functions.Register(new TimeSpanFunction());
            Functions.Register(new FieldExistsFunction());
        }

        private void RegisterBuiltInConverters()
        {
            // Common to-string conversions
            Converters.Register(new SimpleConverter<object, string>(v => v?.ToString() ?? string.Empty));
            Converters.Register(new SimpleConverter<decimal, string>(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Converters.Register(new SimpleConverter<bool, string>(v => v ? "true" : "false"));
            Converters.Register(new SimpleConverter<DateTime, string>(v => v.ToString(Options.DateTimeFormat)));

            // to decimal
            // Note: string -> decimal uses TryParse semantics and does not throw.
            // On conversion failure it returns false; the upper layer throws ExpressionRuntimeException with position info.
            Converters.Register(new StringToDecimalConverter());
            Converters.Register(new SimpleConverter<int, decimal>(v => v));
            Converters.Register(new SimpleConverter<long, decimal>(v => v));
            Converters.Register(new SimpleConverter<double, decimal>(v => (decimal)v));
            Converters.Register(new SimpleConverter<float, decimal>(v => (decimal)v));
            Converters.Register(new SimpleConverter<byte, decimal>(v => v));
            Converters.Register(new SimpleConverter<sbyte, decimal>(v => v));
            Converters.Register(new SimpleConverter<short, decimal>(v => v));
            Converters.Register(new SimpleConverter<ushort, decimal>(v => v));
            Converters.Register(new SimpleConverter<uint, decimal>(v => v));
            Converters.Register(new SimpleConverter<ulong, decimal>(v => (decimal)v));

            // to bool (uses TryParse semantics; on failure returns false and does not throw)
            Converters.Register(new StringToBoolConverter());
            Converters.Register(new NullableAwareConverter<bool>(Options, (string s) =>
            {
                if (bool.TryParse(s, out var b)) return b;
                // Return value will not be used; the outer layer will catch and return false
                throw new FormatException("Invalid boolean");
            }));

            // to datetime (uses TryParseExact semantics; on failure returns false and does not throw)
            Converters.Register(new StringToDateTimeConverter(Options));
            Converters.Register(new NullableAwareConverter<DateTime>(Options, (string s) =>
            {
                if (DateTime.TryParseExact(s, Options.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                    return dt;
                throw new FormatException("Invalid datetime");
            }));
        }
    }

    internal sealed class StringToDecimalConverter : Conversion.ITypeConverter
    {
        public System.Type InputType => typeof(string);
        public System.Type OutputType => typeof(decimal);

        public bool TryConvert(object? value, out object? result)
        {
            if (value is string s)
            {
                if (decimal.TryParse(s, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    result = d;
                    return true;
                }
            }
            result = null;
            return false;
        }
    }

    internal sealed class NullableAwareConverter<TOut> : Conversion.ITypeConverter
    {
        public System.Type InputType => typeof(string);
        public System.Type OutputType => typeof(TOut);
        private readonly System.Func<string, TOut> _factory;
        private readonly ExpressionEngineOptions _options;

        public NullableAwareConverter(ExpressionEngineOptions options, System.Func<string, TOut> factory)
        {
            _options = options; _factory = factory;
        }

        public bool TryConvert(object? value, out object? result)
        {
            if (value == null)
            {
                if (typeof(TOut) == typeof(bool) && _options.TreatNullBoolAsFalse) { result = false; return true; }
                if (typeof(TOut) == typeof(DateTime) && _options.NullDateTimeDefault.HasValue) { result = _options.NullDateTimeDefault.Value; return true; }
                result = null; return false;
            }
            if (value is string s)
            {
                try
                {
                    result = _factory(s);
                    return true;
                }
                catch
                {
                    result = null; return false;
                }
            }
            result = null; return false;
        }
    }

    internal sealed class SimpleConverter<TIn, TOut> : Conversion.ITypeConverter
    {
        public System.Type InputType => typeof(TIn);
        public System.Type OutputType => typeof(TOut);
        private readonly System.Func<TIn, TOut> _func;

        public SimpleConverter(System.Func<TIn, TOut> func)
        { _func = func; }

        public bool TryConvert(object? value, out object? result)
        {
            try
            {
                if (value is TIn v)
                {
                    result = _func(v);
                    return true;
                }
            }
            catch
            {
                result = default; return false;
            }
            result = default; return false;
        }
    }

    internal sealed class StringToBoolConverter : Conversion.ITypeConverter
    {
        public System.Type InputType => typeof(string);
        public System.Type OutputType => typeof(bool);
        public bool TryConvert(object? value, out object? result)
        {
            if (value is string s && bool.TryParse(s, out var b))
            {
                result = b; return true;
            }
            result = null; return false;
        }
    }

    internal sealed class StringToDateTimeConverter : Conversion.ITypeConverter
    {
        private readonly ExpressionEngineOptions _options;
        public StringToDateTimeConverter(ExpressionEngineOptions options) { _options = options; }
        public System.Type InputType => typeof(string);
        public System.Type OutputType => typeof(DateTime);
        public bool TryConvert(object? value, out object? result)
        {
            if (value is string s && DateTime.TryParseExact(s, _options.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                result = dt; return true;
            }
            result = null; return false;
        }
    }
}