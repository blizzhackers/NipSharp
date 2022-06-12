using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using NipSharp.Exceptions;

namespace NipSharp
{
    public class Matcher
    {
        private static readonly Regex ReplaceGetStat = new(@"item.getStatEx\(([^)]+)\)");
        private readonly List<Rule> _rules = new();

        public Matcher()
        {
        }

        public Matcher(string path)
        {
            AddPath(path);
        }

        public Matcher(IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                AddRule(line);
            }
        }

        public void AddPath(string path)
        {
            foreach (string readLine in File.ReadLines(path))
            {
                AddRule(readLine);
            }
        }

        public void Clear()
        {
            _rules.Clear();
        }

        public void AddRule(string rule)
        {
            try
            {
                rule = ReplaceGetStat.Replace(rule, "[$1]");
                rule = rule.ToLower();

                var inputStream = new AntlrInputStream(rule);
                var lexer = new NipLexer(inputStream, TextWriter.Null, TextWriter.Null);
                var tokens = new CommonTokenStream(lexer);
                var parser = new NipParser(tokens, TextWriter.Null, TextWriter.Null);
                var lineExpression = parser.line();

                var result = Expression.Parameter(typeof(Result), "result");
                var valueBag = Expression.Parameter(
                    typeof(Dictionary<string, float>), "valueBag"
                );
                var meBag = Expression.Parameter(
                    typeof(Dictionary<string, float>), "meBag"
                );
                var matchExpression = new ExpressionBuilder(result, valueBag, meBag).Visit(lineExpression);

                BlockExpression block = Expression.Block(
                    new[] { result },
                    Expression.Assign(result, matchExpression),
                    result
                );
                var expression =
                    Expression.Lambda<Func<Dictionary<string, float>, Dictionary<string, float>, Result>>(
                        block, valueBag, meBag
                    );

                var ruleLambda = expression.Compile();
                _rules.Add(
                    new Rule
                    {
                        Line = rule,
                        Matcher = ruleLambda
                    }
                );
            }
            catch (NipException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new InvalidRuleException($"Invalid rule: {rule}", e);
            }
        }

        public IEnumerable<Result> IterateResults(IItem item, IMe me = null)
        {
            var valueBag = CreateValueBag(item);
            var meBag = CreateMeBag(me);

            foreach (Rule rule in _rules)
            {
                var matchResult = rule.Matcher.Invoke(valueBag, meBag);
                matchResult.Line = rule.Line;
                yield return matchResult;
            }
        }

        public Result Match(IItem item, IMe me = null)
        {
            Result? bestSoFar = null;
            foreach (var result in IterateResults(item, me))
            {
                switch (result.Outcome)
                {
                    case Outcome.Keep:
                        return result;
                    case Outcome.Identify:
                        if (bestSoFar?.Outcome == Outcome.Sell)
                        {
                            bestSoFar = result;
                        }

                        break;
                    case Outcome.Sell:
                        if (bestSoFar == null)
                        {
                            bestSoFar = result;
                        }

                        break;
                }
            }

            return bestSoFar ?? new Result
            {
                Outcome = Outcome.Sell
            };
        }

        private Dictionary<string, float> CreateMeBag(IMe me)
        {
            if (me == null)
            {
                return new()
                {
                    { "act", float.NaN },
                    { "charlvl", float.NaN },
                    { "diff", float.NaN },
                    { "gold", float.NaN },
                };
            }

            return new()
            {
                { "act", me.Act },
                { "charlvl", me.Level },
                { "diff", me.Difficulty },
                { "gold", me.Gold },
            };
        }

        public static Dictionary<string, float> CreateValueBag(IItem item)
        {
            Dictionary<string, float> valueBag = new()
            {
                { "type", item.Type },
                { "name", item.Name },
                { "class", item.Class },
                { "color", item.Color },
                { "quality", item.Quality },
                { "flag", item.Flags },
                { "level", item.Level },
            };

            for (var i = 0; i < item.Prefixes?.Count; i++)
            {
                valueBag[$"prefix{i}"] = item.Prefixes.ElementAt(i);
            }

            for (var i = 0; i < item.Suffixes?.Count; i++)
            {
                valueBag[$"suffix{i}"] = item.Suffixes.ElementAt(i);
            }

            foreach (IStat itemStat in item.Stats ?? Array.Empty<IStat>())
            {
                (int, int?)[] combinations =
                {
                    (itemStat.Id, null),
                    (itemStat.Id, itemStat.Layer),
                };

                // For each stat alias combination, put a value in the bag for that alias.
                foreach ((int, int?) key in combinations)
                {
                    if (!NipAliases.InverseStat.Contains(key)) continue;

                    foreach (string alias in NipAliases.InverseStat[key])
                    {
                        valueBag[alias] = itemStat.Value;
                    }
                }
            }

            return valueBag;
        }
    }
}
