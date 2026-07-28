namespace LibApp.Application.Helpers;

public static class ExpressionHelper
{
    public static string GetPropertyPath<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        var member = expression.Body as MemberExpression;
        if (member == null)
            throw new ArgumentException("Expression must be a member expression", nameof(expression));

        var path = new List<string>();
        var current = member;
        while (current != null)
        {
            path.Insert(0, current.Member.Name);
            current = current.Expression as MemberExpression;
        }
        return string.Join(".", path);
    }
    public static Expression<Func<T, bool>> CombineAnd<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var leftVisitor = new ReplaceExpressionVisitor(left.Parameters[0], param);
        var rightVisitor = new ReplaceExpressionVisitor(right.Parameters[0], param);
        var body = Expression.AndAlso(
            leftVisitor.Visit(left.Body),
            rightVisitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
    private class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression Visit(Expression node)
        {
            return node == _oldValue ? _newValue : base.Visit(node);
        }
    }

}