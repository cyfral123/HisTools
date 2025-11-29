using System.Collections.Generic;
using UI;
using UnityEngine;

public interface ICategoryFactory
{
    MyCategory CreateCategory(string categoryName, Vector2 initialPosition);
    MyCategory GetCategory(string categoryName);
    IEnumerable<MyCategory> GetAllCategories();
}