﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using Serilog.Events;

namespace NetCoreVstHost
{
    public static class ObjectHandleExtensions
    {
        public static IntPtr ToIntPtr(this object target)
        {
            return GCHandle.Alloc(target).ToIntPtr();
        }

        public static GCHandle ToGcHandle(this object target)
        {
            return GCHandle.Alloc(target);
        }

        public static IntPtr ToIntPtr(this GCHandle target)
        {
            return GCHandle.ToIntPtr(target);
        }
    }
    public static class NativeMethods
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string fileName);

        // [DllImport("kernel32", SetLastError = true)]
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr module, string procedureName);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        public static string GetLibraryPathName(string filePath)
        {
            // If 64-bit process, load 64-bit DLL
            bool is64bit = System.Environment.Is64BitProcess;

            // default is 32 bit
            // like 7zip.wcx
            string suffix = "";

            if (is64bit)
            {
                // the 64 bit version is 7zip.wcx64
                suffix = "64";
            }

            var libPath = filePath + suffix;
            return libPath;
        }
    }

    /// <summary>
    /// tresult wrapper
    /// </summary>    
    public enum tresult
    {
        kNoInterface = unchecked((int)0x80004002),  // E_NOINTERFACE
        kResultOk = unchecked((int)0x00000000), // S_OK
        kResultTrue = kResultOk,
        kResultFalse = unchecked((int)0x00000001),  // S_FALSE
        kInvalidArgument = unchecked((int)0x80070057),  // E_INVALIDARG
        kNotImplemented = unchecked((int)0x80004001),   // E_NOTIMPL
        kInternalError = unchecked((int)0x80004005),    // E_FAIL
        kNotInitialized = unchecked((int)0x8000FFFF),   // E_UNEXPECTED
        kOutOfMemory = unchecked((int)0x8007000EL)		// E_OUTOFMEMORY        
    }

    #region Structs

    // https://docs.microsoft.com/en-us/dotnet/framework/interop/default-marshaling-for-strings
    // Ansi: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    // char *  f1;                      => [MarshalAs(UnmanagedType.LPStr)] public string f1;
    // char    f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;

    // Unicode: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    // WCHAR * f1;                      => [MarshalAs(UnmanagedType.LPWStr)] public string f1;
    // WCHAR   f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;
    // BSTR    f3;                      => [MarshalAs(UnmanagedType.BStr)] public string f3;

    // Auto: [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    // TCHAR * f1;                      => [MarshalAs(UnmanagedType.LPTStr)] public string f1;
    // TCHAR   f2[256];                 => [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string f2;

    [Flags]
    public enum FactoryFlags
    {
        kNoFlags = 0,		         ///< Nothing
		kClassesDiscardable = 1 << 0,	///< The number of exported classes can change each time the Module is loaded. If this flag is set, the host does not cache class information. This leads to a longer startup time because the host always has to load the Module to get the current class information.
		kLicenseCheck = 1 << 1,	///< Class IDs of components are interpreted as Syncrosoft-License (LICENCE_UID). Loaded in a Steinberg host, the module will not be loaded when the license is not valid
		kComponentNonDiscardable = 1 << 3,	///< Component won't be unloaded until process exit
		kUnicode = 1 << 4    ///< Components have entirely unicode encoded strings. (True for VST 3 Plug-ins so far)
	};

    //------------------------------------------------------------------------
    /** Component Flags used as classFlags in PClassInfo2 */
    //------------------------------------------------------------------------
    [Flags]
    public enum ComponentFlags
    {
        kDistributable = 1 << 0,    ///< Component can be run on remote computer
        kSimpleModeSupported = 1 << 1   ///< Component supports simple IO mode (or works in simple mode anyway) see \ref vst3IoMode
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct PFactoryInfo
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string vendor;  ///< e.g. "Steinberg Media Technologies"
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string url;    ///< e.g. "http://www.steinberg.de"
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string email;  ///< e.g. "info@steinberg.de"
        [MarshalAs(UnmanagedType.I4)] public FactoryFlags flags; ///< (see above)
    }

    public enum ClassCardinality
    {
        kManyInstances = 0x7FFFFFFF
    };


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct PClassInfo
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] cid; ///< Class ID 16 Byte class GUID         
        // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string cid; ///< Class ID 16 Byte class GUID         
        [MarshalAs(UnmanagedType.I4)] public ClassCardinality cardinality; ///< cardinality of the class, set to kManyInstances (see \ref ClassCardinality)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string category; ///< class category, host uses this to categorize interfaces
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string name; ///< class name, visible to the user
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct PClassInfo2
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] cid; ///< Class ID 16 Byte class GUID         
        // [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] public string cid; ///< Class ID 16 Byte class GUID         
        [MarshalAs(UnmanagedType.I4)] public ClassCardinality cardinality; ///< cardinality of the class, set to kManyInstances (see \ref ClassCardinality)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string category; ///< class category, host uses this to categorize interfaces
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string name; ///< class name, visible to the user
        [MarshalAs(UnmanagedType.I4)] public ComponentFlags classFlags; ///< flags used for a specific category, must be defined where category is defined
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string subCategories;  ///< module specific subcategories, can be more than one, logically added by the \c OR operator
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string vendor; ///< overwrite vendor information from factory info
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string version; ///< Version string (e.g. "1.0.0.512" with Major.Minor.Subversion.Build)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string sdkVersion;	///< SDK version used to build this class (e.g. "VST 3.0")
    }

    [StructLayout(LayoutKind.Sequential)]
    public class FUnknown
    {
        // class FUnknown
        /** Query for a pointer to the specified interface.
        Returns kResultOk on success or kNoInterface if the object does not implement the interface.
        The object has to call addRef when returning an interface.
        \param iid : (in) 16 Byte interface identifier (-> FUID)
        \param obj : (out) On return, *obj points to the requested interface */
        public IntPtr queryInterface;

        /** Adds a reference and return the new reference count.
        \par Remarks:
            The initial reference count after creating an object is 1. */
        public IntPtr addRef;

        /** Releases a reference and return the new reference count.
        If the reference count reaches zero, the object will be destroyed in memory. */
        public IntPtr release;

        // static const FUID iid;
        // [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] iid; ///< Class ID 16 Byte class GUID        
    }

    [StructLayout(LayoutKind.Sequential)]
    public class IPluginFactory : FUnknown
    {
        // class IPluginFactory 
        /** Fill a PFactoryInfo structure with information about the Plug-in vendor. */
        public IntPtr getFactoryInfo;

        /** Returns the number of exported classes by this factory.
        If you are using the CPluginFactory implementation provided by the SDK, it returns the number of classes you registered with CPluginFactory::registerClass. */
        public IntPtr countClasses;

        /** Fill a PClassInfo structure with information about the class at the specified index. */
        public IntPtr getClassInfo;

        /** Create a new class instance. */
        public IntPtr createInstance;

        // static const FUID iid;
        // [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] iid; ///< Class ID 16 Byte class GUID        
    };

    [StructLayout(LayoutKind.Sequential)]
    public class IPluginFactory2 : IPluginFactory
    {
        // class IPluginFactory2 
        /** Returns the class info (version 2) for a given index. */
        public IntPtr getClassInfo2;

        // static const FUID iid;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] iid; ///< Class ID 16 Byte class GUID        
    };

    [StructLayout(LayoutKind.Sequential)]
    public class IPluginBase : FUnknown
    {

        /** The host passes a number of interfaces as context to initialize the Plug-in class.
            @note Extensive memory allocations etc. should be performed in this method rather than in the class' constructor!
            If the method does NOT return kResultOk, the object is released immediately. In this case terminate is not called! */
        public IntPtr initialize;

        /** This function is called before the Plug-in is unloaded and can be used for
            cleanups. You have to release all references to any host application interfaces. */
        public IntPtr terminate;

        // static const FUID iid;
    };

    public enum IoModes : Int32
    {
        kSimple = 0,                ///< 1:1 Input / Output. Only used for Instruments. See \ref vst3IoMode
        kAdvanced,                  ///< n:m Input / Output. Only used for Instruments.
        kOfflineProcessing          ///< Plug-in used in an offline processing context
    };

    public enum MediaTypes : Int32
    {
        kAudio = 0,                 ///< audio
        kEvent,                     ///< events
        kNumMediaTypes
    };

    public enum BusDirections : Int32
    {
        kInput = 0,                 ///< input bus
        kOutput                     ///< output bus
    };

    public enum BusTypes : Int32
    {
        kMain = 0,                  ///< main bus
        kAux                        ///< auxiliary bus (sidechain)
    };

    [Flags]
    public enum BusFlags
    {
        kDefaultActive = 1 << 0     ///< bus active per default
	};


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)] // charset is Ansi not Unicode
    public struct BusInfo
    {
        [MarshalAs(UnmanagedType.I4)] public MediaTypes mediaType;       ///< Media type - has to be a value of \ref MediaTypes
        [MarshalAs(UnmanagedType.I4)] public BusDirections direction;    ///< input or output \ref BusDirections
        [MarshalAs(UnmanagedType.I4)] public int channelCount;         ///< number of channels (if used then need to be recheck after \ref
                                    /// IAudioProcessor::setBusArrangements is called).
                                    /// For a bus of type MediaTypes::kEvent the channelCount corresponds
                                    /// to the number of supported MIDI channels by this bus
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string name; ///< name of the bus

        [MarshalAs(UnmanagedType.I4)] public BusTypes busType;           ///< main or aux - has to be a value of \ref BusTypes
        [MarshalAs(UnmanagedType.I4)] public BusFlags flags;             ///< flags - a combination of \ref BusFlags
    };


    /** Routing Information:
    When the Plug-in supports multiple I/O buses, a host may want to know how the buses are related. The
    relation of an event-input-channel to an audio-output-bus in particular is of interest to the host
    (in order to relate MIDI-tracks to audio-channels)
    \n See also: IComponent::getRoutingInfo, \ref vst3Routing */
    [StructLayout(LayoutKind.Sequential)]
    public struct RoutingInfo
    {
        [MarshalAs(UnmanagedType.I4)] public MediaTypes mediaType;   ///< media type see \ref MediaTypes
        [MarshalAs(UnmanagedType.I4)] public int busIndex;         ///< bus index
        [MarshalAs(UnmanagedType.I4)] public int channel;          ///< channel (-1 for all channels)
    };


    [StructLayout(LayoutKind.Sequential)]
    public class IComponent : IPluginBase
    {
        /** Called before initializing the component to get information about the controller class. */
        public IntPtr getControllerClassId;

        /** Called before 'initialize' to set the component usage (optional). See \ref IoModes */
        public IntPtr setIoMode;

        /** Called after the Plug-in is initialized. See \ref MediaTypes, BusDirections */
        public IntPtr getBusCount;

        /** Called after the Plug-in is initialized. See \ref MediaTypes, BusDirections */
        public IntPtr getBusInfo;

        /** Retrieves routing information (to be implemented when more than one regular input or output bus exists).
            The inInfo always refers to an input bus while the returned outInfo must refer to an output bus! */
        public IntPtr getRoutingInfo;

        /** Called upon (de-)activating a bus in the host application. The Plug-in should only processed an activated bus,
            the host could provide less see \ref AudioBusBuffers in the process call (see \ref IAudioProcessor::process) if last buses are not activated */
        public IntPtr activateBus;

        /** Activates / deactivates the component. */
        public IntPtr setActive;

        /** Sets complete state of component. */
        public IntPtr setState;

        /** Retrieves complete state of component. */
        public IntPtr getState;

        // static const FUID iid;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class IAudioProcessor : FUnknown
    {
        /** Try to set (from host) a predefined arrangement for inputs and outputs.
            The host should always deliver the same number of input and output buses than the Plug-in needs 
            (see \ref IComponent::getBusCount).
            The Plug-in returns kResultFalse if wanted arrangements are not supported.
            If the Plug-in accepts these arrangements, it should modify its buses to match the new arrangements
            (asked by the host with IComponent::getInfo () or IAudioProcessor::getBusArrangement ()) and then return kResultTrue.
            If the Plug-in does not accept these arrangements, but can adapt its current arrangements (according to the wanted ones),
            it should modify its buses arrangements and return kResultFalse. */
        public IntPtr setBusArrangements;

        /** Gets the bus arrangement for a given direction (input/output) and index.
            Note: IComponent::getInfo () and IAudioProcessor::getBusArrangement () should be always return the same 
            information about the buses arrangements. */
        public IntPtr getBusArrangement;

        /** Asks if a given sample size is supported see \ref SymbolicSampleSizes. */
        public IntPtr canProcessSampleSize;

        /** Gets the current Latency in samples.
            The returned value defines the group delay or the latency of the Plug-in. For example, if the Plug-in internally needs
            to look in advance (like compressors) 512 samples then this Plug-in should report 512 as latency.
            If during the use of the Plug-in this latency change, the Plug-in has to inform the host by
            using IComponentHandler::restartComponent (kLatencyChanged), this could lead to audio playback interruption
            because the host has to recompute its internal mixer delay compensation.
            Note that for player live recording this latency should be zero or small. */
        public IntPtr getLatencySamples;

        /** Called in disable state (not active) before processing will begin. */
        public IntPtr setupProcessing;

        /** Informs the Plug-in about the processing state. This will be called before any process calls start with true and after with false.
            Note that setProcessing (false) may be called after setProcessing (true) without any process calls.
            In this call the Plug-in should do only light operation (no memory allocation or big setup reconfiguration), 
            this could be used to reset some buffers (like Delay line or Reverb). */
        public IntPtr setProcessing;

        /** The Process call, where all information (parameter changes, event, audio buffer) are passed. */
        public IntPtr process;

        /** Gets tail size in samples. For example, if the Plug-in is a Reverb Plug-in and it knows that
            the maximum length of the Reverb is 2sec, then it has to return in getTailSamples() 
            (in VST2 it was getGetTailSize ()): 2*sampleRate.
            This information could be used by host for offline processing, process optimization and 
            downmix (avoiding signal cut (clicks)).
            It should return:
             - kNoTail when no tail
             - x * sampleRate when x Sec tail.
             - kInfiniteTail when infinite tail. */
        public IntPtr getTailSamples;

        // static const FUID iid;
    };

    [StructLayout(LayoutKind.Sequential)]
    public class IComponentHandler : FUnknown
    {
        /** To be called before calling a performEdit (e.g. on mouse-click-down event). */
        public IntPtr beginEdit;

        /** Called between beginEdit and endEdit to inform the handler that a given parameter has a new value. */
        public IntPtr performEdit;

        /** To be called after calling a performEdit (e.g. on mouse-click-up event). */
        public IntPtr endEdit;

        /** Instructs host to restart the component. This should be called in the UI-Thread context!
        \param flags is a combination of RestartFlags */
        public IntPtr restartComponent;

        // static const FUID iid;
    };

    public enum IStreamSeekMode : Int32
    {
        kIBSeekSet = 0, ///< set absolute seek position
		kIBSeekCur,     ///< set seek position relative to current position
		kIBSeekEnd      ///< set seek position relative to stream end
	}

    /** Base class for streams.
    \ingroup pluginBase
    - read/write binary data from/to stream
    - get/set stream read-write position (read and write position is the same)
    */
    [StructLayout(LayoutKind.Sequential)]
    public class IBStream : FUnknown
    {
        /** Reads binary data from stream.
        \param buffer : destination buffer
        \param numBytes : amount of bytes to be read
        \param numBytesRead : result - how many bytes have been read from stream (set to 0 if this is of no interest) */
        public IntPtr read;

        /** Writes binary data to stream.
        \param buffer : source buffer
        \param numBytes : amount of bytes to write
        \param numBytesWritten : result - how many bytes have been written to stream (set to 0 if this is of no interest) */
        public IntPtr write;

        /** Sets stream read-write position. 
        \param pos : new stream position (dependent on mode)
        \param mode : value of enum IStreamSeekMode
        \param result : new seek position (set to 0 if this is of no interest) */
        public IntPtr seek;

        /** Gets current stream read-write position. 
        \param pos : is assigned the current position if function succeeds */
        public IntPtr tell;

        // static const FUID iid;
    }

    #endregion

    #region Delegates
    // Mandatory VST3 Methods
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate IntPtr GetPluginFactoryDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InitDllDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool ExitDllDelegate();


    // IPluginFactory
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntPtr IPluginFactoryGetFactoryInfoDelegate(IntPtr thisPtr, [In, Out] ref PFactoryInfo info);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate int IPluginFactoryCountClassesDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntPtr IPluginFactoryGetClassInfoDelegate(IntPtr thisPtr, int index, [In, Out] ref PClassInfo info);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    // FIDString = char8*
    // public delegate IntPtr IPluginFactoryCreateInstanceDelegate(IntPtr thisPtr, string cid, string iid, ref IntPtr obj);
    public delegate IntPtr IPluginFactoryCreateInstanceDelegate(IntPtr thisPtr, byte[] cid, byte[] iid, ref IntPtr obj);


    // IPluginFactory2
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate IntPtr IPluginFactory2GetClassInfo2Delegate(IntPtr thisPtr, int index, [In, Out] ref PClassInfo2 info);


    // FUnknown
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    // public delegate tresult FUnknownQueryInterfaceDelegate(IntPtr thisPtr, [MarshalAs(UnmanagedType.LPStr)] ref string iid, IntPtr obj);
    public delegate tresult FUnknownQueryInterfaceDelegate(IntPtr thisPtr, byte[] iid, ref IntPtr obj);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult FUnknownAddRefDelegate(IntPtr thisPtr);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult FUnknownReleaseDelegate(IntPtr thisPtr);


    // IPluginBase
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    // public delegate tresult IPluginBaseInitializeDelegate(IntPtr thisPtr, [In, Out] FUnknown[] context);
    public delegate tresult IPluginBaseInitializeDelegate(IntPtr thisPtr, [In, Out] ref IntPtr context);

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IPluginBaseTerminateDelegate(IntPtr thisPtr);


    // IComponent
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    // public delegate tresult IComponentGetControllerClassIdDelegate(IntPtr thisPtr, [MarshalAs(UnmanagedType.LPStr)] StringBuilder classId);
    public delegate tresult IComponentGetControllerClassIdDelegate(IntPtr thisPtr, byte[] classId);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentSetIoModeDelegate(IntPtr thisPtr, IoModes mode);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate int IComponentGetBusCountDelegate(IntPtr thisPtr, MediaTypes type, BusDirections dir);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentGetBusInfoDelegate(IntPtr thisPtr, MediaTypes type, BusDirections dir, int index, [In, Out] ref BusInfo bus);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentGetRoutingInfoDelegate(IntPtr thisPtr, [In, Out] ref RoutingInfo inInfo, [In, Out] ref RoutingInfo outInfo);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentActivateBusDelegate(IntPtr thisPtr, MediaTypes type, BusDirections dir, Int32 index, bool state);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentSetActiveDelegate(IntPtr thisPtr, bool state);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentSetStateDelegate(IntPtr thisPtr, IBStream state);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IComponentGetStateDelegate(IntPtr thisPtr, IBStream state);


    // IBStream
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IBStreamReadDelegate(IntPtr thisPtr, IntPtr buffer, Int32 numBytes, IntPtr numBytesRead);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IBStreamWriteDelegate(IntPtr thisPtr, IntPtr buffer, Int32 numBytes, IntPtr numBytesWritten);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IBStreamSeekDelegate(IntPtr thisPtr, Int64 pos, Int32 mode, IntPtr result);
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    public delegate tresult IBStreamTellDelegate(IntPtr thisPtr, IntPtr pos);
    #endregion

    class Program
    {
        // ivstaudioprocessor.h

        // Category Name for Audio Processor Component
        const string kVstAudioEffectClass = "Audio Module Class";

        // Plug-in Type used for subCategories 
        const string kFxAnalyzer = "Fx|Analyzer";   ///< Scope, FFT-Display, Loudness Processing...
        const string kFxDelay = "Fx|Delay";     ///< Delay, Multi-tap Delay, Ping-Pong Delay...
        const string kFxDistortion = "Fx|Distortion";   ///< Amp Simulator, Sub-Harmonic, SoftClipper...
        const string kFxDynamics = "Fx|Dynamics";   ///< Compressor, Expander, Gate, Limiter, Maximizer, Tape Simulator, EnvelopeShaper...
        const string kFxEQ = "Fx|EQ";           ///< Equalization, Graphical EQ...
        const string kFxFilter = "Fx|Filter";       ///< WahWah, ToneBooster, Specific Filter,...
        const string kFx = "Fx";                ///< others type (not categorized)
        const string kFxInstrument = "Fx|Instrument";   ///< Fx which could be loaded as Instrument too
        const string kFxInstrumentExternal = "Fx|Instrument|External";  ///< Fx which could be loaded as Instrument too and is external (wrapped Hardware)
        const string kFxSpatial = "Fx|Spatial";     ///< MonoToStereo, StereoEnhancer,...
        const string kFxGenerator = "Fx|Generator"; ///< Tone Generator, Noise Generator...
        const string kFxMastering = "Fx|Mastering"; ///< Dither, Noise Shaping,...
        const string kFxModulation = "Fx|Modulation";   ///< Phaser, Flanger, Chorus, Tremolo, Vibrato, AutoPan, Rotary, Cloner...
        const string kFxPitchShift = "Fx|Pitch Shift";  ///< Pitch Processing, Pitch Correction, Vocal Tuning...
        const string kFxRestoration = "Fx|Restoration"; ///< Denoiser, Declicker,...
        const string kFxReverb = "Fx|Reverb";       ///< Reverberation, Room Simulation, Convolution Reverb...
        const string kFxSurround = "Fx|Surround";   ///< dedicated to surround processing: LFE Splitter, Bass Manager...
        const string kFxTools = "Fx|Tools";     ///< Volume, Mixer, Tuner...
        const string kFxNetwork = "Fx|Network";     ///< using Network

        const string kInstrument = "Instrument";            ///< Effect used as instrument (sound generator), not as insert
        const string kInstrumentDrum = "Instrument|Drum";   ///< Instrument for Drum sounds
        const string kInstrumentExternal = "Instrument|External";///< External Instrument (wrapped Hardware)
        const string kInstrumentPiano = "Instrument|Piano"; ///< Instrument for Piano sounds
        const string kInstrumentSampler = "Instrument|Sampler"; ///< Instrument based on Samples
        const string kInstrumentSynth = "Instrument|Synth"; ///< Instrument based on Synthesis
        const string kInstrumentSynthSampler = "Instrument|Synth|Sampler";  ///< Instrument based on Synthesis and Samples

        const string kSpatial = "Spatial";      ///< used for SurroundPanner
        const string kSpatialFx = "Spatial|Fx";     ///< used for SurroundPanner and as insert effect
        const string kOnlyRealTime = "OnlyRT";          ///< indicates that it supports only realtime process call, no processing faster than realtime
        const string kOnlyOfflineProcess = "OnlyOfflineProcess";    ///< used for Plug-in offline processing  (will not work as normal insert Plug-in)
        const string kNoOfflineProcess = "NoOfflineProcess";    ///< will be NOT used for Plug-in offline processing (will work as normal insert Plug-in)
        const string kUpDownMix = "Up-Downmix";     ///< used for Mixconverter/Up-Mixer/Down-Mixer
        const string kAnalyzer = "Analyzer";        ///< Meter, Scope, FFT-Display, not selectable as insert plugin
        const string kAmbisonics = "Ambisonics";        ///< used for Ambisonics channel (FX or Panner/Mixconverter/Up-Mixer/Down-Mixer when combined with other category)

        const string kMono = "Mono";            ///< used for Mono only Plug-in [optional]
        const string kStereo = "Stereo";            ///< used for Stereo only Plug-in [optional]
        const string kSurround = "Surround";        ///< used for Surround only Plug-in [optional]

        // FUIDs
        static readonly byte[] FUnknownFUID = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        static readonly byte[] IComponentFUID = new byte[] { 0xE8, 0x31, 0xFF, 0x31, 0xF2, 0xD5, 0x43, 0x01, 0x92, 0x8E, 0xBB, 0xEE, 0x25, 0x69, 0x78, 0x02 };
        static readonly byte[] IPluginFactoryFUID = new byte[] { 0x7A, 0x4D, 0x81, 0x1C, 0x52, 0x11, 0x4A, 0x1F, 0xAE, 0xD9, 0xD2, 0xEE, 0x0B, 0x43, 0xBF, 0x9F };
        static readonly byte[] IPluginFactory2FUID = new byte[] { 0x00, 0x07, 0xB6, 0x50, 0xF2, 0x4B, 0x4C, 0x0B, 0xA4, 0x64, 0xED, 0xB9, 0xF0, 0x0B, 0x2A, 0xBB };
        static readonly byte[] IEditControllerFUID = new byte[] { 0xDC, 0xD7, 0xBB, 0xE3, 0x77, 0x42, 0x44, 0x8D, 0xA8, 0x74, 0xAA, 0xCC, 0x97, 0x9C, 0x75, 0x9E };

        // remember to create COM objects within a single thread (STA) 
        [STAThread]
        static void Main(string[] args)
        {
            bool doVerbose = true;

            // Setup Logger
            var logConfig = new LoggerConfiguration()
                .WriteTo.Console(LogEventLevel.Debug)
                ;
            logConfig.MinimumLevel.Verbose();
            Log.Logger = logConfig.CreateLogger();

            Log.Information("Vst3 Host");

            string vstPath = args[0];

            IntPtr hModule = IntPtr.Zero;

            // store filename
            string vstFileName = Path.GetFileName(vstPath);

            try
            {
                // load library
                hModule = NativeMethods.LoadLibrary(vstPath);

                // error handling
                if (hModule == IntPtr.Zero)
                {
                    Log.Error("Failed opening {0}", vstPath);
                    return;
                }
                else
                {
                    if (doVerbose)
                    {
                        Log.Debug("VST plugin loaded '{0}' at {1}.", vstFileName, hModule.ToString("X"));
                    }
                    else
                    {
                        Log.Information("VST plugin loaded '{0}'.", vstFileName);
                    }
                }

                // mandatory functions
                IntPtr pGetPluginFactory = NativeMethods.GetProcAddress(hModule, "GetPluginFactory");
                if (doVerbose)
                {
                    if (pGetPluginFactory != IntPtr.Zero) { Log.Debug("{0} found at {1}", "GetPluginFactory", pGetPluginFactory.ToString("X")); }
                }

                IntPtr pInitDll = NativeMethods.GetProcAddress(hModule, "InitDll");
                if (doVerbose)
                {
                    if (pInitDll != IntPtr.Zero) { Log.Debug("{0} found at {1}", "InitDll", pInitDll.ToString("X")); }
                }

                IntPtr pExitDll = NativeMethods.GetProcAddress(hModule, "ExitDll");
                if (doVerbose)
                {
                    if (pExitDll != IntPtr.Zero) { Log.Debug("{0} found at {1}", "ExitDll", pExitDll.ToString("X")); }
                }

                // Instantiate the plugin
                GetPluginFactoryDelegate GetFactoryProc = null;
                if (pGetPluginFactory != IntPtr.Zero)
                {
                    GetFactoryProc = (GetPluginFactoryDelegate)Marshal.GetDelegateForFunctionPointer(
                            pGetPluginFactory,
                            typeof(GetPluginFactoryDelegate));
                }

                InitDllDelegate InitDllProc = null;
                if (pInitDll != IntPtr.Zero)
                {
                    InitDllProc = (InitDllDelegate)Marshal.GetDelegateForFunctionPointer(
                            pInitDll,
                            typeof(InitDllDelegate));
                }

                ExitDllDelegate ExitDllProc = null;
                if (pExitDll != IntPtr.Zero)
                {
                    ExitDllProc = (ExitDllDelegate)Marshal.GetDelegateForFunctionPointer(
                            pExitDll,
                            typeof(ExitDllDelegate));
                }

                if (InitDllProc != null)
                {
                    if (!InitDllProc())
                    {
                        if (doVerbose)
                        {
                            Log.Debug("Failed initializing vst3 plugin");
                            NativeMethods.FreeLibrary(hModule);
                            return;
                        }
                    }
                }

                if (GetFactoryProc != null)
                {
                    IntPtr factoryPtr = GetFactoryProc();
                    IntPtr factoryVtblPtr = Marshal.ReadIntPtr(factoryPtr, 0);
                    // IPluginFactory factory = (IPluginFactory)Marshal.PtrToStructure(factoryVtblPtr, typeof(IPluginFactory));
                    IPluginFactory2 factory2 = (IPluginFactory2)Marshal.PtrToStructure(factoryVtblPtr, typeof(IPluginFactory2));

                    IPluginFactoryGetFactoryInfoDelegate getFactoryInfo = (IPluginFactoryGetFactoryInfoDelegate)Marshal.GetDelegateForFunctionPointer(factory2.getFactoryInfo, typeof(IPluginFactoryGetFactoryInfoDelegate));
                    IPluginFactoryCountClassesDelegate countClasses = (IPluginFactoryCountClassesDelegate)Marshal.GetDelegateForFunctionPointer(factory2.countClasses, typeof(IPluginFactoryCountClassesDelegate));
                    IPluginFactoryGetClassInfoDelegate getClassInfo = (IPluginFactoryGetClassInfoDelegate)Marshal.GetDelegateForFunctionPointer(factory2.getClassInfo, typeof(IPluginFactoryGetClassInfoDelegate));
                    IPluginFactory2GetClassInfo2Delegate getClassInfo2 = (IPluginFactory2GetClassInfo2Delegate)Marshal.GetDelegateForFunctionPointer(factory2.getClassInfo2, typeof(IPluginFactory2GetClassInfo2Delegate));
                    IPluginFactoryCreateInstanceDelegate createInstance = (IPluginFactoryCreateInstanceDelegate)Marshal.GetDelegateForFunctionPointer(factory2.createInstance, typeof(IPluginFactoryCreateInstanceDelegate));

                    FUnknownReleaseDelegate factoryRelease = (FUnknownReleaseDelegate)Marshal.GetDelegateForFunctionPointer(factory2.release, typeof(FUnknownReleaseDelegate));
                    FUnknownQueryInterfaceDelegate factoryQuery = (FUnknownQueryInterfaceDelegate)Marshal.GetDelegateForFunctionPointer(factory2.queryInterface, typeof(FUnknownQueryInterfaceDelegate));

                    PFactoryInfo fi = new PFactoryInfo();
                    getFactoryInfo(factoryPtr, ref fi);
                    if (doVerbose)
                    {
                        Log.Debug("vendor: '{0}', url: '{1}', email: '{2}', flags: '{3}'", fi.vendor, fi.url, fi.email, fi.flags);
                    }

                    // check what interfaces are implemented
                    var unknown = new FUnknown();
                    IntPtr unknownPtr = unknown.ToIntPtr();
                    tresult queryRes;
                    queryRes = factoryQuery(factoryPtr, IComponentFUID, ref unknownPtr);
                    if (doVerbose)
                    {
                        Log.Debug("IComponentFUID interface query result: {0}", queryRes);
                    }

                    queryRes = factoryQuery(factoryPtr, FUnknownFUID, ref unknownPtr);
                    if (doVerbose)
                    {
                        Log.Debug("FUnknownFUID interface query result: {0}", queryRes);
                    }

                    queryRes = factoryQuery(factoryPtr, IPluginFactoryFUID, ref unknownPtr);
                    if (doVerbose)
                    {
                        Log.Debug("IPluginFactoryFUID interface query result: {0}", queryRes);
                    }

                    queryRes = factoryQuery(factoryPtr, IPluginFactory2FUID, ref unknownPtr);
                    if (doVerbose)
                    {
                        Log.Debug("IPluginFactory2FUID interface query result: {0}", queryRes);
                    }

                    queryRes = factoryQuery(factoryPtr, IEditControllerFUID, ref unknownPtr);
                    if (doVerbose)
                    {
                        Log.Debug("IEditControllerFUID interface query result: {0}", queryRes);
                    }

                    // count classes
                    var numClasses = countClasses(factoryPtr);
                    for (int i = 0; i < numClasses; i++)
                    {
                        // PClassInfo ci = new PClassInfo();
                        // getClassInfo(factoryPtr, i, ref ci);
                        PClassInfo2 ci = new PClassInfo2();
                        getClassInfo2(factoryPtr, i, ref ci);

                        if (doVerbose)
                        {
                            // Log.Debug("id: '{0}', '{1}', '{2}'", Encoding.Default.GetString(ci.cid), ci.category, ci.name);
                            Log.Debug("id: '{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}'", Encoding.Default.GetString(ci.cid), ci.category, ci.name, ci.classFlags, ci.subCategories, ci.vendor, ci.version, ci.sdkVersion);
                        }

                        if (ci.category == kVstAudioEffectClass)
                        {
                            Log.Information("Found Audio Processor ('{0}') for: {1}", ci.category, ci.name);

                            IComponent placeholderComp = new IComponent();
                            IntPtr compPtr = placeholderComp.ToIntPtr();
                            createInstance(factoryPtr, ci.cid, FUnknownFUID, ref compPtr);
                            IntPtr compVtblPtr = Marshal.ReadIntPtr(compPtr, 0);
                            IComponent comp = (IComponent)Marshal.PtrToStructure(compVtblPtr, typeof(IComponent));

                            if (doVerbose)
                            {
                                Log.Debug("initialize: {0}, terminate: {1}, getControllerClassId: {2}, setIoMode: {3}, getBusCount: {4}, getBusInfo: {5}, getRoutingInfo: {6}, activateBus: {7}, setActive: {8}, setState: {9}, getState: {10}",
                                                                                            comp.initialize.ToString("X"),
                                                                                            comp.terminate.ToString("X"),
                                                                                            comp.getControllerClassId.ToString("X"),
                                                                                            comp.setIoMode.ToString("X"),
                                                                                            comp.getBusCount.ToString("X"),
                                                                                            comp.getBusInfo.ToString("X"),
                                                                                            comp.getRoutingInfo.ToString("X"),
                                                                                            comp.activateBus.ToString("X"),
                                                                                            comp.setActive.ToString("X"),
                                                                                            comp.setState.ToString("X"),
                                                                                            comp.getState.ToString("X"));
                            }

                            IPluginBaseInitializeDelegate initialize = (IPluginBaseInitializeDelegate)Marshal.GetDelegateForFunctionPointer(comp.initialize, typeof(IPluginBaseInitializeDelegate));
                            IPluginBaseTerminateDelegate terminate = (IPluginBaseTerminateDelegate)Marshal.GetDelegateForFunctionPointer(comp.terminate, typeof(IPluginBaseTerminateDelegate));

                            IComponentGetBusCountDelegate getBusCount = (IComponentGetBusCountDelegate)Marshal.GetDelegateForFunctionPointer(comp.getBusCount, typeof(IComponentGetBusCountDelegate));
                            IComponentGetBusInfoDelegate getBusInfo = (IComponentGetBusInfoDelegate)Marshal.GetDelegateForFunctionPointer(comp.getBusInfo, typeof(IComponentGetBusInfoDelegate));
                            IComponentGetControllerClassIdDelegate getControllerClassId = (IComponentGetControllerClassIdDelegate)Marshal.GetDelegateForFunctionPointer(comp.getControllerClassId, typeof(IComponentGetControllerClassIdDelegate));
                            IComponentGetRoutingInfoDelegate getRoutingInfo = (IComponentGetRoutingInfoDelegate)Marshal.GetDelegateForFunctionPointer(comp.getRoutingInfo, typeof(IComponentGetRoutingInfoDelegate));
                            IComponentSetIoModeDelegate setIoMode = (IComponentSetIoModeDelegate)Marshal.GetDelegateForFunctionPointer(comp.setIoMode, typeof(IComponentSetIoModeDelegate));

                            // now `comp` shall be valid pointer of Vst::IComponent.

                            // initialize comp
                            tresult setIoModeRes = setIoMode(compPtr, IoModes.kAdvanced);
                            if (doVerbose)
                            {
                                Log.Debug("setIoMode: {0}", setIoModeRes);
                            }

                            // you should define host context object before and pass it here as `FUnknown *`.
                            // the host context object is the class which normally derives Vst::IHostApplication,
                            // Vst::IComponentHandler, Vst::IPluginInterfaceSupport, etc.
                            // comp->initialize(host_context);

                            // find the relevent IEditController class id
                            // (in detail, IEditController interface may be obtained from IComponent directly if the plugin
                            //  derives SingleComponentEffect.
                            //  For such plugins, do not use this method and obtain IEditController with `comp->queryInstance()`
                            // )                            
                            var classIdBytes = new byte[16];
                            getControllerClassId(compPtr, classIdBytes);

                            if (doVerbose)
                            {
                                Log.Debug("ControllerClassId: '{0}'", Encoding.Default.GetString(classIdBytes));
                            }

                            // bus count
                            int busCount = getBusCount(compPtr, MediaTypes.kAudio, BusDirections.kInput);

                            if (doVerbose)
                            {
                                Log.Debug("BusCount: {0}", busCount);
                            }

                            // bus info
                            BusInfo bus = new BusInfo();
                            getBusInfo(compPtr, MediaTypes.kAudio, BusDirections.kInput, 0, ref bus);

                            if (doVerbose)
                            {
                                Log.Debug("BusInfo: {0}, {1}, {2}, {3}, {4}", bus.busType, bus.channelCount, bus.direction, bus.mediaType, bus.name);
                            }

                            RoutingInfo inInfo = new RoutingInfo();
                            RoutingInfo outInfo = new RoutingInfo();
                            getRoutingInfo(compPtr, ref inInfo, ref outInfo);

                            if (doVerbose)
                            {
                                Log.Debug("RoutingInfo. Input: '{0}', '{1}', '{2}', Output: '{3}', '{4}', '{5}'", inInfo.busIndex, inInfo.channel, inInfo.mediaType, outInfo.busIndex, outInfo.channel, outInfo.mediaType);
                            }
                        }
                        else
                        {
                            Log.Information("Found Other ('{0}') for: {1}", ci.category, ci.name);

                            FUnknown placeholderObj = new FUnknown();
                            IntPtr objPtr = placeholderObj.ToIntPtr();
                            createInstance(factoryPtr, ci.cid, FUnknownFUID, ref objPtr);
                            IntPtr objVtblPtr = Marshal.ReadIntPtr(objPtr, 0);
                            FUnknown obj = (FUnknown)Marshal.PtrToStructure(objVtblPtr, typeof(FUnknown));

                            if (doVerbose)
                            {
                                Log.Debug("query: {0}, addRef: {1}, release: {2}",
                                                                obj.queryInterface.ToString("X"),
                                                                obj.addRef.ToString("X"),
                                                                obj.release.ToString("X"));
                            }

                            FUnknownReleaseDelegate instanceRelease = (FUnknownReleaseDelegate)Marshal.GetDelegateForFunctionPointer(obj.release, typeof(FUnknownReleaseDelegate));
                            tresult instanceRelRes = instanceRelease(objPtr);
                            if (doVerbose)
                            {
                                Log.Debug("releasing instance: {0}", instanceRelRes);
                            }
                        }
                    }

                    tresult factoryRelRes = factoryRelease(factoryPtr);
                    if (doVerbose)
                    {
                        Log.Debug("releasing factory: {0}", factoryRelRes);
                    }
                }

                if (ExitDllProc != null)
                {
                    ExitDllProc();
                }

                NativeMethods.FreeLibrary(hModule);
            }
            catch (Exception e)
            {
                Log.Error("Failed opening {0} - {1}", vstPath, e);
                NativeMethods.FreeLibrary(hModule);
            }
        }
    }
}
