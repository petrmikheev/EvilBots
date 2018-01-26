using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace EvilC {
	public enum Opcode : byte {
		CALL = 0, BCALL = 1, RET = 2, RETI = 3, RETV = 4, JMP = 5, JZ = 6, MV_SP = 7, CONST = 8, DUP = 9,
		L1 = 10, L3 = 11, S1 = 12, S3 = 13, AL1 = 14, AL3 = 15, AS1 = 16, AS3 = 17,
		SL1 = 18, SL3 = 19, SS1 = 20, SS3 = 21, SAL1 = 22, SAL3 = 23, SAS1 = 24, SAS3 = 25,
		VX = 26, VY = 27, VZ = 28, F2I = 29, I2F = 30, DI2F = 31,
		ADDI = 32, ADDF = 33, ADDV = 34, SUBI = 35, SUBF = 36, SUBV = 37, MULI = 38, MULF = 39, MULV = 40,
		MOD = 41, DIVI = 42, DIVF = 43, DIVV = 44, NEGI = 45, NEGF = 46, NEGV = 47, NOT = 48,
		EQ = 49, EQV = 50, LTI = 51, LTF = 52, LEQI = 53, LEQF = 54
	}
	public enum BuiltinFunction {
		VECTOR = 0, LENGTH = 1, NORMALIZE = 2, DOT = 3, CROSS = 4,
		ROTATE_RIGHT = 5, ROTATE_UP = 6, ANGLE_HORIZONTAL = 7, ANGLE_VERTICAL = 8,
		ABS = 9, MIN = 10, MAX = 11, ROUND = 12, FLOOR = 13, CEIL = 14, SQRT = 15, SIN = 16, COS = 17,
		ASIN = 18, ACOS = 19, ATAN2 = 20, EXP = 21, LOG = 22, RANDOM_FLOAT = 23, RANDOM_INT = 24,
		SCAN_OBSTACLE = 50, OBJ_TYPE = 51, OBJ_POSITION = 52, OBJ_VELOCITY = 53, OBJ_DISTANCE = 54,
		OBJ_DIRECTION = 55, OBJ_GUN_DIRECTION = 56
	}
	public enum BuiltinVariable {
		MOVE = 0, TURN_CORPUS = 1, TURN_TURRET = 2, TURN_GUN = 3, FIRE1 = 4, FIRE2 = 5,
		JUST_STARTED = 6, JUST_SPAWNED = 7, DELTA_TIME = 8, RUNTIME_ERROR = 9,
		LIVES = 10, BULLETS = 11, ROCKETS = 12, POSITION = 13, GUN_POSITION = 16, VELOCITY = 19,
		CORPUS_DIRECTION = 22, GUN_DIRECTION = 25, UPWARD_DIRECTION = 28,
		COLLISION = 31, COLLISION_POSITION = 32, GRAVITY = 35, FIRING_VELOCITY = 36,
		REMAINING_TIME = 37, TOTAL_BOTS_COUNT = 38,
		OBSTACLE_DISTANCE = 39, OBSTACLE_POSITION = 40, OBSTACLE_NORMAL = 43,
		RUN_TIME = 46, OBJ_COUNT = 47,

		BUILTIN_END = 48
	}

	public enum DataType { VOID = 0, INT = 1, FLOAT = 2, VECTOR = 3 }

	public class BinaryCode {

		public class Command {
			public Opcode opcode;
			public int i;
			public float f;

			public Command(Opcode opcode) {
				this.opcode = opcode;
				this.i = 0;
				this.f = 0;
			}
			public Command(Opcode opcode, int i) {
				this.opcode = opcode;
				this.i = i;
				this.f = 0;
			}
			public Command(Opcode opcode, float f) {
				this.opcode = opcode;
				this.i = 0;
				this.f = f;
			}
			public Command(BinaryReader r) {
				opcode = (Opcode)r.ReadByte();
				i = r.ReadInt32();
				f = 0;
			}
			public override string ToString() {
				if (i != 0)
					return opcode.ToString () + " " + i;
				else if (f != 0)
					return opcode.ToString () + " " + f;
				else
					return opcode.ToString ();
			}
			public void writeToStream(BinaryWriter w) {
				w.Write ((byte)opcode);
				if (i != 0)
					w.Write ((Int32)i);
				else
					w.Write (f);
			}
		}

		public class Variable {
			public string name;
			public int address;
			public DataType type;
			public bool array;
			public int arraySize;
			public bool builtin;
			public bool visible;
			public void writeToStream(BinaryWriter w) {
				w.Write (name);
				w.Write ((Int32)address);
				w.Write ((byte)type);
				w.Write ((Int32)(array ? arraySize : 0));
			}
			public Variable() {}
			public Variable(BinaryReader r) {
				name = r.ReadString ();
				address = r.ReadInt32 ();
				type = (DataType)r.ReadByte ();
				arraySize = r.ReadInt32 ();
				array = arraySize > 0;
				builtin = false;
				visible = true;
			}
		}
		public string model = "";
		public string author = "";
		public string name = "";
		public List<Command> commands = new List<Command>();
		public List<Variable> visibleVariables = new List<Variable> ();

		public BinaryCode () {}

		public BinaryCode (BinaryReader r) {
			name = r.ReadString ();
			author = r.ReadString ();
			model = r.ReadString ();
			int commands_count = r.ReadInt32 ();
			for (int i = 0; i < commands_count; i++)
				commands.Add (new Command (r));
			int variables_count = r.ReadInt32 ();
			for (int i = 0; i < variables_count; i++)
				visibleVariables.Add (new Variable (r));
		}

		public void writeToStream(BinaryWriter w) {
			w.Write (name);
			w.Write (author);
			w.Write (model);
			w.Write ((Int32)commands.Count);
			foreach (Command c in commands)
				c.writeToStream (w);
			w.Write ((Int32)visibleVariables.Count);
			foreach (Variable v in visibleVariables)
				v.writeToStream (w);
		}

		public void print() {
			for (int i = 0; i < commands.Count; ++i) {
				Console.WriteLine (string.Format("{0}: {1}", i, commands[i]));
			}
		}
			
	}
}
