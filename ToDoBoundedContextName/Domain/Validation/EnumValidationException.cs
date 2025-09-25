using System.Net;
using System.Runtime.Serialization;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Validation;

/// <summary>
/// A specific <see cref="ValidationException"/> that indicates client error caused by an undefined enum value.
/// </summary>
[Serializable]
public class EnumValidationException : ValidationException
{
	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	public EnumValidationException(string errorCode)
		: this(HttpStatusCode.BadRequest, errorCode, message: null!)
	{
	}

	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="message">A human-readable message to help solve the issue.</param>
	public EnumValidationException(string errorCode, string message)
		: this(HttpStatusCode.BadRequest, errorCode, message)
	{
	}

	/// <param name="statusCode">A rough categorization of the issue, expressed as an <see cref="HttpStatusCode"/> for its ubiquity.</param>
	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="message">A human-readable message to help solve the issue.</param>
	public EnumValidationException(HttpStatusCode statusCode, string errorCode, string message)
		: base(statusCode, errorCode: errorCode ?? "OptionInvalid", message: message ?? "Only recognized options are permitted.")
	{
	}

	#region Serialization

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	protected EnumValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
	}

	#endregion
}
