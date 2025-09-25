using System.Net;
using System.Runtime.Serialization;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Validation;

/// <summary>
/// <para>
/// An exception that indicates client error.
/// Contains a stable error code as well as a user-friendly message.
/// </para>
/// <para>
/// Outer layers or systems can rely on the error code.
/// For example, a presentation layer could provide message translations, or an external system could respond in a preprogrammed way to predefined errors.
/// </para>
/// </summary>
[Serializable]
public class ValidationException : Exception
{
	// Message is inherited

	/// <summary>
	/// A rough categorization of the issue, expressed as an <see cref="HttpStatusCode"/> for its ubiquity.
	/// </summary>
	public HttpStatusCode StatusCode { get; }

	/// <summary>
	/// The string representation of the error code.
	/// </summary>
	public string ErrorCode { get; }

	/// <summary>
	/// The body of the message, <em>not</em> prefixed by the error code.
	/// </summary>
	public string MessageBody { get; }

	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="message">A human-readable message to help solve the issue.</param>
	public ValidationException(string errorCode, string message, Exception? innerException = null)
		: this(HttpStatusCode.BadRequest, errorCode, message, innerException)
	{
	}

	/// <param name="statusCode">A rough categorization of the issue, expressed as an <see cref="HttpStatusCode"/> for its ubiquity.</param>
	/// <param name="errorCode">A stable error code for use throughout outer layers and/or systems.</param>
	/// <param name="message">A human-readable message to help solve the issue.</param>
	public ValidationException(HttpStatusCode statusCode, string errorCode, string message, Exception? innerException = null)
		: base(GetMessage(errorCode ?? throw new ArgumentNullException(nameof(errorCode)), message), innerException)
	{
		this.StatusCode = statusCode;
		this.ErrorCode = errorCode;
		this.MessageBody = message ?? errorCode;
	}

	private static string GetMessage(string errorCode, string? messageBody = null)
	{
		return messageBody is null
			? $"{errorCode}: {errorCode}."
			: $"{errorCode}: {messageBody}"; // The message body generally consists of one or more full sentences, i.e. with their own trailing dot
	}

	#region Serialization

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	protected ValidationException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		this.StatusCode = (HttpStatusCode)info.GetInt32("StatusCode");
		this.ErrorCode = info.GetString("ErrorCode") ?? throw new IOException("Failed to deserialize: ErrorCode is missing.");
		this.MessageBody = info.GetString("MessageBody") ?? this.ErrorCode;
	}

	[Obsolete("Exists because base class has serializable attribute, which strict tools then expect on subclasses as well", DiagnosticId = "SYSLIB0051")]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);

		info.AddValue("StatusCode", (int)this.StatusCode);
		info.AddValue("ErrorCode", this.ErrorCode);
		info.AddValue("MessageBody", this.MessageBody);
	}

	#endregion
}
