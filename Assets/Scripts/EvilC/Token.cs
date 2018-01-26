using System;

namespace EvilC {

	public struct Token {
		public enum Type { EOF, Word, IntegerNumber, FloatNumber, Special }

		public Type type;
		public string word;
		public float f;
		public int i;

		public Token (Type type) {
			this.type = type;
			this.word = "";
			i = 0; f = 0;
			#if TOKEN_DEBUG
			Console.WriteLine (this);
			#endif
		}

		public Token (Type type, string word) {
			this.type = type;
			this.word = word;
			i = 0; f = 0;
			#if TOKEN_DEBUG
			Console.WriteLine (this);
			#endif
		}

		public Token (float v) {
			this.type = Type.FloatNumber;
			this.word = "";
			i = 0; f = v;
			#if TOKEN_DEBUG
			Console.WriteLine (this);
			#endif
		}

		public Token (int v) {
			this.type = Type.IntegerNumber;
			this.word = "";
			i = v; f = 0;
			#if TOKEN_DEBUG
			Console.WriteLine (this);
			#endif
		}

		public override string ToString ()
		{
			switch (type) {
			case Type.Word:
				return String.Format (" W_{0} ", word);
			case Type.IntegerNumber:
				return String.Format (" I_{0} ", i);
			case Type.FloatNumber:
				return String.Format (" F_{0} ", f);
			case Type.Special:
				return String.Format (" {0} ", word);
			case Type.EOF:
				return "EOF";
			default: return "UNKNOWN";
			}
		}
	}
}