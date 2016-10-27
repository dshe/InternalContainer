﻿using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace StandardContainer.Tests.Tests.Relative
{
    public class CreateInstanceTest
    {
        private readonly Action<string> write;

        public CreateInstanceTest(ITestOutputHelper output)
        {
            write = output.WriteLine;
        }

        public class ClassX
        {
            public ClassX(ClassY y) { }
        }

        public class ClassY
        {
            public ClassY(ClassZ z) { }
        }

        public class ClassZ
        {
            public ClassZ(int x) { }
        }


        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            var container = new Container(DefaultLifestyle.Singleton, log: write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassZ>()).Output(write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassY>()).Output(write);
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassX>()).Output(write);
        }
    }
}
