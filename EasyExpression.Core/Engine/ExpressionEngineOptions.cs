using System;

namespace EasyExpression.Core.Engine
{
	/// <summary>
	/// 全局配置项（仅包含核心引擎相关，不依赖外部包）。
	/// </summary>
	public sealed class ExpressionEngineOptions
	{
		public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        
        /// <summary>
        /// 是否启用脚本注释支持。默认禁用以符合“语法不支持注释”的规范。
        /// 启用后 Lexer 将识别 // 和 /* */ 注释并忽略。
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
		/// null 字符串行为：当参与字符串运算时是否视为空串。按需求固定为 true。
		/// </summary>
		public bool TreatNullStringAsEmpty { get; set; } = true;

		/// <summary>
		/// null 数值行为：当目标类型为 decimal 且字段值为 null 时，是否按 0 处理。
		/// 默认 false，保持严格模式。
		/// </summary>
		public bool TreatNullDecimalAsZero { get; set; } = false;

		/// <summary>
		/// null 布尔行为：当目标类型为 bool 且字段值为 null 时，是否按 false 处理。
		/// 默认 false，保持严格模式。
		/// </summary>
		public bool TreatNullBoolAsFalse { get; set; } = false;

		/// <summary>
		/// null 日期时间行为：当目标类型为 DateTime 且字段值为 null 时，若设置该值，则使用该默认值；
		/// 若为 null 则保持严格模式（抛出无法转换）。
		/// </summary>
		public DateTime? NullDateTimeDefault { get; set; } = null;

		/// <summary>
		/// now 字面量采用的时区：true=本地，false=UTC。
		/// </summary>
		public bool NowUseLocalTime { get; set; } = true;

		/// <summary>
		/// 是否对字段名启用严格校验（仅允许 A-Z, a-z, 0-9, 下划线与空格）。
		/// 若设置了 <see cref="FieldNameValidator"/>，则优先使用自定义校验器。
		/// 仅用于语法解析阶段；默认开启，提升一致性。
		/// </summary>
		public bool StrictFieldNameValidation { get; set; } = true;

		/// <summary>
		/// 自定义字段名校验器。返回 true 表示字段名合法。
		/// 若不为 null，将覆盖 <see cref="StrictFieldNameValidation"/> 的默认行为。
		/// </summary>
		public Func<string, bool>? FieldNameValidator { get; set; } = null;

		/// <summary>
		/// 正则匹配的超时时间（毫秒）。小于等于 0 表示不设超时（Infinite）。
		/// </summary>
		public int RegexTimeoutMilliseconds { get; set; } = 0;

		/// <summary>
		/// 等号比较的类型宽松策略。
		/// </summary>
		public EqualityCoercionMode EqualityCoercion { get; set; } = EqualityCoercionMode.Strict;

		/// <summary>
		/// 加号在存在字符串参与时的行为策略。
		/// </summary>
		public StringConcatMode StringConcat { get; set; } = StringConcatMode.PreferStringIfAnyString;

		/// <summary>
		/// 是否启用编译缓存以提高重复脚本的执行性能。
		/// 启用后，相同的脚本字符串将被缓存编译结果，避免重复解析。
		/// 默认启用。
		/// </summary>
		public bool EnableCompilationCache { get; set; } = true;
	}

	public enum EqualityCoercionMode
	{
		// Strict：严格模式；当类型不匹配时不进行宽松转换。
		// - 字符串参与：直接按字符串比较（使用 Options.StringComparison），不尝试数值/时间/布尔转换。
		// - 同类型（数字/布尔/DateTime）：按各自原生类型比较；
		// - 其他类型不匹配：抛出类型不匹配错误。
		Strict,

		// NumberFriendly：数字友好模式；当任一侧为字符串时，优先尝试两侧都转为 decimal 比较。
		// - 若双方均能解析为数字，按数值相等比较；否则退回字符串比较（Options.StringComparison）。
		// - 非字符串场景与严格模式一致；其他明显不匹配类型通常会报错。
		NumberFriendly,

		// Permissive：宽松模式；在 NumberFriendly 的基础上，更多场景退回字符串比较而不报错。
		// - 任一侧为字符串：先尝试双侧数值化；失败则按字符串比较。
		// - 其他类型不匹配：不抛错，直接将两侧 ToString() 后按 Options.StringComparison 比较。
		Permissive,
		// MixedNumericOnly: 当且仅当一侧为数字类型且另一侧为字符串时，尝试将字符串转换为数字后进行数值比较；
		// 双方均为字符串时始终进行字符串比较；其他类型不匹配场景退回字符串比较而不抛错。
		MixedNumericOnly
	}

	public enum StringConcatMode
	{
		PreferStringIfAnyString,
		PreferNumericIfParsable
	}
}


