using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;

/// <summary>
/// <para>
/// Converts between types <typeparamref name="TModel"/> and <typeparamref name="TProvider"/>, where <typeparamref name="TModel"/> is a type containing a single (non-public) instance field of type <typeparamref name="TProvider"/>.
/// </para>
/// <para>
/// This type primarily supports wrapper value objects.
/// </para>
/// <para>
/// The advantage of this type over <see cref="CastingConverter{TModel, TProvider}"/> is that this avoids constructors.
/// For example, if a domain rule changes but existing data is not changed, the constructor would refuse to load existing data from the database, which is undesirable.
/// </para>
/// </summary>
internal sealed class WrapperConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>
{
	private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject))!;

	public WrapperConverter()
		: base(CreateConversionToProviderExpression(), CreateConversionToModelExpression())
	{
	}

	private static FieldInfo GetExpectedField()
	{
		var field = typeof(TModel).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).SingleOrDefault(field => field.FieldType == typeof(TProvider)) ??
			throw new ArgumentException($"Type {typeof(TModel).Name} cannot be used for wrapping conversions, because it does not have exactly 1 (non-public) instance field of type {typeof(TProvider).Name}.");
		return field;
	}

	private static Expression<Func<TModel, TProvider>> CreateConversionToProviderExpression()
	{
		var field = GetExpectedField();

		var param = Expression.Parameter(typeof(TModel), "instance");
		var fieldAccess = Expression.Field(param, field);
		var conversion = Expression.Lambda<Func<TModel, TProvider>>(fieldAccess, param);

		return conversion;
	}

	private static Expression<Func<TProvider, TModel>> CreateConversionToModelExpression()
	{
		var field = GetExpectedField();
		var fieldSetter = FieldSetterCache.GetFieldSetter(field);

		var param = Expression.Parameter(typeof(TProvider), "value");
		var instance = Expression.Call(GetUninitializedObjectMethod, arguments: Expression.Constant(typeof(TModel)));
		var assignmentAndInstanceResult = Expression.Invoke(Expression.Constant(fieldSetter), instance, param);
		var typedInstanceResult = Expression.Convert(assignmentAndInstanceResult, typeof(TModel));
		var conversion = Expression.Lambda<Func<TProvider, TModel>>(typedInstanceResult, param);

		return conversion;
	}

	/// <summary>
	/// Compiles and caches field setter delegates for fields for the current combination of <typeparamref name="TModel"/> and <typeparamref name="TProvider"/>.
	/// </summary>
	private static class FieldSetterCache
	{
		private static readonly ConcurrentDictionary<FieldInfo, Func<object, TProvider, object>> SettersByField = new ConcurrentDictionary<FieldInfo, Func<object, TProvider, object>>(concurrencyLevel: 1, capacity: 5);

		public static Func<object, TProvider, object> GetFieldSetter(FieldInfo field)
		{
			var result = SettersByField.GetOrAdd(field, CreateFieldSetter);
			return result;
		}

		/// <summary>
		/// Creates a new function that can write to a field.
		/// The result works as expected, even for readonly fields and structs.
		/// </summary>
		private static Func<object, TProvider, object> CreateFieldSetter(FieldInfo field)
		{
			// We must write IL to achieve the following:
			// - Write to readonly fields
			// - Mutate structs in-place (instead of the normal semantics of mutating a copy, which would not help us)

			var setter = new DynamicMethod(
				name: $"AssignFieldValue_{field.Name}",
				returnType: typeof(object),
				parameterTypes: new[] { typeof(object), typeof(TProvider), },
				m: typeof(FieldSetterCache).Module,
				skipVisibility: true);

			var ilGenerator = setter.GetILGenerator();

			ilGenerator.Emit(OpCodes.Ldarg_0); // Load the instance to write to, which will be our return value
			ilGenerator.Emit(OpCodes.Ldarg_0); // Load the instance to write to

			// For value-type instances
			if (field.DeclaringType?.IsValueType == true)
			{
				ilGenerator.DeclareLocal(field.DeclaringType.MakeByRefType()); // Create a local by-ref var
				ilGenerator.Emit(OpCodes.Unbox, field.DeclaringType); // Unbox the object
				ilGenerator.Emit(OpCodes.Stloc_0); // Assign the result of the unboxing to the by-ref var
				ilGenerator.Emit(OpCodes.Ldloc_0); // Load the by-ref var for us to write to
			}

			ilGenerator.Emit(OpCodes.Ldarg_1); // Load the value to be assigned
			ilGenerator.Emit(OpCodes.Stfld, field); // Assign the new value to the instance's field
			ilGenerator.Emit(OpCodes.Ret); // Return

			return setter.CreateDelegate<Func<object, TProvider, object>>();
		}
	}
}
