using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Infix86.Tests
{
    [TestClass]
    public class ReversePolishNotationTests
    {
        [TestMethod]
        public void ConvertFromInfix_XEquParenthesisXAndYParenthesisOr0f()
        {
            // Arrange
            var infix = "x = ( x & y ) | 0f";
            var expected = "x = x y & 0f |";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConvertFromInfix_XEquXAndYOr0f()
        {
            // Arrange
            var infix = "x = x & y | 0f";
            var expected = "x = x y & 0f |";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConvertFromInfix_XEquOpenParXAndYCloseParOr0f()
        {
            // Arrange
            var infix = "x = ( x & y ) | 0f";
            var expected = "x = x y & 0f |";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConvertFromInfix_XEquXCompYAnd0f()
        {
            // Arrange
            var infix = "x = x ? y & 0f";
            var expected = "x = x y 0f & ?";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConvertFromInfix_XEquOpenParXCompYCloseParAnd0f()
        {
            // Arrange
            var infix = "x = ( x ? y ) & 0f";
            var expected = "x = x y ? 0f &";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ConvertFromInfix_XEqu0fOrParenthesisXAndYParenthesis()
        {
            // Arrange
            var infix = "x = 0f | ( x & y )";
            var expected = "x = 0f x y & |";

            // Act
            var actual = ReversePolishNotation.ConvertFromInfix(infix);

            // Assert
            Assert.AreEqual(expected, actual);
        }
    }
}
