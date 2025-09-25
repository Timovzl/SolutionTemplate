using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Validation;

/// <summary>
/// Similar to an <see cref="ArgumentNullException"/>, except in the form of a <see cref="ValidationException"/>.
/// </summary>
[Serializable]
public class NullValidationException : ValidationException
{
	public string ParameterName { get; }

	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="parameterName">The name of the missing parameter or item, for use in the resulting message.</param>
	public NullValidationException(string errorCode, string parameterName,
		[CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		: this(HttpStatusCode.BadRequest, errorCode, parameterName, message: CreateErrorMessage(parameterName: parameterName, callerFilePath: callerFilePath, callerMemberName: callerMemberName))
	{
	}

	/// <param name="statusCode">A rough categorization of the issue, expressed as an <see cref="HttpStatusCode"/> for its ubiquity.</param>
	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="parameterName">The name of the missing parameter or item, for use in the resulting message.</param>
	public NullValidationException(HttpStatusCode statusCode, string errorCode, string parameterName,
		[CallerFilePath] string? callerFilePath = null, [CallerMemberName] string? callerMemberName = null)
		: this(statusCode, errorCode, parameterName, message: CreateErrorMessage(parameterName: parameterName, callerFilePath: callerFilePath, callerMemberName: callerMemberName))
	{
	}

	/// <param name="statusCode">A rough categorization of the issue, expressed as an <see cref="HttpStatusCode"/> for its ubiquity.</param>
	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="parameterName">The name of the missing parameter or item, for use in the resulting message.</param>
	/// <param name="message">A human-readable message to help solve the issue.</param>
	public NullValidationException(HttpStatusCode statusCode, string errorCode, string parameterName, string message)
		: base(statusCode, errorCode: errorCode, message: message)
	{
		this.ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
	}

	private static string CreateErrorMessage(string? parameterName, string? callerFilePath, string? callerMemberName)
	{
		if (callerMemberName == ".ctor" && callerFilePath?.EndsWith(".cs") == true)
			parameterName = $"{Path.GetFileNameWithoutExtension(callerFilePath)} {parameterName}";

		return $"The following required data was missing: {parameterName}.";
	}

	#region Serialization

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	protected NullValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		this.ParameterName = info.GetString("ParameterName") ?? throw new IOException("Failed to deserialize: ParameterName is missing.");
	}

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);

		info.AddValue("ParameterName", this.ParameterName);
	}

	#endregion
}
