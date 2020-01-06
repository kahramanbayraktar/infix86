using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Infix86.Tests
{
    [TestClass]
    public class A86ConverterTests
    {
        private A86Converter _converter;

        [TestInitialize]
        public void Init()
        {
        }

        [TestMethod]
        public void SplitToSimpleOperations_XEqu0fXYAndOr()
        {
            // Arrange
            // x = x & y | 0f
            var postfix = "x = 0f x y & |";
            _converter = new A86Converter(postfix);
            var expected = new List<string>
            {
                "& x y",
                "| 0f NULL",
                "= x NULL",
            };

            // Act
            var actual = _converter.SplitToSingleOperations(postfix);

            // Assert
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < actual.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void SplitToSimpleOperations_XEqu0fXYAndCompare()
        {
            // Arrange
            // x = x & y ? 0f
            var postfix = "x = 0f x y & ?";
            _converter = new A86Converter(postfix);
            var expected = new List<string>
            {
                "& x y",
                "? 0f NULL",
                "= x NULL"
            };

            // Act
            var actual = _converter.SplitToSingleOperations(postfix);

            // Assert
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < actual.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void SplitToSimpleOperations_0fXYAndOr()
        {
            // Arrange
            // x & y | 0f
            var postfix = "0f x y & |";
            _converter = new A86Converter(postfix);
            var expected = new List<string>
            {
                "& x y",
                "| 0f NULL"
            };

            // Act
            var actual = _converter.SplitToSingleOperations(postfix);

            // Assert
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < actual.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void SplitToSimpleOperations_XEqu0fXYAndOr03Comp()
        {
            // Arrange
            // x = 0f | x & y ? 03
            var postfix = "0f x y & | 03 ?";
            _converter = new A86Converter(postfix);
            var expected = new List<string>
            {
                "& x y",
                "| 0f NULL",
                "? NULL 03"
            };

            // Act
            var actual = _converter.SplitToSingleOperations(postfix);

            // Assert
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < actual.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void SplitToSimpleOperations_XEqu0fXYAndOr03AndffComp()
        {
            // Arrange
            // x = 0f | x & y ? 03 & ff
            var postfix = "0f x y & | 03 ff & ?";
            _converter = new A86Converter(postfix);
            var expected = new List<string>
            {
                "& x y",
                "| 0f NULL",
                "& 03 ff",
                "? NULL NULL" // TODO!!!
            };

            // Act
            var actual = _converter.SplitToSingleOperations(postfix);

            // Assert
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < actual.Count; i++)
                Assert.AreEqual(expected[i], actual[i]);
        }

        [TestMethod]
        public void IsHex_5()
        {
            // Arrange
            const string token = "5";
            const bool expected = true;

            // Act
            var actual = A86Converter.IsHex(token);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
