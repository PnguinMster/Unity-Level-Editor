using System;
using UnityEngine;

public enum Transformation
{
    None = 0,
    Transform = 1,
    Scale = 2,
    Rotate = 3
}

public class InfoUI : MonoBehaviour
{
    public static event Action<int> onGridXZChange;
    public static event Action<int> onGridYChange;
    public static event Action<int> onRotateChange;
    public static event Action<int> onScaleChange;
    public static event Action<Transformation> onTransformationChange;

    public static void GridXZChange(int value) => onGridXZChange?.Invoke(value);
    public static void GridYChange(int value) => onGridYChange?.Invoke(value);
    public static void RotateChange(int value) => onRotateChange?.Invoke(value);
    public static void ScaleChange(int value) => onScaleChange?.Invoke(value);
    public void TranformationChange(int enumValue) => onTransformationChange?.Invoke((Transformation)enumValue);
}