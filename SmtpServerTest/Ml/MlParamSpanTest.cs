﻿using NUnit.Framework;
using SmtpServer;

namespace SmtpServerTest {
    [TestFixture]
    internal class MlParamSpanTest{
        
        [SetUp]
        public void SetUp(){
        }

        [TearDown]
        public void TearDown(){
        }

        [TestCase("1-10", 30, 1, 10)]
        [TestCase("1-10", 5, 1, 5)]
        [TestCase("10-5", 30, 5, 10)]
        [TestCase("-1-5", 30, -1, -1)]//無効値
        [TestCase("last:20", 0, -1, -1)]//無効値
        [TestCase("20", 30, 20, 20)]
        [TestCase("last:5", 30, 26, 30)]
        [TestCase("lAST:5", 30, 26, 30)]
        [TestCase("first:5", 30, 1, 5)]
        [TestCase("45-60", 30, -1, -1)]
        public void CtorTest(string paramStr, int current, int start, int end) {
            var mlParamSpan = new MlParamSpan(paramStr,current);
            Assert.AreEqual(mlParamSpan.Start, start);
            Assert.AreEqual(mlParamSpan.End, end);

        }
    }
}
