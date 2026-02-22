using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static DetectObstaclesInPosition;

public class PathRequest
{
    public PathFinderAI AI;
    public Task<List<Node>> PathfindingTask;
}

public class BoundsObj
{
    public Rect bounds;
    public int boundsGameObjID;
}
public struct Node
{
    public Vector2 position;
    public float startCost;
    public float targetDistance;
    public float totalCost;
    public bool evaluated;
    public int2 parentIdx;
    public int2 childIdx;
    public Vector2 dir;
    public int idx;
}
public enum PathfindingStatus
{
    Requested,
    CalculatingFlowField,
    FinishedFlowField,
    Faulted,
    NotRequested
}
public class PathFinderAI
{
    public PathfindingStatus pathStatus;
    public Rigidbody2D rb;
    public GameObject obj;
    public bool needsPath;
    public float velocity;
    public Vector2 currentSector;
    public Vector2 targetSector;
    public bool selected;
    public float maxVelocity = 5f;
    public bool targetSet = false;
    public Vector2 targetPos;
    public int loops;
    public bool currentlyInAITask;
    public bool pathFinding;
    public AIPathFindingData pathData;
    public int instanceID;
    public Task<bool[,]> obstacleReturn;
    public Task<Path> pathfindingTask;
    public Path finishedPathfindingTask;

    public CustomObject objectReference;
    public void PathFind(Vector2 destination)
    {

        targetPos = destination;
        targetSet = true;
    }
}
public struct PathFindTSInput
{
    //Used for inputting thread-safe variables into the background "pathfind()" function


    public Float2 AIPos;
    public Float2 TargetPos;
    public NativeArray<Obstacle> obstaclesInScene;
    public NativeArray<MapTarget> mapTargetsInScene;
    public NativeArray<CustomObject> objectsInScene;
    public FlowFieldReturnStructCollection flowFieldReturns;
    public int flowFieldSize;
    public Float2Bounds mapbounds;
    public CompleteObstacleMapReturnStruct lowQualityMapReturn;
    public CompleteObstacleStructCollection obstacleMapReturns;
    public float nodeSize;
    public FlowFieldReturnStructCollection destinationFlowFields;

}
public struct FlowFieldReturnStructCollection
{
    public NativeArray<float2> worldPos;
    public NativeArray<int> loopsSinceUsed;
    public NativeArray<int> loopsSinceUpdated;
    public NativeArray<int> nodeX;
    public NativeArray<int> nodeY;
    public NativeArray<float2> dir;
    public NativeArray<float2> pos;
    public NativeArray<int> pathRef;

    public NativeArray<FlowNode> flattenedFields;
    public NativeArray<int2> flowFieldOffsets;
    public NativeArray<int2> tileOffsets;

    public int totalFields;
    public int2 gridSize;

    public void Dispose()
    {
        if (worldPos.IsCreated) worldPos.Dispose();
        if (loopsSinceUsed.IsCreated) loopsSinceUsed.Dispose();
        if (loopsSinceUpdated.IsCreated) loopsSinceUpdated.Dispose();
        if (nodeX.IsCreated) nodeX.Dispose();
        if (nodeY.IsCreated) nodeY.Dispose();
        if (dir.IsCreated) dir.Dispose();
        if (pos.IsCreated) pos.Dispose();
        if (pathRef.IsCreated) pathRef.Dispose();
        if (flattenedFields.IsCreated) flattenedFields.Dispose();
        if (flowFieldOffsets.IsCreated) flowFieldOffsets.Dispose();
        if (tileOffsets.IsCreated) tileOffsets.Dispose();
    }
}
public struct FlowFieldReturnStruct
{
    public bool failed;
    public float2 worldPos;
    public int loopsSinceUsed;
    public int loopsSinceUpdated;
    public int nodeX;
    public int nodeY;
    //this must be fixed, cannot nest nativelists
    public NativeArray<FlowNode> flattenedField;
    public float2 dir;
    public float2 pos;
    public int pathRef;
}

public struct FlowFieldReturnSlice
{
    public float2 worldPos;
    public int loopsSinceUsed;
    public int loopsSinceUpdated;
    public int nodeX;
    public int nodeY;
    public NativeSlice<FlowNode> flattenedField;
    public float2 dir;
    public float2 pos;
    public int pathRef;
}
public class FlowFieldReturn
{
    public Vector2 worldPos;
    public int loopsSinceUsed;
    public int loopsSinceUpdated;
    public int nodeX;
    public int nodeY;
    public FlowNode[,] field;
    public Vector2 dir;
    public Vector2 pos;
    public int pathRef;
}
public struct FlowFieldSliceGroup
{
    public NativeSlice<float2> worldPositions;
    public NativeSlice<int2> nodeOffsets;
    public NativeSlice<float2> dir;
    public NativeSlice<int> nodeX;
    public NativeSlice<int> nodeY;
    public NativeSlice<float2> pos;
}
public static class FlowFieldUtils
{
    public static FlowFieldReturnStructCollection FlattenMultipleFlowFieldReturns(NativeArray<FlowFieldReturnStruct> array)
    {
        int size = array.Length;
        int totalElements = 0;
        for (int i = 0; i < size; i++)
        {
            totalElements += array[i].flattenedField.Length;
        }

        FlowFieldReturnStructCollection collection = new FlowFieldReturnStructCollection
        {
            worldPos = new NativeArray<float2>(size, Allocator.Persistent),
            totalFields = size,
            loopsSinceUpdated = new NativeArray<int>(size, Allocator.Persistent),
            loopsSinceUsed = new NativeArray<int>(size, Allocator.Persistent),
            dir = new NativeArray<float2>(size, Allocator.Persistent),
            flattenedFields = new NativeArray<FlowNode>(totalElements, Allocator.Persistent),
            flowFieldOffsets = new NativeArray<int2>(size, Allocator.Persistent),
            nodeX = new NativeArray<int>(size, Allocator.Persistent),
            nodeY = new NativeArray<int>(size, Allocator.Persistent),
            pathRef = new NativeArray<int>(size, Allocator.Persistent),
            pos = new NativeArray<float2>(size, Allocator.Persistent)
        };

        int count = 0;
        for (int i = 0; i < size; i++)
        {
            collection.worldPos[i] = array[i].worldPos;
            collection.pos[i] = array[i].pos;
            collection.dir[i] = array[i].dir;
            collection.pathRef[i] = array[i].pathRef;
            collection.loopsSinceUpdated[i] = array[i].loopsSinceUpdated;
            collection.loopsSinceUsed[i] = array[i].loopsSinceUsed;
            collection.nodeX[i] = array[i].nodeX;
            collection.nodeY[i] = array[i].nodeY;

            int length = array[i].flattenedField.Length;
            collection.flowFieldOffsets[i] = new int2(count, length);

            if (length > 0)
            {
                NativeArray<FlowNode>.Copy(array[i].flattenedField, 0, collection.flattenedFields, count, length);
            }
            if (array[i].flattenedField.IsCreated)
            {
                array[i].flattenedField.Dispose();
            }

            count += length;
        }
        return collection;
    }

    public static FlowFieldReturnSlice RetrieveFlowFieldReturn(FlowFieldReturnStructCollection collection, int idx)
    {
        return new FlowFieldReturnSlice
        {
            dir = collection.dir[idx],
            loopsSinceUpdated = collection.loopsSinceUpdated[idx],
            loopsSinceUsed = collection.loopsSinceUsed[idx],
            nodeX = collection.nodeX[idx],
            nodeY = collection.nodeY[idx],
            pathRef = collection.pathRef[idx],
            pos = collection.pos[idx],
            worldPos = collection.worldPos[idx],
            flattenedField = new NativeSlice<FlowNode>(collection.flattenedFields, collection.flowFieldOffsets[idx].x, collection.flowFieldOffsets[idx].y),
        };
    }

    public static int FindIndexPositionNative(NativeArray<float2> list, Vector2 target)
    {
        for (int i = 0; i < list.Length; i++)
        {
            if (new Vector2(list[i].x, list[i].y) == target)
            {
                return i;
            }
        }
        return -1;
    }
    public static FlowFieldReturnStruct FlowFieldReturnToStruct(FlowFieldReturn flr)
    {
        return new FlowFieldReturnStruct
        {
            worldPos = flr.worldPos,
            loopsSinceUpdated = flr.loopsSinceUpdated,
            loopsSinceUsed = flr.loopsSinceUsed,
            nodeX = flr.nodeX,
            nodeY = flr.nodeY,
            flattenedField = FlattenFlowNodeArray(flr.field),
            dir = flr.dir,
            pos = flr.pos,
        };
    }

    public static FlowFieldReturn StructToFlowFieldReturn(FlowFieldReturnStruct flrs, int size)
    {
        return new FlowFieldReturn
        {
            worldPos = flrs.worldPos,
            loopsSinceUpdated = flrs.loopsSinceUpdated,
            loopsSinceUsed = flrs.loopsSinceUsed,
            nodeX = flrs.nodeX,
            nodeY = flrs.nodeY,
            field = ReconstructFlowNodeArray(flrs.flattenedField.ToList(), size),
            dir = flrs.dir,
            pos = flrs.pos
        };
    }
    ///<summary>
    ///Reconstructs a 2D array from a 1D array. Assumes that the size is equal for both x and y
    /// </summary>
    public static FlowNode[,] ReconstructFlowNodeArray(List<FlowNode> flattenedArray, int size)
    {
        if (size * size > flattenedArray.Count || size * size < flattenedArray.Count)
        {
            Debug.LogError("Size is not equal to input array size. Check that you have the correct variables");
            return null;
        }
        FlowNode[,] returnArray = new FlowNode[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                returnArray[x, y] = flattenedArray[y * size + x];
            }
        }
        return returnArray;

    }
    ///<summary>
    ///Flattens a 2D array into a 1D array. IMPORTANT: remember to dispose of the result when you are done to avoid a leak
    /// </summary>
    public static NativeArray<FlowNode> FlattenFlowNodeArray(FlowNode[,] array)
    {

        int length = array.GetLength(0) * array.GetLength(1);
        NativeArray<FlowNode> newArray = new NativeArray<FlowNode>(length, Allocator.Persistent);
        for (int i = 0; i < length; i++)
        {
            int x = i % array.GetLength(0);
            int y = i / array.GetLength(0);
            newArray[i] = array[x, y];
        }
        return newArray;
    }
    public static NativeSlice<float2> GetTileWorldPositions(FlowFieldReturnStructCollection collection, int2 gridPos)
    {
        int flatTileIdx = gridPos.y * collection.gridSize.x + gridPos.x;
        int2 range = collection.tileOffsets[flatTileIdx];
        return new NativeSlice<float2>(collection.worldPos, range.x, range.y);
    }
    public static FlowFieldReturnStruct GetFlowFieldMetadataOnly(FlowFieldReturnStructCollection collection, int globalFieldIndex)
    {
        return new FlowFieldReturnStruct
        {
            worldPos = collection.worldPos[globalFieldIndex],
            dir = collection.dir[globalFieldIndex],
            pos = collection.pos[globalFieldIndex],
            pathRef = collection.pathRef[globalFieldIndex],
            loopsSinceUpdated = collection.loopsSinceUpdated[globalFieldIndex],
            loopsSinceUsed = collection.loopsSinceUsed[globalFieldIndex],
            nodeX = collection.nodeX[globalFieldIndex],
            nodeY = collection.nodeY[globalFieldIndex],
        };
    }
    /// <summary>
    /// Retrieves all FlowField metadata and node data for a specific grid tile.
    /// </summary>
    public static FlowFieldSliceGroup GetFlowFieldsAtTile(FlowFieldReturnStructCollection collection, int x, int y)
    {
        int flatTileIdx = y * collection.gridSize.x + x;
        int2 tileRange = collection.tileOffsets[flatTileIdx];

        if (tileRange.y == 0) return default;

        return new FlowFieldSliceGroup
        {
            worldPositions = new NativeSlice<float2>(collection.worldPos, tileRange.x, tileRange.y),
            nodeOffsets = new NativeSlice<int2>(collection.flowFieldOffsets, tileRange.x, tileRange.y),
            dir = new NativeSlice<float2>(collection.dir, tileRange.x, tileRange.y),
            nodeX = new NativeSlice<int>(collection.nodeX, tileRange.x, tileRange.y),
            nodeY = new NativeSlice<int>(collection.nodeY, tileRange.x, tileRange.y),
            pos = new NativeSlice<float2>(collection.pos, tileRange.x, tileRange.y),
        };
    }

    /// <summary>
    /// Retrieves the actual FlowNode grid for a specific field within a group.
    /// </summary>
    public static NativeSlice<FlowNode> GetNodesForField(FlowFieldReturnStructCollection collection, int2 fieldOffset)
    {
        return new NativeSlice<FlowNode>(collection.flattenedFields, fieldOffset.x, fieldOffset.y);
    }
    /// <summary>
    /// Returns a nested native array of flow field structs. IMPORTANT: remember to dispose of the result when you are done to avoid a leak
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static FlowFieldReturnStructCollection Multiple2DFlowFieldReturnToStruct(List<FlowFieldReturn>[,] list)
    {
        int rows = list.GetLength(0);
        int cols = list.GetLength(1);
        int totalTiles = rows * cols;

        int totalFields = 0;
        int totalNodes = 0;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                var tile = list[x, y];
                totalFields += tile.Count;
                foreach (var flr in tile)
                {
                    totalNodes += flr.field.GetLength(0) * flr.field.GetLength(1);
                }
            }
        }
        var collection = new FlowFieldReturnStructCollection
        {
            worldPos = new NativeArray<float2>(totalFields, Allocator.Persistent),
            loopsSinceUsed = new NativeArray<int>(totalFields, Allocator.Persistent),
            loopsSinceUpdated = new NativeArray<int>(totalFields, Allocator.Persistent),
            nodeX = new NativeArray<int>(totalFields, Allocator.Persistent),
            nodeY = new NativeArray<int>(totalFields, Allocator.Persistent),
            dir = new NativeArray<float2>(totalFields, Allocator.Persistent),
            pos = new NativeArray<float2>(totalFields, Allocator.Persistent),
            pathRef = new NativeArray<int>(totalFields, Allocator.Persistent),

            flattenedFields = new NativeArray<FlowNode>(totalNodes, Allocator.Persistent),
            flowFieldOffsets = new NativeArray<int2>(totalFields, Allocator.Persistent),
            tileOffsets = new NativeArray<int2>(totalTiles, Allocator.Persistent),

            totalFields = totalFields,
            gridSize = new int2(rows, cols)
        };

        int fieldPtr = 0;
        int nodePtr = 0;
        for (int y = 0; y < cols; y++)
        {
            for (int x = 0; x < rows; x++)
            {
                var currentTile = list[x, y];
                int tileStartInFields = fieldPtr;

                foreach (var flr in currentTile)
                {
                    collection.worldPos[fieldPtr] = flr.worldPos;
                    collection.loopsSinceUsed[fieldPtr] = flr.loopsSinceUsed;
                    collection.loopsSinceUpdated[fieldPtr] = flr.loopsSinceUpdated;
                    collection.nodeX[fieldPtr] = flr.nodeX;
                    collection.nodeY[fieldPtr] = flr.nodeY;
                    collection.dir[fieldPtr] = flr.dir;
                    collection.pos[fieldPtr] = flr.pos;
                    collection.pathRef[fieldPtr] = flr.pathRef;

                    int nodeStartInMegabuffer = nodePtr;
                    var grid = flr.field;
                    int fW = grid.GetLength(0);
                    int fH = grid.GetLength(1);

                    for (int fy = 0; fy < fH; fy++)
                    {
                        for (int fx = 0; fx < fW; fx++)
                        {
                            collection.flattenedFields[nodePtr++] = grid[fx, fy];
                        }
                    }

                    collection.flowFieldOffsets[fieldPtr] = new int2(nodeStartInMegabuffer, nodePtr - nodeStartInMegabuffer);
                    fieldPtr++;
                }
                collection.tileOffsets[y * rows + x] = new int2(tileStartInFields, fieldPtr - tileStartInFields);
            }
        }
        return collection;
    }
}
//public struct NativeFlowFieldCollection : IDisposable
//{
//    public NativeArray<FlowFieldReturnStruct> Data;
//    /// <summary>
//    /// Index into here to get values from data. structed as: start index, count
//    /// </summary>
//    public NativeArray<int2> Offsets;
//    public int2 size;

//    public void Dispose()
//    {
//        if (Data.IsCreated) Data.Dispose();
//        if (Offsets.IsCreated) Offsets.Dispose();
//    }
//}

public struct FlowNode
{
    public Vector2 position;
    public int2 gridPos;
    public float totalCost;
    public Vector2 dir;
    public int idx;
}
public class Path
{
    public PathFinderAI AI;
    public List<int3> flowFieldIdxReferences;
    public int destinationFlowFieldIdxReference;
    public List<FlowFieldReturn> sectorPath;
    public List<FlowFieldReturn> cachedSectorPath;

}

public struct NativePath
{
    public NativeList<int3> flowFieldIdxReferences;
    public int destinationFlowFieldIdxReference;
    public FlowFieldReturnStructCollection sectorPath;
    public int sectorSize;

    public static implicit operator Path(NativePath nativePath)
    {
        Path managedPath = new Path();
        managedPath.sectorPath = new List<FlowFieldReturn>();
        for (int i = 0; i < nativePath.flowFieldIdxReferences.Length; i++)
        {
            int3 reference = nativePath.flowFieldIdxReferences[i];

            int flatTileIdx = reference.y * nativePath.sectorPath.gridSize.x + reference.x;
            int globalIdx = nativePath.sectorPath.tileOffsets[flatTileIdx].x + reference.z;

            FlowFieldReturnStruct field = FlowFieldUtils.GetFlowFieldMetadataOnly(nativePath.sectorPath, globalIdx);

            int2 nodeRange = nativePath.sectorPath.flowFieldOffsets[globalIdx];
            NativeSlice<FlowNode> nodeSlice = new NativeSlice<FlowNode>(nativePath.sectorPath.flattenedFields, nodeRange.x, nodeRange.y);

            field.flattenedField = new NativeArray<FlowNode>(nodeSlice.Length, Allocator.Persistent);
            field.flattenedField.CopyFrom(nodeSlice);

            managedPath.sectorPath.Add(FlowFieldUtils.StructToFlowFieldReturn(field, nativePath.sectorSize));
        }

        if (nativePath.destinationFlowFieldIdxReference != -1)
        {
            managedPath.destinationFlowFieldIdxReference = nativePath.destinationFlowFieldIdxReference;

        }

        return managedPath;
    }
}
public struct PathReturn
{
    public bool failed;
    public PathFinderAI AI;
    public List<int3> flowFieldIdxReferences;
    public int destinationFlowFieldIdxReference;
    public List<FlowFieldReturn> sectorPath;
    public List<Float2> sectorVectorPath;
}
public class AIPathFindingData
{
    public Vector2 mapOrigin;
    public int mapWidth;
    public int mapHeight;
    public float pathResolution;
}
public class ObstacleMapReturn
{
    public bool[,] obstacleMap;
    public PathFinderAI pathfinderAI;
}
public struct Float2
{
    public float x;
    public float y;
    public Float2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public static implicit operator float2(Float2 fr)
    {
        return new float2(fr.x, fr.y);
    }
    public static implicit operator Vector2(Float2 fr)
    {
        return new Vector2(fr.x, fr.y);
    }
    public static implicit operator int2(Float2 fr)
    {
        return new int2(Mathf.RoundToInt(fr.x), Mathf.RoundToInt(fr.y));
    }
    public static Vector2 ConvertToV2(Float2 fr)
    {
        return new Vector2(fr.x, fr.y);
    }
    public static Float2 ConvertToF2(Vector2 fr)
    {
        return new Float2(fr.x, fr.y);
    }
    public static Float2 Lerp(Float2 a, Float2 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Float2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator +(Float2 a, Float2 b)
    {
        return new Float2(a.x + b.x, a.y + b.y);
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator -(Float2 a, Float2 b)
    {
        return new Float2(a.x - b.x, a.y - b.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator *(Float2 a, Float2 b)
    {
        return new Float2(a.x * b.x, a.y * b.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator /(Float2 a, Float2 b)
    {
        return new Float2(a.x / b.x, a.y / b.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator -(Float2 a)
    {
        return new Float2(0f - a.x, 0f - a.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator *(Float2 a, float d)
    {
        return new Float2(a.x * d, a.y * d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator *(float d, Float2 a)
    {
        return new Float2(a.x * d, a.y * d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Float2 operator /(Float2 a, float d)
    {
        return new Float2(a.x / d, a.y / d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Float2 lhs, Float2 rhs)
    {
        float num = lhs.x - rhs.x;
        float num2 = lhs.y - rhs.y;
        return num * num + num2 * num2 < 9.99999944E-11f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Float2 lhs, Float2 rhs)
    {
        return !(lhs == rhs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Float2(Vector3 v)
    {
        return new Float2(v.x, v.y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(Float2 v)
    {
        return new Vector3(v.x, v.y, 0f);
    }
    public static implicit operator Float2(Vector2 fr)
    {
        return new Float2(fr.x, fr.y);
    }
    public static float Distance(Float2 a, Float2 b)
    {
        float num = a.x - b.x;
        float num2 = a.y - b.y;
        return (float)Math.Sqrt(num * num + num2 * num2);
    }
    public static Float2 Min(Float2 lhs, Float2 rhs)
    {
        return new Float2(Mathf.Min(lhs.x, rhs.x), Mathf.Min(lhs.y, rhs.y));
    }
    public static Float2 Max(Float2 lhs, Float2 rhs)
    {
        return new Float2(Mathf.Max(lhs.x, rhs.x), Mathf.Max(lhs.y, rhs.y));
    }

    public override bool Equals(object obj)
    {
        return obj is Float2 @float &&
               x == @float.x &&
               y == @float.y;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(x, y);
    }
}

public struct Float2Bounds

{
    public Float2 position;
    public Float2 size;
    public Float2Bounds(Float2 position, Float2 size)
    {
        this.position = position;
        this.size = size;
    }
    public void SetMinMax(Float2 min, Float2 max)
    {
        size = (max - min) * 0.5f;
        position = min + size;
    }
    public Float2 min
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return position - size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            SetMinMax(value, max);
        }
    }
    public Float2 max
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return position + size;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            SetMinMax(value, max);
        }
    }
    public void Encapsulate(Float2 point)
    {
        SetMinMax(Float2.Min(min, point), Float2.Max(max, point));
    }

    public void Encapsulate(Float2Bounds bounds)
    {
        Encapsulate(bounds.position - bounds.size);
        Encapsulate(bounds.position + bounds.size);
    }
}
public struct CustomObject
{
    public Float2 position;
    public Float2 size;
    public float instanceID;
    public FixedString32Bytes name;
}
public static class DetectObstaclesInPosition
{
    public static void DrawRectangle(Vector3 position, Vector3 extent, Color color)
    {
        // Calculate the four corner points relative to the center
        Vector3 rightOffset = Vector3.right * extent.x;
        Vector3 upOffset = Vector3.up * extent.y;

        Vector3 p1 = position + rightOffset + upOffset;  // Top-Right
        Vector3 p2 = position - rightOffset + upOffset;  // Top-Left
        Vector3 p3 = position - rightOffset - upOffset;  // Bottom-Left
        Vector3 p4 = position + rightOffset - upOffset;  // Bottom-Right

        // Draw the four lines connecting the corners
        Debug.DrawLine(p1, p2, color); // Top
        Debug.DrawLine(p2, p3, color); // Left
        Debug.DrawLine(p3, p4, color); // Bottom
        Debug.DrawLine(p4, p1, color); // Right
    }
    public static Obstacle SetupObstacle(GameObject obj)
    {
        if (obj.GetComponent<Collider2D>() != null)
        {
            Obstacle returnObstacle = new Obstacle
            {
                position = new Float2(obj.transform.position.x, obj.transform.position.y),
                size = new Float2(obj.GetComponent<Collider2D>().bounds.size.x, obj.GetComponent<Collider2D>().bounds.size.y),
                name = obj.name.ToString(),
                instanceID = obj.GetInstanceID()
            };
            //Debug.Log("Set up obstacle with size " + returnObstacle.size.x + ", " + returnObstacle.size.y);
            return returnObstacle;
        }
        else
        {
            Debug.LogError("ERR: OBJ " + obj.name + " DOES NOT INCLUDE A COLLIDER, PLEASE ADD ONE!");
            return new Obstacle();
        }
    }
    public static bool IsInteger(float value)
    {
        return value == Math.Truncate(value);
    }
    public static CustomObject SetupObject(GameObject obj)
    {
        if (obj.GetComponent<Collider2D>() != null)
        {
            CustomObject returnObstacle = new CustomObject
            {
                position = new Float2(obj.transform.position.x, obj.transform.position.y),
                size = new Float2(obj.GetComponent<Collider2D>().bounds.size.x, obj.GetComponent<Collider2D>().bounds.size.y),
                name = obj.name.ToString(),
                instanceID = obj.GetInstanceID()
            };
            //Debug.Log("Set up obstacle with size " + returnObstacle.size.x + ", " + returnObstacle.size.y);
            return returnObstacle;
        }
        else
        {
            CustomObject returnObstacle = new CustomObject
            {
                position = new Float2(obj.transform.position.x, obj.transform.position.y),
                size = new Float2(5, 5),
                name = obj.name.ToString(),
                instanceID = obj.GetInstanceID()
            };
            //Debug.Log("Set up obstacle with size " + returnObstacle.size.x + ", " + returnObstacle.size.y);
            return returnObstacle;
        }
    }
    public static List<CustomObject> SetupObjects(List<GameObject> obj)
    {
        List<CustomObject> returnList = new List<CustomObject>();
        for (int i = 0; i < obj.Count; i++)
        {
            if (obj[i].GetComponent<Collider2D>() != null)
            {
                CustomObject returnObstacle = new CustomObject
                {
                    position = new Float2(obj[i].transform.position.x, obj[i].transform.position.y),
                    size = new Float2(obj[i].GetComponent<Collider2D>().bounds.size.x, obj[i].GetComponent<Collider2D>().bounds.size.y),
                    name = obj[i].name.ToString(),
                    instanceID = obj[i].GetInstanceID()
                };
                //Debug.Log("Set up obstacle with size " + returnObstacle.size.x + ", " + returnObstacle.size.y);
                returnList.Add(returnObstacle);
            }
            else
            {
                CustomObject returnObstacle = new CustomObject
                {
                    position = new Float2(obj[i].transform.position.x, obj[i].transform.position.y),
                    size = new Float2(5, 5),
                    name = obj[i].name.ToString(),
                    instanceID = obj[i].GetInstanceID()
                };
                //Debug.Log("Set up obstacle with size " + returnObstacle.size.x + ", " + returnObstacle.size.y);
                returnList.Add(returnObstacle);
            }
        }
        return returnList;

    }
    public static MapTarget SetupMapTarget(GameObject obj)
    {
        MapTarget returnTarget = new MapTarget
        {
            position = new Float2(obj.transform.position.x, obj.transform.position.y),
            name = obj.name.ToString(),
            instanceID = obj.GetInstanceID()
        };
        return returnTarget;
    }

    public static List<MapTarget> SetupMapTargets(List<GameObject> objs)
    {
        List<MapTarget> targets = new List<MapTarget>();
        for (int i = 0; i < objs.Count; i++)
        {
            MapTarget returnTarget = new MapTarget
            {
                position = new Float2(objs[i].transform.position.x, objs[i].transform.position.y),
                name = objs[i].name.ToString(),
                instanceID = objs[i].GetInstanceID()
            };
            targets.Add(returnTarget);
        }
        return targets;
    }

    public static CompleteObstacleMapReturn CompleteObstacleMap(List<Obstacle> obstacles, float nodeSize, List<MapTarget> mapTargets, List<CustomObject> others)
    {
        Float2Bounds boundingBox = new Float2Bounds();
        if (obstacles.Count > 0)
        {
            boundingBox = new Float2Bounds(obstacles[0].position, obstacles[0].size * 0.5f);

        }
        else if (mapTargets.Count > 0)
        {
            boundingBox = new Float2Bounds(mapTargets[0].position, new Float2(5, 5));
        }
        else if (others.Count > 0)
        {
            boundingBox = new Float2Bounds(others[0].position, others[0].size * 0.5f);
        }
        else
        {
            Debug.LogError("No objects found in any lists, maybe this was an accidental call?");
            return null;
        }
        foreach (var target in mapTargets)
        {
            if (math.abs(target.position.x) > 100000 || math.abs(target.position.y) > 100000)
            {
                Debug.LogError($"CRITICAL: MapTarget at {target.position} is way too far away!");
            }
        }

        foreach (var other in others)
        {
            if (other.size.x > 10000 || other.size.y > 10000)
            {
                Debug.LogError($"CRITICAL: Object {other} has a massive size: {other.size}");
            }
            if (math.abs(other.position.x) > 100000)
            {
                Debug.LogError($"CRITICAL: Object {other} is at a crazy position: {other.position}");
            }
        }
        for (int i = 0; i < mapTargets.Count; i++)
        {
            boundingBox.Encapsulate(new Float2Bounds(mapTargets[i].position, new Float2(5, 5)));
        }
        for (int i = 0; i < others.Count; i++)
        {
            boundingBox.Encapsulate(new Float2Bounds(others[i].position, others[i].size));
        }
        Float2 leftBCorner = new Float2(boundingBox.position.x - (boundingBox.size.x), boundingBox.position.y - (boundingBox.size.y));
        Float2 rightBCorner = new Float2(boundingBox.position.x + (boundingBox.size.x), boundingBox.position.y - (boundingBox.size.y));
        Float2 leftTCorner = new Float2(boundingBox.position.x - (boundingBox.size.x), boundingBox.position.y + (boundingBox.size.y));
        Float2 rightTCorner = new Float2(boundingBox.position.x + (boundingBox.size.x), boundingBox.position.y + (boundingBox.size.y));
        Float2 center = boundingBox.position;
        Float2 size = boundingBox.size;
        //Debug.Log($"Bounding box size: {size.x}, {size.y}, Origin is {leftBCorner.x}, {leftBCorner.y}");
        int nodeGridWidth = Mathf.Max(1, Mathf.CeilToInt((size.x * 2) / nodeSize));
        int nodeGridHeight = Mathf.Max(1, Mathf.CeilToInt((size.y * 2) / nodeSize));
        long totalCells = (long)nodeGridWidth * nodeGridHeight;
        Debug.Log($"Attempting to allocate a grid of {nodeGridWidth}x{nodeGridHeight} ({totalCells} cells)");
        CompleteObstacleMapReturn objMap = new CompleteObstacleMapReturn
        {
            obstacleMap = new List<Obstacle>[nodeGridWidth, nodeGridHeight],
            obstacleMapBool = new bool[nodeGridWidth, nodeGridHeight],
            size = new Float2(nodeGridWidth, nodeGridHeight),
            startArea = leftBCorner,
            bounds = boundingBox
        };
        for (int i = 0; i < obstacles.Count; i++)
        {
            Float2 obsMin = obstacles[i].position - (obstacles[i].size / 2f);
            Float2 obsMax = obstacles[i].position + (obstacles[i].size / 2f);

            int startX = Mathf.FloorToInt((obsMin.x - objMap.startArea.x) / nodeSize);
            int startY = Mathf.FloorToInt((obsMin.y - objMap.startArea.y) / nodeSize);

            int endX = Mathf.Min(nodeGridWidth, Mathf.CeilToInt((obsMax.x - objMap.startArea.x) / nodeSize));
            int endY = Mathf.Min(nodeGridHeight, Mathf.CeilToInt((obsMax.y - objMap.startArea.y) / nodeSize));


            startX = Mathf.Max(0, startX);
            startY = Mathf.Max(0, startY);

            endX = Mathf.Min(nodeGridWidth, endX);
            endY = Mathf.Min(nodeGridHeight, endY);
            for (int x = startX; x < endX; x++)
            {
                for (int y = startY; y < endY; y++)
                {
                    if (objMap.obstacleMap[x, y] == null)
                    {
                        objMap.obstacleMap[x, y] = new List<Obstacle>();
                    }
                    objMap.obstacleMap[x, y].Add(obstacles[i]);
                    objMap.obstacleMapBool[x, y] = true;
                    //Debug.Log($"Obstacle detected at {x}, {y}");
                }
            }
        }
        return objMap;
    }

    //Deprecated
    public static bool[,] DetectObstacleMap(Float2 size, List<Obstacle> obstacles, Obstacle exclude, float pathResolution)
    {
        int halfWidth = Mathf.RoundToInt(size.x / 2);
        int halfHeight = Mathf.RoundToInt(size.y / 2);
        int nodeGridWidth = Mathf.Max(1, Mathf.RoundToInt(size.x / pathResolution));
        int nodeGridHeight = Mathf.Max(1, Mathf.RoundToInt(size.y / pathResolution));
        bool[,] obstacleMapReturn = new bool[(int)size.x, (int)size.y];
        Float2 nodePosOrigin = new Float2(exclude.position.x - halfWidth, exclude.position.y - halfHeight);
        for (int x = 0; x < nodeGridWidth; x++)
        {
            for (int y = 0; y < nodeGridHeight; y++)
            {
                Float2 nodePos = new Float2(
                    nodePosOrigin.x + x * pathResolution,
                    nodePosOrigin.y + y * pathResolution
                );
                if (DetectInPositionExclusive(new Float2(nodePos.x, nodePos.y), exclude.size, exclude.instanceID, obstacles))
                {
                    obstacleMapReturn[x, y] = true;
                }
                else
                {
                    obstacleMapReturn[x, y] = false;
                }
            }
        }
        return obstacleMapReturn;
    }
    //Deprecated
    public static bool DetectInPositionExclusive(Float2 position, Float2 size, int ExcludeinstanceID, List<Obstacle> obstacles)
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].instanceID == ExcludeinstanceID)
            {
                continue;
            }
            else
            {
                if (Contains(position, size, obstacles[i].position, obstacles[i].size))
                {
                    return true;
                }
            }
        }
        return false;
    }
    public static bool Contains(Float2 position1, Float2 size1, Float2 position2, Float2 size2)
    {
        Float2 hA = new Float2(size1.x / 2, size1.y / 2);
        Float2 hB = new Float2(size2.x / 2, size2.y / 2);
        Float2 s1A = new Float2(position1.x - hA.x, position1.y + hA.y);
        Float2 s2A = new Float2(position1.x + hA.x, position1.y + hA.y);
        Float2 s3A = new Float2(position1.x + hA.x, position1.y - hA.y);
        Float2 s4A = new Float2(position1.x - hA.x, position1.y - hA.y);
        Float2 s1B = new Float2(position2.x - hB.x, position2.y + hB.y);
        Float2 s2B = new Float2(position2.x + hB.x, position2.y + hB.y);
        Float2 s3B = new Float2(position2.x + hB.x, position2.y - hB.y);
        Float2 s4B = new Float2(position2.x - hB.x, position2.y - hB.y);

        if (s1A.x < s2B.x && s2A.x > s1B.x && s4A.y < s2B.y && s2A.y > s4B.y)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static ContainsPointReturn ContainsPoint(Float2 position1, Float2 size1, Float2 position2)
    {
        Float2 hA = new Float2(size1.x / 2, size1.y / 2);
        Float2 s1A = new Float2(position1.x - hA.x, position1.y + hA.y);
        Float2 s2A = new Float2(position1.x + hA.x, position1.y + hA.y);
        Float2 s3A = new Float2(position1.x + hA.x, position1.y - hA.y);
        Float2 s4A = new Float2(position1.x - hA.x, position1.y - hA.y);

        if (s1A.x < position2.x && s2A.x > position2.x && s4A.y < position2.y && s2A.y > position2.y)
        {
            return new ContainsPointReturn();
        }
        else
        {
            ContainsPointReturn returnVal = new ContainsPointReturn();
            if (s1A.x > position2.x)
            {
                returnVal.s1 = true;
                returnVal.s3 = false;
            }
            else if (s2A.x < position2.x)
            {
                returnVal.s3 = true;
                returnVal.s1 = false;
            }
            else
            {
                returnVal.s3 = false;
                returnVal.s1 = false;
            }

            if (s4A.y > position2.y)
            {
                returnVal.s4 = true;
                returnVal.s2 = false;
            }
            else if (s2A.y < position2.y)
            {
                returnVal.s2 = true;
                returnVal.s4 = false;
            }
            else
            {
                returnVal.s2 = false;
                returnVal.s4 = false;
            }
            returnVal.any = false;
            return returnVal;
        }

    }
}
public class ObstacleMapRequest
{
    public Task<bool[,]> obstacleMapReturn;
    public float timeStarted;
    public float timeCompleted;
}
public struct Obstacle
{
    public Float2 position;
    public Float2 size;
    public FixedString32Bytes name;
    public int instanceID;
}
public struct MapTarget
{
    public Float2 position;
    public FixedString32Bytes name;
    public int instanceID;
}
public class CompleteObstacleMapReturn
{
    public List<Obstacle>[,] obstacleMap;
    public bool[,] obstacleMapBool;
    public Float2 startArea;
    public Float2 size;
    public Float2Bounds bounds;
}
public struct CompleteObstacleMapReturnStruct
{
    public NativeArray<Obstacle> obstacleMap;
    public NativeArray<bool> obstacleMapBool;
    public Float2 startArea;
    public Float2 size;
    public Float2Bounds bounds;
}
/// <summary>
/// A collection of flattened data from a 2d array of complete obstacle maps. To retrieve a value it is recommended to use ObstacleMapUtils.RetrieveObstacleMap
/// </summary>
public struct CompleteObstacleStructCollection
{

    public NativeArray<Float2> sizes;
    public NativeArray<Float2> startAreas;
    public NativeArray<Float2Bounds> bounds;

    public NativeArray<bool> obstacleMapBools;

    public NativeArray<Obstacle> obstacleMapData;


    /// <summary>
    /// Index into here to get values from data. structed as: start index, count
    /// </summary>
    public NativeArray<int2> obstacleMapOffsets;
    public NativeArray<int2> obstacleMapBoolOffsets;
    public int2 size;
    public int2 Individualsize;

    public void Dispose()
    {
        if (obstacleMapData.IsCreated) obstacleMapData.Dispose();
        if (obstacleMapBools.IsCreated) obstacleMapBools.Dispose();
        if (sizes.IsCreated) sizes.Dispose();
        if (startAreas.IsCreated) startAreas.Dispose();
        if (bounds.IsCreated) bounds.Dispose();
        if (obstacleMapOffsets.IsCreated) obstacleMapOffsets.Dispose();
        if (obstacleMapBoolOffsets.IsCreated) obstacleMapBoolOffsets.Dispose();
    }
}
public struct CompleteObstacleMapSlice
{
    public Float2 size;
    public Float2 startArea;
    public Float2Bounds bounds;
    public NativeSlice<Obstacle> obstacleMap;
    public NativeSlice<bool> obstacleMapBool;
}
public static class ObstacleMapUtils
{


    public static CompleteObstacleMapSlice RetrieveObstacleMap(CompleteObstacleStructCollection collection, int2 index)
    {
        int width = collection.size.x;
        int flatIdx = index.y * width + index.x;

        int2 dataOff = collection.obstacleMapOffsets[flatIdx];
        int2 boolOff = collection.obstacleMapBoolOffsets[flatIdx];

        return new CompleteObstacleMapSlice
        {
            size = collection.sizes[flatIdx],
            startArea = collection.startAreas[flatIdx],
            bounds = collection.bounds[flatIdx],

            obstacleMap = new NativeSlice<Obstacle>(collection.obstacleMapData, dataOff.x, dataOff.y),
            obstacleMapBool = new NativeSlice<bool>(collection.obstacleMapBools, boolOff.x, boolOff.y)
        };
    }
    public static CompleteObstacleMapReturnStruct ObstacleMapReturnToStruct(CompleteObstacleMapReturn obr)
    {
        return new CompleteObstacleMapReturnStruct
        {
            size = obr.size,
            startArea = obr.startArea,
            bounds = obr.bounds,

        };
    }
    /// Returns a nested native array of obstacle structs. IMPORTANT: remember to dispose of the result when you are done to avoid a leak
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static CompleteObstacleStructCollection MultipleObstaclesToStruct(CompleteObstacleMapReturn[,] rtn)
    {
        int rows = rtn.GetLength(0);
        int cols = rtn.GetLength(1);
        int totalMaps = rows * cols;

        int totalObstacleElements = 0;
        int totalBoolElements = 0;
        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                var map = rtn[x, y].obstacleMap;
                for (int i = 0; i < map.GetLength(0); i++)
                    for (int j = 0; j < map.GetLength(1); j++)
                        totalObstacleElements += map[i, j].Count;

                totalBoolElements += rtn[x, y].obstacleMapBool.Length;
            }
        }

        var collection = new CompleteObstacleStructCollection
        {
            size = new int2(rows, cols),
            Individualsize = new int2(rtn[0, 0].obstacleMapBool.GetLength(0), rtn[0, 0].obstacleMapBool.GetLength(1)),
            sizes = new NativeArray<Float2>(totalMaps, Allocator.Persistent),
            startAreas = new NativeArray<Float2>(totalMaps, Allocator.Persistent),
            bounds = new NativeArray<Float2Bounds>(totalMaps, Allocator.Persistent),
            obstacleMapData = new NativeArray<Obstacle>(totalObstacleElements, Allocator.Persistent),
            obstacleMapBools = new NativeArray<bool>(totalBoolElements, Allocator.Persistent),
            obstacleMapOffsets = new NativeArray<int2>(totalMaps, Allocator.Persistent),
            obstacleMapBoolOffsets = new NativeArray<int2>(totalMaps, Allocator.Persistent)
        };

        int dataPtr = 0;
        int boolPtr = 0;

        for (int y = 0; y < cols; y++)
        {
            for (int x = 0; x < rows; x++)
            {
                int flatIdx = y * rows + x;
                var currentSource = rtn[x, y];

                collection.sizes[flatIdx] = currentSource.size;
                collection.startAreas[flatIdx] = currentSource.startArea;
                collection.bounds[flatIdx] = currentSource.bounds;

                var map = currentSource.obstacleMap;
                int mapWidth = map.GetLength(0);
                int mapHeight = map.GetLength(1);

                int obstacleStart = dataPtr;
                for (int i = 0; i < mapWidth; i++)
                {
                    for (int j = 0; j < mapHeight; j++)
                    {
                        var cell = map[i, j];
                        for (int c = 0; c < cell.Count; c++)
                        {
                            collection.obstacleMapData[dataPtr++] = cell[c];
                        }
                    }
                }
                collection.obstacleMapOffsets[flatIdx] = new int2(obstacleStart, dataPtr - obstacleStart);

                int boolStart = boolPtr;
                var boolMap = currentSource.obstacleMapBool;
                for (int i = 0; i < boolMap.GetLength(0); i++)
                {
                    for (int j = 0; j < boolMap.GetLength(1); j++)
                    {
                        collection.obstacleMapBools[boolPtr++] = boolMap[i, j];
                    }
                }
                collection.obstacleMapBoolOffsets[flatIdx] = new int2(boolStart, boolPtr - boolStart);
            }
        }

        return collection;
    }
}
public class ContainsPointReturn
{
    //Left
    public bool s1 = true;
    //Top
    public bool s2 = true;
    //Right
    public bool s3 = true;
    //Bottom
    public bool s4 = true;

    public bool any = true;
}
