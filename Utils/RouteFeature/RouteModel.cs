using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace HisTools.Routes
{
    [Serializable]
    public class Vector3Serializable
    {
        public float x, y, z;
        public bool jump;

        public Vector3Serializable() { }

        public Vector3Serializable(Vector3 v, bool jump = false)
        {
            x = v.x; y = v.y; z = v.z;
            this.jump = jump;
        }

        public Vector3 ToVector3() => new(x, y, z);
    }

    [Serializable]
    public class NotePoint
    {
        public float x, y, z;
        public string note;

        [JsonIgnore]
        public Vector3 Position => new(x, y, z);

        public NotePoint() { }

        public NotePoint(Vector3 pos, string text)
        {
            x = pos.x;
            y = pos.y;
            z = pos.z;
            note = text;
        }
    }

    [Serializable]
    public class PathPoint
    {
        public float x, y, z;
        public bool jump;

        [JsonIgnore]
        public Vector3 Position => new(x, y, z);

        public PathPoint() { }

        public PathPoint(Vector3 pos, bool isJump = false)
        {
            x = pos.x;
            y = pos.y;
            z = pos.z;
            jump = isJump;
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

        [JsonIgnore] public Color CompletedColor = Color.clear;
        [JsonIgnore] public Color RemainingColor = Color.clear;
        [JsonIgnore] public Color TextColor = Color.clear;

        [JsonProperty("preferredCompletedColor")]
        public string CompletedColorHex
        {
            get => "#" + ColorUtility.ToHtmlStringRGBA(CompletedColor);
            set
            {
                CompletedColor = Utils.Palette.FromHtml(value);
            }
        }

        [JsonProperty("preferredRemainingColor")]
        public string RemainingColorHex
        {
            get => "#" + ColorUtility.ToHtmlStringRGBA(RemainingColor);
            set
            {
                RemainingColor = Utils.Palette.FromHtml(value);
            }
        }

        [JsonProperty("preferredNoteColor")]
        public string TextColorHex
        {
            get => "#" + ColorUtility.ToHtmlStringRGBA(TextColor);
            set
            {
                TextColor = Utils.Palette.FromHtml(value);
            }
        }
    }

    [Serializable]
    public class RouteData
    {
        public OnlyForDebug onlyForDebug;
        public RouteInfo info;
        public List<Vector3Serializable> points;
        public List<NotePoint> notes;
    }

    public class RouteInstance
    {
        public RouteInfo Info;
        public GameObject Root;
        public LineRenderer Line;
        public List<GameObject> JumpMarkers = [];
        public List<GameObject> NoteLabels = [];
        public List<GameObject> InfoLabels = [];
        public float MaxProgress = 0f;

        public Vector3[] CachedPositions;
        public int LastClosestIndex = 0;
        public Gradient CachedGradient;
    }

    public class RouteSet
    {
        public RouteInfo info;

        public List<Vector3> points = [];
        public HashSet<int> jumpIndices = [];
        public List<NotePoint> notes = [];
    }
}


[Serializable]
public class OnlyForDebug
{
    public float minDistanceBetweenPoints;
}