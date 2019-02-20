namespace Unity.Mathematics {
	/// <summary>
	/// Blittable substitute for bool, for ECS and Job purposes.
	/// </summary>
	public struct bool1b {
		byte _value;
		
		public static implicit operator bool (bool1b me) {
			return me._value != (byte)0;
		}
		public static implicit operator bool1b (bool me) {
			return new bool1b {_value = (byte)(math.select(0, 0x1, me)) };
		}
	}

	/// <summary>
	/// Blittable substitute for bool2, for ECS and Job purposes.
	/// </summary>
	public struct bool2b {
		byte _value;

		public bool x {
			get {
				return (_value & 1) != 0;
			} set {
				_value = (byte)(_value & 0xFE);
				_value |= (byte)math.select(0, 1, value);
			}
		}
		public bool y {
			get {
				return (_value & 2) != 0;
			} set {
				_value = (byte)(_value & 0xFD);
				_value |= (byte)math.select(0, 2, value);
			}
		}

		public static implicit operator bool2b (bool me) {
			return new bool2b {_value = (byte)(math.select(0, 0x3, me)) };
		}
	}

	/// <summary>
	/// Blittable substitute for bool3, for ECS and Job purposes.
	/// </summary>
	public struct bool3b {
		byte _value;

		public bool x {
			get {
				return (_value & 1) != 0;
			} set {
				_value = (byte)(_value & 0xFE);
				_value |= (byte)math.select(0, 1, value);
			}
		}
		public bool y {
			get {
				return (_value & 2) != 0;
			} set {
				_value = (byte)(_value & 0xFD);
				_value |= (byte)math.select(0, 2, value);
			}
		}
		public bool z {
			get {
				return (_value & 4) != 0;
			} set {
				_value = (byte)(_value & 0xFB);
				_value |= (byte)math.select(0, 4, value);
			}
		}

		public static implicit operator bool3b (bool me) {
			return new bool3b {_value = (byte)(math.select(0, 0x7, me)) };
		}
	}

	/// <summary>
	/// Blittable substitute for bool4, for ECS and Job purposes.
	/// </summary>
	public struct bool4b {
		byte _value;

		public bool x {
			get {
				return (_value & 1) != 0;
			} set {
				_value = (byte)(_value & 0xFE);
				_value |= (byte)math.select(0, 1, value);
			}
		}
		public bool y {
			get {
				return (_value & 2) != 0;
			} set {
				_value = (byte)(_value & 0xFD);
				_value |= (byte)math.select(0, 2, value);
			}
		}
		public bool z {
			get {
				return (_value & 4) != 0;
			} set {
				_value = (byte)(_value & 0xFB);
				_value |= (byte)math.select(0, 4, value);
			}
		}
		public bool w {
			get {
				return (_value & 8) != 0;
			} set {
				_value = (byte)(_value & 0xF7);
				_value |= (byte)math.select(0, 8, value);
			}
		}

		public static implicit operator bool4b (bool me) {
			return new bool4b {_value = (byte)(math.select(0, 0xf, me)) };
		}
	}
}