using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace NipSharp
{
    public class Matcher
    {
        private static readonly Regex ReplaceGetStat = new Regex(@"item.getStatEx\(([^)]+)\)");
        private readonly List<Func<Dictionary<string, int>, Result>> _rules = new();

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

        public void AddRule(string rule)
        {
            rule = ReplaceGetStat.Replace(rule, "[$1]");
            rule = rule.ToLower();

            var inputStream = new AntlrInputStream(rule);
            var lexer = new NipLexer(inputStream, TextWriter.Null, TextWriter.Null);
            var tokens = new CommonTokenStream(lexer);
            var parser = new NipParser(tokens, TextWriter.Null, TextWriter.Null);
            var lineExpression = parser.line();

            var valueBag = Expression.Parameter(
                typeof(Dictionary<string, int>), "valueBag"
            );
            var matchExpression = new ExpressionBuilder(valueBag).Visit(lineExpression);

            ParameterExpression result = Expression.Parameter(typeof(Result), "result");
            BlockExpression block = Expression.Block(
                new[] { result },
                Expression.Assign(result, matchExpression),
                result
            );
            var ruleLambda = Expression.Lambda<Func<Dictionary<string, int>, Result>>(block, valueBag).Compile();
            _rules.Add(ruleLambda);
        }

        public Result Match(IItem item, IEnumerable<IItem> otherItems = null)
        {
            otherItems ??= Array.Empty<IItem>();
            var valueBag = CreateValueBag(item);
            var otherValuesBags = otherItems.Select(CreateValueBag).ToList();

            var result = Result.Sell;

            foreach (Func<Dictionary<string, int>, Result> rule in _rules)
            {
                int otherCount = otherValuesBags.Count(o => rule.Invoke(o) == Result.Keep);
                valueBag["currentquantity"] = otherCount;
                switch (rule.Invoke(valueBag))
                {
                    case Result.Keep:
                        return Result.Keep;
                    case Result.Identify:
                        result = Result.Identify;
                        break;
                    case Result.Sell:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }

        public static Dictionary<string, int> CreateValueBag(IItem item)
        {
            Dictionary<string, int> valueBag = new()
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
