using System;
using System.Linq.Expressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NipSharp.Exceptions;

namespace NipSharp
{
    public class ExpressionBuilder : NipBaseVisitor<Expression>
    {
        private static Expression IdentifiedFlag = Expression.Constant(NipAliases.Flag["identified"]);
        private static Expression Sell = Expression.Constant(Outcome.Sell);
        private static Expression Keep = Expression.Constant(Outcome.Keep);
        private static Expression Identify = Expression.Constant(Outcome.Identify);

        private readonly ParameterExpression _valueBag;

        public ExpressionBuilder(ParameterExpression valueBag)
        {
            // This is a bit of a witch-craft. We create a variable which will store the <string, float> values for each of the properties/item stats.
            // This will then be used when evaluating the expression tree, but will be passed down as part of the lambda block. 
            _valueBag = valueBag;
        }

        public override Expression VisitNumber(NipParser.NumberContext context)
        {
            return Expression.Constant(float.Parse(context.GetText()));
        }

        public override Expression VisitNumberOrAlias(NipParser.NumberOrAliasContext context)
        {
            // This is probably a bit grim, as we are reaching out to the other side of the expression to get the 
            // type of property it is, so we'd know which alias table to look at.
            // This could be avoided with more grammar (and more code), by having separate rules for each property type.
            var maybePropNameNode = context.Parent.GetChild(1).GetChild(0);
            if (maybePropNameNode is not ITerminalNode propNameNode)
            {
                // Bad grammar somehow.
                throw new ApplicationException($"Expected a terminal node at {maybePropNameNode.GetText()}");
            }

            var propName = propNameNode.GetText();

            var aliases = propName switch
            {
                "type" => NipAliases.Type,
                "name" => NipAliases.ClassId,
                "class" => NipAliases.Class,
                "color" => NipAliases.Color,
                "quality" => NipAliases.Quality,
                "flag" => NipAliases.Flag,
                // Makes no sense.
                "level" => new(),
                // Needs access to data tables to do it properly.
                "prefix" => new(),
                "suffix" => new(),
                _ => throw new ArgumentOutOfRangeException(nameof(propName), propName),
            };

            var value = context.GetText();
            if (float.TryParse(value, out float numericValue))
            {
                return Expression.Constant(numericValue);
            }

            if (aliases.TryGetValue(value, out var aliasNumericValue))
            {
                return Expression.Constant((float)aliasNumericValue);
            }

            throw new InvalidAliasException($"Invalid alias {value} for property [{propName}]");
        }

        // Use -1 here, as the grammar does not allow negative numbers, so it will always evaluate to false.
        private Expression GetValue(string variable, float defaultValue = float.NaN)
        {
            // This is grim, as C# does not have GetOrDefault (only as extension which are not supported in lambda),
            // and TryGetValue is cancer.
            // _valueBag.ContainsKey(variable)
            var checkIfExists = Expression.Call(_valueBag, "ContainsKey", null, Expression.Constant(variable));
            // _valueBag[variable]
            var getValue = Expression.Property(_valueBag, "Item", Expression.Constant(variable));
            // checkIfExists ? getValue : defaultValue;
            return Expression.Condition(checkIfExists, getValue, Expression.Constant(defaultValue));
        }

        public override Expression VisitStat(NipParser.StatContext context)
        {
            var name = context.GetText();
            if (!NipAliases.Stat.ContainsKey(name))
            {
                throw new InvalidStatException($"Unknown stat: {name}");
            }

            return GetValue(name);
        }

        public override Expression VisitProperty(NipParser.PropertyContext context)
        {
            var name = context.GetText();
            if (!NipAliases.KnownProperties.Contains(name))
            {
                throw new UnknownPropertyNameException($"Unknown property name: {name}");
            }

            return GetValue(name);
        }

        public override Expression VisitStatNameRule(NipParser.StatNameRuleContext context)
        {
            var exp = Visit(context.stat());
            if (context.op?.Type == NipParser.SUB)
            {
                return Expression.Negate(exp);
            }

            return exp;
        }

        public override Expression VisitStatNumberRule(NipParser.StatNumberRuleContext context)
        {
            var exp = Visit(context.number());
            if (context.op?.Type == NipParser.SUB)
            {
                return Expression.Negate(exp);
            }

            return exp;
        }

        public override Expression VisitStatAddSubRule(NipParser.StatAddSubRuleContext context)
        {
            return Op(context.op, Visit(context.statExpr(0)), Visit(context.statExpr(1)));
        }

        public override Expression VisitStatMulDivRule(NipParser.StatMulDivRuleContext context)
        {
            return Op(context.op, Visit(context.statExpr(0)), Visit(context.statExpr(1)));
        }

        public override Expression VisitStatLogicalRule(NipParser.StatLogicalRuleContext context)
        {
            return Op(context.op, Visit(context.statRule(0)), Visit(context.statRule(1)));
        }

        public override Expression VisitPropLogicalRule(NipParser.PropLogicalRuleContext context)
        {
            return Op(context.op, Visit(context.propertyRule(0)), Visit(context.propertyRule(1)));
        }

        public override Expression VisitPropRelationalRule(NipParser.PropRelationalRuleContext context)
        {
            return Op(context.op, Visit(context.property()), Visit(context.numberOrAlias()));
        }

        public override Expression VisitPropFlagRule(NipParser.PropFlagRuleContext context)
        {
            // Values are floats, so convert them to ints as we need to mask the flag.
            var expectedFlag = Expression.Convert(Visit(context.numberOrAlias()), typeof(int));
            var actualFlags = Expression.Convert(GetValue("flag", 0), typeof(int));
            // [flag] == identified
            // is actually:
            // [flag]&identified == identified
            return Op(context.op, Expression.And(actualFlags, expectedFlag), expectedFlag);
        }

        public override Expression VisitPropAffixRule(NipParser.PropAffixRuleContext context)
        {
            // [prefix] == 123
            // actually needs to check each of the available prefixes, so we need to construct an "or" with a different
            // bunch of keys.
            // The assumption is that the value bag will have prefix0 prefix1 prefix2 etc for each available prefix.
            var name = context.GetChild(1).GetChild(0).GetText();
            var value = Visit(context.numberOrAlias());
            Expression exp = Expression.Constant(false);
            for (int i = 0; i < 3; i++)
            {
                var check = Op(context.op, GetValue($"{name}{i}", -1f), value);
                exp = Expression.Or(exp, check);
            }

            return exp;
        }

        public override Expression VisitStatRelationalRule(NipParser.StatRelationalRuleContext context)
        {
            return Op(context.op, Visit(context.statExpr(0)), Visit(context.statExpr(1)));
        }

        public override Expression VisitPropParenRule(NipParser.PropParenRuleContext context)
        {
            return Visit(context.propertyRule());
        }

        public override Expression VisitStatParenRule(NipParser.StatParenRuleContext context)
        {
            return Visit(context.statRule());
        }

        public override Expression VisitAdditionalParenRule(NipParser.AdditionalParenRuleContext context)
        {
            return Visit(context.additionalRule());
        }

        public override Expression VisitStatExprParenRule(NipParser.StatExprParenRuleContext context)
        {
            var exp = Visit(context.statExpr());
            if (context.op?.Type == NipParser.SUB)
            {
                return Expression.Negate(exp);
            }

            return exp;
        }

        public override Expression VisitNipRule(NipParser.NipRuleContext context)
        {
            var propertyMatch = context.propertyRule() == null
                ? Expression.Constant(true)
                : Visit(context.propertyRule());

            var statMatch = context.statRule() == null ? Expression.Constant(true) : Visit(context.statRule());

            var isIdentified = Expression.Equal(
                Expression.And(Expression.Convert(GetValue("flag", 0), typeof(int)), IdentifiedFlag), IdentifiedFlag
            );

            var additionalMatch = context.additionalRule() == null
                ? Expression.Constant(true)
                : Visit(context.additionalRule());

            /*
            if (propertyMatch)
            {
                if (statsMatch)
                {
                    if (additionalMatch)
                    {
                        return "keep"
                    }
                    return "sell"
                }
                if (isIdentified) {
                    return sell;   
                }   
                return identify             
            }
            */

            return Expression.Condition(
                propertyMatch,
                Expression.Condition(
                    statMatch,
                    Expression.Condition(
                        additionalMatch,
                        Keep,
                        Sell
                    ),
                    Expression.Condition(
                        isIdentified,
                        Sell,
                        Identify
                    )
                ),
                Sell
            );
        }

        public override Expression VisitAdditionalMaxQuantityRule(NipParser.AdditionalMaxQuantityRuleContext context)
        {
            return Expression.LessThan(GetValue("currentquantity", 0), Visit(context.statExpr()));
        }

        public override Expression VisitAdditionalMercTierRule(NipParser.AdditionalMercTierRuleContext context)
        {
            // TODO: Not sure what to do with this
            return Expression.Constant(true);
        }

        public override Expression VisitAdditionalTierRule(NipParser.AdditionalTierRuleContext context)
        {
            // TODO: Not sure what to do with this
            return Expression.Constant(true);
        }

        public override Expression VisitAdditionalLogicalRule(NipParser.AdditionalLogicalRuleContext context)
        {
            return Op(context.op, Visit(context.additionalRule(0)), Visit(context.additionalRule(1)));
        }

        public override Expression VisitLine(NipParser.LineContext context)
        {
            var child = context.GetChild(0);
            return child.ChildCount == 0 ? Sell : Visit(context.nipRule());
        }

        private Expression Op(IToken op, Expression left, Expression right)
        {
            return op.Type switch
            {
                NipParser.EQ => Expression.Equal(left, right),
                NipParser.NEQ => Expression.NotEqual(left, right),
                NipParser.GT => Expression.GreaterThan(left, right),
                NipParser.GTE => Expression.GreaterThanOrEqual(left, right),
                NipParser.LT => Expression.LessThan(left, right),
                NipParser.LTE => Expression.LessThanOrEqual(left, right),
                NipParser.AND => Expression.And(left, right),
                NipParser.OR => Expression.Or(left, right),
                NipParser.ADD => Expression.AddChecked(left, right),
                NipParser.SUB => Expression.SubtractChecked(left, right),
                NipParser.MUL => Expression.MultiplyChecked(left, right),
                NipParser.DIV => Expression.Divide(left, right),
                _ => throw new ArgumentException("operator type", op.Text)
            };
        }
    }
}
