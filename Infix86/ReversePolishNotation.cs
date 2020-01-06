using System;
using System.Collections.Generic;
using System.Linq;

namespace Infix86
{
    /// <summary>
    /// This class is used to convert a given infix expression to the corresponding postfix expression.
    /// </summary>
    public class ReversePolishNotation
    {
        /// <summary>
        /// Returns the corresponding postfix expression of a given expression written in infix notation.
        /// </summary>
        /// <param name="infix">Infix expression — each token must be separated by space.</param>
        /// <returns></returns>
        public static string ConvertFromInfix(string infix)
        {
            // Split into array of tokens.
            string[] tokens = infix.Split();

            // Output stack.
            var output = new Stack<string>();

            // Operator stack.
            var operators = new Stack<string>();

            for (var i = 0; i <tokens.Length; i ++)
            {
                var token = tokens[i];

                // If the token is a number or a variable (or = operator), then push it onto the output stack.
                if (!IsOperator(token) && !IsParenthesis(token))
                {
                    output.Push(token);
                    continue;
                }

                // Push left parenthesis onto stack.
                if (token == Constants.OpenParenthesis)
                {
                    operators.Push(token);
                    continue;
                }

                // If the token is a function, then push it onto the output stack.
                if (IsFunction(token))
                {
                    bool keepCheckingStack = true;

                    // While there is an operator token, o2, at the top of the stack, and
                    while (keepCheckingStack)
                    {
                        // The while loop will be aborted when the conditions are not met.
                        keepCheckingStack = false;

                        if (!operators.Any()) continue;

                        int topOfStackPrecendence = GetFunctionPrecedence(operators.Peek());

                        // Either o1 is left-associative and its precedence is equal to that of o2,
                        var funcAssociativity = GetOperatorAssociativity(token);
                        var funcPrecedence = GetFunctionPrecedence(token);
                        if (funcAssociativity == Associativity.Left & funcPrecedence == topOfStackPrecendence)
                        {
                            // Pop o2 off the stack, onto the output stack.
                            output.Push(operators.Pop());
                            // The condition was met so the loop must continue.
                            keepCheckingStack = true;
                        }

                        // Or o1 has precedence less than that of o2,
                        if (funcPrecedence >= topOfStackPrecendence)
                            continue;

                        // Pop o2 off the stack, onto the output stack;
                        output.Push(operators.Pop());
                        // The condition was met so the loop must continue.
                        keepCheckingStack = true;
                    }

                    operators.Push(token);
                    continue;
                }

                // If the token is a close parenthesis:
                if (token == Constants.CloseParenthesis)
                {
                    // Until the token at the top of the stack is a left parenthesis, pop operators off the stack onto the output stack.
                    if (!operators.Any()) continue;

                    while (operators.Peek() != Constants.OpenParenthesis & operators.Any())
                    {
                        // Pop the left parenthesis from the stack, but not onto the output stack.

                        // If the token at the top of the stack is a function token, pop it onto the output stack.
                        output.Push(operators.Pop());

                        // If the stack runs out without finding a left parenthesis, then there are mismatched parentheses.
                        if (operators.Count == 0)
                            throw new Exception("Syntax Error. Mismatch Parenthesis.");
                    }

                    // The loop halted before the pop.
                    if (operators.Peek() == Constants.OpenParenthesis)
                        operators.Pop();
                }
            }

            // Pop the remain operators from the stack.
            while (operators.Count > 0)
            {
                if (IsFunction(operators.Peek()))
                {
                    output.Push(operators.Pop());
                }
                else
                {
                    // Junk like parenthesis?
                    operators.Pop();
                }
            }

            // Get all the tokens.
            string[] result = output.ToArray();
            // Reverse the array.
            Array.Reverse(result);
            return string.Join(" ", result);
        }

        private static Associativity GetOperatorAssociativity(string opt)
        {
            //if (opt == "^")
            //    return Associativity.Right;
            if (opt == Constants.And || opt == Constants.Or || opt == Constants.Compare
                || opt == Constants.Add || opt == Constants.Sub || opt == Constants.Mul || opt == Constants.Div)
                return Associativity.Left;
            return Associativity.None;
        }

        private static int GetFunctionPrecedence(string token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            if (token == Constants.And)
                return 2;
            if (token == Constants.Mul)
                return 2;
            if (token == Constants.Div)
                return 2;
            if (token == Constants.Add)
                return 1;
            if (token == Constants.Sub)
                return 1;
            if (token == Constants.Or)
                return 1;
            if (token == Constants.Compare)
                return 1;
            return -1;
        }

        /// <summary>
        /// Returns if a given string is a function name.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsFunction(string token)
        {
            return token == Constants.And || token == Constants.Or || token == Constants.Compare
                   || token == Constants.Add || token == Constants.Sub || token == Constants.Mul || token == Constants.Div;
        }

        /// <summary>
        /// Returns if a given string is a numeric value.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsNumeric(object token)
        {
            return double.TryParse(Convert.ToString(token), out _);
        }

        /// <summary>
        /// Returns if a given string is a variable.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsVariable(string token)
        {
            return !IsNumeric(token) && !IsFunction(token);
        }

        /// <summary>
        /// Returns if a given string is a numeric value or a variable.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsNumberOrVariable(string token)
        {
            return (IsNumeric(token) || IsVariable(token)) && token != Constants.OpenParenthesis && token != Constants.CloseParenthesis;
        }

        /// <summary>
        /// Returns if a given string is an operator.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsOperator(string token)
        {
            return token == Constants.And || token == Constants.Or || token == Constants.Compare
                || token == Constants.Add || token == Constants.Sub || token == Constants.Mul || token == Constants.Div;
        }

        /// <summary>
        /// Returns if a given string is a parenthesis.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsParenthesis(string token)
        {
            return token == Constants.OpenParenthesis || token == Constants.CloseParenthesis;
        }

        private enum Associativity
        {
            None,
            Left,
            Right
        }
    }
}
