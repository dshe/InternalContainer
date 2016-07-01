﻿using System;
using System.Reflection;
using InternalContainer;
using Xunit;
using Xunit.Abstractions;

namespace InternalContainerTests.Tests.Relative
{
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

    public class CreateInstanceTest
    {
        private readonly Container container;

        public CreateInstanceTest(ITestOutputHelper output)
        {
            container = new Container(Container.Lifestyle.Singleton, log: output.WriteLine, assemblies:Assembly.GetExecutingAssembly());
        }

        [Fact]
        public void Test_Cannot_Create_Dependency()
        {
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassZ>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassY>());
            Assert.Throws<TypeAccessException>(() => container.GetInstance<ClassX>());
        }
    }
}
