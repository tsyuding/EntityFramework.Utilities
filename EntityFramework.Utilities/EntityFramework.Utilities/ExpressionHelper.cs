﻿using System;
using System.Linq.Expressions;

namespace EntityFramework.Utilities
{
	internal class ReplaceVisitor : ExpressionVisitor
	{
		private readonly Expression _from, _to;
		public ReplaceVisitor(Expression from, Expression to)
		{
			_from = from;
			_to = to;
		}

		public override Expression Visit(Expression node)
		{
			return node == _from ? _to : base.Visit(node);
		}
	}

	internal static class ExpressionHelper
	{
		internal static Expression<Func<T, bool>> CombineExpressions<T, TP>(Expression<Func<T, TP>> prop, Expression<Func<T, TP>> modifier) where T : class
		{
			var propRewritten = new ReplaceVisitor(prop.Parameters[0], modifier.Parameters[0]).Visit(prop.Body);
			var expr = Expression.Equal(propRewritten, modifier.Body);
			var final = Expression.Lambda<Func<T, bool>>(expr, modifier.Parameters[0]);
			return final;
		}

		public static string GetPropertyName<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
		{
			var temp = propertyLambda.Body;

			while (temp is UnaryExpression)
			{
				temp = (temp as UnaryExpression).Operand;
			}

			// Use ToString to deal with nested property names, remove prefix using Substring
			var name = ((MemberExpression)temp).ToString();
			var index = name.IndexOf('.');
			return name.Substring(index + 1);
		}

		//http://stackoverflow.com/a/2824409/507279
		internal static Action<T, TP> PropertyExpressionToSetter<T, TP>(Expression<Func<T, TP>> prop)
		{
			// re-write in .NET 4.0 as a "set"
			var member = (MemberExpression)prop.Body;
			var param = Expression.Parameter(typeof(TP), "value");
			var set = Expression.Lambda<Action<T, TP>>(
				Expression.Assign(member, param), prop.Parameters[0], param);

			// compile it
			return set.Compile();
		}
	}
}
