using UnityEngine;

namespace HisTools.Features.Controllers;

public interface ICategory
{
    string Name { get; }
    Transform LayoutTransform { get; }
}