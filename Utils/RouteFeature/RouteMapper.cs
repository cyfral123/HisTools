using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HisTools.Utils.RouteFeature;

public static class RouteMapper
{
    public static RouteData ToDto(
        IReadOnlyList<Vector3> points,
        IReadOnlyCollection<int> jumpIndices,
        IReadOnlyList<Note> notes,
        RouteInfo info,
        float minDistance)
    {
        return new RouteData
        {
            info = info,

            points = points
                .Select(p => Vec3Dto.From(p, 0))
                .ToList(),

            jumpIndices = jumpIndices.ToList(),

            notes = notes
                .Select(n => new RouteNoteDto
                {
                    position = Vec3Dto.From(n.Position, 2),
                    text = n.Text
                })
                .ToList()
        };
    }

}