using System;
using System.Collections.Generic;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

[Serializable]
public struct Vec3Dto
{
    public float x;
    public float y;
    public float z;

    public static Vec3Dto From(Vector3 v, int precision = 2) => new()
    {
        x = (float)Math.Round(v.x, precision),
        y = (float)Math.Round(v.y, precision),
        z = (float)Math.Round(v.z, precision)
    };
}

[Serializable]
public class RouteNoteDto
{
    public Vec3Dto position;
    public string text;
}


public class Note
{
    public Vector3 Position { get; }
    public string Text { get; }

    public Note(Vector3 position, string text)
    {
        Position = position;
        Text = text;
    }
}

[Serializable]
public class RouteInfo
{
    public string uid;
    public string name;
    public string author;
    public string description;
    public string targetLevel;
}

[Serializable]
public class RouteData
{
    public RouteInfo info;
    public List<Vec3Dto> points;
    public List<int> jumpIndices;
    public List<RouteNoteDto> notes;
}

public class RouteInstance
{
    public RouteInfo Info;
    public GameObject Root;
    public LineRenderer Line;
    public readonly List<GameObject> JumpMarkers = [];
    public readonly List<GameObject> NoteLabels = [];
    public float MaxProgress = 0f;

    public Vector3[] CachedPositions;
    public int LastClosestIndex = 0;
    public Gradient CachedGradient;
}