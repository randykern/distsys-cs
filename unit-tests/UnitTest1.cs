using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Distsys.Threading;

namespace unit_tests
{
    public class UnitTest1
    {
        #region Helpers 
        private const int TimeoutRequired = 1000;
        private const int TimeoutOptional = 500;

        private const int DelayImmediate = 0;
        private const int DelayFast = 100;
        private const int DelayMedium = 350;
        private const int DelaySlow = 650;
        private const int DelaySlower = 750;
        private const int DelayGlacial = 2000;

        internal void _TestInner<TResult, TResult2>(
            Task<TResult>[] required,
            Task<TResult2>[] optional,
            Action<TResult[], TResult2[]> asserts)
        {
            var whenAll = TaskHelpers.WhenAll<TResult, TResult2>(
                required, TimeoutRequired,
                optional, TimeoutOptional);
            whenAll.Wait();

            asserts(whenAll.Result.Item1, whenAll.Result.Item2);
        }

        internal Task<TResult> CreateTestTask<TResult>(TResult result, int delay)
        {
            if (delay == DelayImmediate)
            {
                return Task.FromResult(result);
            }
            else
            {
                return Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    return result;
                });
            }
        }
        #endregion


        #region Tests for required promises
        [Fact]
        public void TestWhen1RequiredImmediate()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("immediate", DelayImmediate),
                },
                TimeoutRequired,
                new Task<string>[] {
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("immediate", whenAll.Result.Item1[0]);
        }

        [Fact]
        public void TestWhen1RequiredSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutRequired,
                new Task<string>[] {
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("slow", whenAll.Result.Item1[0]);
        }

        [Fact]
        public void TestWhen1RequiredGlacial()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("glacial", DelayGlacial),
                },
                TimeoutRequired,
                new Task<string>[] {
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Null(whenAll.Result.Item1[0]);
        }

        [Fact]
        public void TestWhen3RequiredFastSlowGlacial()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("fast", DelayFast),
                    CreateTestTask("slow", DelaySlow),
                    CreateTestTask("glacial", DelayGlacial),
                },
                TimeoutRequired,
                new Task<string>[] {
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("fast", whenAll.Result.Item1[0]);
            Assert.Equal("slow", whenAll.Result.Item1[1]);
            Assert.Null(whenAll.Result.Item1[2]);
        }

        [Fact]
        public void TestWhen3RequiredFastFastGlacial()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("fast 1", DelayFast),
                    CreateTestTask("fast 2", DelayFast),
                    CreateTestTask("glacial", DelayGlacial),
                },
                TimeoutRequired,
                new Task<string>[] {
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("fast 1", whenAll.Result.Item1[0]);
            Assert.Equal("fast 2", whenAll.Result.Item1[1]);
            Assert.Null(whenAll.Result.Item1[2]);
        }
        #endregion


        #region Test for optional tasks
        [Fact]
        public void TestWhen1OptionalFast()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("fast", DelayFast),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("fast", whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen1OptionalSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Null(whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen3OptionalFastFastSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                    CreateTestTask("fast 1", DelayFast),
                    CreateTestTask("fast 2", DelayFast),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Null(whenAll.Result.Item2[0]);
            Assert.Equal("fast 1", whenAll.Result.Item2[1]);
            Assert.Equal("fast 2", whenAll.Result.Item2[2]);
        }
        #endregion

        #region Tests for mixed (required and optional) tasks
        [Fact]
        public void TestWhen1RequiredFast1OptionalFast()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("fast 1", DelayFast),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("fast 2", DelayFast),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("fast 1", whenAll.Result.Item1[0]);
            Assert.Equal("fast 2", whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen1RequiredFast1OptionalSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("fast 1", DelayFast),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("fast 1", whenAll.Result.Item1[0]);
            Assert.Null(whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen1RequiredSlow1OptionalSlower()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slower", DelaySlower),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("slow", whenAll.Result.Item1[0]);
            Assert.Null(whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen1RequiredSlower1OptionalSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    CreateTestTask("slower", DelaySlower),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("slower", whenAll.Result.Item1[0]);
            Assert.Equal("slow", whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen2RequiredSlowerFast1OptionalSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                  CreateTestTask("slower", DelaySlower),
                    CreateTestTask("fast", DelayFast),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("slower", whenAll.Result.Item1[0]);
            Assert.Equal("fast", whenAll.Result.Item1[1]);
            Assert.Equal("slow", whenAll.Result.Item2[0]);
        }

        [Fact]
        public void TestWhen1RequiredImmediate2OptionalMediumSlow()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                 CreateTestTask("immediate", DelayImmediate),
                },
                TimeoutRequired,
                new Task<string>[] {
                 CreateTestTask("medium", DelayMedium),
                    CreateTestTask("slow", DelaySlow),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Equal("immediate", whenAll.Result.Item1[0]);
            Assert.Equal("medium", whenAll.Result.Item2[0]);
            Assert.Null(whenAll.Result.Item2[1]);
        }
        #endregion

        #region Tests for tasks that throw exceptions
        [Fact]
        public void TestWhen1RequiredExceptionFast1OptionalFast()
        {
            var whenAll = TaskHelpers.WhenAll<string, string>(
                new Task<string>[] {
                    Task.FromException<string>(new Exception()),
                },
                TimeoutRequired,
                new Task<string>[] {
                    CreateTestTask("fast", DelayFast),
                },
                TimeoutOptional);
            whenAll.Wait();

            Assert.Null(whenAll.Result.Item1[0]);
            Assert.Equal("fast", whenAll.Result.Item2[0]);
        }
        #endregion
    }
}