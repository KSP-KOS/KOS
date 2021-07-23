using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Serialization;
using NUnit.Framework;

namespace kOS.Safe.Test.Collections
{
    [TestFixture]
    public class MixedCollectionPrintingTest : CollectionValueTest
    {

        [Test]
        public void CanSerialize()
        {

            var runner = new KSRunner(@"
local l to lex(
  ""Boolean"", true,
  ""List"", list(1, false, ""foo""),
  ""Pid"", PidLoop(1, 2, 3, 4, 5)
//  ""Queue"", queue(1, false, ""foo""),
//  ""Range"", range(1, 2),
//  ""Double"", 1.1,
//  ""Int"", 42,
//  ""Stack"", stack(1, 2),
//  ""String"", ""foo"",
//  ""Set"", uniqueset(1, 2)
).
writejson(l, ""serialization.json"").
return l.
");
            Assert.AreEqual("", runner.Output);

            Lexicon l = runner.Result as Lexicon;
            Assert.NotNull(l);

            var file = runner.Volume.RootHarddiskDirectory.GetFileContent("serialization.json");
            Assert.NotNull(file);

            string reference = @"{
    ""entries"": [
        {
            ""value"": ""Boolean"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""value"": true,
            ""$type"": ""kOS.Safe.Encapsulation.BooleanValue""
        },
        {
            ""value"": ""List"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""items"": [
                {
                    ""value"": 1,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                },
                {
                    ""value"": false,
                    ""$type"": ""kOS.Safe.Encapsulation.BooleanValue""
                },
                {
                    ""value"": ""foo"",
                    ""$type"": ""kOS.Safe.Encapsulation.StringValue""
                }
            ],
            ""$type"": ""kOS.Safe.Encapsulation.ListValue""
        },
        {
            ""value"": ""Pid"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""Kp"": 1,
            ""Ki"": 2,
            ""Kd"": 3,
            ""Setpoint"": 0,
            ""MaxOutput"": 5,
            ""MinOutput"": 4,
            ""ExtraUnwind"": false,
            ""$type"": ""kOS.Safe.Encapsulation.PIDLoop""
        },
        {
            ""value"": ""Queue"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""items"": [
                {
                    ""value"": 1,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                },
                {
                    ""value"": false,
                    ""$type"": ""kOS.Safe.Encapsulation.BooleanValue""
                },
                {
                    ""value"": ""foo"",
                    ""$type"": ""kOS.Safe.Encapsulation.StringValue""
                }
            ],
            ""$type"": ""kOS.Safe.Encapsulation.QueueValue""
        },
        {
            ""value"": ""Range"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""stop"": {
                ""value"": 2,
                ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
            },
            ""start"": {
                ""value"": 1,
                ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
            },
            ""step"": {
                ""value"": 1,
                ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
            },
            ""$type"": ""kOS.Safe.RangeValue""
        },
        {
            ""value"": ""Double"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""value"": 1.1,
            ""$type"": ""kOS.Safe.Encapsulation.ScalarDoubleValue""
        },
        {
            ""value"": ""Int"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""value"": 42,
            ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
        },
        {
            ""value"": ""Stack"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""items"": [
                {
                    ""value"": 2,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                },
                {
                    ""value"": 1,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                }
            ],
            ""$type"": ""kOS.Safe.Encapsulation.StackValue""
        },
        {
            ""value"": ""String"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""value"": ""foo"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""value"": ""Set"",
            ""$type"": ""kOS.Safe.Encapsulation.StringValue""
        },
        {
            ""items"": [
                {
                    ""value"": 1,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                },
                {
                    ""value"": 2,
                    ""$type"": ""kOS.Safe.Encapsulation.ScalarIntValue""
                }
            ],
            ""$type"": ""kOS.Safe.Encapsulation.UniqueSetValue""
        }
    ],
    ""$type"": ""kOS.Safe.Encapsulation.Lexicon""
}".Replace("\r\n", "\n");

            string serializationResult = file.String.Replace("\r\n", "\n");
            System.Diagnostics.Debug.WriteLine(serializationResult);
            Assert.AreEqual(reference, serializationResult);

        }



        [Test]
        public void DoesNotContainInvalidToString()
        {
            var list = new ListValue
            {
                new StringValue("First In List"), 
                new StringValue("Second In List"), 
                new StringValue("Last In List")
            };

            var lexicon = new Lexicon
            {
                {new StringValue("list"), list}, 
                {new StringValue("not list"), new ScalarIntValue(2)}
            };

            var result = (StringValue)InvokeDelegate(lexicon, "DUMP");

            Assert.IsFalse(result.Contains("System"));
            Assert.IsFalse(result.Contains("string[]"));
        }
    }
}
