using System;
using System.Collections.Generic;
using System.Linq;
using Game.Autoload;
using Game.Component;
using Game.Level.Util;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
    private const string IS_BUILDABLE = "is_buildable";
    private const string IS_WOOD = "is_wood";
    private const string IS_IGNORED = "is_ignored";

    [Signal]
    public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);
    [Signal]
    public delegate void GridStateUpdatedEventHandler();

    private HashSet<Vector2I> validBuildableTiles = new();
    private HashSet<Vector2I> allTilesInBuildingRadius = new();
    private HashSet<Vector2I> collectedResourceTiles = new();
    private HashSet<Vector2I> occupiedTiles = new();
    private HashSet<Vector2I> goblinOccupiedTiles = new();

    [Export]
    private TileMapLayer highlightTileMapLayer;
    [Export]
    private TileMapLayer baseTerrainTileMapLayer;

    private List<TileMapLayer> allTileMapLayers = new();
    private Dictionary<TileMapLayer, ElevationLayer> tileMapLayerToElevationLayer = new();

    public override void _Ready()
    {
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingPlaced, Callable.From<BuildingComponent>(OnBuildingPlaced));
        GameEvents.Instance.Connect(GameEvents.SignalName.BuildingDestroyed, Callable.From<BuildingComponent>(OnBuildingDestroyed));
        allTileMapLayers = GetAllTileMapLayers(baseTerrainTileMapLayer);
        MapTileMapLayersToElevationLayers();
    }

    public void ClearHighlightedTiles()
    {
        highlightTileMapLayer.Clear();
    }

    public void HighlightGoblinOccupiedTiles()
    {
        var atlasCoords = new Vector2I(2, 0);
        foreach (var tilePosition in goblinOccupiedTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightBuildableTiles()
    {
        foreach (var tilePosition in validBuildableTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
        }
    }

    public void HighlightExpandedBuildableTiles(Rect2I tileArea, int radius)
    {
        var validTiles = GetValidTilesInRadius(tileArea, radius).ToHashSet();
        var expandedTiles = validTiles
            .Except(validBuildableTiles)
            .Except(occupiedTiles)
            .Except(goblinOccupiedTiles);
        var atlasCoords = new Vector2I(1, 0);

        foreach (var tilePosition in expandedTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public void HighlightResourceTiles(Rect2I tileArea, int radius)
    {
        var resourceTiles = GetResourceTilesInRadius(tileArea, radius);
        var atlasCoords = new Vector2I(1, 0);

        foreach (var tilePosition in resourceTiles)
        {
            highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
        }
    }

    public (TileMapLayer, bool) GetTileCustomData(Vector2I tilePosition, string dataName)
    {
        foreach (var layer in allTileMapLayers)
        {
            var customData = layer.GetCellTileData(tilePosition);
            if (customData == null || (bool)customData.GetCustomData(IS_IGNORED)) continue;
            return (layer, (bool)customData.GetCustomData(dataName));
        }

        return (null, false);
    }

    public bool IsTilePositionBuildable(Vector2I tilePosition)
    {
        return validBuildableTiles.Contains(tilePosition);
    }

    public bool IsTileAreaBuildable(Rect2I tileArea)
    {
        var tiles = tileArea.ToTiles();

        if (tiles.Count == 0) return false;

        // get the target elevation layer from the first tile
        // we can do this because all tiles in the area must be in the same layer to be considered buildable
        (TileMapLayer firstTileMapLayer, _) = GetTileCustomData(tiles[0], IS_BUILDABLE);
        var targetElevationLayer = firstTileMapLayer != null ? tileMapLayerToElevationLayer[firstTileMapLayer] : null;

        return tiles.All((tilePosition) =>
        {
            // get the layer that the each tile belongs to
            (TileMapLayer tileMapLayer, bool isBuildable) = GetTileCustomData(tilePosition, IS_BUILDABLE);
            // get the elevation layer that each tile belongs to
            var elevationLayer = tileMapLayer != null ? tileMapLayerToElevationLayer[tileMapLayer] : null;
            // each tile must be buildable, must not have other buildings on it, and must be in the same elevation layer as the target cell
            return isBuildable && validBuildableTiles.Contains(tilePosition) && elevationLayer == targetElevationLayer;
        });
    }

    public Vector2I GetMouseGridCellPositionWithDimensionOffset(Vector2 dimensions)
    {
        var mouseGridPosition = highlightTileMapLayer.GetGlobalMousePosition() / 64;
        mouseGridPosition -= dimensions / 2;
        mouseGridPosition = mouseGridPosition.Round();
        return new Vector2I((int)mouseGridPosition.X, (int)mouseGridPosition.Y);
    }

    public Vector2I GetMouseGridCellPosition()
    {
        var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
        return ConvertWorldPositionToTilePosition(mousePosition);
    }

    public Vector2I ConvertWorldPositionToTilePosition(Vector2 worldPosition)
    {
        var tilePosition = worldPosition / 64;
        tilePosition = tilePosition.Floor();
        return new Vector2I((int)tilePosition.X, (int)tilePosition.Y);
    }

    public bool IsTilePositionInAnyBuildingRadius(Vector2I tilePosition)
    {
        return allTilesInBuildingRadius.Contains(tilePosition);
    }

    private List<TileMapLayer> GetAllTileMapLayers(Node2D rootNode)
    {
        var result = new List<TileMapLayer>();
        var children = rootNode.GetChildren();
        children.Reverse();
        foreach (var child in children)
        {
            if (child is Node2D childNode)
            {
                result.AddRange(GetAllTileMapLayers(childNode));
            }
        }
        if (rootNode is TileMapLayer tileMapLayer)
        {
            result.Add(tileMapLayer);
        }
        return result;
    }

    private void MapTileMapLayersToElevationLayers()
    {
        foreach (var layer in allTileMapLayers)
        {
            ElevationLayer elevationLayer;
            Node startNode = layer;

            // do statement will run first before the while condition is checked
            do
            {
                var parent = startNode.GetParent();
                elevationLayer = parent as ElevationLayer;
                startNode = parent;
            } while (elevationLayer == null && startNode != null);

            tileMapLayerToElevationLayer[layer] = elevationLayer;
        }
    }

    private void UpdateGoblinOccupiedTiles(BuildingComponent buildingComponent)
    {
        occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());
        var rootCell = buildingComponent.GetGridCellPosition();
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        if (buildingComponent.BuildingResource.DangerRadius > 0)
        {
            var tilesInRadius = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.DangerRadius).ToHashSet();
            tilesInRadius.ExceptWith(occupiedTiles);
            goblinOccupiedTiles.UnionWith(tilesInRadius);
        }
    }

    private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
    {
        occupiedTiles.UnionWith(buildingComponent.GetOccupiedCellPositions());

        var rootCell = buildingComponent.GetGridCellPosition();
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);

        var allTiles = GetTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius, (_) => true);
        allTilesInBuildingRadius.UnionWith(allTiles);

        var validTiles = GetValidTilesInRadius(tileArea, buildingComponent.BuildingResource.BuildableRadius);
        validBuildableTiles.UnionWith(validTiles);
        validBuildableTiles.ExceptWith(occupiedTiles);
        validBuildableTiles.ExceptWith(goblinOccupiedTiles);

        EmitSignal(SignalName.GridStateUpdated);
    }

    private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
    {
        var rootCell = buildingComponent.GetGridCellPosition();
        var tileArea = new Rect2I(rootCell, buildingComponent.BuildingResource.Dimensions);
        var resourceTiles = GetResourceTilesInRadius(tileArea, buildingComponent.BuildingResource.ResourceRadius);

        var oldResourceTileCount = collectedResourceTiles.Count;
        collectedResourceTiles.UnionWith(resourceTiles);

        if (collectedResourceTiles.Count != oldResourceTileCount)
        {
            EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
        }

        EmitSignal(SignalName.GridStateUpdated);
    }

    private void RecalculateGrid()
    {
        occupiedTiles.Clear();
        validBuildableTiles.Clear();
        allTilesInBuildingRadius.Clear();
        collectedResourceTiles.Clear();

        var buildingComponents = BuildingComponent.GetValidBuildingComponents(this);

        foreach (var buildingComponent in buildingComponents)
        {
            UpdateValidBuildableTiles(buildingComponent);
            UpdateCollectedResourceTiles(buildingComponent);
        }

        EmitSignal(SignalName.ResourceTilesUpdated, collectedResourceTiles.Count);
        EmitSignal(SignalName.GridStateUpdated);
    }

    private bool IsTileInsideCircle(Vector2 centerPosition, Vector2 tilePosition, float radius)
    {
        var distanceX = centerPosition.X - (tilePosition.X + .5);
        var distanceY = centerPosition.Y - (tilePosition.Y + .5);
        var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        return distanceSquared <= radius * radius;
    }

    private List<Vector2I> GetTilesInRadius(Rect2I tileArea, int radius, Func<Vector2I, bool> filterFn)
    {
        var result = new List<Vector2I>();
        var tileAreaF = tileArea.ToRect2F();
        var tileAreaCenter = tileAreaF.GetCenter();
        var radiusMod = Mathf.Max(tileAreaF.Size.X, tileAreaF.Size.Y) / 2;
        for (var x = tileArea.Position.X - radius; x <= tileArea.End.X + radius; x++)
        {
            for (var y = tileArea.Position.Y - radius; y <= tileArea.End.Y + radius; y++)
            {
                var tilePosition = new Vector2I(x, y);
                if (!IsTileInsideCircle(tileAreaCenter, tilePosition, radius + radiusMod) || !filterFn(tilePosition)) continue;

                result.Add(tilePosition);
            }
        }
        return result;
    }

    private List<Vector2I> GetValidTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) =>
        {
            return GetTileCustomData(tilePosition, IS_BUILDABLE).Item2;
        });
    }

    private List<Vector2I> GetResourceTilesInRadius(Rect2I tileArea, int radius)
    {
        return GetTilesInRadius(tileArea, radius, (tilePosition) =>
        {
            return GetTileCustomData(tilePosition, IS_WOOD).Item2;
        });
    }

    private void OnBuildingPlaced(BuildingComponent buildingComponent)
    {
        UpdateGoblinOccupiedTiles(buildingComponent);
        UpdateValidBuildableTiles(buildingComponent);
        UpdateCollectedResourceTiles(buildingComponent);
    }

    private void OnBuildingDestroyed(BuildingComponent buildingComponent)
    {
        RecalculateGrid();
    }
}
