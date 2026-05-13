using System.Numerics;
using System.Runtime.CompilerServices;

namespace Misaki.HighPerformance.Mathematics;

public static class VectorExtension
{

    extension(ref Vector2 v)
    {
        public Vector2 XX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X);
            }
        }

        public Vector2 XY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 YX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 YY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y);
            }
        }

    }

    extension(ref Vector3 v)
    {
        public Vector2 XX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X);
            }
        }

        public Vector2 XY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 XZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
            }
        }

        public Vector2 YX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 YY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y);
            }
        }

        public Vector2 YZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
            }
        }

        public Vector2 ZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 ZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 ZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z);
            }
        }

        public Vector3 XXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X);
            }
        }

        public Vector3 XXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y);
            }
        }

        public Vector3 XXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z);
            }
        }

        public Vector3 XYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X);
            }
        }

        public Vector3 XYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y);
            }
        }

        public Vector3 XYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 XZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X);
            }
        }

        public Vector3 XZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 XZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z);
            }
        }

        public Vector3 YXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X);
            }
        }

        public Vector3 YXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y);
            }
        }

        public Vector3 YXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 YYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X);
            }
        }

        public Vector3 YYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y);
            }
        }

        public Vector3 YYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z);
            }
        }

        public Vector3 YZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 YZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y);
            }
        }

        public Vector3 YZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z);
            }
        }

        public Vector3 ZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X);
            }
        }

        public Vector3 ZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 ZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z);
            }
        }

        public Vector3 ZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 ZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y);
            }
        }

        public Vector3 ZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z);
            }
        }

        public Vector3 ZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X);
            }
        }

        public Vector3 ZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y);
            }
        }

        public Vector3 ZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z);
            }
        }

    }

    extension(ref Vector4 v)
    {
        public Vector2 XX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X);
            }
        }

        public Vector2 XY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 XZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
            }
        }

        public Vector2 XW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.W = value.Y;
            }
        }

        public Vector2 YX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 YY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y);
            }
        }

        public Vector2 YZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
            }
        }

        public Vector2 YW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.W = value.Y;
            }
        }

        public Vector2 ZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 ZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 ZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z);
            }
        }

        public Vector2 ZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.W = value.Y;
            }
        }

        public Vector2 WX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.X = value.Y;
            }
        }

        public Vector2 WY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Y = value.Y;
            }
        }

        public Vector2 WZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Z = value.Y;
            }
        }

        public Vector2 WW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W);
            }
        }

        public Vector3 XXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X);
            }
        }

        public Vector3 XXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y);
            }
        }

        public Vector3 XXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z);
            }
        }

        public Vector3 XXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.W);
            }
        }

        public Vector3 XYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X);
            }
        }

        public Vector3 XYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y);
            }
        }

        public Vector3 XYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 XYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 XZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X);
            }
        }

        public Vector3 XZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 XZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z);
            }
        }

        public Vector3 XZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 XWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.X);
            }
        }

        public Vector3 XWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.W = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 XWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.W = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 XWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.W);
            }
        }

        public Vector3 YXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X);
            }
        }

        public Vector3 YXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y);
            }
        }

        public Vector3 YXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 YXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 YYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X);
            }
        }

        public Vector3 YYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y);
            }
        }

        public Vector3 YYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z);
            }
        }

        public Vector3 YYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.W);
            }
        }

        public Vector3 YZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 YZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y);
            }
        }

        public Vector3 YZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z);
            }
        }

        public Vector3 YZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 YWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.W = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 YWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Y);
            }
        }

        public Vector3 YWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.W = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 YWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.W);
            }
        }

        public Vector3 ZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X);
            }
        }

        public Vector3 ZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 ZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z);
            }
        }

        public Vector3 ZXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 ZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 ZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y);
            }
        }

        public Vector3 ZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z);
            }
        }

        public Vector3 ZYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
                v.W = value.Z;
            }
        }

        public Vector3 ZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X);
            }
        }

        public Vector3 ZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y);
            }
        }

        public Vector3 ZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z);
            }
        }

        public Vector3 ZZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.W);
            }
        }

        public Vector3 ZWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.W = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 ZWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.W = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 ZWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Z);
            }
        }

        public Vector3 ZWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.W);
            }
        }

        public Vector3 WXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.X);
            }
        }

        public Vector3 WXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.X = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 WXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.X = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 WXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.W);
            }
        }

        public Vector3 WYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Y = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 WYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Y);
            }
        }

        public Vector3 WYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Y = value.Y;
                v.Z = value.Z;
            }
        }

        public Vector3 WYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.W);
            }
        }

        public Vector3 WZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Z = value.Y;
                v.X = value.Z;
            }
        }

        public Vector3 WZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Z = value.Y;
                v.Y = value.Z;
            }
        }

        public Vector3 WZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Z);
            }
        }

        public Vector3 WZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.W);
            }
        }

        public Vector3 WWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.X);
            }
        }

        public Vector3 WWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Y);
            }
        }

        public Vector3 WWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Z);
            }
        }

        public Vector3 WWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.W);
            }
        }

        public Vector4 XXXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X, v.X);
            }
        }

        public Vector4 XXXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X, v.Y);
            }
        }

        public Vector4 XXXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X, v.Z);
            }
        }

        public Vector4 XXXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.X, v.W);
            }
        }

        public Vector4 XXYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y, v.X);
            }
        }

        public Vector4 XXYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y, v.Y);
            }
        }

        public Vector4 XXYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y, v.Z);
            }
        }

        public Vector4 XXYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Y, v.W);
            }
        }

        public Vector4 XXZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z, v.X);
            }
        }

        public Vector4 XXZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z, v.Y);
            }
        }

        public Vector4 XXZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z, v.Z);
            }
        }

        public Vector4 XXZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.Z, v.W);
            }
        }

        public Vector4 XXWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.W, v.X);
            }
        }

        public Vector4 XXWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.W, v.Y);
            }
        }

        public Vector4 XXWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.W, v.Z);
            }
        }

        public Vector4 XXWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.X, v.W, v.W);
            }
        }

        public Vector4 XYXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X, v.X);
            }
        }

        public Vector4 XYXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X, v.Y);
            }
        }

        public Vector4 XYXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X, v.Z);
            }
        }

        public Vector4 XYXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.X, v.W);
            }
        }

        public Vector4 XYYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y, v.X);
            }
        }

        public Vector4 XYYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y, v.Y);
            }
        }

        public Vector4 XYYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y, v.Z);
            }
        }

        public Vector4 XYYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Y, v.W);
            }
        }

        public Vector4 XYZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z, v.X);
            }
        }

        public Vector4 XYZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z, v.Y);
            }
        }

        public Vector4 XYZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z, v.Z);
            }
        }

        public Vector4 XYZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.Z, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
                v.Z = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 XYWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.W, v.X);
            }
        }

        public Vector4 XYWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.W, v.Y);
            }
        }

        public Vector4 XYWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.W, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Y = value.Y;
                v.W = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 XYWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Y, v.W, v.W);
            }
        }

        public Vector4 XZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X, v.X);
            }
        }

        public Vector4 XZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X, v.Y);
            }
        }

        public Vector4 XZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X, v.Z);
            }
        }

        public Vector4 XZXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.X, v.W);
            }
        }

        public Vector4 XZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y, v.X);
            }
        }

        public Vector4 XZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y, v.Y);
            }
        }

        public Vector4 XZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y, v.Z);
            }
        }

        public Vector4 XZYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Y, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
                v.Y = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 XZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z, v.X);
            }
        }

        public Vector4 XZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z, v.Y);
            }
        }

        public Vector4 XZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z, v.Z);
            }
        }

        public Vector4 XZZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.Z, v.W);
            }
        }

        public Vector4 XZWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.W, v.X);
            }
        }

        public Vector4 XZWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.W, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.Z = value.Y;
                v.W = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 XZWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.W, v.Z);
            }
        }

        public Vector4 XZWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.Z, v.W, v.W);
            }
        }

        public Vector4 XWXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.X, v.X);
            }
        }

        public Vector4 XWXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.X, v.Y);
            }
        }

        public Vector4 XWXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.X, v.Z);
            }
        }

        public Vector4 XWXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.X, v.W);
            }
        }

        public Vector4 XWYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Y, v.X);
            }
        }

        public Vector4 XWYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Y, v.Y);
            }
        }

        public Vector4 XWYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.W = value.Y;
                v.Y = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 XWYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Y, v.W);
            }
        }

        public Vector4 XWZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Z, v.X);
            }
        }

        public Vector4 XWZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.X = value.X;
                v.W = value.Y;
                v.Z = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 XWZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Z, v.Z);
            }
        }

        public Vector4 XWZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.Z, v.W);
            }
        }

        public Vector4 XWWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.W, v.X);
            }
        }

        public Vector4 XWWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.W, v.Y);
            }
        }

        public Vector4 XWWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.W, v.Z);
            }
        }

        public Vector4 XWWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.X, v.W, v.W, v.W);
            }
        }

        public Vector4 YXXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X, v.X);
            }
        }

        public Vector4 YXXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X, v.Y);
            }
        }

        public Vector4 YXXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X, v.Z);
            }
        }

        public Vector4 YXXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.X, v.W);
            }
        }

        public Vector4 YXYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y, v.X);
            }
        }

        public Vector4 YXYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y, v.Y);
            }
        }

        public Vector4 YXYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y, v.Z);
            }
        }

        public Vector4 YXYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Y, v.W);
            }
        }

        public Vector4 YXZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z, v.X);
            }
        }

        public Vector4 YXZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z, v.Y);
            }
        }

        public Vector4 YXZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z, v.Z);
            }
        }

        public Vector4 YXZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.Z, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
                v.Z = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 YXWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.W, v.X);
            }
        }

        public Vector4 YXWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.W, v.Y);
            }
        }

        public Vector4 YXWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.W, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.X = value.Y;
                v.W = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 YXWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.X, v.W, v.W);
            }
        }

        public Vector4 YYXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X, v.X);
            }
        }

        public Vector4 YYXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X, v.Y);
            }
        }

        public Vector4 YYXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X, v.Z);
            }
        }

        public Vector4 YYXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.X, v.W);
            }
        }

        public Vector4 YYYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y, v.X);
            }
        }

        public Vector4 YYYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y, v.Y);
            }
        }

        public Vector4 YYYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y, v.Z);
            }
        }

        public Vector4 YYYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Y, v.W);
            }
        }

        public Vector4 YYZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z, v.X);
            }
        }

        public Vector4 YYZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z, v.Y);
            }
        }

        public Vector4 YYZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z, v.Z);
            }
        }

        public Vector4 YYZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.Z, v.W);
            }
        }

        public Vector4 YYWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.W, v.X);
            }
        }

        public Vector4 YYWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.W, v.Y);
            }
        }

        public Vector4 YYWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.W, v.Z);
            }
        }

        public Vector4 YYWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Y, v.W, v.W);
            }
        }

        public Vector4 YZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X, v.X);
            }
        }

        public Vector4 YZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X, v.Y);
            }
        }

        public Vector4 YZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X, v.Z);
            }
        }

        public Vector4 YZXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.X, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
                v.X = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 YZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y, v.X);
            }
        }

        public Vector4 YZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y, v.Y);
            }
        }

        public Vector4 YZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y, v.Z);
            }
        }

        public Vector4 YZYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Y, v.W);
            }
        }

        public Vector4 YZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z, v.X);
            }
        }

        public Vector4 YZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z, v.Y);
            }
        }

        public Vector4 YZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z, v.Z);
            }
        }

        public Vector4 YZZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.Z, v.W);
            }
        }

        public Vector4 YZWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.W, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.Z = value.Y;
                v.W = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 YZWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.W, v.Y);
            }
        }

        public Vector4 YZWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.W, v.Z);
            }
        }

        public Vector4 YZWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.Z, v.W, v.W);
            }
        }

        public Vector4 YWXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.X, v.X);
            }
        }

        public Vector4 YWXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.X, v.Y);
            }
        }

        public Vector4 YWXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.W = value.Y;
                v.X = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 YWXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.X, v.W);
            }
        }

        public Vector4 YWYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Y, v.X);
            }
        }

        public Vector4 YWYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Y, v.Y);
            }
        }

        public Vector4 YWYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Y, v.Z);
            }
        }

        public Vector4 YWYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Y, v.W);
            }
        }

        public Vector4 YWZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Y = value.X;
                v.W = value.Y;
                v.Z = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 YWZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Z, v.Y);
            }
        }

        public Vector4 YWZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Z, v.Z);
            }
        }

        public Vector4 YWZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.Z, v.W);
            }
        }

        public Vector4 YWWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.W, v.X);
            }
        }

        public Vector4 YWWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.W, v.Y);
            }
        }

        public Vector4 YWWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.W, v.Z);
            }
        }

        public Vector4 YWWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Y, v.W, v.W, v.W);
            }
        }

        public Vector4 ZXXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X, v.X);
            }
        }

        public Vector4 ZXXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X, v.Y);
            }
        }

        public Vector4 ZXXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X, v.Z);
            }
        }

        public Vector4 ZXXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.X, v.W);
            }
        }

        public Vector4 ZXYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y, v.X);
            }
        }

        public Vector4 ZXYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y, v.Y);
            }
        }

        public Vector4 ZXYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y, v.Z);
            }
        }

        public Vector4 ZXYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Y, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
                v.Y = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 ZXZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z, v.X);
            }
        }

        public Vector4 ZXZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z, v.Y);
            }
        }

        public Vector4 ZXZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z, v.Z);
            }
        }

        public Vector4 ZXZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.Z, v.W);
            }
        }

        public Vector4 ZXWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.W, v.X);
            }
        }

        public Vector4 ZXWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.W, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.X = value.Y;
                v.W = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 ZXWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.W, v.Z);
            }
        }

        public Vector4 ZXWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.X, v.W, v.W);
            }
        }

        public Vector4 ZYXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X, v.X);
            }
        }

        public Vector4 ZYXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X, v.Y);
            }
        }

        public Vector4 ZYXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X, v.Z);
            }
        }

        public Vector4 ZYXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.X, v.W);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
                v.X = value.Z;
                v.W = value.W;
            }
        }

        public Vector4 ZYYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y, v.X);
            }
        }

        public Vector4 ZYYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y, v.Y);
            }
        }

        public Vector4 ZYYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y, v.Z);
            }
        }

        public Vector4 ZYYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Y, v.W);
            }
        }

        public Vector4 ZYZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z, v.X);
            }
        }

        public Vector4 ZYZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z, v.Y);
            }
        }

        public Vector4 ZYZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z, v.Z);
            }
        }

        public Vector4 ZYZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.Z, v.W);
            }
        }

        public Vector4 ZYWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.W, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.Y = value.Y;
                v.W = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 ZYWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.W, v.Y);
            }
        }

        public Vector4 ZYWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.W, v.Z);
            }
        }

        public Vector4 ZYWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Y, v.W, v.W);
            }
        }

        public Vector4 ZZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X, v.X);
            }
        }

        public Vector4 ZZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X, v.Y);
            }
        }

        public Vector4 ZZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X, v.Z);
            }
        }

        public Vector4 ZZXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.X, v.W);
            }
        }

        public Vector4 ZZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y, v.X);
            }
        }

        public Vector4 ZZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y, v.Y);
            }
        }

        public Vector4 ZZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y, v.Z);
            }
        }

        public Vector4 ZZYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Y, v.W);
            }
        }

        public Vector4 ZZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z, v.X);
            }
        }

        public Vector4 ZZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z, v.Y);
            }
        }

        public Vector4 ZZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z, v.Z);
            }
        }

        public Vector4 ZZZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.Z, v.W);
            }
        }

        public Vector4 ZZWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.W, v.X);
            }
        }

        public Vector4 ZZWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.W, v.Y);
            }
        }

        public Vector4 ZZWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.W, v.Z);
            }
        }

        public Vector4 ZZWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.Z, v.W, v.W);
            }
        }

        public Vector4 ZWXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.X, v.X);
            }
        }

        public Vector4 ZWXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.W = value.Y;
                v.X = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 ZWXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.X, v.Z);
            }
        }

        public Vector4 ZWXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.X, v.W);
            }
        }

        public Vector4 ZWYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.Z = value.X;
                v.W = value.Y;
                v.Y = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 ZWYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Y, v.Y);
            }
        }

        public Vector4 ZWYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Y, v.Z);
            }
        }

        public Vector4 ZWYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Y, v.W);
            }
        }

        public Vector4 ZWZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Z, v.X);
            }
        }

        public Vector4 ZWZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Z, v.Y);
            }
        }

        public Vector4 ZWZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Z, v.Z);
            }
        }

        public Vector4 ZWZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.Z, v.W);
            }
        }

        public Vector4 ZWWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.W, v.X);
            }
        }

        public Vector4 ZWWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.W, v.Y);
            }
        }

        public Vector4 ZWWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.W, v.Z);
            }
        }

        public Vector4 ZWWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.Z, v.W, v.W, v.W);
            }
        }

        public Vector4 WXXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.X, v.X);
            }
        }

        public Vector4 WXXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.X, v.Y);
            }
        }

        public Vector4 WXXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.X, v.Z);
            }
        }

        public Vector4 WXXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.X, v.W);
            }
        }

        public Vector4 WXYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Y, v.X);
            }
        }

        public Vector4 WXYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Y, v.Y);
            }
        }

        public Vector4 WXYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Y, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.X = value.Y;
                v.Y = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 WXYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Y, v.W);
            }
        }

        public Vector4 WXZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Z, v.X);
            }
        }

        public Vector4 WXZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Z, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.X = value.Y;
                v.Z = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 WXZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Z, v.Z);
            }
        }

        public Vector4 WXZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.Z, v.W);
            }
        }

        public Vector4 WXWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.W, v.X);
            }
        }

        public Vector4 WXWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.W, v.Y);
            }
        }

        public Vector4 WXWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.W, v.Z);
            }
        }

        public Vector4 WXWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.X, v.W, v.W);
            }
        }

        public Vector4 WYXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.X, v.X);
            }
        }

        public Vector4 WYXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.X, v.Y);
            }
        }

        public Vector4 WYXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.X, v.Z);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Y = value.Y;
                v.X = value.Z;
                v.Z = value.W;
            }
        }

        public Vector4 WYXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.X, v.W);
            }
        }

        public Vector4 WYYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Y, v.X);
            }
        }

        public Vector4 WYYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Y, v.Y);
            }
        }

        public Vector4 WYYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Y, v.Z);
            }
        }

        public Vector4 WYYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Y, v.W);
            }
        }

        public Vector4 WYZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Z, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Y = value.Y;
                v.Z = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 WYZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Z, v.Y);
            }
        }

        public Vector4 WYZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Z, v.Z);
            }
        }

        public Vector4 WYZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.Z, v.W);
            }
        }

        public Vector4 WYWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.W, v.X);
            }
        }

        public Vector4 WYWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.W, v.Y);
            }
        }

        public Vector4 WYWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.W, v.Z);
            }
        }

        public Vector4 WYWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Y, v.W, v.W);
            }
        }

        public Vector4 WZXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.X, v.X);
            }
        }

        public Vector4 WZXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.X, v.Y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Z = value.Y;
                v.X = value.Z;
                v.Y = value.W;
            }
        }

        public Vector4 WZXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.X, v.Z);
            }
        }

        public Vector4 WZXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.X, v.W);
            }
        }

        public Vector4 WZYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Y, v.X);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                v.W = value.X;
                v.Z = value.Y;
                v.Y = value.Z;
                v.X = value.W;
            }
        }

        public Vector4 WZYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Y, v.Y);
            }
        }

        public Vector4 WZYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Y, v.Z);
            }
        }

        public Vector4 WZYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Y, v.W);
            }
        }

        public Vector4 WZZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Z, v.X);
            }
        }

        public Vector4 WZZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Z, v.Y);
            }
        }

        public Vector4 WZZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Z, v.Z);
            }
        }

        public Vector4 WZZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.Z, v.W);
            }
        }

        public Vector4 WZWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.W, v.X);
            }
        }

        public Vector4 WZWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.W, v.Y);
            }
        }

        public Vector4 WZWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.W, v.Z);
            }
        }

        public Vector4 WZWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.Z, v.W, v.W);
            }
        }

        public Vector4 WWXX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.X, v.X);
            }
        }

        public Vector4 WWXY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.X, v.Y);
            }
        }

        public Vector4 WWXZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.X, v.Z);
            }
        }

        public Vector4 WWXW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.X, v.W);
            }
        }

        public Vector4 WWYX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Y, v.X);
            }
        }

        public Vector4 WWYY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Y, v.Y);
            }
        }

        public Vector4 WWYZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Y, v.Z);
            }
        }

        public Vector4 WWYW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Y, v.W);
            }
        }

        public Vector4 WWZX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Z, v.X);
            }
        }

        public Vector4 WWZY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Z, v.Y);
            }
        }

        public Vector4 WWZZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Z, v.Z);
            }
        }

        public Vector4 WWZW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.Z, v.W);
            }
        }

        public Vector4 WWWX
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.W, v.X);
            }
        }

        public Vector4 WWWY
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.W, v.Y);
            }
        }

        public Vector4 WWWZ
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.W, v.Z);
            }
        }

        public Vector4 WWWW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new(v.W, v.W, v.W, v.W);
            }
        }

    }


}