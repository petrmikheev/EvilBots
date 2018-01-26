using System;
using System.Collections;
using System.Collections.Generic;

namespace EvilC {
	public class TokenParser {
		private string code;
		private int pos, pos_before;
		private int line;
		private int lineBegin;
		private enum State {Before, LineComment, BlockComment};
		private Dictionary<string, string> defines = new Dictionary<string, string>();
		private TokenParser itk = null;

		public TokenParser (string code) {
			this.code = code;
			this.pos = 0;
			this.line = 0;
			this.lineBegin = 0;
		}

		public int getLine() { return line + 1; }
		public int getColumn() { return pos_before - lineBegin + 1; }
		public string errorPrefix() {
			return string.Format("Error ({0}:{1}): ", getLine(), getColumn());
		}

		public Token next() {
			State state = State.Before;
			while (true) {
				if (itk != null) {
					Token t = itk.next ();
					if (t.type == Token.Type.EOF)
						itk = null;
					else
						return t;
				}
				if (pos >= code.Length)
					return new Token (Token.Type.EOF);
				char c = code [pos];
				if (c == '\n') {
					line++;
					lineBegin = pos + 1;
				}
				string c2;
				if (pos < code.Length - 1)
					c2 = code.Substring (pos, 2);
				else
					c2 = "  ";
				pos_before = pos;
				switch (state) {
				case State.LineComment:
					pos++;
					if (c == '\n') state = State.Before;
					break;
				case State.BlockComment:
					if (c2 == "*/") {
						state = State.Before;
						pos += 2;
					} else
						pos++;
					break;
				case State.Before:
					if (c == '#') {
						int sp_pos = pos + 1;
						while (sp_pos < code.Length && code [sp_pos] != ' ' && code [sp_pos] != '\t' && code [sp_pos] != '\n')
							sp_pos++;
						if (code.Substring (pos, sp_pos - pos) != "#define")
							throw new Exception (errorPrefix () + "Only #define allowed");
						pos = sp_pos;
						while (sp_pos < code.Length && code [sp_pos] != '\n' && code [sp_pos] != '\r') 
							sp_pos++;
						string s = code.Substring (pos, sp_pos - pos).Trim ();
						pos = sp_pos;
						sp_pos = s.IndexOf (' ');
						if (s.Length == 0 || sp_pos == -1) throw new Exception (errorPrefix () + "Expected #define KEY VALUE");
						string key = s.Substring (0, sp_pos);
						string value = s.Substring (sp_pos).Trim();
						if (!char.IsLetter(key[0]) && key[0] != '_') throw new Exception (errorPrefix () + "Incorrect #define");
						foreach (char ch in key)
							if (!char.IsLetterOrDigit(ch) && ch != '_') throw new Exception (errorPrefix () + "Incorrect #define");
						defines [key] = value;
						break;
					}
					if (c == ' ' || c == '\t' || c == '\r' || c == '\n') {
						pos++;
						break;
					}
					if (c2 == "/*") {
						state = State.BlockComment;
						pos += 2;
						break;
					}
					if (c2 == "//") {
						state = State.LineComment;
						pos += 2;
						break;
					}
					if (c2 == "+=" || c2 == "-=" ||
					    c2 == "*=" || c2 == "/=" ||
					    c2 == "++" || c2 == "--" ||
					    c2 == ">=" || c2 == "<=" ||
						c2 == "==" || c2 == "!=" ||
						c2 == "&&" || c2 == "||" ||
						c2 == "%=") {
						pos += 2;
						return new Token (Token.Type.Special, c2);
					}
					if (char.IsDigit (c) || (c=='.'&& char.IsDigit(c2[1]))) {
						int epos = pos + 1;
						while (epos < code.Length && (char.IsDigit (code [epos]) || code [epos] == '.'))
							epos++;
						if (epos < code.Length && (code [epos] == 'E' || code [epos] == 'e')) {
							epos++;
							if (code [epos] == '+' || code [epos] == '-')
								epos++;
							while (epos < code.Length && char.IsDigit (code [epos]))
								epos++;
						} else try {
							string s = code.Substring(pos, epos-pos);
							int i = int.Parse(s);
							pos = epos;
							return new Token(i);
						} catch (Exception) {}
						try {
							string s = code.Substring(pos, epos-pos);
							float f = float.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
							pos = epos;
							return new Token(f);
						} catch (Exception) {
							throw new Exception (errorPrefix() + "Incorrect number");
						}
					}
					if ("[](){}.,+-*/=%;!<>?:".Contains (""+c)) {
						pos ++;
						return new Token (Token.Type.Special, ""+c);
					}
					if (char.IsLetter (c) || c == '_') {
						int fpos = pos;
						while (pos < code.Length && (char.IsLetterOrDigit (code [pos]) || code [pos] == '_'))
							pos++;
						string word = code.Substring (fpos, pos - fpos);
						if (defines.ContainsKey (word)) {
							itk = new TokenParser (defines [word]);
							continue;
						}
						if (word == "bool") return new Token (Token.Type.Word, "int");
						if (word == "M_PI") return new Token (3.1415926535f);
						if (word == "M_E") return new Token (2.71828182845f);
						if (word == "true") return new Token (1);
						if (word == "false") return new Token (0);
						if (word == "NONE") return new Token (0);
						if (word == "ENEMY") return new Token (1);
						if (word == "BULLET_BONUS") return new Token (2);
						if (word == "ROCKET_BONUS") return new Token (3);
						if (word == "REPAIR_BONUS") return new Token (4);
						if (word == "BULLET") return new Token (5);
						if (word == "ROCKET") return new Token (6);
						if (word == "EXPLOSION") return new Token (7);
						return new Token (Token.Type.Word, word);
					}
					throw new Exception (errorPrefix() + "Unexpected char");
				}
			}
		}
	}
}
