using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Infix86
{
    class Program
    {
        private static string _filePath = "";

        static void Main(string[] args)
        {
            if (args.Any())
            {
                var fileName = args[0];
                var infix = ReadInfixFromFile(fileName);
                if (infix == null)
                {
                    Console.WriteLine("The file content could not be read. Program exiting...");
                    return;
                }
                var postfix = PostfixFromFile(infix);
                var converter = new FasmConverter(postfix); // FASM / A86
                var asm = converter.ToAsm();
                var asmFileName = Path.GetFileNameWithoutExtension(fileName) + ".asm";
                SaveAsmFile(asm, asmFileName);
            }
            else
                Console.WriteLine("You must provide a file name. Program exiting...");

            // Uncomment the below code to run the program in Visual Studio.
            //RunFasmFromFile();
            //RunMultiOptional();
        }

        private static void RunMultiOptional()
        {
            Console.WriteLine("Press a for A86, f for FASM:");
            var type = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 1 || !new[] { "a", "f" }.Contains(type.Trim().ToLower()))
            {
                Console.WriteLine("Press a for A86, f for FASM:");
                type = Console.ReadLine();
            }

            Console.WriteLine("Press c for Console, f for File:");
            var inputType = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(inputType) || inputType.Trim().Length > 1 || !new[] { "c", "f" }.Contains(inputType.Trim().ToLower()))
            {
                Console.WriteLine("Press c for Console, f for File:");
                inputType = Console.ReadLine();
            }

            if (type.ToLower() == "a")
            {
                if (inputType.ToLower() == "c")
                    RunA86FromConsole();
                else
                    RunA86FromFile();
            }
            else if (type.ToLower() == "f")
            {
                if (inputType.ToLower() == "c")
                    RunFasmFromConsole();
                else
                    RunFasmFromFile();
            }
        }

        private static void RunA86FromConsole()
        {
            var postfix = PostfixFromConsole();
            var converter = new A86Converter(postfix);

            if (!converter.IsValid)
            {
                Console.WriteLine("Postfix expression has invalid tokens: " + string.Join(", ", converter.InvalidTokens));
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
            }
            else
            {
                var asm = converter.ToAsm();
                var asmFileName = "consoleinputa86.asm";
                SaveAsmFile(asm, asmFileName);
            }
        }

        private static void RunFasmFromConsole()
        {
            var postfix = PostfixFromConsole();
            var converter = new FasmConverter(postfix);

            if (!converter.IsValid)
            {
                Console.WriteLine("Postfix expression has invalid tokens: " + string.Join(", ", converter.InvalidTokens));
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
            else
            {
                var asm = converter.ToAsm();
                var asmFileName = "consoleinputfasm.asm";
                SaveAsmFile(asm, asmFileName);
            }
        }

        private static void RunA86FromFile()
        {
            var infix = ReadInfixFromFile();
            var postfix = PostfixFromFile(infix);
            var converter = new A86Converter(postfix);
            var asm = converter.ToAsm();
            var asmFileName = Path.GetFileNameWithoutExtension(_filePath) + "a86.asm";
            SaveAsmFile(asm, asmFileName);
        }

        private static void RunFasmFromFile()
        {
            var infix = ReadInfixFromFile();
            var postfix = PostfixFromFile(infix);
            var converter = new FasmConverter(postfix);
            var asm = converter.ToAsm();
            var asmFileName = Path.GetFileNameWithoutExtension(_filePath) + "fasm.asm";
            SaveAsmFile(asm, asmFileName);
        }

        private static string PostfixFromConsole()
        {
            Console.WriteLine("Enter your infix expression separating each line by enter. To finish your expression press enter after an empty line.");

            var expr = new List<string>();
            var input = Console.ReadLine();
            expr.Add(input);
            while (!string.IsNullOrWhiteSpace(input))
            {
                input = Console.ReadLine();
                expr.Add(input);
            }

            var postfix = "";
            foreach (var line in expr)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var p = ReversePolishNotation.ConvertFromInfix(line);
                    postfix += p;
                    if (!string.IsNullOrWhiteSpace(p))
                        postfix += Constants.NewLine;
                }
            }
            return postfix;
        }

        private static string ReadInfixFromFile()
        {
            _filePath = ReadFilePath();
            while (string.IsNullOrWhiteSpace(_filePath))
                _filePath = ReadFilePath();

            var infix = ReadFileContent(_filePath);

            while (string.IsNullOrWhiteSpace(infix))
            {
                _filePath = ReadFilePath();
                while (string.IsNullOrWhiteSpace(_filePath))
                    _filePath = ReadFilePath();

                infix = ReadFileContent(_filePath);
            }

            return infix;
        }

        private static string ReadInfixFromFile(string fileName)
        {
            var infix = ReadFileContent(fileName);
            if (string.IsNullOrWhiteSpace(fileName))
                return null;
            return infix;
        }

        private static string PostfixFromFile(string infix)
        {
            var postfix = "";
            foreach (var line in infix.Split(new[] { Constants.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var p = ReversePolishNotation.ConvertFromInfix(line);
                    postfix += p;
                    if (!string.IsNullOrWhiteSpace(p))
                        postfix += Constants.NewLine;
                }
            }

            return postfix;
        }

        private static string ReadFilePath()
        {
            Console.WriteLine("Enter the path of your file containing any infix expression:");
            return Console.ReadLine();
        }

        private static string ReadFileContent(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveAsmFile(string asm, string filePath)
        {
            File.WriteAllText(filePath, asm);
            Console.WriteLine(filePath + " was generated.");
        }
    }
}
