﻿using System;
using System.Reflection;
using StandardContainer.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class RecursionTest : TestBase
    {
        public RecursionTest(ITestOutputHelper output) : base(output) {}

        public class Class1
        {
            public Class1(Class2 c2) { }
        }

        public class Class2
        {
            public Class2(Class3 c3) { }
        }
        public class Class3
        {
            public Class3(Class1 c1) { }
        }


        [Fact]
        public void Test_Recursive_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class1>()).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class2>()).Output(Write);
            Assert.Throws<TypeAccessException>(() => container.Resolve<Class3>()).Output(Write);
        }

    }
}
