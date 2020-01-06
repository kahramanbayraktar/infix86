using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Infix86
{
    /// <summary>
    /// This class is used to convert a given postfix expression to the corresponding assembly code.
    /// </summary>
    public class A86Converter
    {
        /// <summary>
        /// The whole postfix expression to be converted to assembly code.
        /// </summary>
        private readonly string _postfix;

        public List<string> InvalidTokens { get; private set; }

        public bool IsValid => !InvalidTokens.Any();

        /// <summary>
        /// The section which contains variable declarations and assignments.
        /// </summary>
        private static readonly StringBuilder AsmSectionData = new StringBuilder();

        /// <summary>
        /// All variables including their value in the postfix expression.
        /// </summary>
        private static Dictionary<string, string> _variables;

        /// <summary>
        /// The number to be used in creating unique label names.
        /// </summary>
        private int _labelNumber = 1;

        /// <summary>
        /// Default value for hexadecimal variables.
        /// </summary>
        private const string DefHexVal = "0";

        public A86Converter(string postfix)
        {
            _postfix = postfix;

            Validate();
        }

        /// <summary>
        /// Returns the assembly code corresponding to the postfix value of the converter which may consist of lines separated by new line code.
        /// </summary>
        /// <returns></returns>
        public string ToAsm()
        {
            var exprLines = _postfix.Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var asm = new StringBuilder(";A86" + Constants.NewLine);

            AppendVarDeclarations();

            // The only difference between A86 and FASM is the position of the data section so far.
            asm.Append(AsmSectionData + Constants.NewLine);

            foreach (var exprLine in exprLines)
            {
                if (exprLine.Split().Length > 3)
                    asm.Append(";" + exprLine + Constants.NewLine);

                // If the postfix expression is a simple assignment, such as x = 81, then just skip it.
                if (exprLine.Contains(Constants.Assign) && exprLine.Split().Length == 3)
                    continue;

                var operations = SplitToSingleOperations(exprLine);

                foreach (var operation in operations)
                {
                    var ops = operation.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries);

                    var opt = ops[0];
                    var opd1 = ops[1];
                    var opd2 = ops[2];

                    if (opt == Constants.Assign)
                    {
                        var assigned = opd1;
                        asm.Append(ToAsmSetVarFromRegister(assigned));
                    }
                    else
                    {
                        asm.Append(ToAsm(opt, opd1, opd2));
                    }
                }

                if (operations.Any())
                    asm.Append(Constants.NewLine);
            }

            foreach (var variable in _variables)
                asm.Append(ToAsmPrintBlock(variable.Key) + Constants.NewLine);

            asm.Append(ToAsmExit() + Constants.NewLine);
            asm.Append(ToAsmPrintHex() + Constants.NewLine);
            asm.Append(ToAsmPrintNewLine());

            return asm.ToString();
        }

        /// <summary>
        /// Returns an assembly code block based on type of the given operation.
        /// </summary>
        /// <param name="opt">Operator. Ex: x.</param>
        /// <param name="opd1">Left operand. Ex: ff.</param>
        /// <param name="opd2">Right operand. Ex: 03.</param>
        /// <returns></returns>
        private string ToAsm(string opt, string opd1, string opd2)
        {
            if (opd1 == "NULL") opd1 = null;
            if (opd2 == "NULL") opd2 = null;

            if (opt == Constants.And || opt == Constants.Or 
                                     || opt == Constants.Add || opt == Constants.Sub
                                     || opt == Constants.Mul || opt == Constants.Div)
                return ToAsmBitwise(opt, opd1, opd2);

            if (opt == Constants.Compare)
                return ToAsmCompare(opd1, opd2, GetLabelNumber());

            throw new ArgumentException(nameof(opt));
        }

        /// <summary>
        /// Returns an assembly code block which declares a variable with a given initial value.
        /// Example corresponding infix/postfix expression: x = 81
        /// </summary>
        /// <param name="assigned">Assigned variable.</param>
        /// <param name="initVal">Initial value of the variable.</param>
        /// <returns>Output assembly code. Ex: x DW 081h.</returns>
        private static string ToAsmVarDeclaration(string assigned, string initVal)
        {
            var asm = new StringBuilder();
            asm.Append($"{assigned} ");
            asm.Append($"DW 0{initVal}h" + "\t\t\t;Declare and initialize " + assigned);
            asm.Append(Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns an assembly code block which assigns a variable the value in the AX register.
        /// Example corresponding infix/postfix expression: x =
        /// </summary>
        /// <param name="assigned">Variable name. Ex: x.</param>
        /// <returns>Output assembly code. Ex: MOV [x], AX.</returns>
        private static string ToAsmSetVarFromRegister(string assigned)
        {
            if (IsHex(assigned))
                assigned = "0" + assigned + "h";

            var asm = $"MOV\t {assigned}, AX" + Constants.NewLine;
            return asm;
        }

        /// <summary>
        /// Returns an assembly code block which does bitwise logical operations (and/or).
        /// Example corresponding postfix expression: x y &
        /// </summary>
        /// <param name="opt">Operator. Ex: x.</param>
        /// <param name="opd1">Left operand. Ex: ff.</param>
        /// <param name="opd2">Right operand. Ex: 03.</param>
        /// <returns>Assembly code. Ex:
        /// MOV AX, x
        /// AND AX, y
        /// </returns>
        private static string ToAsmBitwise(string opt, string opd1, string opd2)
        {
            opd2 = FixHex(opd2);

            opt = ToAsmOperator(opt);

            var asm = "";
            if (opd1 != null)
                asm += $"MOV\t AX, {opd1}" + Constants.NewLine;
            asm += $"{opt}\t AX, {opd2}" + Constants.NewLine;
            return asm;
        }

        /// <summary>
        /// Returns an assembly code block which compares two values according to a special logic
        /// — if the left operand is larger than 0, then returns the right operand, else returns 0.
        /// This operation is specific to the ? operation which is called Constants.Compare in the program.
        /// Example corresponding postfix expressions:
        /// x 03 ?
        /// 03 ?
        /// </summary>
        /// <param name="opd1">Left operand. Ex: ff.</param>
        /// <param name="opd2">Right operand. Ex: 03.</param>
        /// <param name="assigned">Assigned variable. Ex: x.</param>
        /// <returns></returns>
        private static string ToAsmCompare(string opd1, string opd2, string assigned)
        {
            // If the left operand is null, then the right operand becomes the AX register.
            if (string.IsNullOrWhiteSpace(opd2))
                opd2 = "AX";

            opd2 = FixHex(opd2);

            var asm = new StringBuilder();
            if (string.IsNullOrWhiteSpace(opd1))
                asm.Append("CMP\t AX, 00h" + Constants.NewLine);
            else
                asm.Append($"CMP\t {opd1}, 00h" + Constants.NewLine);
            asm.Append($"JG\t COMPARE_{assigned}" + Constants.NewLine);
            asm.Append("MOV\t AX, 00h" + Constants.NewLine);
            asm.Append($"COMPARE_{assigned}:" + Constants.NewLine);
            asm.Append($"MOV\t AX, {opd2}" + Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns an assembly code block which exits the program.
        /// </summary>
        /// <returns></returns>
        private static string ToAsmExit()
        {
            var asm = new StringBuilder();
            asm.Append("MOV\t AH, 04ch" + "\t\t;Clear AH for exit." + Constants.NewLine);
            asm.Append("INT\t 021h" + "\t\t\t;Interrupt for the system call." + Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns an assembly function which prints a hexadecimal value in the console.
        /// </summary>
        /// <returns></returns>
        private static string ToAsmPrintHex()
        {
            var asm = new StringBuilder("PRINT_HEX:" + Constants.NewLine);
            asm.Append("MOV\t BL, DL" + "\t\t\t;Load input to BL (Ex. 8F)." + Constants.NewLine);
            asm.Append("MOV\t BH, 00h" + "\t\t;Set BH to zero (00)." + Constants.NewLine);
            asm.Append("MOV\t CL, 04h" + "\t\t;Initialize rotation counter." + Constants.NewLine);
            asm.Append("SHL\t BX, CL" + "\t\t\t;Shift BX (008F) 4 bits to the left (08F0)." + Constants.NewLine);
            asm.Append("MOV\t DL, BH" + "\t\t\t;Load first digit to DL (08)." + Constants.NewLine);
            asm.Append("CALL ONE_DIGIT" + "\t\t;Call the function ONE_DIGIT." + Constants.NewLine);
            asm.Append("MOV\t CL, 04h" + "\t\t;Initialize rotation counter." + Constants.NewLine);
            asm.Append("SHR\t BL, CL" + "\t\t\t;Shift BL (F0) 4 bits to the right (0F)." + Constants.NewLine);
            asm.Append("MOV\t DL, BL" + "\t\t\t;Load second digit to DL (0F)." + Constants.NewLine);
            asm.Append("CALL ONE_DIGIT" + "\t\t;Call the function ONE_DIGIT." + Constants.NewLine);
            asm.Append("RET" + "\t\t\t\t\t;Return the execution to where the function called." + Constants.NewLine);
            asm.Append(Constants.NewLine);
            asm.Append("ONE_DIGIT:" + "\t\t\t;Start for the function ONE_DIGIT." + Constants.NewLine);
            asm.Append("CMP\t DL, 09h" + "\t\t;Check if the character is a number." + Constants.NewLine);
            asm.Append("JA\t LETTER" + "\t\t\t;Jump if it is above 9, that is, A-F." + Constants.NewLine);
            asm.Append("ADD\t DL, 030h" + "\t\t;Add 30h (48d) to get the ASCII equivalent of the character." + Constants.NewLine);
            asm.Append("JMP\t NEXT" + "\t\t\t;Jump to the function NEXT." + Constants.NewLine);
            asm.Append("LETTER:" + "\t\t\t\t;The function LETTER converts a hex value to a character." + Constants.NewLine);
            asm.Append("ADD\t DL, 037h" + "\t\t;Convert the character into a hexadecimal number by adding 037h (037h = 'A' - 10)." + Constants.NewLine);
            asm.Append("NEXT:" + "\t\t\t\t;Start for the function NEXT." + Constants.NewLine);
            asm.Append("MOV\t AH, 02h" + "\t\t;Load function number (2 is for the write system call)." + Constants.NewLine);
            asm.Append("INT\t 021h" + "\t\t\t;Interrupt for the system call." + Constants.NewLine);
            asm.Append("RET" + "\t\t\t\t\t;Return the execution to where the function called." + Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns an assembly code block which prints an empty line.
        /// </summary>
        /// <returns></returns>
        private static string ToAsmPrintNewLine()
        {
            var asm = new StringBuilder();
            asm.Append("PRINT_NEWLINE:" + "\t\t;Start for the function PRINT_NEWLINE." + Constants.NewLine);
            asm.Append("MOV\t DL, 0ah" + "\t\t;0ah = 10 = LF (line feed) / NL (new line)." + Constants.NewLine);
            asm.Append("MOV\t AH, 02h" + "\t\t;Interrupt to write the character." + Constants.NewLine);
            asm.Append("INT\t 021h" + "\t\t\t;Interrupt for the system call." + Constants.NewLine);
            asm.Append("MOV\t DL, 0dh" + "\t\t;0dh = 12 = CR (carriage return)." + Constants.NewLine);
            asm.Append("MOV\t AH, 02h" + "\t\t;Load function number (2 is for the write system call)." + Constants.NewLine);
            asm.Append("INT\t 021h" + "\t\t\t;Interrupt for the system call." + Constants.NewLine);
            asm.Append("RET" + "\t\t\t\t\t;Return the execution to where the function called." + Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns three assembly code lines — load a variable value to a register, print hexadecimal value in this register, and print an empty line.
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        private static string ToAsmPrintBlock(string variable)
        {
            var asm = new StringBuilder();
            asm.Append($"MOV\t DX, {variable}" + Constants.NewLine);
            asm.Append("CALL PRINT_HEX" + Constants.NewLine);
            asm.Append("CALL PRINT_NEWLINE" + Constants.NewLine);
            return asm.ToString();
        }

        /// <summary>
        /// Returns the mnemonics corresponding a given operator name.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        private static string ToAsmOperator(string op)
        {
            switch (op)
            {
                case Constants.And:
                    return "AND";
                case Constants.Or:
                    return "OR";
                case Constants.Add:
                    return "ADD";
                case Constants.Sub:
                    return "SUB";
                case Constants.Mul:
                    return "MUL";
                case Constants.Div:
                    return "DIV";
            }
            return null;
        }

        private void Validate()
        {
            InvalidTokens = new List<string>();

            foreach (var line in _postfix.Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var token in line.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!IsOperator(token) && !IsHex(token) && !IsVarName(token))
                        InvalidTokens.Add(token);
                }
            }
        }

        /// <summary>
        /// Splits the given postfix expression to parts having only one operation.
        /// </summary>
        /// <param name="postfix">y = x | y ? 03</param>
        /// <returns>[{? x y}, {? NULL 03}, {= y NULL}]</returns>
        public List<string> SplitToSingleOperations(string postfix)
        {
            var tokens = new Stack<string>();
            var temp = new Stack<string>();

            var singleOps = new List<string>();

            foreach (var token in postfix.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries).Reverse())
                tokens.Push(token);

            while (tokens.Count > 0)
            {
                var token = tokens.Pop();
                temp.Push(token);

                if (IsOperator(token))
                {
                    var opt = temp.Pop();
                    var opd1 = "NULL";
                    var opd2 = "NULL";

                    var ops = postfix.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries);
                    // Check if the expression is a simple assignment, such as x = 81.
                    if (token == Constants.Assign && ops.Length == 3) // TODO
                    {
                        opt = ops[1];
                        opd1 = ops[0];
                        opd2 = ops[2];
                    }
                    else
                    {
                        if (temp.Count >= 2)
                        {
                            opd2 = temp.Pop();
                            opd1 = temp.Pop();
                        }
                        else
                        {
                            opd1 = temp.Pop();
                        }
                    }

                    // Push the result which is not computed yet as NULL to be able to identify it later.
                    temp.Push("NULL");

                    singleOps.Add(opt + Constants.Space + opd1 + Constants.Space + opd2);
                }
            }

            foreach (var op in singleOps)
            {
                if (op.StartsWith(Constants.Assign))
                {
                    var tmp = op;
                    singleOps.Remove(op);
                    singleOps.Add(tmp);
                    break;
                }
            }

            return singleOps;
        }

        /// <summary>
        /// Inserts variable declarations with initial values to the assembly output.
        /// </summary>
        private void AppendVarDeclarations()
        {
            IdentifyVars();

            var assignedVariables = GetAssignedVars();
            foreach (var variable in assignedVariables)
            {
                _variables.Remove(variable.Key);
                _variables.Add(variable.Key, assignedVariables[variable.Key]);
            }

            foreach (var variable in _variables)
                AsmSectionData.Append(ToAsmVarDeclaration(variable.Key, variable.Value));
        }

        /// <summary>
        /// Returns all variables assigning the default value in the postfix property of the converter class.
        /// </summary>
        private void IdentifyVars()
        {
            // Scan postfix for variable names
            var variables = new Dictionary<string, string>();
            var exprLines = _postfix.Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in exprLines)
            {
                foreach (var op in line.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (IsVarName(op) && !variables.ContainsKey(op))
                        variables.Add(op, DefHexVal);
                }
            }
            _variables = variables;
        }

        /// <summary>
        /// Returns the variables which are assigned a value in the postfix property of the converter class.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetAssignedVars()
        {
            var variables = new Dictionary<string, string>();
            var exprLines = _postfix.Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in exprLines)
            {
                var ops = line.Split(new[] { Constants.Space }, StringSplitOptions.RemoveEmptyEntries);
                if (ops.Length == 3 && ops[1] == Constants.Assign && IsHex(ops[2]))
                    variables.Add(ops[0], ops[2]);
            }
            return variables;
        }

        /// <summary>
        /// Returns if a given string is an operator.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsOperator(string token)
        {
            return token == Constants.And || token == Constants.Or || token == Constants.Compare 
                   || token == Constants.Add || token == Constants.Sub || token == Constants.Mul || token == Constants.Div
                   || token == Constants.Assign;
        }

        /// <summary>
        /// Returns if a given string is a valid hexadecimal value.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool IsHex(string token)
        {
            return Regex.Match(token, "^([0-9a-fA-F]{1,2})$").Success;
        }

        /// <summary>
        /// Returns if a given string is a valid variable name.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static bool IsVarName(string token)
        {
            // Can only be a single letter between g and z or G and Z.
            return Regex.Match(token, "^([g-zG-Z]{1})$").Success;
        }

        /// <summary>
        /// Standardizes a given hexadecimal value putting 0 prefix and h suffix into it if not available.
        /// </summary>
        /// <param name="hex">Ex: b4</param>
        /// <returns>Ex: 0b4h</returns>
        private static string FixHex(string hex)
        {
            if (hex == "0")
                return hex;

            if (IsHex(hex))
            {
                if (!hex.StartsWith("0"))
                    hex = "0" + hex;
                if (!hex.EndsWith("h"))
                    hex += "h";
            }

            return hex;
        }

        /// <summary>
        /// Returns a unique number to create unique labels in the assembly code.
        /// </summary>
        /// <returns></returns>
        private string GetLabelNumber()
        {
            return _labelNumber++.ToString();
        }
    }
}
