using SapLoVcSharp.Core.Parsing.DependencyParsers;
using SapLoVcSharp.Execution.Compilation;

Console.WriteLine("=== SAP LO-VC Configuration Test Console ===\n");

var code = @"$SELF.CHAR1 = 'A', $SELF.CHAR2 = $SELF.CHAR1, $SELF.CHAR3 = 'true' IF $SELF.CHAR1 = 'A', TABLE T_MYTABLE (CHAR1 = $SELF.CHAR1, CHAR2 = $SELF.CHAR2)";

var procedure = new ProcedureParser(code).Parse();

var instructionCompiler = new InstructionCompiler();
var instructionList = instructionCompiler.Compile(procedure);





return;
