﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RogueTraderUnityToolkit.Core;

public static class AsciiStringPool
{
    static AsciiStringPool()
    {
        _smallStringBlocks = new Memory<byte>[_blockEndIndex + 1];

        for (int i = 0; i < _smallStringBlocks.Length; ++i)
        {
            _smallStringBlocks[i] = new byte[_blockSize];
        }

        _optimizedStrings = [
            "Transform"u8.ToArray(),  // refs: 19833740
            "Object"u8.ToArray(),     // refs: 14074879
            "GameObject"u8.ToArray(), // refs: 11254279
            "Component"u8.ToArray(),  // refs: 11153768
        ];

#if DEBUG_VERBOSE
        foreach (ReadOnlyMemory<byte> mem in _optimizedStrings)
        {
            Log.Write($"{Encoding.ASCII.GetString(mem.Span)} = {Util.Hash(mem.Span)}");
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AsciiString Fetch(ReadOnlySpan<byte> memory)
    {
        AsciiStringKey key = new(Util.Hash(memory), memory.Length);
        AsciiString str = FetchInternal(memory, key);
        Debug.Assert(str.Bytes.SequenceEqual(memory), $"Hash collision for {key} {str}?");
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetLength(in AsciiString asciiString) => asciiString.BlockData switch
    {
        _blockIsOptimizedString => asciiString.BlockOffset >> 8,
        _blockIsLargeString => GetCSharpString(asciiString).Length,
        _ => asciiString.BlockData
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlyMemory<byte> GetBytes(in AsciiString asciiString) => asciiString.BlockData switch
    {
        _blockIsOptimizedString => _optimizedStrings[asciiString.BlockIdx].Slice(
            asciiString.BlockOffset & 0xFF,
            asciiString.BlockOffset >> 8),
        _blockIsLargeString => _largeStrings[GetLargeStringIdx(asciiString)],
        _ => _smallStringBlocks[asciiString.BlockIdx].Slice(
            asciiString.BlockOffset,
            asciiString.BlockData)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetCSharpString(in AsciiString asciiString)
    {
        ReadOnlyMemory<byte> bytes = GetBytes(asciiString);
        return Encoding.ASCII.GetString(bytes.Span);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static AsciiString Slice(in AsciiString asciiString, int offset, int length)
    {
        if (offset < 0 || offset + length > asciiString.Length)
        {
            throw new($"Invalid slice range {offset} .. {offset + length}");
        }

        ReadOnlySpan<byte> slicedBytes = asciiString.Bytes.Slice(offset, length);
        int hash = AsciiStringKey.Fold(Util.Hash(slicedBytes));

        AsciiString sliced = asciiString.BlockData switch
        {
            _blockIsOptimizedString => asciiString with {
                BlockOffset = (ushort)((offset & 0xFF) | (length << 8)),
                Hash = hash
            },
            _blockIsLargeString => AsciiString.From(asciiString.ToString().Substring(offset, length)),
            _ => asciiString with {
                BlockData = (byte)length,
                BlockOffset = (ushort)(asciiString.BlockOffset + offset),
                Hash = hash
            }
        };

        Debug.Assert(sliced.Length == length);
        Debug.Assert(sliced.Bytes.Length == length);
        Debug.Assert(sliced.Bytes.SequenceEqual(slicedBytes));
        Debug.Assert(sliced.Hash == hash);

        return sliced;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSmallBlockString(in AsciiString asciiString) =>
        asciiString.BlockData <= _blockEndIndex;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOptimizedString(in AsciiString asciiString) =>
        asciiString.BlockData == _blockIsOptimizedString;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLargeString(in AsciiString asciiString) =>
        asciiString.BlockData == _blockIsLargeString;

    private static readonly ConcurrentDictionary<AsciiStringKey, AsciiString> _pool = [];
    private static readonly Memory<byte>[] _smallStringBlocks;
    private static readonly SemaphoreSlim _smallStringBlocksLock = new(1, 1);
    private static readonly ConcurrentDictionary<int, Memory<byte>> _largeStrings = [];
    private static readonly Memory<byte>[] _optimizedStrings;

    private static int _currentBlockIndex;
    private static int _currentBlockOffset;
    private static int _currentLargeStringId;
    private const int _blockSize = 65535;

    private const int _blockEndIndex = 250;
    private const int _blockIsOptimizedString = 254;
    private const int _blockIsLargeString = 255;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static AsciiString FetchInternal(
        ReadOnlySpan<byte> memory,
        in AsciiStringKey key)
    {
        if (FetchInternal_TryOptimized(key, out AsciiString str) ||
            _pool.TryGetValue(key, out str))
        {
            return str;
        }

        int foldedHash = key.Fold();

        if (memory.Length <= _blockEndIndex && _currentBlockIndex <= _blockEndIndex)
        {
            str = FetchInternal_CreateSmallBlockString(memory.Length, foldedHash);
            memory.CopyTo(_smallStringBlocks[str.BlockIdx]
                .Slice(str.BlockOffset, memory.Length)
                .Span); // copy into fixed size blocks
        }
        else
        {
            str = FetchInternal_CreateLargeBlockString(foldedHash);
            _largeStrings[GetLargeStringIdx(str)] = memory.ToArray(); // owning copy
        }

        _pool[key] = str;
        return str;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool FetchInternal_TryOptimized(
        in AsciiStringKey key,
        out AsciiString str)
    {
        switch (key.Hash)
        {
            case 17039155849707443173: // Transform
                Debug.Assert(key.Length == 9);
                str = new(0, _blockIsOptimizedString, 9 << 8, key.Fold());
                return true;

            case 1452706007932390838: // Object
                Debug.Assert(key.Length == 6);
                str = new(1, _blockIsOptimizedString, 6 << 8, key.Fold());
                return true;

            case 4950167451912200535: // GameObject
                Debug.Assert(key.Length == 10);
                str = new(2, _blockIsOptimizedString, 10 << 8, key.Fold());
                return true;

            case 8191976625239845070: // Component
                Debug.Assert(key.Length == 9);
                str = new(3, _blockIsOptimizedString, 9 << 8, key.Fold());
                return true;
        }

        str = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static AsciiString FetchInternal_CreateSmallBlockString(
        int length,
        int foldedHash)
    {
        _smallStringBlocksLock.Wait();

        Debug.Assert(length <= _blockEndIndex);
        Debug.Assert(_currentBlockIndex <= _blockEndIndex);

        if (_currentBlockOffset + length > _blockSize)
        {
            ++_currentBlockIndex;
            _currentBlockOffset = 0;
        }

        byte idx = (byte)_currentBlockIndex;
        byte data = (byte)length;
        ushort offset = (ushort)_currentBlockOffset;
        _currentBlockOffset += length;

        _smallStringBlocksLock.Release();

        return new(idx, data, offset, foldedHash);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static AsciiString FetchInternal_CreateLargeBlockString(
        int foldedHash)
    {
        int id = Interlocked.Increment(ref _currentLargeStringId);

        byte idx = (byte)(id & 0xFF);
        byte data = _blockIsLargeString;
        ushort offset = (ushort)(id >> 8);

        return new(idx, data, offset, foldedHash);
    }

    private static int GetLargeStringIdx(AsciiString str) => str.BlockIdx | str.BlockOffset << 8;

    private readonly record struct AsciiStringKey(ulong Hash, int Length)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Fold() => Fold(Hash);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Fold(ulong hash) => (int)((uint)(hash >> 32) ^ (uint)(hash & 0xFFFFFFFF));
    }
}
