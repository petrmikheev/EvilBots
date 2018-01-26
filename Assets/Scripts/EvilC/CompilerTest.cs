using System;

namespace EvilC
{
	public class CompilerTest {
		static void Main(string[] args) {
			EvilCompiler compiler = new EvilCompiler();
			string test = "";
			while (true) {
				string s = Console.ReadLine ();
				if (s == null)
					break;
				test += s + "\n";
			}
			BinaryCode code = compiler.compile (test);
			code.print ();
			EvilRuntime r = new EvilRuntime (code);
			if (!r.run ()) Console.WriteLine("Interrupted");
			Console.Write (r.showVariables());
		}
	}
}

