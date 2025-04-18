using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Editor.Scene
{
    public static class MeshHelper
    {
        // We have to do this because Unity's "low level" API isn't low level enough and relies on a fixed-size type.
        // Since we have the vertex stream data as a raw array of bytes, we have to use generated types to pass the API.
        // The alternative is to use reflection to call their private internal API.
        public static void SetVertexBufferData(
            Mesh mesh,
            NativeArray<byte> byteArray,
            int vertexCount,
            int vertexSize)
        {
            switch (vertexSize)
            {
#region why does unity do this to me
                case 1: SetVertexBufferDataTyped<Size1>(mesh, byteArray, vertexCount); break;
                case 2: SetVertexBufferDataTyped<Size2>(mesh, byteArray, vertexCount); break;
                case 3: SetVertexBufferDataTyped<Size3>(mesh, byteArray, vertexCount); break;
                case 4: SetVertexBufferDataTyped<Size4>(mesh, byteArray, vertexCount); break;
                case 5: SetVertexBufferDataTyped<Size5>(mesh, byteArray, vertexCount); break;
                case 6: SetVertexBufferDataTyped<Size6>(mesh, byteArray, vertexCount); break;
                case 7: SetVertexBufferDataTyped<Size7>(mesh, byteArray, vertexCount); break;
                case 8: SetVertexBufferDataTyped<Size8>(mesh, byteArray, vertexCount); break;
                case 9: SetVertexBufferDataTyped<Size9>(mesh, byteArray, vertexCount); break;
                case 10: SetVertexBufferDataTyped<Size10>(mesh, byteArray, vertexCount); break;
                case 11: SetVertexBufferDataTyped<Size11>(mesh, byteArray, vertexCount); break;
                case 12: SetVertexBufferDataTyped<Size12>(mesh, byteArray, vertexCount); break;
                case 13: SetVertexBufferDataTyped<Size13>(mesh, byteArray, vertexCount); break;
                case 14: SetVertexBufferDataTyped<Size14>(mesh, byteArray, vertexCount); break;
                case 15: SetVertexBufferDataTyped<Size15>(mesh, byteArray, vertexCount); break;
                case 16: SetVertexBufferDataTyped<Size16>(mesh, byteArray, vertexCount); break;
                case 17: SetVertexBufferDataTyped<Size17>(mesh, byteArray, vertexCount); break;
                case 18: SetVertexBufferDataTyped<Size18>(mesh, byteArray, vertexCount); break;
                case 19: SetVertexBufferDataTyped<Size19>(mesh, byteArray, vertexCount); break;
                case 20: SetVertexBufferDataTyped<Size20>(mesh, byteArray, vertexCount); break;
                case 21: SetVertexBufferDataTyped<Size21>(mesh, byteArray, vertexCount); break;
                case 22: SetVertexBufferDataTyped<Size22>(mesh, byteArray, vertexCount); break;
                case 23: SetVertexBufferDataTyped<Size23>(mesh, byteArray, vertexCount); break;
                case 24: SetVertexBufferDataTyped<Size24>(mesh, byteArray, vertexCount); break;
                case 25: SetVertexBufferDataTyped<Size25>(mesh, byteArray, vertexCount); break;
                case 26: SetVertexBufferDataTyped<Size26>(mesh, byteArray, vertexCount); break;
                case 27: SetVertexBufferDataTyped<Size27>(mesh, byteArray, vertexCount); break;
                case 28: SetVertexBufferDataTyped<Size28>(mesh, byteArray, vertexCount); break;
                case 29: SetVertexBufferDataTyped<Size29>(mesh, byteArray, vertexCount); break;
                case 30: SetVertexBufferDataTyped<Size30>(mesh, byteArray, vertexCount); break;
                case 31: SetVertexBufferDataTyped<Size31>(mesh, byteArray, vertexCount); break;
                case 32: SetVertexBufferDataTyped<Size32>(mesh, byteArray, vertexCount); break;
                case 33: SetVertexBufferDataTyped<Size33>(mesh, byteArray, vertexCount); break;
                case 34: SetVertexBufferDataTyped<Size34>(mesh, byteArray, vertexCount); break;
                case 35: SetVertexBufferDataTyped<Size35>(mesh, byteArray, vertexCount); break;
                case 36: SetVertexBufferDataTyped<Size36>(mesh, byteArray, vertexCount); break;
                case 37: SetVertexBufferDataTyped<Size37>(mesh, byteArray, vertexCount); break;
                case 38: SetVertexBufferDataTyped<Size38>(mesh, byteArray, vertexCount); break;
                case 39: SetVertexBufferDataTyped<Size39>(mesh, byteArray, vertexCount); break;
                case 40: SetVertexBufferDataTyped<Size40>(mesh, byteArray, vertexCount); break;
                case 41: SetVertexBufferDataTyped<Size41>(mesh, byteArray, vertexCount); break;
                case 42: SetVertexBufferDataTyped<Size42>(mesh, byteArray, vertexCount); break;
                case 43: SetVertexBufferDataTyped<Size43>(mesh, byteArray, vertexCount); break;
                case 44: SetVertexBufferDataTyped<Size44>(mesh, byteArray, vertexCount); break;
                case 45: SetVertexBufferDataTyped<Size45>(mesh, byteArray, vertexCount); break;
                case 46: SetVertexBufferDataTyped<Size46>(mesh, byteArray, vertexCount); break;
                case 47: SetVertexBufferDataTyped<Size47>(mesh, byteArray, vertexCount); break;
                case 48: SetVertexBufferDataTyped<Size48>(mesh, byteArray, vertexCount); break;
                case 49: SetVertexBufferDataTyped<Size49>(mesh, byteArray, vertexCount); break;
                case 50: SetVertexBufferDataTyped<Size50>(mesh, byteArray, vertexCount); break;
                case 51: SetVertexBufferDataTyped<Size51>(mesh, byteArray, vertexCount); break;
                case 52: SetVertexBufferDataTyped<Size52>(mesh, byteArray, vertexCount); break;
                case 53: SetVertexBufferDataTyped<Size53>(mesh, byteArray, vertexCount); break;
                case 54: SetVertexBufferDataTyped<Size54>(mesh, byteArray, vertexCount); break;
                case 55: SetVertexBufferDataTyped<Size55>(mesh, byteArray, vertexCount); break;
                case 56: SetVertexBufferDataTyped<Size56>(mesh, byteArray, vertexCount); break;
                case 57: SetVertexBufferDataTyped<Size57>(mesh, byteArray, vertexCount); break;
                case 58: SetVertexBufferDataTyped<Size58>(mesh, byteArray, vertexCount); break;
                case 59: SetVertexBufferDataTyped<Size59>(mesh, byteArray, vertexCount); break;
                case 60: SetVertexBufferDataTyped<Size60>(mesh, byteArray, vertexCount); break;
                case 61: SetVertexBufferDataTyped<Size61>(mesh, byteArray, vertexCount); break;
                case 62: SetVertexBufferDataTyped<Size62>(mesh, byteArray, vertexCount); break;
                case 63: SetVertexBufferDataTyped<Size63>(mesh, byteArray, vertexCount); break;
                case 64: SetVertexBufferDataTyped<Size64>(mesh, byteArray, vertexCount); break;
                case 65: SetVertexBufferDataTyped<Size65>(mesh, byteArray, vertexCount); break;
                case 66: SetVertexBufferDataTyped<Size66>(mesh, byteArray, vertexCount); break;
                case 67: SetVertexBufferDataTyped<Size67>(mesh, byteArray, vertexCount); break;
                case 68: SetVertexBufferDataTyped<Size68>(mesh, byteArray, vertexCount); break;
                case 69: SetVertexBufferDataTyped<Size69>(mesh, byteArray, vertexCount); break;
                case 70: SetVertexBufferDataTyped<Size70>(mesh, byteArray, vertexCount); break;
                case 71: SetVertexBufferDataTyped<Size71>(mesh, byteArray, vertexCount); break;
                case 72: SetVertexBufferDataTyped<Size72>(mesh, byteArray, vertexCount); break;
                case 73: SetVertexBufferDataTyped<Size73>(mesh, byteArray, vertexCount); break;
                case 74: SetVertexBufferDataTyped<Size74>(mesh, byteArray, vertexCount); break;
                case 75: SetVertexBufferDataTyped<Size75>(mesh, byteArray, vertexCount); break;
                case 76: SetVertexBufferDataTyped<Size76>(mesh, byteArray, vertexCount); break;
                case 77: SetVertexBufferDataTyped<Size77>(mesh, byteArray, vertexCount); break;
                case 78: SetVertexBufferDataTyped<Size78>(mesh, byteArray, vertexCount); break;
                case 79: SetVertexBufferDataTyped<Size79>(mesh, byteArray, vertexCount); break;
                case 80: SetVertexBufferDataTyped<Size80>(mesh, byteArray, vertexCount); break;
                case 81: SetVertexBufferDataTyped<Size81>(mesh, byteArray, vertexCount); break;
                case 82: SetVertexBufferDataTyped<Size82>(mesh, byteArray, vertexCount); break;
                case 83: SetVertexBufferDataTyped<Size83>(mesh, byteArray, vertexCount); break;
                case 84: SetVertexBufferDataTyped<Size84>(mesh, byteArray, vertexCount); break;
                case 85: SetVertexBufferDataTyped<Size85>(mesh, byteArray, vertexCount); break;
                case 86: SetVertexBufferDataTyped<Size86>(mesh, byteArray, vertexCount); break;
                case 87: SetVertexBufferDataTyped<Size87>(mesh, byteArray, vertexCount); break;
                case 88: SetVertexBufferDataTyped<Size88>(mesh, byteArray, vertexCount); break;
                case 89: SetVertexBufferDataTyped<Size89>(mesh, byteArray, vertexCount); break;
                case 90: SetVertexBufferDataTyped<Size90>(mesh, byteArray, vertexCount); break;
                case 91: SetVertexBufferDataTyped<Size91>(mesh, byteArray, vertexCount); break;
                case 92: SetVertexBufferDataTyped<Size92>(mesh, byteArray, vertexCount); break;
                case 93: SetVertexBufferDataTyped<Size93>(mesh, byteArray, vertexCount); break;
                case 94: SetVertexBufferDataTyped<Size94>(mesh, byteArray, vertexCount); break;
                case 95: SetVertexBufferDataTyped<Size95>(mesh, byteArray, vertexCount); break;
                case 96: SetVertexBufferDataTyped<Size96>(mesh, byteArray, vertexCount); break;
                case 97: SetVertexBufferDataTyped<Size97>(mesh, byteArray, vertexCount); break;
                case 98: SetVertexBufferDataTyped<Size98>(mesh, byteArray, vertexCount); break;
                case 99: SetVertexBufferDataTyped<Size99>(mesh, byteArray, vertexCount); break;
                case 100: SetVertexBufferDataTyped<Size100>(mesh, byteArray, vertexCount); break;
                case 101: SetVertexBufferDataTyped<Size101>(mesh, byteArray, vertexCount); break;
                case 102: SetVertexBufferDataTyped<Size102>(mesh, byteArray, vertexCount); break;
                case 103: SetVertexBufferDataTyped<Size103>(mesh, byteArray, vertexCount); break;
                case 104: SetVertexBufferDataTyped<Size104>(mesh, byteArray, vertexCount); break;
                case 105: SetVertexBufferDataTyped<Size105>(mesh, byteArray, vertexCount); break;
                case 106: SetVertexBufferDataTyped<Size106>(mesh, byteArray, vertexCount); break;
                case 107: SetVertexBufferDataTyped<Size107>(mesh, byteArray, vertexCount); break;
                case 108: SetVertexBufferDataTyped<Size108>(mesh, byteArray, vertexCount); break;
                case 109: SetVertexBufferDataTyped<Size109>(mesh, byteArray, vertexCount); break;
                case 110: SetVertexBufferDataTyped<Size110>(mesh, byteArray, vertexCount); break;
                case 111: SetVertexBufferDataTyped<Size111>(mesh, byteArray, vertexCount); break;
                case 112: SetVertexBufferDataTyped<Size112>(mesh, byteArray, vertexCount); break;
                case 113: SetVertexBufferDataTyped<Size113>(mesh, byteArray, vertexCount); break;
                case 114: SetVertexBufferDataTyped<Size114>(mesh, byteArray, vertexCount); break;
                case 115: SetVertexBufferDataTyped<Size115>(mesh, byteArray, vertexCount); break;
                case 116: SetVertexBufferDataTyped<Size116>(mesh, byteArray, vertexCount); break;
                case 117: SetVertexBufferDataTyped<Size117>(mesh, byteArray, vertexCount); break;
                case 118: SetVertexBufferDataTyped<Size118>(mesh, byteArray, vertexCount); break;
                case 119: SetVertexBufferDataTyped<Size119>(mesh, byteArray, vertexCount); break;
                case 120: SetVertexBufferDataTyped<Size120>(mesh, byteArray, vertexCount); break;
                case 121: SetVertexBufferDataTyped<Size121>(mesh, byteArray, vertexCount); break;
                case 122: SetVertexBufferDataTyped<Size122>(mesh, byteArray, vertexCount); break;
                case 123: SetVertexBufferDataTyped<Size123>(mesh, byteArray, vertexCount); break;
                case 124: SetVertexBufferDataTyped<Size124>(mesh, byteArray, vertexCount); break;
                case 125: SetVertexBufferDataTyped<Size125>(mesh, byteArray, vertexCount); break;
                case 126: SetVertexBufferDataTyped<Size126>(mesh, byteArray, vertexCount); break;
                case 127: SetVertexBufferDataTyped<Size127>(mesh, byteArray, vertexCount); break;
                case 128: SetVertexBufferDataTyped<Size128>(mesh, byteArray, vertexCount); break;
                default: throw new();
#endregion
            }
        }

        public static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (!_meshes.TryGetValue(type, out Mesh mesh))
            {
                GameObject primitive = GameObject.CreatePrimitive(type);
                mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(primitive);
                _meshes[type] = mesh;
            }
            return mesh;
        }

        private static readonly Dictionary<PrimitiveType, Mesh> _meshes = new();

        private static void SetVertexBufferDataTyped<T>(
            Mesh mesh,
            NativeArray<byte> byteArray,
            int vertexCount) where T : unmanaged
        {
            mesh.SetVertexBufferData(byteArray.Reinterpret<T>(sizeof(byte)), 0, 0, vertexCount);
        }

#region why does unity do this to me
        [StructLayout(LayoutKind.Sequential, Size = 1)] private struct Size1 { }
        [StructLayout(LayoutKind.Sequential, Size = 2)] private struct Size2 { }
        [StructLayout(LayoutKind.Sequential, Size = 3)] private struct Size3 { }
        [StructLayout(LayoutKind.Sequential, Size = 4)] private struct Size4 { }
        [StructLayout(LayoutKind.Sequential, Size = 5)] private struct Size5 { }
        [StructLayout(LayoutKind.Sequential, Size = 6)] private struct Size6 { }
        [StructLayout(LayoutKind.Sequential, Size = 7)] private struct Size7 { }
        [StructLayout(LayoutKind.Sequential, Size = 8)] private struct Size8 { }
        [StructLayout(LayoutKind.Sequential, Size = 9)] private struct Size9 { }
        [StructLayout(LayoutKind.Sequential, Size = 10)] private struct Size10 { }
        [StructLayout(LayoutKind.Sequential, Size = 11)] private struct Size11 { }
        [StructLayout(LayoutKind.Sequential, Size = 12)] private struct Size12 { }
        [StructLayout(LayoutKind.Sequential, Size = 13)] private struct Size13 { }
        [StructLayout(LayoutKind.Sequential, Size = 14)] private struct Size14 { }
        [StructLayout(LayoutKind.Sequential, Size = 15)] private struct Size15 { }
        [StructLayout(LayoutKind.Sequential, Size = 16)] private struct Size16 { }
        [StructLayout(LayoutKind.Sequential, Size = 17)] private struct Size17 { }
        [StructLayout(LayoutKind.Sequential, Size = 18)] private struct Size18 { }
        [StructLayout(LayoutKind.Sequential, Size = 19)] private struct Size19 { }
        [StructLayout(LayoutKind.Sequential, Size = 20)] private struct Size20 { }
        [StructLayout(LayoutKind.Sequential, Size = 21)] private struct Size21 { }
        [StructLayout(LayoutKind.Sequential, Size = 22)] private struct Size22 { }
        [StructLayout(LayoutKind.Sequential, Size = 23)] private struct Size23 { }
        [StructLayout(LayoutKind.Sequential, Size = 24)] private struct Size24 { }
        [StructLayout(LayoutKind.Sequential, Size = 25)] private struct Size25 { }
        [StructLayout(LayoutKind.Sequential, Size = 26)] private struct Size26 { }
        [StructLayout(LayoutKind.Sequential, Size = 27)] private struct Size27 { }
        [StructLayout(LayoutKind.Sequential, Size = 28)] private struct Size28 { }
        [StructLayout(LayoutKind.Sequential, Size = 29)] private struct Size29 { }
        [StructLayout(LayoutKind.Sequential, Size = 30)] private struct Size30 { }
        [StructLayout(LayoutKind.Sequential, Size = 31)] private struct Size31 { }
        [StructLayout(LayoutKind.Sequential, Size = 32)] private struct Size32 { }
        [StructLayout(LayoutKind.Sequential, Size = 33)] private struct Size33 { }
        [StructLayout(LayoutKind.Sequential, Size = 34)] private struct Size34 { }
        [StructLayout(LayoutKind.Sequential, Size = 35)] private struct Size35 { }
        [StructLayout(LayoutKind.Sequential, Size = 36)] private struct Size36 { }
        [StructLayout(LayoutKind.Sequential, Size = 37)] private struct Size37 { }
        [StructLayout(LayoutKind.Sequential, Size = 38)] private struct Size38 { }
        [StructLayout(LayoutKind.Sequential, Size = 39)] private struct Size39 { }
        [StructLayout(LayoutKind.Sequential, Size = 40)] private struct Size40 { }
        [StructLayout(LayoutKind.Sequential, Size = 41)] private struct Size41 { }
        [StructLayout(LayoutKind.Sequential, Size = 42)] private struct Size42 { }
        [StructLayout(LayoutKind.Sequential, Size = 43)] private struct Size43 { }
        [StructLayout(LayoutKind.Sequential, Size = 44)] private struct Size44 { }
        [StructLayout(LayoutKind.Sequential, Size = 45)] private struct Size45 { }
        [StructLayout(LayoutKind.Sequential, Size = 46)] private struct Size46 { }
        [StructLayout(LayoutKind.Sequential, Size = 47)] private struct Size47 { }
        [StructLayout(LayoutKind.Sequential, Size = 48)] private struct Size48 { }
        [StructLayout(LayoutKind.Sequential, Size = 49)] private struct Size49 { }
        [StructLayout(LayoutKind.Sequential, Size = 50)] private struct Size50 { }
        [StructLayout(LayoutKind.Sequential, Size = 51)] private struct Size51 { }
        [StructLayout(LayoutKind.Sequential, Size = 52)] private struct Size52 { }
        [StructLayout(LayoutKind.Sequential, Size = 53)] private struct Size53 { }
        [StructLayout(LayoutKind.Sequential, Size = 54)] private struct Size54 { }
        [StructLayout(LayoutKind.Sequential, Size = 55)] private struct Size55 { }
        [StructLayout(LayoutKind.Sequential, Size = 56)] private struct Size56 { }
        [StructLayout(LayoutKind.Sequential, Size = 57)] private struct Size57 { }
        [StructLayout(LayoutKind.Sequential, Size = 58)] private struct Size58 { }
        [StructLayout(LayoutKind.Sequential, Size = 59)] private struct Size59 { }
        [StructLayout(LayoutKind.Sequential, Size = 60)] private struct Size60 { }
        [StructLayout(LayoutKind.Sequential, Size = 61)] private struct Size61 { }
        [StructLayout(LayoutKind.Sequential, Size = 62)] private struct Size62 { }
        [StructLayout(LayoutKind.Sequential, Size = 63)] private struct Size63 { }
        [StructLayout(LayoutKind.Sequential, Size = 64)] private struct Size64 { }
        [StructLayout(LayoutKind.Sequential, Size = 65)] private struct Size65 { }
        [StructLayout(LayoutKind.Sequential, Size = 66)] private struct Size66 { }
        [StructLayout(LayoutKind.Sequential, Size = 67)] private struct Size67 { }
        [StructLayout(LayoutKind.Sequential, Size = 68)] private struct Size68 { }
        [StructLayout(LayoutKind.Sequential, Size = 69)] private struct Size69 { }
        [StructLayout(LayoutKind.Sequential, Size = 70)] private struct Size70 { }
        [StructLayout(LayoutKind.Sequential, Size = 71)] private struct Size71 { }
        [StructLayout(LayoutKind.Sequential, Size = 72)] private struct Size72 { }
        [StructLayout(LayoutKind.Sequential, Size = 73)] private struct Size73 { }
        [StructLayout(LayoutKind.Sequential, Size = 74)] private struct Size74 { }
        [StructLayout(LayoutKind.Sequential, Size = 75)] private struct Size75 { }
        [StructLayout(LayoutKind.Sequential, Size = 76)] private struct Size76 { }
        [StructLayout(LayoutKind.Sequential, Size = 77)] private struct Size77 { }
        [StructLayout(LayoutKind.Sequential, Size = 78)] private struct Size78 { }
        [StructLayout(LayoutKind.Sequential, Size = 79)] private struct Size79 { }
        [StructLayout(LayoutKind.Sequential, Size = 80)] private struct Size80 { }
        [StructLayout(LayoutKind.Sequential, Size = 81)] private struct Size81 { }
        [StructLayout(LayoutKind.Sequential, Size = 82)] private struct Size82 { }
        [StructLayout(LayoutKind.Sequential, Size = 83)] private struct Size83 { }
        [StructLayout(LayoutKind.Sequential, Size = 84)] private struct Size84 { }
        [StructLayout(LayoutKind.Sequential, Size = 85)] private struct Size85 { }
        [StructLayout(LayoutKind.Sequential, Size = 86)] private struct Size86 { }
        [StructLayout(LayoutKind.Sequential, Size = 87)] private struct Size87 { }
        [StructLayout(LayoutKind.Sequential, Size = 88)] private struct Size88 { }
        [StructLayout(LayoutKind.Sequential, Size = 89)] private struct Size89 { }
        [StructLayout(LayoutKind.Sequential, Size = 90)] private struct Size90 { }
        [StructLayout(LayoutKind.Sequential, Size = 91)] private struct Size91 { }
        [StructLayout(LayoutKind.Sequential, Size = 92)] private struct Size92 { }
        [StructLayout(LayoutKind.Sequential, Size = 93)] private struct Size93 { }
        [StructLayout(LayoutKind.Sequential, Size = 94)] private struct Size94 { }
        [StructLayout(LayoutKind.Sequential, Size = 95)] private struct Size95 { }
        [StructLayout(LayoutKind.Sequential, Size = 96)] private struct Size96 { }
        [StructLayout(LayoutKind.Sequential, Size = 97)] private struct Size97 { }
        [StructLayout(LayoutKind.Sequential, Size = 98)] private struct Size98 { }
        [StructLayout(LayoutKind.Sequential, Size = 99)] private struct Size99 { }
        [StructLayout(LayoutKind.Sequential, Size = 100)] private struct Size100 { }
        [StructLayout(LayoutKind.Sequential, Size = 101)] private struct Size101 { }
        [StructLayout(LayoutKind.Sequential, Size = 102)] private struct Size102 { }
        [StructLayout(LayoutKind.Sequential, Size = 103)] private struct Size103 { }
        [StructLayout(LayoutKind.Sequential, Size = 104)] private struct Size104 { }
        [StructLayout(LayoutKind.Sequential, Size = 105)] private struct Size105 { }
        [StructLayout(LayoutKind.Sequential, Size = 106)] private struct Size106 { }
        [StructLayout(LayoutKind.Sequential, Size = 107)] private struct Size107 { }
        [StructLayout(LayoutKind.Sequential, Size = 108)] private struct Size108 { }
        [StructLayout(LayoutKind.Sequential, Size = 109)] private struct Size109 { }
        [StructLayout(LayoutKind.Sequential, Size = 110)] private struct Size110 { }
        [StructLayout(LayoutKind.Sequential, Size = 111)] private struct Size111 { }
        [StructLayout(LayoutKind.Sequential, Size = 112)] private struct Size112 { }
        [StructLayout(LayoutKind.Sequential, Size = 113)] private struct Size113 { }
        [StructLayout(LayoutKind.Sequential, Size = 114)] private struct Size114 { }
        [StructLayout(LayoutKind.Sequential, Size = 115)] private struct Size115 { }
        [StructLayout(LayoutKind.Sequential, Size = 116)] private struct Size116 { }
        [StructLayout(LayoutKind.Sequential, Size = 117)] private struct Size117 { }
        [StructLayout(LayoutKind.Sequential, Size = 118)] private struct Size118 { }
        [StructLayout(LayoutKind.Sequential, Size = 119)] private struct Size119 { }
        [StructLayout(LayoutKind.Sequential, Size = 120)] private struct Size120 { }
        [StructLayout(LayoutKind.Sequential, Size = 121)] private struct Size121 { }
        [StructLayout(LayoutKind.Sequential, Size = 122)] private struct Size122 { }
        [StructLayout(LayoutKind.Sequential, Size = 123)] private struct Size123 { }
        [StructLayout(LayoutKind.Sequential, Size = 124)] private struct Size124 { }
        [StructLayout(LayoutKind.Sequential, Size = 125)] private struct Size125 { }
        [StructLayout(LayoutKind.Sequential, Size = 126)] private struct Size126 { }
        [StructLayout(LayoutKind.Sequential, Size = 127)] private struct Size127 { }
        [StructLayout(LayoutKind.Sequential, Size = 128)] private struct Size128 { }
#endregion

    }
}
