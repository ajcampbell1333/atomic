using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// ADM: Atomic Debug Manager - enables metadata including class name, line num, and category
/// to be added to log statements automatically if the class is pre-registered.
/// To pre-register a class, add it to the switch statement in the GetCategoryForClass() method below.
/// Drag /Assets/Atomic/Debugging/csc.rsp directly into /Assets/csc.rsp and open it to 
/// toggle log categories on/off by adding or removing an X next to the category name
/// </summary>
public class ADM
{
    public static void Log(string text, AtomicLogCategory category)
    {
        switch (category)
        {
            case AtomicLogCategory.input:
                Debug.Log("Input: " + text);
                break;
            case AtomicLogCategory.molecules:
                Debug.Log("Molecules: " + text);
                break;
            case AtomicLogCategory.selection:
                Debug.Log("Selection: " + text);
                break;
            case AtomicLogCategory.transformation:
                Debug.Log("Transformation: " + text);
                break;
        }
    }

    private static AtomicLogCategory GetCategoryForClass(string className)
    {
        switch (className)
        {
#if ATOMIC_LOGCAT_INPUT
            case "OculusHandInput": return AtomicLogCategory.input;
            case "OculusHaptics": return AtomicLogCategory.input;
            case "OculusTouchInputTest": return AtomicLogCategory.input;
#endif
#if ATOMIC_LOGCAT_MOLECULES
            // molecules
            case "CreateAtom": return AtomicLogCategory.molecules;
#endif
#if ATOMIC_LOGCAT_SELECTION
            case "AtomicModeController": return AtomicLogCategory.selection;
            case "AtomicRaycaster": return AtomicLogCategory.selection;
            case "AtomicSelection": return AtomicLogCategory.selection;
#endif
#if ATOMIC_LOGCAT_TRANSFORMATION
            case "IListenForTransformation": return AtomicLogCategory.transformation;
            case "TransformationModeHighlight": return AtomicLogCategory.transformation;
            case "TransformListener": return AtomicLogCategory.transformation;
            case "TransformationMode": return AtomicLogCategory.transformation;
            case "TransformPivot": return AtomicLogCategory.transformation;
            case "TransformPivotController": return AtomicLogCategory.transformation;
            case "TransformRotationController": return AtomicLogCategory.transformation;
            case "TransformScaleController": return AtomicLogCategory.transformation;
            case "TransformTranslationController": return AtomicLogCategory.transformation;
#endif
        }
        return AtomicLogCategory.none;
    }

    /// <summary>
    /// Quick log designed to write to console without having to input the category if
    /// you've pre-registered the class with that category (see GetCatForClass above)
    /// </summary>
    /// <param name="text"></param>
    /// <param name="fileName"></param>
    /// <param name="lineNumber"></param>
    public static void QLog(string text, [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
    {
        string[] parsedFileName = fileName.Split('/');
        string className = parsedFileName[parsedFileName.Length - 1];
        if (className.Contains(".cs"))
            className.Remove(className.Length - 3);
        AtomicLogCategory cat = GetCategoryForClass(className);
        if (cat == AtomicLogCategory.none) return;
        else Log(className.ToUpper() + " " + lineNumber + ": " + text, cat);
    }
}

public enum AtomicLogCategory
{
    none,
    input,
    molecules,
    selection,
    transformation
}
