﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core.Public.Values;
using Xunit;

namespace Microsoft.PowerFx.Core.Tests
{
    // Tests for validating the TestRunner
    public class TestRunnerTests
    {
        [Fact]
        public void Test1()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            var tests = runner.Tests.ToArray();
            Assert.Equal(2, tests.Length);
                        
            // Ordered by how we see them in the file. 
            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("expected_result1", tests[0].GetExpected("-"));
            Assert.Equal("file1.txt:input1", tests[0].GetUniqueId(null));
            Assert.Equal("File1.txt", Path.GetFileName(tests[0].SourceFile), ignoreCase: true);
            Assert.Equal(3, tests[0].SourceLine);

            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].GetExpected("-"));
            Assert.Equal("file1.txt:input2", tests[1].GetUniqueId(null));
        }

        [Fact]
        public void Test2()
        {
            var runner = new TestRunner();
            AddFile(runner, "File2.txt");

            var tests = runner.Tests.ToArray();
            Assert.Equal(2, tests.Length);

            Assert.Equal("MultiInput\n  secondline", tests[0].Input.Replace("\r", string.Empty));
            Assert.Equal("Result", tests[0].GetExpected("-"));
                        
            Assert.Equal("Engines2", tests[1].Input);
            Assert.Equal("Normal", tests[1].GetExpected("-"));
            Assert.Equal("ER1", tests[1].GetExpected("Engine1"));
            Assert.Equal("ER2", tests[1].GetExpected("Engine2"));
        }

        // Override a single file
        [Fact]
        public void TestOverride()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");
            AddFile(runner, "FileOverride.txt");

            var tests = runner.Tests.OrderBy(x => x.Input).ToArray();
            Assert.Equal(2, tests.Length);

            Assert.Equal("input1", tests[0].Input);
            Assert.Equal("override_result1", tests[0].GetExpected("-"));

            // Other test is unchanged. 
            Assert.Equal("input2", tests[1].Input);
            Assert.Equal("expected_result2", tests[1].GetExpected("-"));
        }

        [Fact]
        public void TestBadParse()
        {
            var runner = new TestRunner();

            Assert.Throws<InvalidOperationException>(
                () => AddFile(runner, "Bad1.txt"));
        }

        // #DISABLE directive to remove an entire file. 
        [Fact]
        public void TestDisable()
        {
            var runner = new TestRunner();
            AddFile(runner, "File1.txt");

            AddFile(runner, "FileDisable.txt");

            Assert.Single(runner.DisabledFiles);
            Assert.Equal("File1.txt", runner.DisabledFiles.First());

            var tests = runner.Tests.ToArray();
            Assert.Single(tests);

            Assert.Equal("input3", tests[0].Input);
            Assert.Equal("result3", tests[0].GetExpected("-"));
            Assert.Equal("filedisable.txt:input3", tests[0].GetUniqueId(null));
        }

        private static readonly ErrorValue _errorValue = new ErrorValue(IR.IRContext.NotInSource(Public.Types.FormulaType.Number));

        private class MockRunner : BaseRunner
        {
            public Func<string, string, FormulaValue> _hook;

            protected override Task<FormulaValue> RunAsyncInternal(string expr, string setupHandlerName = null)
            {
                return Task.FromResult(_hook(expr, setupHandlerName));
            }
        }

        [Fact]
        public async Task TestRunnerSuccess()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => FormulaValue.New(1)
            };

            var test = new TestCase();
            test.SetExpected("1");
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);            
        }

        [Fact]
        public async Task TestRunnerFail()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => FormulaValue.New(1)
            };

            var test = new TestCase();
            test.SetExpected("2"); // Mismatch!
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerSkip()
        {
            var runner = new MockRunner();

            // #SKIP won't even call runner.
            var test = new TestCase();
            test.SetExpected("#SKIP");
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Skip, result.Item1);
            Assert.NotNull(result.Item2);
        }

        [Fact]
        public async Task TestRunnerError()
        {
            var runner = new MockRunner
            {
                _hook = (expr, setup) => _errorValue // error
            };
                        
            var test = new TestCase();
            test.SetExpected("#ERROR");
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);

            // It's a failure if #error casesucceeds. 
            runner._hook = (expr, setup) => FormulaValue.New(1); // success
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestRunnerCompilerError()
        {
            // Compiler error is a throw from Check()
            var runner = new MockRunner
            {
                _hook = (expr, setup) => throw new InvalidOperationException("Errors: Error X") 
            };

            var test = new TestCase();
            test.SetExpected("Errors: Error X");
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Pass, result.Item1);

            // It's a failure if we have the wrong error
            runner._hook = (expr, setup) => throw new InvalidOperationException("Errors: Error Y");            
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
            
            // Failure if the compiler error is unexpected
            test.SetExpected("1");
            result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Fail, result.Item1);
        }

        [Fact]
        public async Task TestSetupHandler()
        {
            const string handlerName = "myhandler";

            var runner = new MockRunner
            {
                _hook = (expr, setup) =>
                {
                    Assert.Equal(setup, handlerName);

                    throw new SetupHandlerNotFoundException();
                }
            };

            var test = new TestCase
            {
                SetupHandlerName = handlerName
            };
            test.SetExpected("1");
            var result = await runner.RunAsync(test);

            Assert.Equal(TestResult.Skip, result.Item1);            
        }

        private static void AddFile(TestRunner runner, string filename)
        {
            var test1 = Path.GetFullPath(filename, TxtFileDataAttribute.GetDefaultTestDir("TestRunnerTests"));            
            runner.AddFile(test1);
        }
    }
}
