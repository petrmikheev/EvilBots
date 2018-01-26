using System;
using System.Collections;
using System.Collections.Generic;

namespace EvilC
{
	public class EvilCompiler
	{
		private TokenParser tk;
		private BinaryCode code;
		private Token t;
		private class Function {
			public int address;
			public int max_local_data_size;
			public List<DataType> parameters;
			public DataType returnType;
			public bool special;
			public int id;
		}
		private class LoopData {
			public int continue_pos;
			public List<int> break_points = new List<int>();
		}
		private Stack<LoopData> loops = new Stack<LoopData>();
		private Dictionary<string, Function> functions = new Dictionary<string, Function>();
		private List<Function> functions_list = new List<Function> ();
		private Function current_function = null;
		private Dictionary<string, BinaryCode.Variable> global_variables = new Dictionary<string, BinaryCode.Variable>();
		private List<Dictionary<string, BinaryCode.Variable>> local_variables = new List<Dictionary<string, BinaryCode.Variable>>();
		int global_data_size = 0;
		int local_data_size = 0;
		int max_local_data_size = 0;

		private void addSpecialFunction (BuiltinFunction id, DataType ret, string name, List<DataType> parameters) {
			Function f = new Function ();
			f.address = f.id = (int)id;
			f.special = true;
			f.parameters = parameters;
			f.returnType = ret;
			functions [name] = f;
		}
		private void addBuiltinVariable(BuiltinVariable addr, string name, DataType type) {
			BinaryCode.Variable v = new BinaryCode.Variable ();
			v.address = (int)addr;
			v.name = name;
			v.type = type;
			v.builtin = true;
			v.visible = false;
			v.array = false;
			v.arraySize = 1;
			global_variables [name] = v;
		}

		private void deleteLocalVariablesBlock() {
			Dictionary<string, BinaryCode.Variable> b = local_variables [local_variables.Count - 1];
			local_variables.RemoveAt (local_variables.Count - 1);
			foreach (var kv in b) {
				BinaryCode.Variable v = kv.Value;
				int vsize = v.arraySize;
				if (v.type == DataType.VECTOR)
					vsize *= 3;
				local_data_size -= vsize;
			}
		}

		public EvilCompiler () {}

		private void parseBlock() {
			while (t.type != Token.Type.EOF && t.word != "}") parseCommand();
		}

		private void parseCommand() {
			if (t.word == ";") {
				t = tk.next ();
				return;
			}
			if (t.word == "{") {
				t = tk.next ();
				local_variables.Add (new Dictionary<string, BinaryCode.Variable>());
				parseBlock ();
				deleteLocalVariablesBlock ();
				if (t.word != "}")
					throw new Exception (tk.errorPrefix() + "Unexpected end of file");
				t = tk.next ();
				return;
			}
			if (t.word == "if") { parseIf(); return; }
			if (t.word == "for") { parseFor(); return; }
			if (t.word == "while") { parseWhile(); return; }
			if (t.word == "return") {
				if (current_function == null) throw new Exception (tk.errorPrefix() + "Unexpected return");
				t = tk.next ();
				if (current_function.returnType != DataType.VOID) {
					parseExpression (current_function.returnType);
					if (current_function.returnType == DataType.VECTOR)
						code.commands.Add (new BinaryCode.Command (Opcode.RETV, current_function.parameters.Count));
					else
						code.commands.Add (new BinaryCode.Command (Opcode.RETI, current_function.parameters.Count));
				} else
					code.commands.Add (new BinaryCode.Command (Opcode.RET, current_function.parameters.Count));
				if (t.word != ";") throw new Exception (tk.errorPrefix() + "';' expected");
				return;
			}
			if (t.word == "break" || t.word == "continue") {
				if (loops.Count == 0) throw new Exception (tk.errorPrefix() + "unexpected " + t.word);
				if (t.word == "break") {
					loops.Peek ().break_points.Add (code.commands.Count);
					code.commands.Add (new BinaryCode.Command (Opcode.JMP));
				} else
					code.commands.Add (new BinaryCode.Command (Opcode.JMP, loops.Peek().continue_pos));
				t = tk.next ();
				if (t.word != ";") throw new Exception (tk.errorPrefix() + "';' expected");
				return;
			}

			bool visible = t.word == "visible";
			if (visible) t = tk.next ();
			if (t.type != Token.Type.Word)
				throw new Exception (tk.errorPrefix() + "Identifier expected");
			if (t.word != "void" && t.word != "int" && t.word != "float" && t.word != "vector") {
				if (visible) throw new Exception (tk.errorPrefix() + "Type expected");
				DataType dt = parseExpression ();
				if (dt == DataType.FLOAT || dt == DataType.INT) code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -1));
				if (dt == DataType.VECTOR) code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -3));
				if (t.word != ";") throw new Exception (tk.errorPrefix() + "';' expected");
				t = tk.next ();
				return;
			} else {
				string type = t.word;
				t = tk.next ();
				if (t.type != Token.Type.Word)
					throw new Exception (tk.errorPrefix() + "Identifier expected");
				string name = t.word;
				t = tk.next ();
				if (t.word == "(") {
					if (visible)
						throw new Exception (tk.errorPrefix() + "Function can't be declared 'visible'");
					parseFunctionDefinition (name, type);
					return;
				}
				if (type == "void") throw new Exception (tk.errorPrefix() + "Incorrect type");
				if (visible && local_variables.Count > 0)
					throw new Exception (tk.errorPrefix() + "Only global variables can be declared 'visible'");
				bool array = false;
				int arraySize = 1;
				if (t.word == "[") {
					array = true;
					t = tk.next ();
					if (t.type != Token.Type.IntegerNumber)
						throw new Exception (tk.errorPrefix() + "Integer value expected");
					arraySize = t.i;
					if (arraySize < 1)
						throw new Exception (tk.errorPrefix() + "Incorrect array size");
					t = tk.next ();
					if (t.word != "]")
						throw new Exception (tk.errorPrefix() + "']' expected");
					t = tk.next ();
				}
				int vsize = arraySize;
				if (type == "vector")
					vsize *= 3;
				if (t.word == "=" || t.word == ";") {
					BinaryCode.Variable v = new BinaryCode.Variable ();
					v.array = array;
					v.arraySize = arraySize;
					v.name = name;
					if (type == "int")
						v.type = DataType.INT;
					else if (type == "float")
						v.type = DataType.FLOAT;
					else
						v.type = DataType.VECTOR;
					v.builtin = false;
					v.visible = visible;
					if (local_variables.Count == 0) {
						v.address = global_data_size;
						global_data_size += vsize;
						if (global_variables.ContainsKey(name))
							throw new Exception (tk.errorPrefix() + "Variable already exists");
						global_variables [name] = v;
					} else {
						v.address = local_data_size;
						local_data_size += vsize;
						if (current_function != null && local_data_size > current_function.max_local_data_size)
							current_function.max_local_data_size = local_data_size;
						if (current_function == null && local_data_size > max_local_data_size)
							max_local_data_size = local_data_size;
						if (local_variables [local_variables.Count - 1].ContainsKey(name))
							throw new Exception (tk.errorPrefix() + "Variable already exists");
						local_variables [local_variables.Count - 1] [name] = v;
					}
					if (visible)
						code.visibleVariables.Add (v);
					if (t.word == "=") {
						if (array) throw new Exception (tk.errorPrefix() + "Array initialization is not allowed");
						parseVariable(name);
						if (v.type == DataType.FLOAT || v.type == DataType.INT)
							code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -1));
						if (v.type == DataType.VECTOR) code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -3));
					}
					if (t.word != ";") throw new Exception (tk.errorPrefix() + "';' expected");
					t = tk.next ();
					return;
				}
			}
			throw new Exception (tk.errorPrefix() + "Unknown command");
		}

		private void parseIf() {
			if (t.word != "if") throw new Exception (tk.errorPrefix() + "'if' expected");
			t = tk.next ();
			if (t.word != "(") throw new Exception (tk.errorPrefix() + "'(' expected");
			t = tk.next ();
			DataType ct = parseExpression ();
			if (ct == DataType.VOID || ct == DataType.VECTOR)
				throw new Exception (tk.errorPrefix() + "Incorrect condition");
			if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
			t = tk.next ();
			int jz_pos = code.commands.Count;
			code.commands.Add (new BinaryCode.Command (Opcode.JZ));
			parseCommand ();
			if (t.word == "else") {
				t = tk.next ();
				int jmp_pos = code.commands.Count;
				code.commands.Add (new BinaryCode.Command (Opcode.JMP));
				code.commands [jz_pos].i = code.commands.Count;
				parseCommand ();
				code.commands [jmp_pos].i = code.commands.Count;
			} else code.commands [jz_pos].i = code.commands.Count;
		}
		private void parseFor() {
			if (t.word != "for") throw new Exception (tk.errorPrefix() + "'for' expected");
			t = tk.next ();
			if (t.word != "(") throw new Exception (tk.errorPrefix() + "'(' expected");
			t = tk.next ();
			local_variables.Add (new Dictionary<string, BinaryCode.Variable>());
			parseCommand ();
			int continue_pos = code.commands.Count;
			int jz_pos = -1;
			if (t.word != ";") {
				DataType ct = parseExpression ();
				if (ct == DataType.VOID || ct == DataType.VECTOR)
					throw new Exception (tk.errorPrefix() + "Incorrect condition");
				jz_pos = code.commands.Count;
				code.commands.Add (new BinaryCode.Command (Opcode.JZ));
			}
			if (t.word != ";") throw new Exception (tk.errorPrefix() + "';' expected");
			t = tk.next ();
			List<BinaryCode.Command> step = null;
			if (t.word != ")") {
				int before_pos = code.commands.Count;
				DataType dt = parseExpression ();
				if (dt == DataType.FLOAT || dt == DataType.INT) code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -1));
				if (dt == DataType.VECTOR) code.commands.Add (new BinaryCode.Command (Opcode.MV_SP, -3));
				int after_pos = code.commands.Count;
				step = code.commands.GetRange (before_pos, after_pos - before_pos);
				code.commands.RemoveRange (before_pos, after_pos - before_pos);
			}
			if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
			t = tk.next ();
			LoopData ld = new LoopData ();
			ld.continue_pos = continue_pos;
			loops.Push (ld);

			parseCommand();

			if (step != null)
				code.commands.AddRange (step);
			code.commands.Add (new BinaryCode.Command (Opcode.JMP, continue_pos));
			if (jz_pos >= 0)
				code.commands [jz_pos].i = code.commands.Count;
			loops.Pop ();
			foreach (int i in ld.break_points)
				code.commands [i].i = code.commands.Count;
			deleteLocalVariablesBlock ();
		}
		private void parseWhile() {
			if (t.word != "while") throw new Exception (tk.errorPrefix() + "'while' expected");
			LoopData ld = new LoopData ();
			ld.continue_pos = code.commands.Count;
			loops.Push (ld);
			t = tk.next ();
			if (t.word != "(") throw new Exception (tk.errorPrefix() + "'(' expected");
			t = tk.next ();
			DataType ct = parseExpression ();
			if (ct == DataType.VOID || ct == DataType.VECTOR)
				throw new Exception (tk.errorPrefix() + "Incorrect condition");
			if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
			t = tk.next ();
			int jz_pos = code.commands.Count;
			code.commands.Add (new BinaryCode.Command (Opcode.JZ));
			parseCommand ();
			code.commands.Add (new BinaryCode.Command (Opcode.JMP, ld.continue_pos));
			loops.Pop ();
			foreach (int i in ld.break_points)
				code.commands [i].i = code.commands.Count;
			code.commands[jz_pos].i = code.commands.Count;
		}

		private DataType parseFunctionCall(string name) {
			if (!functions.ContainsKey(name))
				throw new Exception (tk.errorPrefix() + "Unknown function");
			Function f = functions [name];
			if (t.word != "(") throw new Exception (tk.errorPrefix() + "'(' expected");
			for (int i = 0; i < f.parameters.Count; i++) {
				if (i > 0 && t.word != ",") throw new Exception (tk.errorPrefix() + "',' expected");
				t = tk.next ();
				parseExpression (f.parameters[i]);
			}
			if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
			t = tk.next ();
			if (f.special)
				code.commands.Add (new BinaryCode.Command (Opcode.BCALL, f.address));
			else
				code.commands.Add (new BinaryCode.Command (Opcode.CALL, f.id));
			return f.returnType;
		}
		private void parseFunctionDefinition(string name, string type) {
			DataType rt = DataType.VOID;
			if (type == "int")
				rt = DataType.INT;
			else if (type == "float")
				rt = DataType.FLOAT;
			else if (type == "vector")
				rt = DataType.VECTOR;
			if (t.word != "(") throw new Exception (tk.errorPrefix() + "'(' expected");
			List<DataType> types = new List<DataType> ();
			List<string> names = new List<string> ();
			t = tk.next ();
			while (t.word != ")") {
				DataType dt;
				if (t.word == "int")
					dt = DataType.INT;
				else if (t.word == "float")
					dt = DataType.FLOAT;
				else if (t.word == "vector")
					dt = DataType.VECTOR;
				else throw new Exception (tk.errorPrefix() + "Type expected");
				types.Add (dt);
				t = tk.next ();
				if (t.type == Token.Type.Word) {
					names.Add (t.word);
					t = tk.next ();
				} else
					names.Add ("");
				if (t.word != "," && t.word != ")") throw new Exception (tk.errorPrefix() + "',' or ')' expected");
				if (t.word == ",")
					t = tk.next ();
			}
			Function f;
			if (functions.ContainsKey (name)) {
				f = functions [name];
				if (f.special) throw new Exception (tk.errorPrefix() + "Function with the same name already exists");
				if (f.returnType != rt) throw new Exception (tk.errorPrefix() + "Return type mismatch");
				if (types.Count != f.parameters.Count)
					throw new Exception (tk.errorPrefix() + "Parameters count mismatch");
				for (int i = 0; i < types.Count; i++)
					if (types[i] != f.parameters[i])
						throw new Exception (tk.errorPrefix() + "Parameter type mismatch");
			} else {
				f = new Function ();
				f.returnType = rt;
				f.special = false;
				f.address = -1;
				f.parameters = types;
				f.max_local_data_size = 0;
				f.id = functions_list.Count;
				functions [name] = f;
				functions_list.Add (f);
			}
			t = tk.next ();
			if (t.word == ";") {
				t = tk.next ();
				return;
			}
			if (f.address != -1) throw new Exception (tk.errorPrefix() + "';' expected");
			if (t.word != "{") throw new Exception (tk.errorPrefix() + "';' or '{' expected");
			current_function = f;
			t = tk.next ();
			var lv = new Dictionary<string, BinaryCode.Variable> ();
			local_variables.Add (lv);

			code.commands.Add (new BinaryCode.Command (Opcode.JMP));
			f.address = code.commands.Count;
			code.commands.Add (new BinaryCode.Command (Opcode.MV_SP));

			int laddr = -2;
			for (int i = types.Count-1; i >= 0; i--) {
				if (types [i] == DataType.VECTOR)
					laddr -= 3;
				else
					laddr -= 1;
				BinaryCode.Variable v = new BinaryCode.Variable ();
				v.address = laddr;
				v.builtin = false;
				v.array = false;
				v.arraySize = 1;
				v.name = names [i];
				v.type = types [i];
				v.visible = false;
				lv [v.name] = v;
			}

			parseBlock ();

			switch (f.returnType) {
			case DataType.FLOAT:
			case DataType.INT:
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, 0));
				code.commands.Add (new BinaryCode.Command (Opcode.RETI, current_function.parameters.Count));
				break;
			case DataType.VECTOR:
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, 0));
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, 0));
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, 0));
				code.commands.Add (new BinaryCode.Command (Opcode.RETV, current_function.parameters.Count));
				break;
			case DataType.VOID:
				code.commands.Add (new BinaryCode.Command (Opcode.RET, current_function.parameters.Count));
				break;
			}
			code.commands [f.address - 1].i = code.commands.Count;
			code.commands [f.address].i = f.max_local_data_size;

			deleteLocalVariablesBlock ();
			if (t.word != "}")
				throw new Exception (tk.errorPrefix() + "Unexpected end of file");
			t = tk.next ();
			current_function = null;
		}

		private DataType parseVariable(string name, string ppmm="") {
			bool glob = false;
			BinaryCode.Variable v = null;
			foreach (var d in local_variables) {
				if (d.ContainsKey (name)) {
					v = d [name];
				}
			}
			if (v == null && global_variables.ContainsKey (name)) {
				glob = true;
				v = global_variables [name];
			}
			if (v == null) throw new Exception (tk.errorPrefix() + "Unknown variable");

			bool array = false;
			if (t.word == "[") {
				if (!v.array) throw new Exception (tk.errorPrefix() + "It is not an array");
				t = tk.next ();
				array = true;
				parseExpression (DataType.INT);
				if (t.word != "]") throw new Exception (tk.errorPrefix() + "']' expected");
				t = tk.next ();
			} else if (v.array) throw new Exception (tk.errorPrefix() + "Index required");

			int addr = v.address;
			DataType dt = v.type;
			if (t.word == ".") {
				t = tk.next ();
				string suffix = t.word;
				t = tk.next ();
				if (v.type != DataType.VECTOR) throw new Exception (tk.errorPrefix() + "It is a scalar variable");
				dt = DataType.FLOAT;
				if (suffix == "z")
					addr += 2;
				else if (suffix == "y")
					addr += 1;
				else if (suffix != "x") throw new Exception (tk.errorPrefix() + "Incorrect field");
				if (v.array) {
					code.commands.Add (new BinaryCode.Command (Opcode.CONST, 3));
					code.commands.Add (new BinaryCode.Command (Opcode.MULI));
				}
			}
			Opcode op_write, op_read;
			if (array) {
				if (dt == DataType.VECTOR) {
					op_write = glob ? Opcode.AS3 : Opcode.SAS3;
					op_read = glob ? Opcode.AL3 : Opcode.SAL3;
				} else {
					op_write = glob ? Opcode.AS1 : Opcode.SAS1;
					op_read = glob ? Opcode.AL1 : Opcode.SAL1;
				}
			} else {
				if (dt == DataType.VECTOR) {
					op_write = glob ? Opcode.S3 : Opcode.SS3;
					op_read = glob ? Opcode.L3 : Opcode.SL3;
				} else {
					op_write = glob ? Opcode.S1 : Opcode.SS1;
					op_read = glob ? Opcode.L1 : Opcode.SL1;
				}
			}
			if (t.word == "=") {
				if (ppmm != "") throw new Exception (tk.errorPrefix() + "Bad operation " + ppmm);
				t = tk.next ();
				parseExpression (dt);
				code.commands.Add (new BinaryCode.Command (op_write, addr));
			} else {
				if (t.word == "++" || t.word == "--") {
					if (ppmm != "") throw new Exception (tk.errorPrefix() + "Bad operation " + t.word);
					ppmm = t.word;
				}
				if (array && (ppmm != "" || t.word == "+=" || t.word == "-=" || t.word == "*=" || t.word == "/=" || t.word == "%="))
					code.commands.Add (new BinaryCode.Command (Opcode.DUP));
				code.commands.Add (new BinaryCode.Command (op_read, addr));
				if (ppmm != "") {
					if (dt == DataType.VECTOR)
						throw new Exception (tk.errorPrefix() + "Bad operation " + ppmm + " for vector");
					if (dt == DataType.FLOAT) {
						code.commands.Add (new BinaryCode.Command (Opcode.CONST, 1.0f));
						code.commands.Add (new BinaryCode.Command (ppmm == "++" ? Opcode.ADDF : Opcode.SUBF));
					} else {
						code.commands.Add (new BinaryCode.Command (Opcode.CONST, 1));
						code.commands.Add (new BinaryCode.Command (ppmm == "++" ? Opcode.ADDI : Opcode.SUBI));
					}
					code.commands.Add (new BinaryCode.Command (op_write, addr));
				}
				if (t.word == "++" || t.word == "--") {
					if (dt == DataType.FLOAT) {
						code.commands.Add (new BinaryCode.Command (Opcode.CONST, -1.0f));
						code.commands.Add (new BinaryCode.Command (ppmm == "++" ? Opcode.ADDF : Opcode.SUBF));
					} else {
						code.commands.Add (new BinaryCode.Command (Opcode.CONST, -1));
						code.commands.Add (new BinaryCode.Command (ppmm == "++" ? Opcode.ADDI : Opcode.SUBI));
					}
					t = tk.next ();
				} else if (t.word == "+=" || t.word == "-=" || t.word == "*=" || t.word == "/=" || t.word == "%=") {
					if (ppmm != "") throw new Exception (tk.errorPrefix() + "Bad operation " + t.word);
					Opcode opcode = Opcode.MOD;
					switch (dt) {
					case DataType.FLOAT:
						if (t.word == "+=") opcode = Opcode.ADDF;
						if (t.word == "-=") opcode = Opcode.SUBF;
						if (t.word == "*=") opcode = Opcode.MULF;
						if (t.word == "/=") opcode = Opcode.DIVF;
						break;
					case DataType.INT:
						if (t.word == "+=") opcode = Opcode.ADDI;
						if (t.word == "-=") opcode = Opcode.SUBI;
						if (t.word == "*=") opcode = Opcode.MULI;
						if (t.word == "/=") opcode = Opcode.DIVI;
						break;
					case DataType.VECTOR:
						if (t.word == "+=") opcode = Opcode.ADDV;
						if (t.word == "-=") opcode = Opcode.SUBV;
						if (t.word == "*=") opcode = Opcode.MULV;
						if (t.word == "/=") opcode = Opcode.DIVV;
						break;
					}
					if ((dt == DataType.FLOAT || dt == DataType.VECTOR) && t.word == "%=")
						throw new Exception (tk.errorPrefix() + "Bad operation " + t.word);
					if (dt == DataType.VECTOR && (t.word == "*=" || t.word == "/="))
						dt = DataType.FLOAT;
					t = tk.next ();
					parseExpression (dt);

					code.commands.Add (new BinaryCode.Command (opcode));
					code.commands.Add (new BinaryCode.Command (op_write, addr));
				}
			}

			return dt;
		}

		private void parseField() {
			t = tk.next ();
			if (t.word == "x") code.commands.Add (new BinaryCode.Command (Opcode.VX));
			else if (t.word == "y") code.commands.Add (new BinaryCode.Command (Opcode.VY));
			else if (t.word == "z") code.commands.Add (new BinaryCode.Command (Opcode.VZ));
			else throw new Exception (tk.errorPrefix() + "Incorrect field");
			t = tk.next ();
		}

		private DataType parsePriority0() {
			switch (t.type) {
			case Token.Type.FloatNumber:
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, t.f));
				t = tk.next ();
				return DataType.FLOAT;
			case Token.Type.IntegerNumber:
				code.commands.Add (new BinaryCode.Command (Opcode.CONST, t.i));
				t = tk.next ();
				return DataType.INT;
			case Token.Type.Word: {
					string name = t.word;
					t = tk.next ();
					if (t.word == "(") {
						DataType dt = parseFunctionCall (name);
						if (t.word == ".") {
							if (dt != DataType.VECTOR) throw new Exception (tk.errorPrefix() + "It is a scalar variable");
							parseField ();
							return DataType.FLOAT;
						} else
							return dt;
					} else
						return parseVariable (name);
				}
			default:
				if (t.word == "(") {
					t = tk.next ();
					if (t.word == "int" || t.word == "float" || t.word == "vector") {
						DataType dt = DataType.FLOAT;
						if (t.word == "int") dt = DataType.INT;
						if (t.word == "vector") dt = DataType.VECTOR;
						t = tk.next ();
						if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
						t = tk.next ();
						DataType dt_src = parsePriority0m ();
						if (dt == dt_src)
							return dt;
						if (dt == DataType.VECTOR || dt_src == DataType.VECTOR || dt_src == DataType.VOID)
							throw new Exception (tk.errorPrefix() + "Invalid conversion");
						code.commands.Add (new BinaryCode.Command (dt == DataType.FLOAT ? Opcode.I2F : Opcode.F2I));
						return dt;
					} else {
						DataType dt = parseExpression ();
						if (t.word != ")") throw new Exception (tk.errorPrefix() + "')' expected");
						t = tk.next ();
						if (t.word == ".") {
							if (dt != DataType.VECTOR) throw new Exception (tk.errorPrefix() + "It is a scalar variable");
							parseField ();
							return DataType.FLOAT;
						} else
							return dt;
					}
				} else if (t.word == "++" || t.word == "--") {
					string ppmm = t.word;
					t = tk.next ();
					if (t.type != Token.Type.Word) throw new Exception (tk.errorPrefix() + "Variable expected");
					string name = t.word;
					t = tk.next ();
					return parseVariable (name, ppmm);
				} else throw new Exception (tk.errorPrefix() + "Value expected");
			}
		}

		private DataType parsePriority0m() {
			bool inv = t.word == "-";
			if (inv) t = tk.next ();
			bool not = t.word == "!";
			if (not) t = tk.next ();
			DataType dt = parsePriority0 ();
			if (not) {
				if (dt == DataType.FLOAT) {
					code.commands.Add (new BinaryCode.Command (Opcode.F2I));
					dt = DataType.INT;
				}
				if (dt != DataType.INT)
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
				code.commands.Add (new BinaryCode.Command (Opcode.NOT));
			}
			if (inv) {
				switch (dt) {
				case DataType.INT:
					code.commands.Add (new BinaryCode.Command (Opcode.NEGI));
					break;
				case DataType.FLOAT:
					code.commands.Add (new BinaryCode.Command (Opcode.NEGF));
					break;
				case DataType.VECTOR:
					code.commands.Add (new BinaryCode.Command (Opcode.NEGV));
					break;
				default:
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
				}
			}
			return dt;
		}

		private DataType parsePriority1() {
			DataType dt = parsePriority0m ();
			while (t.word == "*" || t.word == "/" || t.word == "%") {
				string op = t.word;
				t = tk.next ();
				DataType dt2 = parsePriority0m ();
				if (dt == DataType.VOID || dt2 == DataType.VOID || dt2 == DataType.VECTOR)
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
				else if (dt == DataType.VECTOR) {
					if (op == "%" || (dt2 != DataType.INT && dt2 != DataType.FLOAT))
						throw new Exception (tk.errorPrefix () + "Incorrect operation");
					if (dt2 == DataType.INT)
						code.commands.Add (new BinaryCode.Command (Opcode.I2F));
					code.commands.Add (new BinaryCode.Command (Opcode.MULV));
				} else if (dt == DataType.FLOAT) {
					if (dt2 != DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.I2F));
					if (op == "*") code.commands.Add (new BinaryCode.Command (Opcode.MULF));
					else if (op == "/") code.commands.Add (new BinaryCode.Command (Opcode.DIVF));
					else throw new Exception (tk.errorPrefix () + "Incorrect operation");
				} else if (dt == DataType.INT) {
					if (op == "%") {
						if (dt2 != DataType.INT)
							throw new Exception (tk.errorPrefix () + "Incorrect operation");
						code.commands.Add (new BinaryCode.Command (Opcode.MOD));
					} else if (op == "*") {
						if (dt2 == DataType.INT)
							code.commands.Add (new BinaryCode.Command (Opcode.MULI));
						else {
							code.commands.Add (new BinaryCode.Command (Opcode.DI2F));
							code.commands.Add (new BinaryCode.Command (Opcode.MULF));
							dt = DataType.FLOAT;
						}
					} else {
						if (dt2 == DataType.INT)
							code.commands.Add (new BinaryCode.Command (Opcode.DIVI));
						else {
							code.commands.Add (new BinaryCode.Command (Opcode.DI2F));
							code.commands.Add (new BinaryCode.Command (Opcode.DIVF));
							dt = DataType.FLOAT;
						}
					}
				} else throw new Exception (tk.errorPrefix () + "Unknown type");
			}
			return dt;
		}

		private DataType parsePriority2() {
			DataType dt = parsePriority1 ();
			while (t.word == "+" || t.word == "-") {
				string op = t.word;
				t = tk.next ();
				DataType dt2 = parsePriority1 ();
				if (dt == DataType.VOID || dt2 == DataType.VOID)
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
				if (dt != dt2) {
					if (dt == DataType.VECTOR || dt2 == DataType.VECTOR)
						throw new Exception (tk.errorPrefix () + "Incorrect operation");
					if (dt != DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.DI2F));
					if (dt2 != DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.I2F));
					dt = DataType.FLOAT;
				}
				if (dt == DataType.INT)
					code.commands.Add (new BinaryCode.Command (op == "+" ? Opcode.ADDI : Opcode.SUBI));
				else if (dt == DataType.FLOAT)
					code.commands.Add (new BinaryCode.Command (op == "+" ? Opcode.ADDF : Opcode.SUBF));
				else if (dt == DataType.VECTOR)
					code.commands.Add (new BinaryCode.Command (op == "+" ? Opcode.ADDV : Opcode.SUBV));
				else throw new Exception (tk.errorPrefix () + "Unknown type");
			}
			return dt;
		}

		private DataType parsePriority3() {
			DataType dt = parsePriority2 ();
			string op = t.word;
			if (!op.Contains ("<") && !op.Contains (">") && op != "==" && op != "!=")
				return dt;
			t = tk.next ();
			DataType dt2 = parsePriority2 ();
			if (dt == DataType.VOID || dt2 == DataType.VOID)
				throw new Exception (tk.errorPrefix () + "Incorrect operation");
			if (dt != dt2) {
				if (dt == DataType.VECTOR || dt2 == DataType.VECTOR)
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
				if (dt != DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.DI2F));
				if (dt2 != DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.I2F));
				dt = DataType.FLOAT;
			}
			if (dt == DataType.VECTOR) {
				if (op == "==" || op == "!=") code.commands.Add (new BinaryCode.Command (Opcode.EQV));
				else throw new Exception (tk.errorPrefix () + "Incorrect operation");
			} else if (dt == DataType.INT) {
				if (op == "==" || op == "!=") code.commands.Add (new BinaryCode.Command (Opcode.EQ));
				else if (op == "<" || op == ">=") code.commands.Add (new BinaryCode.Command (Opcode.LTI));
				else if (op == "<=" || op == ">") code.commands.Add (new BinaryCode.Command (Opcode.LEQI));
				else throw new Exception (tk.errorPrefix () + "Incorrect operation");
			} else {
				if (op == "==" || op == "!=") code.commands.Add (new BinaryCode.Command (Opcode.EQ));
				else if (op == "<" || op == ">=") code.commands.Add (new BinaryCode.Command (Opcode.LTF));
				else if (op == "<=" || op == ">") code.commands.Add (new BinaryCode.Command (Opcode.LEQF));
				else throw new Exception (tk.errorPrefix () + "Incorrect operation");
			}
			if (op == "!=" || op == ">=" || op == ">")
				code.commands.Add (new BinaryCode.Command (Opcode.NOT));
			return DataType.INT;
		}

		private DataType parsePriority4() {
			DataType dt = parsePriority3 ();
			string op = t.word;
			if (op != "&&" && op != "||")
				return dt;
			if (dt == DataType.VOID || dt == DataType.VECTOR)
				throw new Exception (tk.errorPrefix () + "Incorrect operation");
			List<int> jz_pos = new List<int> ();
			while (t.word == op) {
				if (op == "||") code.commands.Add (new BinaryCode.Command (Opcode.NOT));
				jz_pos.Add (code.commands.Count);
				code.commands.Add (new BinaryCode.Command (Opcode.JZ));
				t = tk.next ();
				DataType dt2 = parsePriority3 ();
				if (dt2 == DataType.VOID || dt2 == DataType.VECTOR)
					throw new Exception (tk.errorPrefix () + "Incorrect operation");
			}
			if (op == "||") code.commands.Add (new BinaryCode.Command (Opcode.NOT));
			jz_pos.Add (code.commands.Count);
			code.commands.Add (new BinaryCode.Command (Opcode.JZ));

			code.commands.Add (new BinaryCode.Command (Opcode.CONST, op == "&&" ? 1 : 0));
			int jmp_pos = code.commands.Count;
			code.commands.Add (new BinaryCode.Command (Opcode.JMP));

			foreach (int p in jz_pos)
				code.commands [p].i = code.commands.Count;
			code.commands.Add (new BinaryCode.Command (Opcode.CONST, op == "&&" ? 0 : 1));
			code.commands [jmp_pos].i = code.commands.Count;

			return dt;
		}

		private DataType parseExpression(DataType requiredType = DataType.VOID) {
			if (t.word == ";") throw new Exception (tk.errorPrefix() + "Expression expected");
			DataType dt = parsePriority4 ();
			if (t.word == "?") {
				t = tk.next ();
				if (dt == DataType.VECTOR || dt == DataType.VOID) throw new Exception (tk.errorPrefix() + "Incorrect condition");
				int jz_pos = code.commands.Count;
				code.commands.Add (new BinaryCode.Command (Opcode.JZ));
				DataType d1 = parsePriority4 ();
				int jmp_pos = code.commands.Count;
				code.commands.Add (new BinaryCode.Command (Opcode.JMP));
				code.commands[jz_pos].i = code.commands.Count;
				if (t.word != ":") throw new Exception (tk.errorPrefix() + "':' expected");
				t = tk.next ();
				DataType d2 = parsePriority4 ();
				if (d1 != d2) throw new Exception (tk.errorPrefix() + "Type mismatch: in (A ? B : C) type of B and C should be the same");
				code.commands[jmp_pos].i = code.commands.Count;
				dt = d1;
			}
			if (dt != requiredType && requiredType != DataType.VOID) {
				if (requiredType == DataType.VECTOR || dt == DataType.VECTOR || dt == DataType.VOID)
					throw new Exception (tk.errorPrefix() + "Incorrect type");
				if (dt == DataType.FLOAT) code.commands.Add (new BinaryCode.Command (Opcode.F2I));
				else code.commands.Add (new BinaryCode.Command (Opcode.I2F));
			}
			return dt;
		}

		public BinaryCode compile(string source) {
			loops.Clear();
			functions.Clear();
			functions_list.Clear ();
			current_function = null;
			global_variables.Clear ();
			local_variables.Clear();
			global_data_size = (int)BuiltinVariable.BUILTIN_END;
			local_data_size = 0;
			max_local_data_size = 0;

			addSpecialFunction(BuiltinFunction.VECTOR, DataType.VECTOR, "vector",
				new List<DataType>{DataType.FLOAT, DataType.FLOAT, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.LENGTH, DataType.FLOAT, "length",
				new List<DataType>{DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.NORMALIZE, DataType.VECTOR, "normalize",
				new List<DataType>{DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.DOT, DataType.FLOAT, "dot",
				new List<DataType>{DataType.VECTOR, DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.CROSS, DataType.FLOAT, "cross",
				new List<DataType>{DataType.VECTOR, DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.ROTATE_RIGHT, DataType.VECTOR, "rotateRight",
				new List<DataType>{DataType.VECTOR, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ROTATE_UP, DataType.VECTOR, "rotateUp",
				new List<DataType>{DataType.VECTOR, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ANGLE_HORIZONTAL, DataType.FLOAT, "angleHorizontal",
				new List<DataType>{DataType.VECTOR, DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.ANGLE_VERTICAL, DataType.FLOAT, "angleVertical",
				new List<DataType>{DataType.VECTOR, DataType.VECTOR});

			addSpecialFunction(BuiltinFunction.ABS, DataType.FLOAT, "abs", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.SQRT, DataType.FLOAT, "sqrt", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.SIN, DataType.FLOAT, "sin", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.COS, DataType.FLOAT, "cos", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ASIN, DataType.FLOAT, "asin", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ACOS, DataType.FLOAT, "acos", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.EXP, DataType.FLOAT, "exp", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.LOG, DataType.FLOAT, "log", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ROUND, DataType.INT, "round", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.FLOOR, DataType.INT, "floor", new List<DataType>{DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.CEIL, DataType.INT, "ceil", new List<DataType>{DataType.FLOAT});

			addSpecialFunction(BuiltinFunction.MIN, DataType.FLOAT, "min",
				new List<DataType>{DataType.FLOAT, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.MAX, DataType.FLOAT, "max",
				new List<DataType>{DataType.FLOAT, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.ATAN2, DataType.FLOAT, "atan2",
				new List<DataType>{DataType.FLOAT, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.RANDOM_FLOAT, DataType.FLOAT, "randomFloat",
				new List<DataType>{DataType.FLOAT, DataType.FLOAT});
			addSpecialFunction(BuiltinFunction.RANDOM_INT, DataType.INT, "randomInt",
				new List<DataType>{DataType.INT, DataType.INT});

			addSpecialFunction(BuiltinFunction.SCAN_OBSTACLE, DataType.FLOAT, "scanObstacle",
				new List<DataType>{DataType.VECTOR});
			addSpecialFunction(BuiltinFunction.OBJ_TYPE, DataType.INT, "objType",
				new List<DataType>{DataType.INT});
			addSpecialFunction(BuiltinFunction.OBJ_DISTANCE, DataType.FLOAT, "objDistance",
				new List<DataType>{DataType.INT});
			addSpecialFunction(BuiltinFunction.OBJ_POSITION, DataType.VECTOR, "objPosition",
				new List<DataType>{DataType.INT});
			addSpecialFunction(BuiltinFunction.OBJ_VELOCITY, DataType.VECTOR, "objVelocity",
				new List<DataType>{DataType.INT});
			addSpecialFunction(BuiltinFunction.OBJ_DIRECTION, DataType.VECTOR, "objDirection",
				new List<DataType>{DataType.INT});
			addSpecialFunction(BuiltinFunction.OBJ_GUN_DIRECTION, DataType.VECTOR, "objGunDirection",
				new List<DataType>{DataType.INT});

			addBuiltinVariable (BuiltinVariable.LIVES, "lives", DataType.INT);
			addBuiltinVariable (BuiltinVariable.BULLETS, "bullets", DataType.INT);
			addBuiltinVariable (BuiltinVariable.ROCKETS, "rockets", DataType.INT);
			addBuiltinVariable (BuiltinVariable.OBJ_COUNT, "visibleObjectsCount", DataType.INT);

			addBuiltinVariable (BuiltinVariable.MOVE, "move", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.TURN_CORPUS, "turnCorpus", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.TURN_TURRET, "turnTurret", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.TURN_GUN, "turnGun", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.FIRE1, "fire1", DataType.INT);
			addBuiltinVariable (BuiltinVariable.FIRE2, "fire2", DataType.INT);

			addBuiltinVariable (BuiltinVariable.JUST_STARTED, "justStarted", DataType.INT);
			addBuiltinVariable (BuiltinVariable.JUST_SPAWNED, "justSpawned", DataType.INT);
			addBuiltinVariable (BuiltinVariable.DELTA_TIME, "deltaTime", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.RUN_TIME, "runTime", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.RUNTIME_ERROR, "runtimeError", DataType.INT);
			addBuiltinVariable (BuiltinVariable.COLLISION, "collision", DataType.INT);
			addBuiltinVariable (BuiltinVariable.TOTAL_BOTS_COUNT, "totalBotsCount", DataType.INT);
			addBuiltinVariable (BuiltinVariable.GRAVITY, "gravity", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.FIRING_VELOCITY, "firingVelocity", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.REMAINING_TIME, "remainingTime", DataType.FLOAT);
			addBuiltinVariable (BuiltinVariable.OBSTACLE_DISTANCE, "obstacleDistance", DataType.FLOAT);

			addBuiltinVariable (BuiltinVariable.POSITION, "position", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.GUN_POSITION, "gunPosition", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.VELOCITY, "velocity", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.CORPUS_DIRECTION, "corpusDirection", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.GUN_DIRECTION, "gunDirection", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.UPWARD_DIRECTION, "upwardDirection", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.COLLISION_POSITION, "collisionPosition", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.OBSTACLE_POSITION, "obstaclePosition", DataType.VECTOR);
			addBuiltinVariable (BuiltinVariable.OBSTACLE_NORMAL, "obstacleNormal", DataType.VECTOR);

			tk = new TokenParser (source);
			code = new BinaryCode ();
			code.commands.Add (new BinaryCode.Command (Opcode.MV_SP));
			t = tk.next ();
			parseBlock ();
			code.commands [0].i = max_local_data_size;
			if (t.type != Token.Type.EOF)
				throw new Exception (tk.errorPrefix() + "Unexpected '}'");
			foreach (var c in code.commands) {
				if (c.opcode == Opcode.CALL)
					c.i = functions_list [c.i].address;
			}
			if (global_data_size > EvilRuntime.DATA_SIZE)
				throw new Exception ("Memory limit exceeded");
			return code;
		}
	}
}
