using System.Collections.Generic;

namespace HisTools.Utils.RouteFeature.BackwardCompatibility;

class V1Route
{
    public V1RouteInfo info;
    public List<V1Point> points;
    public List<V1Note> notes;
}

class V1RouteInfo
{
    public string uid;
    public string name;
    public string author;
    public string description;
    public string targetLevel;
}

class V1Point
{
    public float x, y, z;
    public bool jump;
}

class V1Note
{
    public float x, y, z;
    public string note;
}
