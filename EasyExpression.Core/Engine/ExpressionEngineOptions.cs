using System;

namespace EasyExpression.Core.Engine
{
	/// <summary>
	/// Global options (core engine only; no external package dependencies).
	/// </summary>
	public sealed class ExpressionEngineOptions
	{
		public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        
        /// <summary>
        /// Whether to enable script comment support. Disabled by default to comply with the
        /// "syntax does not support comments" spec.
        /// When enabled, the Lexer recognizes // and /* */ comments and ignores them.
        /// </summary>
        public bool EnableComments { get; set; } = true;

		public int MaxDepth { get; set; } = 64;

		public int MaxNodes { get; set; } = 2000;

		public int MaxNodeVisits { get; set; } = 10000;

		public int TimeoutMilliseconds { get; set; } = 2000;

		public bool CaseInsensitiveFieldNames { get; set; } = true;

		public StringComparison StringComparison { get; set; } = StringComparison.OrdinalIgnoreCase;

		public int RoundingDigits { get; set; } = 2;

		public MidpointRounding MidpointRounding { get; set; } = MidpointRounding.AwayFromZero;

		/// <summary>
		/// Null-string behavior: when participating in string operations, whether to treat null as empty.
		/// Fixed to true by requirement.
		/// </summary>
		public bool TreatNullStringAsEmpty { get; set; } = true;

		/// <summary>
		/// Null-number behavior: when the target type is decimal and the field value is null, whether to treat it as 0.
		/// Default is false to keep strict behavior.
		/// </summary>
		public bool TreatNullDecimalAsZero { get; set; } = false;

		/// <summary>
		/// Null-boolean behavior: when the target type is bool and the field value is null, whether to treat it as false.
		/// Default is false to keep strict behavior.
		/// </summary>
		public bool TreatNullBoolAsFalse { get; set; } = false;

		/// <summary>
		/// Null DateTime behavior: when the target type is DateTime and the field value is null, if this value is set,
		/// use it as the default; if null, keep strict behavior (throw on conversion failure).
		/// </summary>
		public DateTime? NullDateTimeDefault { get; set; } = null;

		/// <summary>
		/// Time zone for the now literal: true = local time, false = UTC.
		/// </summary>
		public bool NowUseLocalTime { get; set; } = true;

		/// <summary>
		/// Whether to enable strict field-name validation (only allows A-Z, a-z, 0-9, underscore, and space).
		/// If <see cref="FieldNameValidator"/> is set, the custom validator takes precedence.
		/// Used only during parsing; enabled by default to improve consistency.
		/// </summary>
		public bool StrictFieldNameValidation { get; set; } = true;

		/// <summary>
		/// Custom field-name validator. Return true if the field name is valid.
		/// When non-null, it overrides the default behavior of <see cref="StrictFieldNameValidation"/>.
		/// </summary>
		public Func<string, bool>? FieldNameValidator { get; set; } = null;

		/// <summary>
		/// Regex matching timeout (milliseconds). Less than or equal to 0 means no timeout (Infinite).
		/// </summary>
		public int RegexTimeoutMilliseconds { get; set; } = 0;

		/// <summary>
		/// Type-coercion strategy for equality comparisons.
		/// </summary>
		public EqualityCoercionMode EqualityCoercion { get; set; } = EqualityCoercionMode.Strict;

		/// <summary>
		/// Behavior strategy for plus operator when strings are involved.
		/// </summary>
		public StringConcatMode StringConcat { get; set; } = StringConcatMode.PreferStringIfAnyString;

		/// <summary>
		/// Whether to enable compilation cache to improve repeated-script execution performance.
		/// When enabled, identical script strings cache the compiled result to avoid repeated parsing.
		/// Enabled by default.
		/// </summary>
		public bool EnableCompilationCache { get; set; } = true;
	}

	public enum EqualityCoercionMode
	{
		// Strict: strict mode; no permissive conversion when types mismatch.
		// - If either side is a string: compare as strings (using Options.StringComparison); do not try numeric/datetime/bool conversion.
		// - Same type (number/bool/DateTime): compare using the native type.
		// - Other mismatches: throw a type-mismatch error.
		Strict,

		// NumberFriendly: number-friendly mode; when either side is a string, first try converting both sides to decimal for comparison.
		// - If both can be parsed as numbers, compare numerically; otherwise fall back to string comparison (Options.StringComparison).
		// - Non-string scenarios behave the same as strict mode; other obvious mismatches typically error.
		NumberFriendly,

		// Permissive: permissive mode; based on NumberFriendly, more cases fall back to string comparison instead of erroring.
		// - If either side is a string: try numeric conversion on both sides first; on failure compare as strings.
		// - Other mismatches: do not throw; compare both sides via ToString() using Options.StringComparison.
		Permissive,
		// MixedNumericOnly: only when one side is a numeric type and the other side is a string, try converting the string to a number
		// and compare numerically; if both sides are strings, always compare as strings; for other mismatches, fall back to string
		// comparison without throwing.
		MixedNumericOnly
	}

	public enum StringConcatMode
	{
		PreferStringIfAnyString,
		PreferNumericIfParsable
	}
}


