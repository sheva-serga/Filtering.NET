using System.Linq.Expressions;
using System.Reflection;

namespace Filtering.Net;

// A member access on a constant holder is the shape a C# closure produces, which query providers
// turn into a SQL parameter. A bare ConstantExpression would be inlined into the SQL text instead.
internal sealed class FilterValueHolder<TValue>(TValue value)
{
    private static readonly MemberInfo ValueMember =
        ((MemberExpression)((Expression<Func<FilterValueHolder<TValue>, TValue>>)(holder => holder.Value)).Body).Member;

    public TValue Value { get; } = value;

    public static Expression AsExpression(TValue value) =>
        Expression.MakeMemberAccess(Expression.Constant(new FilterValueHolder<TValue>(value)), ValueMember);
}
