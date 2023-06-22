using System;
using UnityEngine;

//https://gist.github.com/ChemiKhazi/11395776

[Flags]
public enum Directions
{
    X = 1,
    Y = 1 << 1,
    Z = 1 << 2
}

public class TransformController : MonoBehaviour
{
    [EnumFlag]
    public Directions direction;
}