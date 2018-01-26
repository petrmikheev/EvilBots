using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;

namespace EvilC
{
	public class EvilRuntime {
		public const int DATA_SIZE = 16 * 1024 * 1024;
		public const int STACK_SIZE = 16 * 1024 * 1024;
		public const int MAX_INSTRUCTIONS_PER_FRAME = 2048;

		[StructLayout(LayoutKind.Explicit)]
		public struct Value {
			[FieldOffset(0)]
			public int i;
			[FieldOffset(0)]
			public float f;
		}

		public void setInt(BuiltinVariable addr, int v) {
			data [(int)addr].i = v;
		}
		public int getInt(BuiltinVariable addr) {
			return data [(int)addr].i;
		}
		public void setFloat(BuiltinVariable addr, float v) {
			data [(int)addr].f = v;
		}
		public float getFloat(BuiltinVariable addr) {
			return data [(int)addr].f;
		}
		public void setVector(BuiltinVariable addr, float x, float y, float z) {
			data [(int)addr].f = x;
			data [(int)addr+1].f = y;
			data [(int)addr+2].f = z;
		}

		private BinaryCode code;
		public Value[] data = new Value[DATA_SIZE + 3];
		private Value[] stack = new Value[STACK_SIZE + 3];
		private int ip = 0; // instruction pointer
		private int sp = 0; // stack pointer
		private int lp = 0; // local variables pointer
		public bool runtimeError = false;
		public float loadAverage = 0;
		private Random rnd = new Random();

		public EvilRuntime (BinaryCode code) {
			this.code = code;
			for (int i = 0; i < DATA_SIZE; i++)
				data [i].i = 0;
		}

		public string showVariables() {
			string res = "";
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			foreach (var v in code.visibleVariables) {
				string s = v.name + " = ";
				if (v.array)
					s += "[";
				if (v.type == DataType.FLOAT)
					s += data [v.address].f;
				else if (v.type == DataType.INT)
					s += data [v.address].i;
				else if (v.type == DataType.VECTOR)
					s += string.Format ("({0}, {1}, {2})", data [v.address].f, data [v.address+1].f, data [v.address+2].f);
				if (v.array) {
					for (int i = 1; i < v.arraySize; i++) {
						s += ", ";
						if (v.type == DataType.FLOAT)
							s += data [v.address+i].f.ToString();
						else if (v.type == DataType.INT)
							s += data [v.address+i].i;
						else if (v.type == DataType.VECTOR)
							s += string.Format ("({0}, {1}, {2})", data [v.address+i*3].f, data [v.address+i*3+1].f, data [v.address+i*3+2].f);
					}
					s += "]";
				}
				res += s + "\n";
			}
			return res;
		}

		public bool isOnStart() {
			return ip == 0;
		}

		public void restart() {
			ip = 0;
			sp = 0;
			lp = 0;
		}

		public virtual float scanObstacle(float x, float y, float z) {
			return 1e9f;
		}

		public virtual int objType(int i) {
			return 0;
		}

		public virtual float objDistance(int i) {
			return 1e9f;
		}

		public virtual void objPosition(int i, out float x, out float y, out float z) { x = y = z = 0; }
		public virtual void objVelocity(int i, out float x, out float y, out float z) { x = y = z = 0; }
		public virtual void objDirection(int i, out float x, out float y, out float z) { x = y = z = 0; }
		public virtual void objGunDirection(int i, out float x, out float y, out float z) { x = y = z = 0; }

		public bool run(int max_instructions = MAX_INSTRUCTIONS_PER_FRAME) {
			runtimeError = false;
			loadAverage = 0;
			if (code.commands.Count == 0)
				return true;
			if (ip >= code.commands.Count) {
				ip = 0;
				sp = 0;
				lp = 0;
			}
			int scans_count = 0;
			int n;
			for (n = 0; n < max_instructions; n++) {
				#if EVIL_RUNTIME_DEBUG
				string s = "ip=" + ip + " sp=" + sp;
				for (int i = 15; i >= 1; --i)
					if (sp-i>=0)
						s += "  " + stack [sp - i].i;
				Console.WriteLine (s);
				#endif
				BinaryCode.Command c = code.commands [ip++];
				switch (c.opcode) {
				case Opcode.CALL:
					stack [sp++].i = ip;
					stack [sp++].i = lp;
					ip = c.i;
					lp = sp;
					break;
				case Opcode.BCALL:
					switch ((BuiltinFunction)c.i) {
					case BuiltinFunction.VECTOR:
						break;
					case BuiltinFunction.LENGTH:
						{
							float vx = stack [sp - 3].f;
							float vy = stack [sp - 2].f;
							float vz = stack [sp - 1].f;
							stack [sp - 3].f = (float)Math.Sqrt (vx * vx + vy * vy + vz * vz);
							sp -= 2;
						} break;
					case BuiltinFunction.NORMALIZE:
						{
							float vx = stack [sp - 3].f;
							float vy = stack [sp - 2].f;
							float vz = stack [sp - 1].f;
							float l = (float)Math.Sqrt (vx * vx + vy * vy + vz * vz);
							stack [sp - 3].f = vx / l;
							stack [sp - 2].f = vy / l;
							stack [sp - 1].f = vz / l;
						} break;
					case BuiltinFunction.DOT:
						{
							float vx1 = stack [sp - 6].f;
							float vy1 = stack [sp - 5].f;
							float vz1 = stack [sp - 4].f;
							float vx2 = stack [sp - 3].f;
							float vy2 = stack [sp - 2].f;
							float vz2 = stack [sp - 1].f;
							stack [sp - 6].f = vx1 * vx2 + vy1 * vy2 + vz1 * vz2;
							sp -= 5;
						} break;
					case BuiltinFunction.CROSS:
						{
							float vx1 = stack [sp - 6].f;
							float vy1 = stack [sp - 5].f;
							float vz1 = stack [sp - 4].f;
							float vx2 = stack [sp - 3].f;
							float vy2 = stack [sp - 2].f;
							float vz2 = stack [sp - 1].f;
							stack [sp - 6].f = vy1 * vz2 - vz1 * vy2;
							stack [sp - 5].f = vz1 * vx2 - vx1 * vz2;
							stack [sp - 4].f = vx1 * vy2 - vy1 * vx2;
							sp -= 3;
						} break;
					case BuiltinFunction.ROTATE_RIGHT:
						{
							double a = stack [sp - 1].f * Math.PI / 180;
							float sn = (float)Math.Sin (a);
							float cs = (float)Math.Cos (a);
							sp--;
							float vx = stack [sp - 3].f;
							float vz = stack [sp - 1].f;
							stack [sp - 3].f = vx * cs + vz * sn;
							stack [sp - 1].f = -vx * sn + vz * cs;
						} break;
					case BuiltinFunction.ROTATE_UP:
						{
							double a = stack [sp - 1].f * Math.PI / 180;
							float sn = (float)Math.Sin (a);
							float cs = (float)Math.Cos (a);
							sp--;
							float vx = stack [sp - 3].f;
							float vy = stack [sp - 2].f;
							float vz = stack [sp - 1].f;
							float v = (float)Math.Sqrt (vx * vx + vz * vz);
							float nv = v * cs - vy * sn;
							stack [sp - 2].f = v * sn + vy * cs;
							if (v == 0)
								v = 0.000000001f;
							stack [sp - 3].f *= nv / v;
							stack [sp - 1].f *= nv / v;
						}
						break;

					case BuiltinFunction.ANGLE_HORIZONTAL:
						{
							float x1 = stack [sp - 6].f;
							float z1 = stack [sp - 4].f;
							float x2 = stack [sp - 3].f;
							float z2 = stack [sp - 1].f;
							sp -= 5;
							double angle = (Math.Atan2 (z1, x1) - Math.Atan2 (z2, x2)) / Math.PI * 180;
							while (angle > 180) angle -= 360;
							while (angle < -180) angle += 360;
							stack [sp - 1].f = (float)angle;
						} break;
					case BuiltinFunction.ANGLE_VERTICAL:
						{
							float x1 = stack [sp - 6].f;
							float y1 = stack [sp - 5].f;
							float z1 = stack [sp - 4].f;
							float x2 = stack [sp - 3].f;
							float y2 = stack [sp - 2].f;
							float z2 = stack [sp - 1].f;
							sp -= 5;
							double a1 = Math.Asin (y1 / Math.Sqrt (x1 * x1 + z1 * z1));
							double a2 = Math.Asin (y2 / Math.Sqrt (x2 * x2 + z2 * z2));
							stack [sp - 1].f = (float)((a2 - a1) / Math.PI * 180);
						} break;
					case BuiltinFunction.ABS: stack [sp - 1].f = (float)Math.Abs(stack [sp - 1].f); break;
					case BuiltinFunction.MIN:
						{
							float f1 = stack [sp - 2].f;
							float f2 = stack [sp - 1].f;
							sp--;
							stack [sp - 1].f = f1 < f2 ? f1 : f2;
						} break;
					case BuiltinFunction.MAX:
						{
							float f1 = stack [sp - 2].f;
							float f2 = stack [sp - 1].f;
							sp--;
							stack [sp - 1].f = f1 > f2 ? f1 : f2;
						} break;
					case BuiltinFunction.ROUND: stack [sp - 1].i = (int)Math.Round(stack [sp - 1].f); break;
					case BuiltinFunction.FLOOR: stack [sp - 1].i = (int)Math.Floor(stack [sp - 1].f); break;
					case BuiltinFunction.CEIL: stack [sp - 1].i = (int)Math.Ceiling(stack [sp - 1].f); break;
					case BuiltinFunction.SQRT:
						if (stack [sp - 1].f <= 0)
							runtimeError = true;
						else
							stack [sp - 1].f = (float)Math.Sqrt(stack [sp - 1].f); break;
					case BuiltinFunction.SIN: stack [sp - 1].f = (float)Math.Sin(stack [sp - 1].f); break;
					case BuiltinFunction.COS: stack [sp - 1].f = (float)Math.Cos(stack [sp - 1].f); break;
					case BuiltinFunction.ASIN: stack [sp - 1].f = (float)Math.Asin(stack [sp - 1].f); break;
					case BuiltinFunction.ACOS: stack [sp - 1].f = (float)Math.Acos(stack [sp - 1].f); break;
					case BuiltinFunction.ATAN2:
						{
							float f1 = stack [sp - 2].f;
							float f2 = stack [sp - 1].f;
							sp--;
							stack [sp - 1].f = (float)Math.Atan2(f1, f2);
						} break;
					case BuiltinFunction.EXP: stack [sp - 1].f = (float)Math.Exp(stack [sp - 1].f); break;
					case BuiltinFunction.LOG:
						if (stack [sp - 1].f <= 0)
							runtimeError = true;
						else
							stack [sp - 1].f = (float)Math.Log(stack [sp - 1].f);
						break;
					case BuiltinFunction.RANDOM_FLOAT:
						{
							float f1 = stack [sp - 2].f;
							float f2 = stack [sp - 1].f;
							sp--;
							stack [sp - 1].f = (float)(rnd.NextDouble() * (f2-f1) + f1);
						} break;
					case BuiltinFunction.RANDOM_INT:
						{
							int i1 = stack [sp - 2].i;
							int i2 = stack [sp - 1].i;
							sp--;
							if (i1 >= i2)
								stack [sp - 1].i = i1;
							else
								stack [sp - 1].i = rnd.Next() % (i2-i1) + i1;
						} break;

					case BuiltinFunction.SCAN_OBSTACLE:
						if (scans_count >= 4) {
							n = max_instructions;
							ip--;
						} else {
							scans_count++;
							float x = stack [sp - 3].f;
							float y = stack [sp - 2].f;
							float z = stack [sp - 1].f;
							sp -= 2;
							stack [sp - 1].f = scanObstacle (x, y, z);
						}
						break;
					case BuiltinFunction.OBJ_TYPE: stack [sp - 1].i = objType(stack [sp - 1].i); break;
					case BuiltinFunction.OBJ_POSITION:
						{
							float x, y, z;
							objPosition (stack [sp - 1].i, out x, out y, out z);
							sp += 2;
							stack [sp - 3].f = x;
							stack [sp - 2].f = y;
							stack [sp - 1].f = z;
						} break;
					case BuiltinFunction.OBJ_VELOCITY:
						{
							float x, y, z;
							objVelocity (stack [sp - 1].i, out x, out y, out z);
							sp += 2;
							stack [sp - 3].f = x;
							stack [sp - 2].f = y;
							stack [sp - 1].f = z;
						} break;
					case BuiltinFunction.OBJ_DISTANCE: stack [sp - 1].f = objDistance(stack [sp - 1].i); break;
					case BuiltinFunction.OBJ_DIRECTION:
						{
							float x, y, z;
							objDirection (stack [sp - 1].i, out x, out y, out z);
							sp += 2;
							stack [sp - 3].f = x;
							stack [sp - 2].f = y;
							stack [sp - 1].f = z;
						} break;
					case BuiltinFunction.OBJ_GUN_DIRECTION:
						{
							float x, y, z;
							objGunDirection (stack [sp - 1].i, out x, out y, out z);
							sp += 2;
							stack [sp - 3].f = x;
							stack [sp - 2].f = y;
							stack [sp - 1].f = z;
						} break;
					default:
						throw new Exception ("Unknown builtin function: " + (BuiltinFunction)c.i);
					}
					break;
				case Opcode.RET:
					sp = lp - 2 - c.i;
					ip = stack [lp - 2].i;
					lp = stack [lp - 1].i;
					break;
				case Opcode.RETI: {
						int rv = stack [sp - 1].i;
						sp = lp - 2 - c.i + 1;
						ip = stack [lp - 2].i;
						lp = stack [lp - 1].i;
						stack[sp - 1].i = rv;
					} break;
				case Opcode.RETV: {
						int ra = sp - 3;
						sp = lp - 2 - c.i + 3;
						ip = stack [lp - 2].i;
						lp = stack [lp - 1].i;
						stack [sp - 3].i = stack [ra].i;
						stack [sp - 2].i = stack [ra + 1].i;
						stack [sp - 1].i = stack [ra + 2].i;
					} break;
				case Opcode.JMP:
					ip = c.i;
					break;
				case Opcode.JZ:
					if (stack[--sp].i == 0) ip = c.i;
					break;
				case Opcode.MV_SP:
					sp += c.i;
					break;
				case Opcode.CONST:
					if (c.i == 0 && c.f != 0)
						stack [sp++].f = c.f;
					else
						stack [sp++].i = c.i;
					break;
				case Opcode.DUP:
					stack [sp].i = stack [sp - 1].i;
					sp++;
					break;
				case Opcode.L1:
					stack [sp++].i = data [c.i&(DATA_SIZE-1)].i;
					break;
				case Opcode.L3:
					stack [sp++].i = data [c.i&(DATA_SIZE-1)].i;
					stack [sp++].i = data [(c.i+1)&(DATA_SIZE-1)].i;
					stack [sp++].i = data [(c.i+2)&(DATA_SIZE-1)].i;
					break;
				case Opcode.S1:
					data [c.i&(DATA_SIZE-1)].i = stack [sp - 1].i;
					break;
				case Opcode.S3:
					data [c.i&(DATA_SIZE-1)].i = stack [sp - 3].i;
					data [(c.i+1)&(DATA_SIZE-1)].i = stack [sp - 2].i;
					data [(c.i+2)&(DATA_SIZE-1)].i = stack [sp - 1].i;
					break;
				case Opcode.AL1:
					stack [sp - 1].i = data [(c.i + stack [sp - 1].i)&(DATA_SIZE-1)].i;
					break;
				case Opcode.AL3:
					{
						int addr = (c.i + stack [sp - 1].i)&(DATA_SIZE-1);
						stack [sp - 1].i = data [addr].i;
						stack [sp++].i = data [addr+1].i;
						stack [sp++].i = data [addr+2].i;
					} break;
				case Opcode.AS1:
					data [(c.i + stack [sp - 2].i)&(DATA_SIZE-1)].i = stack [sp - 1].i;
					stack [sp - 2].i = stack [sp - 1].i;
					sp--;
					break;
				case Opcode.AS3:
					{
						int addr = (c.i + stack [sp - 4].i)&(DATA_SIZE-1);
						stack [sp - 4].i = data [addr].i = stack [sp - 3].i;
						stack [sp - 3].i = data [addr + 1].i = stack [sp - 2].i;
						stack [sp - 2].i = data [addr + 2].i = stack [sp - 1].i;
						sp--;
					} break;
				case Opcode.SL1:
					stack [sp++].i = stack [lp + c.i].i;
					break;
				case Opcode.SL3:
					stack [sp++].i = stack [lp + c.i].i;
					stack [sp++].i = stack [lp + c.i+1].i;
					stack [sp++].i = stack [lp + c.i+2].i;
					break;
				case Opcode.SS1:
					stack [lp + c.i].i = stack [sp - 1].i;
					break;
				case Opcode.SS3:
					stack [lp + c.i].i = stack [sp - 3].i;
					stack [lp + c.i+1].i = stack [sp - 2].i;
					stack [lp + c.i+2].i = stack [sp - 1].i;
					break;
				case Opcode.SAL1:
					stack [sp - 1].i = stack [lp + c.i + stack [sp - 1].i].i;
					break;
				case Opcode.SAL3:
					{
						int addr = lp + c.i + stack [sp - 1].i;
						stack [sp - 1].i = stack [addr].i;
						stack [sp++].i = stack [addr+1].i;
						stack [sp++].i = stack [addr+2].i;
					} break;
				case Opcode.SAS1:
					stack [lp + c.i + stack [sp - 2].i].i = stack [sp - 1].i;
					stack [sp - 2].i = stack [sp - 1].i;
					sp--;
					break;
				case Opcode.SAS3:
					{
						int addr = lp + c.i + stack [sp - 4].i;
						stack [sp - 4].i = stack [addr].i = stack [sp - 3].i;
						stack [sp - 3].i = stack [addr + 1].i = stack [sp - 2].i;
						stack [sp - 2].i = stack [addr + 2].i = stack [sp - 1].i;
						sp--;
					} break;
				case Opcode.VX:
					sp -= 2;
					break;
				case Opcode.VY:
					stack [sp - 3] = stack [sp - 2];
					sp -= 2;
					break;
				case Opcode.VZ:
					stack [sp - 3] = stack [sp - 1];
					sp -= 2;
					break;
				case Opcode.F2I:
					stack [sp - 1].i = (int)stack [sp - 1].f;
					break;
				case Opcode.I2F:
					stack [sp - 1].f = (float)stack [sp - 1].i;
					break;
				case Opcode.DI2F:
					stack [sp - 2].f = (float)stack [sp - 2].i;
					break;
				case Opcode.ADDI:
					stack [sp - 2].i += stack [sp - 1].i;
					sp--;
					break;
				case Opcode.ADDF:
					stack [sp - 2].f += stack [sp - 1].f;
					sp--;
					break;
				case Opcode.ADDV:
					stack [sp - 6].f += stack [sp - 3].f;
					stack [sp - 5].f += stack [sp - 2].f;
					stack [sp - 4].f += stack [sp - 1].f;
					sp -= 3;
					break;
				case Opcode.SUBI:
					stack [sp - 2].i -= stack [sp - 1].i;
					sp--;
					break;
				case Opcode.SUBF:
					stack [sp - 2].f -= stack [sp - 1].f;
					sp--;
					break;
				case Opcode.SUBV:
					stack [sp - 6].f -= stack [sp - 3].f;
					stack [sp - 5].f -= stack [sp - 2].f;
					stack [sp - 4].f -= stack [sp - 1].f;
					sp -= 3;
					break;
				case Opcode.MULI:
					stack [sp - 2].i *= stack [sp - 1].i;
					sp--;
					break;
				case Opcode.MULF:
					stack [sp - 2].f *= stack [sp - 1].f;
					sp--;
					break;
				case Opcode.MULV: {
						float v = stack [sp - 1].f;
						stack [sp - 4].f *= v;
						stack [sp - 3].f *= v;
						stack [sp - 2].f *= v;
						sp--;
					} break;
				case Opcode.MOD:
					if (stack [sp - 1].i == 0)
						runtimeError = true;
					else
						stack [sp - 2].i = stack [sp - 2].i % stack [sp - 1].i;
					sp--;
					break;
				case Opcode.DIVI:
					if (stack [sp - 1].i == 0)
						runtimeError = true;
					else
						stack [sp - 2].i = stack [sp - 2].i / stack [sp - 1].i;
					sp--;
					break;
				case Opcode.DIVF:
					if (stack [sp - 1].f == 0)
						runtimeError = true;
					else
						stack [sp - 2].f = stack [sp - 2].f / stack [sp - 1].f;
					sp--;
					break;
				case Opcode.DIVV:
					if (stack [sp - 1].f != 0) {
						float v = stack [sp - 1].f;
						stack [sp - 4].f /= v;
						stack [sp - 3].f /= v;
						stack [sp - 2].f /= v;
					} else
						runtimeError = true;
					sp--;
					break;
				case Opcode.NEGI:
					stack [sp - 1].i = -stack [sp - 1].i;
					break;
				case Opcode.NEGF:
					stack [sp - 1].f = -stack [sp - 1].f;
					break;
				case Opcode.NEGV:
					stack [sp - 3].f = -stack [sp - 3].f;
					stack [sp - 2].f = -stack [sp - 2].f;
					stack [sp - 1].f = -stack [sp - 1].f;
					break;
				case Opcode.NOT:
					stack [sp - 1].i = stack [sp - 1].i == 0 ? 1 : 0;
					break;
				case Opcode.EQ:
					stack [sp - 2].i = stack [sp - 2].i == stack [sp - 1].i ? 1 : 0;
					sp--;
					break;
				case Opcode.EQV:
					stack [sp - 6].i = (stack [sp - 6].i == stack [sp - 3].i &&
										stack [sp - 5].i == stack [sp - 2].i &&
										stack [sp - 4].i == stack [sp - 1].i) ? 1 : 0;
					sp -= 5;
					break;
				case Opcode.LTI:
					stack [sp - 2].i = stack [sp - 2].i < stack [sp - 1].i ? 1 : 0;
					sp--;
					break;
				case Opcode.LTF:
					stack [sp - 2].i = stack [sp - 2].f < stack [sp - 1].f ? 1 : 0;
					sp--;
					break;
				case Opcode.LEQI:
					stack [sp - 2].i = stack [sp - 2].i <= stack [sp - 1].i ? 1 : 0;
					sp--;
					break;
				case Opcode.LEQF:
					stack [sp - 2].i = stack [sp - 2].f <= stack [sp - 1].f ? 1 : 0;
					sp--;
					break;
				default:
					throw new Exception ("Unknown command: " + c.opcode);
				}
				if (sp >= STACK_SIZE || runtimeError) {
					ip = 0;
					sp = 0;
					lp = 0;
					runtimeError = true;
					n++;
					break;
				}
				if (ip >= code.commands.Count) {
					ip = 0;
					sp = 0;
					lp = 0;
					loadAverage = (float)(n+1) / max_instructions;
					return true;
				}
			}
			loadAverage = (float)n / max_instructions;
			return false;
		}
	}
}
