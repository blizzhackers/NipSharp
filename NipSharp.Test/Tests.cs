using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NipSharp.Test
{
    public class Tests
    {
        private Matcher matcher;
        
        [SetUp]
        public void Setup()
        {
            matcher = new Matcher();
        }

        [Test]
        public void SimpleTest()
        {
            var item = new FakeItem
            {
                Type = NipAliases.Type["amulet"],
                Class = NipAliases.Class["normal"],
                Quality = NipAliases.Quality["crafted"],
                Flags = NipAliases.Flag["identified"],
                Prefixes = new Collection<int>
                {
                    10,
                },
                Stats = new List<IStat>
                {
                    new FakeStat
                    {
                        Id = NipAliases.Stat["itemaddclassskills"].Item1,
                        Value = 2
                    },
                    new FakeStat
                    {
                        Id = NipAliases.Stat["fcr"].Item1,
                        Value = 5
                    },
                    new FakeStat
                    {
                        Id = NipAliases.Stat["dexterity"].Item1,
                        Value = 10
                    },
                    new FakeStat
                    {
                        Id = NipAliases.Stat["coldresist"].Item1,
                        Value = 15,
                    },
                    new FakeStat
                    {
                        Id = NipAliases.Stat["fireresist"].Item1,
                        Value = 15,
                    }
                }
            };
            
            matcher.AddRule(
                "[type] == amulet && [quality] == crafted # [itemaddclassskills] == 2 && [fcr] >= 5 && ([dexterity] >= 10 || [strength] >= 10) && (([coldresist]+[fireresist] >= 30) || ([coldresist]+[lightresist] >= 30) || ([fireresist]+[lightresist] >= 30)) # [maxquantity] == 30"
            );

            var result = matcher.Match(item);

            Assert.AreEqual(Result.Keep, result);
        }

        [Test]
        public void TestNumericStats()
        {
            matcher.AddRule("# [frw] >= 10 && [39]+[41]+[43] >= 50 && ([79] >= 78 || [maxmana] >= 30)");
            var result = matcher.Match(
                new FakeItem
                {
                    Flags = NipAliases.Flag["identified"],
                    Stats = new List<IStat>
                    {
                        new FakeStat
                        {
                            Id = NipAliases.Stat["frw"].Item1,
                            Value = 10, 
                        },
                        new FakeStat
                        {
                            Id = 39,
                            Value = 10
                        },
                        new FakeStat
                        {
                            Id = 41,
                            Value = 10
                        },
                        new FakeStat
                        {
                            Id = 43,
                            Value = 30
                        },
                        new FakeStat
                        {
                            Id = NipAliases.Stat["maxmana"].Item1,
                            Value = 30
                        }
                    }
                }
            );

            Assert.AreEqual(Result.Keep, result);
        }

        [Test]
        public void TestItemGetStatReplace()
        {
            var rule =
                "# [frw] >= 10 && item.getStatEx(39)+item.getStatEx(41,  43) >= 50 && ([79] >= 78 || [maxmana] >= 30)";
            
            matcher.AddRule(rule);
            var result = matcher.Match(
                new FakeItem
                {
                    Flags = NipAliases.Flag["identified"],
                    Stats = new List<IStat>
                    {
                        new FakeStat
                        {
                            Id = NipAliases.Stat["frw"].Item1,
                            Value = 10, 
                        },
                        new FakeStat
                        {
                            Id = 39,
                            Value = 10
                        },
                        new FakeStat
                        {
                            Id = 41,
                            Layer = 43,
                            Value = 40
                        },
                        new FakeStat
                        {
                            Id = NipAliases.Stat["maxmana"].Item1,
                            Value = 30
                        }
                    }
                }
            );
            
            Assert.AreEqual(Result.Keep, result);
        }

        [Test]
        public void TestFlags()
        {
            matcher.AddRule("[flag] == identified && [flag] == eth");
            var result = matcher.Match(
                new FakeItem
                {
                    Flags = NipAliases.Flag["eth"] | NipAliases.Flag["identified"]
                }
            );
            
            Assert.AreEqual(Result.Keep, result);
        }

        [Test]
        public void TestBlizzhackerPickits()
        {
            //Assert.Ignore();
            var directoryName = Path.GetFullPath(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\..\..\..\..\..\pickits"
            );
            if (!Directory.Exists(directoryName))
            {
                Assert.Ignore($"Could not find Blizzhackers pickits at {directoryName}");
            }

            var matcher = new Matcher();
            var lines = 0;
            var failed = 0;
            foreach (string enumerateFile in Directory.EnumerateFiles(
                directoryName, "*.nip", SearchOption.AllDirectories
            ))
            {
                foreach (string readLine in File.ReadLines(enumerateFile))
                {
                    lines += 1;
                    try
                    {
                        matcher.AddRule(readLine);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Failed to parse: {readLine}: {e.Message}");
                        failed += 1;
                    }
                }
            }

            Console.WriteLine($"Failed to parse {failed} out of {lines}");
            var expectedFailures = 284;
            Assert.LessOrEqual(failed, expectedFailures);
        }
    }
}
