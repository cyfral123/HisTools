using System;
using System.Collections.Generic;
using UnityEngine;
using UI;

public class CategoryFactory : ICategoryFactory
{
    private readonly GameObject _parent;
    private readonly Dictionary<string, MyCategory> _categories = new();

    public CategoryFactory(GameObject parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    public MyCategory CreateCategory(string categoryName, Vector2 initialPosition)
    {
        if (_categories.ContainsKey(categoryName))
            throw new Exception($"Category '{categoryName}' already exists.");

        var category = new MyCategory(_parent, categoryName, initialPosition);
        _categories[categoryName] = category;
        return category;
    }

    public MyCategory GetCategory(string categoryName)
    {
        _categories.TryGetValue(categoryName, out var category);
        return category;
    }

    public IEnumerable<MyCategory> GetAllCategories() => _categories.Values;
}
