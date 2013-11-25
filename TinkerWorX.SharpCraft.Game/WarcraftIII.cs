﻿using EasyHook;
using TinkerWorX.SharpCraft.Core;
using TinkerWorX.SharpCraft.Game.Jass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

namespace TinkerWorX.SharpCraft.Game
{
    public delegate void GameStartEventHandler();
    public delegate void GameEndEventHandler();
    public delegate void MapStartEventHandler();
    public delegate void MapEndEventHandler();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]     // A normal __cdecl function.
    internal delegate Int32 InitNativesPrototype();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]     // An argumentless __cdecl, for use in the __fastcall workaround.
    internal delegate void BindNativePrototype();

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A cheat for __fastcall when there is only one argument.
    internal delegate Int32 StringToJassStringIndexPrototype(IntPtr stringPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A cheat for __fastcall when there is only one argument.
    internal delegate IntPtr JassStringHandleToStringPrototype(IntPtr jassStringHandle);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A normal __thiscall function.
    internal delegate IntPtr CJassConstructorPrototype(IntPtr cJass);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A normal __thiscall function.
    internal delegate Int32 GameStatePrototype(IntPtr _this, Boolean endMap, Boolean endGame);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]  // A normal __thiscall function.
    internal delegate Boolean MousePrototype(IntPtr _this, Single uiX, Single uiY, IntPtr terrainPtr, IntPtr a4);

    public static class WarcraftIII
    {
        public static event GameStartEventHandler GameStart;
        public static event GameEndEventHandler GameEnd;
        public static event MapStartEventHandler MapStart;
        public static event MapEndEventHandler MapEnd;

        public static Vector2 Mouse { internal set; get; }
        public static Vector2 MouseUI { internal set; get; }
        public static Vector3 MouseTerrain { internal set; get; }
        public static Boolean IsMouseOverUI { internal set; get; }

        public static String HackPath { internal set; get; }
        public static String InstallPath { internal set; get; }

        public static IntPtr Module { internal set; get; }
        public static ProcessMemory Memory { internal set; get; }

        private static IntPtr Jass;

        private static IntPtr initNativesPtr;
        private static IntPtr cJassConstructorPtr;
        private static IntPtr gameStatePtr;
        private static IntPtr stringToJassStringIndexPtr;
        private static IntPtr jassStringHandleToStringPtr;
        private static IntPtr bindNativePtr;
        private static IntPtr mousePtr;

        private static InitNativesPrototype initNatives;
        private static CJassConstructorPrototype cJassConstructor;
        private static GameStatePrototype gameState;
        private static StringToJassStringIndexPrototype stringToJassStringIndex;
        private static JassStringHandleToStringPrototype jassStringHandleToString;
        private static MousePrototype mouse;

        private static LocalHook initNativesHook;
        private static LocalHook cJassConstructorHook;
        private static LocalHook gameStateHook;
        private static LocalHook mouseHook;

        private static readonly List<Native> AllNatives = new List<Native>();

        private static readonly List<Native> customNatives = new List<Native>();

        internal static void Initialize()
        {
            WarcraftIII.Module = Kernel32.GetModuleHandle("game.dll");
            WarcraftIII.Memory = ProcessMemory.FromProcess(Process.GetCurrentProcess());

            WarcraftIII.initNativesPtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.InitNatives);
            WarcraftIII.bindNativePtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.BindNative);
            WarcraftIII.stringToJassStringIndexPtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.StringToJassStringIndex);
            WarcraftIII.jassStringHandleToStringPtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.JassStringHandleToString);
            WarcraftIII.cJassConstructorPtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.JassConstructor);
            WarcraftIII.gameStatePtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.GameState);
            WarcraftIII.mousePtr = new IntPtr((UInt32)WarcraftIII.Module + (UInt32)Settings.Current.Addresses.Mouse);

            Trace.WriteLine(" - - Fetching function pointer for InitNatives");
            WarcraftIII.initNatives = (InitNativesPrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.initNativesPtr, typeof(InitNativesPrototype));
            Trace.WriteLine(" - - Fetching function pointer for StringToJassStringIndex");
            WarcraftIII.stringToJassStringIndex = (StringToJassStringIndexPrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.stringToJassStringIndexPtr, typeof(StringToJassStringIndexPrototype));
            Trace.WriteLine(" - - Fetching function pointer for JassStringHandleToString");
            WarcraftIII.jassStringHandleToString = (JassStringHandleToStringPrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.jassStringHandleToStringPtr, typeof(JassStringHandleToStringPrototype));
            Trace.WriteLine(" - - Fetching function pointer for CJassConstructor");
            WarcraftIII.cJassConstructor = (CJassConstructorPrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.cJassConstructorPtr, typeof(CJassConstructorPrototype));
            Trace.WriteLine(" - - Fetching function pointer for GameState");
            WarcraftIII.gameState = (GameStatePrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.gameStatePtr, typeof(GameStatePrototype));
            Trace.WriteLine(" - - Fetching function pointer for Mouse");
            WarcraftIII.mouse = (MousePrototype)Marshal.GetDelegateForFunctionPointer(WarcraftIII.mousePtr, typeof(MousePrototype));

            Trace.WriteLine(" - - Installing InitNatives hook to: 0x" + WarcraftIII.initNativesPtr.ToString("X8"));
            WarcraftIII.initNativesHook = LocalHook.Create(WarcraftIII.initNativesPtr, new InitNativesPrototype(WarcraftIII.InitNativesHook), null);
            WarcraftIII.initNativesHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Trace.WriteLine(" - - Installing CJassConstructor hook to: 0x" + WarcraftIII.cJassConstructorPtr.ToString("X8"));
            WarcraftIII.cJassConstructorHook = LocalHook.Create(WarcraftIII.cJassConstructorPtr, new CJassConstructorPrototype(WarcraftIII.CJassConstructorHook), null);
            WarcraftIII.cJassConstructorHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Trace.WriteLine(" - - Installing GameState hook to: 0x" + WarcraftIII.gameStatePtr.ToString("X8"));
            WarcraftIII.gameStateHook = LocalHook.Create(WarcraftIII.gameStatePtr, new GameStatePrototype(WarcraftIII.GameStateHook), null);
            WarcraftIII.gameStateHook.ThreadACL.SetExclusiveACL(new[] { 0 });
            Trace.WriteLine(" - - Installing MouseHook hook to: 0x" + WarcraftIII.mousePtr.ToString("X8"));
            WarcraftIII.mouseHook = LocalHook.Create(WarcraftIII.mousePtr, new MousePrototype(WarcraftIII.MouseHook), null);
            WarcraftIII.mouseHook.ThreadACL.SetExclusiveACL(new[] { 0 });

            var baseAddress = WarcraftIII.initNativesPtr;
            var offset = 0x05u;
            while (Marshal.ReadByte(new IntPtr((UInt32)baseAddress + offset)) == 0x68)
            {
                WarcraftIII.AllNatives.Add(new Native(new IntPtr((UInt32)baseAddress + offset)));
                offset += 0x14;
            }
        }

        // Wrappers

        private static IntPtr CJassConstructor(IntPtr cJass)
        {
            return WarcraftIII.cJassConstructor(cJass);
        }

        private static Int32 InitNatives()
        {
            return WarcraftIII.initNatives();
        }

        private static Int32 GameState(IntPtr _this, Boolean endMap, Boolean endGame)
        {
            return WarcraftIII.gameState(_this, endMap, endGame);
        }

        private static void BindNative(IntPtr functionPtr, String name, String prototype)
        {
            /* 
             * Manual implementation of a __cdecl function calling a __fastcall function.
             * 1. Allocate Executable memory.
             * 2. Write the function.
             * 3. Call the function.
             * 4. Release allocated memory.
             * TODO: Improve this to a more static function.

             * push, prototype string pointer
             * mov edx, name string pointer
             * mov ecx, function pointer
             * call, BindNative pointer; Remember to calculate the relative offset
             * retn
             */
            var code = new Byte[21];

            using (var writer = new AssemblyWriter(new MemoryStream(code)))
            {
                var codePtr = Kernel32.VirtualAlloc(IntPtr.Zero, code.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);

                writer.Write(Assembly.PushLV, prototype);
                writer.Write(Assembly.MoveEDX, name);
                writer.Write(Assembly.MoveECX, functionPtr);
                writer.Write(Assembly.Call, (UInt32)bindNativePtr - (UInt32)codePtr - (UInt32)writer.BaseStream.Position - 5u); // -5u is to get back to the start of the call instruction, 5 is the size of the instruction.
                writer.Write(Assembly.Return);

                Marshal.Copy(code, 0, codePtr, code.Length);
                var bindNative = (BindNativePrototype)Marshal.GetDelegateForFunctionPointer(codePtr, typeof(BindNativePrototype));
                bindNative();
                Kernel32.VirtualFree(codePtr, code.Length, MemoryFreeType.Release);
            }
        }

        private static void BindNative(Delegate function, String name, String prototype)
        {
            BindNative(Marshal.GetFunctionPointerForDelegate(function), name, prototype);
        }

        internal static Int32 StringToJassStringIndex(String str)
        {
            return stringToJassStringIndex(Marshal.StringToHGlobalAnsi(str));
        }

        internal static String JassStringHandleToString(IntPtr jassStringHandle)
        {
            return Marshal.PtrToStringAnsi(jassStringHandleToString(jassStringHandle));
        }

        internal static IntPtr JassStringIndexToJassStringHandle(Int32 jassStringIndex)
        {
            return (IntPtr)((Int32)Marshal.ReadIntPtr(Marshal.ReadIntPtr(Marshal.ReadIntPtr(Marshal.ReadIntPtr(WarcraftIII.Jass, 0x0C)), 0x2874), 0x0008) + 0x10 * jassStringIndex);
            // the above code may be a bit confusing, but we're essentially doing the following, without needing to
            // find the function every patch, and avoid the convoluted class hierarchy.
            // return Jass->VirtualMachine->StringManager->Table[jassStringIndex];
            // TODO: Future proof this.
        }

        // Hooks

        private static IntPtr CJassConstructorHook(IntPtr cJass)
        {
            var result = WarcraftIII.CJassConstructor(cJass);
            Debug.WriteLine("WarcraftIII.CJassConstructor(cJass:{0})", new object[] { "0x" + cJass.ToString("X8") });

            WarcraftIII.Jass = result;

            return result;
        }

        private static Int32 InitNativesHook()
        {
            var result = WarcraftIII.InitNatives();
            Debug.WriteLine("WarcraftIII.InitNatives()");

            foreach (var native in customNatives)
            {
                WarcraftIII.BindNative(native.Function, native.Name, native.Prototype);
            }

            return result;
        }

        private static Boolean gameStarted = false;
        private static Boolean mapStarted = false;
        private static Int32 GameStateHook(IntPtr _this, Boolean endMap, Boolean endGame)
        {
            Debug.WriteLine("WarcraftIII.GameStateHook(_this:{0}, endMap:{1}, endGame:{2})", new object[] { "0x" + _this.ToString("X8"), endMap, endGame });
            if (endGame)
            {
                if (mapStarted)
                {
                    mapStarted = false;
                    OnMapEnd();
                }
                if (gameStarted)
                {
                    gameStarted = false;
                    OnGameEnd();
                }
            }
            else
            {
                if (endMap)
                {
                    if (mapStarted)
                    {
                        mapStarted = false;
                        OnMapEnd();
                    }
                }
                else
                {
                    if (!gameStarted)
                    {
                        OnGameStart();
                        gameStarted = true;
                    }

                    if (mapStarted)
                    {
                        OnMapEnd();
                    }
                    OnMapStart();
                    mapStarted = true;
                }
            }

            return WarcraftIII.GameState(_this, endMap, endGame);
        }

        private static Boolean MouseHook(IntPtr _this, Single uiX, Single uiY, IntPtr terrainPtr, IntPtr a4)
        {
            var result = mouse(_this, uiX, uiY, terrainPtr, a4);

            WarcraftIII.IsMouseOverUI = !result;
            WarcraftIII.MouseUI = new Vector2(uiX, uiY);
            WarcraftIII.MouseTerrain = (Vector3)Marshal.PtrToStructure(terrainPtr, typeof(Vector3));

            return result;
        }

        // Functions

        private static void OnGameStart()
        {
            if (WarcraftIII.GameStart != null)
                WarcraftIII.GameStart();
        }

        private static void OnGameEnd()
        {
            if (WarcraftIII.GameEnd != null)
                WarcraftIII.GameEnd();
        }

        private static void OnMapStart()
        {
            if (WarcraftIII.MapStart != null)
                WarcraftIII.MapStart();
        }

        private static void OnMapEnd()
        {
            if (WarcraftIII.MapEnd != null)
                WarcraftIII.MapEnd();
        }

        private static void AddNative(Native native)
        {
            customNatives.Add(native);
        }

        public static void AddNative(Delegate function, String name, String prototype)
        {
            AddNative(new Native(function, name, prototype));
        }

        public static void AddNative(Delegate function, String name)
        {
            JassTypeAttribute attribute;

            var prototype = "(";
            foreach (var parameter in function.Method.GetParameters())
            {
                attribute = (JassTypeAttribute)parameter.ParameterType.GetCustomAttributes(typeof(JassTypeAttribute), true).Single();
                prototype += attribute.TypeString;
            }
            prototype += ")";

            if (function.Method.ReturnType == typeof(void))
            {
                prototype += "V";
            }
            else
            {
                attribute = (JassTypeAttribute)function.Method.ReturnType.GetCustomAttributes(typeof(JassTypeAttribute), true).Single();
                prototype += attribute.TypeString;
            }

            AddNative(function, name, prototype);
        }

        public static void AddNative(Delegate function)
        {
            AddNative(function, function.Method.Name);
        }

        public static Native GetNative(String name)
        {
            return WarcraftIII.AllNatives.First(native => native.Name == name);
        }
    }
}
