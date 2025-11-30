using System.Collections.Generic;
using UnityEngine;

namespace HisTools.UI.Controllers;

public interface ICategoryFactory
{
    MyCategory CreateCategory(string categoryName, Vector2 initialPosition);
    MyCategory GetCategory(string categoryName);
    IEnumerable<MyCategory> GetAllCategories();
}