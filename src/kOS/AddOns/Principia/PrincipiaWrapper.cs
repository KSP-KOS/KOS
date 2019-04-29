using kOS.Safe.Encapsulation;
using kOS.Safe.Utilities;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace kOS.AddOns.Principia
{
    /*
          1    0 00080070 principia__ActivateRecorder = principia__ActivateRecorder
          2    1 000804E0 principia__AdvanceTime = principia__AdvanceTime
          3    2 000806B0 principia__CatchUpLaggingVessels = principia__CatchUpLaggingVessels
          4    3 00080A50 principia__CelestialFromParent = principia__CelestialFromParent
          5    4 00080C80 principia__CelestialInitialRotationInDegrees = principia__CelestialInitialRotationInDegrees
          6    5 00080F30 principia__CelestialRotation = principia__CelestialRotation
          7    6 00081240 principia__CelestialRotationPeriod = principia__CelestialRotationPeriod
          8    7 000814F0 principia__CelestialSphereRotation = principia__CelestialSphereRotation
          9    8 00081800 principia__CelestialWorldDegreesOfFreedom = principia__CelestialWorldDegreesOfFreedom
         10    9 000A18D0 principia__ClearTargetVessel = principia__ClearTargetVessel
         11    A 00081B50 principia__ClearWorldRotationalReferenceFrame = principia__ClearWorldRotationalReferenceFrame
         12    B 00081CD0 principia__CurrentTime = principia__CurrentTime
         13    C 00081F90 principia__DeletePlugin = principia__DeletePlugin
         14    D 000822A0 principia__DeleteString = principia__DeleteString
         15    E 00082530 principia__DeleteU16String = principia__DeleteU16String
         16    F 000827C0 principia__DeserializePlugin = principia__DeserializePlugin
         17   10 00082DB0 principia__EndInitialization = principia__EndInitialization
         18   11 0008D350 principia__ExternalFlowFreefall = principia__ExternalFlowFreefall
         19   12 0008D640 principia__ExternalGeopotentialGetCoefficient = principia__ExternalGeopotentialGetCoefficient
         20   13 0008DBA0 principia__ExternalGeopotentialGetReferenceRadius = principia__ExternalGeopotentialGetReferenceRadius
         21   14 0008DF40 principia__ExternalGetNearestPlannedCoastDegreesOfFreedom = principia__ExternalGetNearestPlannedCoastDegreesOfFreedom
         22   15 00094480 principia__FlightPlanAppend = principia__FlightPlanAppend
         23   16 00094780 principia__FlightPlanCreate = principia__FlightPlanCreate
         24   17 000949C0 principia__FlightPlanDelete = principia__FlightPlanDelete
         25   18 00094BD0 principia__FlightPlanExists = principia__FlightPlanExists
         26   19 00094F10 principia__FlightPlanGetActualFinalTime = principia__FlightPlanGetActualFinalTime
         27   1A 00095180 principia__FlightPlanGetAdaptiveStepParameters = principia__FlightPlanGetAdaptiveStepParameters
         28   1B 00095550 principia__FlightPlanGetDesiredFinalTime = principia__FlightPlanGetDesiredFinalTime
         29   1C 00095820 principia__FlightPlanGetGuidance = principia__FlightPlanGetGuidance
         30   1D 00095E30 principia__FlightPlanGetInitialTime = principia__FlightPlanGetInitialTime
         31   1E 00096100 principia__FlightPlanGetManoeuvre = principia__FlightPlanGetManoeuvre
         32   1F 00096360 principia__FlightPlanGetManoeuvreFrenetTrihedron = principia__FlightPlanGetManoeuvreFrenetTrihedron
         33   20 00096680 principia__FlightPlanNumberOfManoeuvres = principia__FlightPlanNumberOfManoeuvres
         34   21 00096920 principia__FlightPlanNumberOfSegments = principia__FlightPlanNumberOfSegments
         35   22 00096BC0 principia__FlightPlanRemoveLast = principia__FlightPlanRemoveLast
         36   23 00096D50 principia__FlightPlanRenderedApsides = principia__FlightPlanRenderedApsides
         37   24 000972A0 principia__FlightPlanRenderedClosestApproaches = principia__FlightPlanRenderedClosestApproaches
         38   25 00097700 principia__FlightPlanRenderedNodes = principia__FlightPlanRenderedNodes
         39   26 00097C30 principia__FlightPlanRenderedSegment = principia__FlightPlanRenderedSegment
         40   27 000982B0 principia__FlightPlanReplaceLast = principia__FlightPlanReplaceLast
         41   28 000985B0 principia__FlightPlanSetAdaptiveStepParameters = principia__FlightPlanSetAdaptiveStepParameters
         42   29 00098890 principia__FlightPlanSetDesiredFinalTime = principia__FlightPlanSetDesiredFinalTime
         43   2A 00082F30 principia__ForgetAllHistoriesBefore = principia__ForgetAllHistoriesBefore
         44   2B 000830E0 principia__FreeVesselsAndPartsAndCollectPileUps = principia__FreeVesselsAndPartsAndCollectPileUps
         45   2C 00099020 principia__FutureCatchUpVessel = principia__FutureCatchUpVessel
         46   2D 00099400 principia__FutureWaitForVesselToCatchUp = principia__FutureWaitForVesselToCatchUp
         47   2E 00083280 principia__GetBufferDuration = principia__GetBufferDuration
         48   2F 00083490 principia__GetBufferedLogging = principia__GetBufferedLogging
         49   30 000836A0 principia__GetPartActualDegreesOfFreedom = principia__GetPartActualDegreesOfFreedom
         50   31 000839B0 principia__GetStderrLogging = principia__GetStderrLogging
         51   32 00083BC0 principia__GetSuppressedLogging = principia__GetSuppressedLogging
         52   33 00083DD0 principia__GetVerboseLogging = principia__GetVerboseLogging
         53   34 00083FE0 principia__GetVersion = principia__GetVersion
         54   35 000842E0 principia__HasEncounteredApocalypse = principia__HasEncounteredApocalypse
         55   36 00084670 principia__HasVessel = principia__HasVessel
         56   37 000849A0 principia__IncrementPartIntrinsicForce = principia__IncrementPartIntrinsicForce
         57   38 00084B80 principia__InitGoogleLogging = principia__InitGoogleLogging
         58   39 00084E90 principia__InitializeEphemerisParameters = principia__InitializeEphemerisParameters
         59   3A 00085150 principia__InitializeHistoryParameters = principia__InitializeHistoryParameters
         60   3B 000852E0 principia__InitializePsychohistoryParameters = principia__InitializePsychohistoryParameters
         61   3C 00085480 principia__InsertCelestialAbsoluteCartesian = principia__InsertCelestialAbsoluteCartesian
         62   3D 00085730 principia__InsertCelestialJacobiKeplerian = principia__InsertCelestialJacobiKeplerian
         63   3E 00085C30 principia__InsertOrKeepLoadedPart = principia__InsertOrKeepLoadedPart
         64   3F 000860B0 principia__InsertOrKeepVessel = principia__InsertOrKeepVessel
         65   40 00086490 principia__InsertUnloadedPart = principia__InsertUnloadedPart
         66   41 0009B7C0 principia__IteratorAtEnd = principia__IteratorAtEnd
         67   42 0009BA50 principia__IteratorDelete = principia__IteratorDelete
         68   43 0009BCE0 principia__IteratorGetDiscreteTrajectoryQP = principia__IteratorGetDiscreteTrajectoryQP
         69   44 0009BFC0 principia__IteratorGetDiscreteTrajectoryTime = principia__IteratorGetDiscreteTrajectoryTime
         70   45 0009C3A0 principia__IteratorGetDiscreteTrajectoryXYZ = principia__IteratorGetDiscreteTrajectoryXYZ
         71   46 0009C7B0 principia__IteratorGetRP2LineXY = principia__IteratorGetRP2LineXY
         72   47 0009CB70 principia__IteratorGetRP2LinesIterator = principia__IteratorGetRP2LinesIterator
         73   48 0009CF10 principia__IteratorGetVesselGuid = principia__IteratorGetVesselGuid
         74   49 0009D240 principia__IteratorIncrement = principia__IteratorIncrement
         75   4A 0009D3C0 principia__IteratorReset = principia__IteratorReset
         76   4B 0009D540 principia__IteratorSize = principia__IteratorSize
         77   4C 000867A0 principia__LogError = principia__LogError
         78   4D 000868F0 principia__LogFatal = principia__LogFatal
         79   4E 00086A30 principia__LogInfo = principia__LogInfo
         80   4F 00086B70 principia__LogWarning = principia__LogWarning
         81   50 0009D7D0 principia__MonitorSetName = principia__MonitorSetName
         82   51 0009D870 principia__MonitorStart = principia__MonitorStart
         83   52 0009D8E0 principia__MonitorStop = principia__MonitorStop
         84   53 00086CC0 principia__NavballOrientation = principia__NavballOrientation
         85   54 000870A0 principia__NewPlugin = principia__NewPlugin
         86   55 0009F220 principia__PlanetariumCreate = principia__PlanetariumCreate
         87   56 0009F9E0 principia__PlanetariumDelete = principia__PlanetariumDelete
         88   57 0009FC70 principia__PlanetariumPlotFlightPlanSegment = principia__PlanetariumPlotFlightPlanSegment
         89   58 000A02B0 principia__PlanetariumPlotPrediction = principia__PlanetariumPlotPrediction
         90   59 000A0830 principia__PlanetariumPlotPsychohistory = principia__PlanetariumPlotPsychohistory
         91   5A 000873D0 principia__PrepareToReportCollisions = principia__PrepareToReportCollisions
         92   5B 000A19F0 principia__RenderedPredictionApsides = principia__RenderedPredictionApsides
         93   5C 000A1F30 principia__RenderedPredictionClosestApproaches = principia__RenderedPredictionClosestApproaches
         94   5D 000A23B0 principia__RenderedPredictionNodes = principia__RenderedPredictionNodes
         95   5E 00087550 principia__ReportGroundCollision = principia__ReportGroundCollision
         96   5F 000876D0 principia__ReportPartCollision = principia__ReportPartCollision
         97   60 00087860 principia__SayHello = principia__SayHello
         98   61 00087A60 principia__SerializePlugin = principia__SerializePlugin
         99   62 00087FD0 principia__SetBufferDuration = principia__SetBufferDuration
        100   63 000880E0 principia__SetBufferedLogging = principia__SetBufferedLogging
        101   64 000881F0 principia__SetMainBody = principia__SetMainBody
        102   65 00088370 principia__SetPartApparentDegreesOfFreedom = principia__SetPartApparentDegreesOfFreedom
        103   66 000A28D0 principia__SetPlottingFrame = principia__SetPlottingFrame
        104   67 00088630 principia__SetStderrLogging = principia__SetStderrLogging
        105   68 00088740 principia__SetSuppressedLogging = principia__SetSuppressedLogging
        106   69 000A2AE0 principia__SetTargetVessel = principia__SetTargetVessel
        107   6A 00088850 principia__SetVerboseLogging = principia__SetVerboseLogging
        108   6B 00088960 principia__SetWorldRotationalReferenceFrame = principia__SetWorldRotationalReferenceFrame
        109   6C 00088AE0 principia__UnmanageableVesselVelocity = principia__UnmanageableVesselVelocity
        110   6D 00088C30 principia__UpdateCelestialHierarchy = principia__UpdateCelestialHierarchy
        111   6E 00088DB0 principia__UpdatePrediction = principia__UpdatePrediction
        112   6F 000A3E10 principia__VesselBinormal = principia__VesselBinormal
        113   70 000A41A0 principia__VesselFromParent = principia__VesselFromParent
        114   71 000A4460 principia__VesselGetPredictionAdaptiveStepParameters = principia__VesselGetPredictionAdaptiveStepParameters
        115   72 000A4860 principia__VesselNormal = principia__VesselNormal
        116   73 000A4BF0 principia__VesselSetPredictionAdaptiveStepParameters = principia__VesselSetPredictionAdaptiveStepParameters
        117   74 000A4EC0 principia__VesselTangent = principia__VesselTangent
        118   75 000A5250 principia__VesselVelocity = principia__VesselVelocity
   */

    public class PrincipiaWrapper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string dllToLoad);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void GetVersionDlg(out IntPtr build_date, out IntPtr version);

        private static bool? wrapped = null;
        private static IntPtr principiaDll;

        private static GetVersionDlg GetVersion;

        private static void init()
        {
            SafeHouse.Logger.Log("Attempting to load Principia DLL...");

            principiaDll = LoadLibrary("principia.dll");

            if ( principiaDll == IntPtr.Zero )
            {
                SafeHouse.Logger.Log("Principia DLL did not load.");
                wrapped = false;
                return;
            }

            GetVersion = (GetVersionDlg)GetFunctionDlg("principia__GetVersion", typeof(GetVersionDlg));
            if (GetVersion == null)
                return;

            IntPtr build_date_ptr;
            IntPtr version_ptr;
            GetVersion(out build_date_ptr, out version_ptr);
            string build_date = Marshal.PtrToStringAnsi(build_date_ptr);
            string version = Marshal.PtrToStringAnsi(version_ptr);

            SafeHouse.Logger.Log("Principia " + version + "(" + build_date + ") loaded." );

            wrapped = true;
        }

        private static Delegate GetFunctionDlg( string funcName, Type t )
        {
            IntPtr funcPtr = GetProcAddress(principiaDll, funcName);
            if (funcPtr == IntPtr.Zero)
            {
                SafeHouse.Logger.Log("Could not get function pointer for " + funcName + ", bad principia DLL?");
                wrapped = false;
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(funcPtr, t);
        }

        public static BooleanValue Wrapped()
        {
            if (wrapped != null)
            {
                return wrapped;
            }
            else //if wrapped == null
            {
                init();
                return wrapped;
            }
        }
    }
}