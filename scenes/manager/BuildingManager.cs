using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Game.Building;
using Game.Component;
using Game.Resources.Building;
using Game.UI;
using Godot;

namespace Game.Manager;

public partial class BuildingManager : Node
{
    private readonly StringName ACTION_LEFT_CLICK = "left_click";
    private readonly StringName ACTION_CANCEL = "cancel";
    private readonly StringName ACTION_RIGHT_CLICK = "right_click";

    [Signal]
    public delegate void AvailableResourceCountChangedEventHandler(int availableResourceCount);

    [Export]
    private GridManager gridManager;
    [Export]
    private GameUI gameUI;
    [Export]
    private Node2D ySortRoot;
    [Export]
    private PackedScene buildingGhostScene;


    private enum State
    {
        Normal,
        PlacingBuilding
    }

    private int startingResourceCount;
    private int currentResourceCount;
    private int currentlyUsedResourceCount;
    private BuildingResource toPlaceBuildingResource;
    private Rect2I hoveredGridArea = new(Vector2I.Zero, Vector2I.One);
    private BuildingGhost buildingGhost;
    private Vector2 buildingGhostDimensions;

    private State currentState = State.Normal;

    private int AvailableResourceCount => startingResourceCount + currentResourceCount - currentlyUsedResourceCount;

    public override void _Ready()
    {
        gridManager.ResourceTilesUpdated += OnResourceTilesUpdated;
        gameUI.BuildingResourceSelected += OnBuildingResourceSelected;

        Callable.From(() => EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount)).CallDeferred();
    }

    public override void _UnhandledInput(InputEvent evt)
    {
        switch (currentState)
        {
            case State.Normal:
                if (evt.IsActionPressed(ACTION_RIGHT_CLICK))
                {
                    DestroyBuildingAtHoveredCellPosition();
                }
                break;
            case State.PlacingBuilding:
                if (evt.IsActionPressed(ACTION_CANCEL))
                {
                    ChangeState(State.Normal);
                }
                else if (
                    toPlaceBuildingResource != null &&
                    evt.IsActionPressed(ACTION_LEFT_CLICK) &&
                    IsBuildingPlaceableAtArea(hoveredGridArea)
                )
                {
                    PlaceBuildingAtHoveredCellPosition();
                }
                break;
            default:
                break;
        }
    }

    public override void _Process(double delta)
    {
        Vector2I mouseGridPosition = Vector2I.Zero;

        switch (currentState)
        {
            case State.Normal:
                mouseGridPosition = gridManager.GetMouseGridCellPosition();
                break;
            case State.PlacingBuilding:
                mouseGridPosition = gridManager.GetMouseGridCellPositionWithDimensionOffset(buildingGhostDimensions);
                buildingGhost.GlobalPosition = mouseGridPosition * 64;
                break;
        }

        var rootCell = hoveredGridArea.Position;
        if (rootCell != mouseGridPosition)
        {
            hoveredGridArea.Position = mouseGridPosition;
            UpdateHoveredGridArea();
        }

    }

    public void SetStartingResourceCount(int count)
    {
        startingResourceCount = count;
    }

    private void UpdateGridDisplay()
    {
        gridManager.ClearHighlightedTiles();
        gridManager.HighlightGoblinOccupiedTiles();
        gridManager.HighlightBuildableTiles();
        if (IsBuildingPlaceableAtArea(hoveredGridArea))
        {
            gridManager.HighlightExpandedBuildableTiles(hoveredGridArea, toPlaceBuildingResource.BuildableRadius);
            gridManager.HighlightResourceTiles(hoveredGridArea, toPlaceBuildingResource.ResourceRadius);
            buildingGhost.SetValid();
        }
        else
        {
            buildingGhost.SetInvalid();
        }

        buildingGhost.DoHoverAnimation();
    }

    private void PlaceBuildingAtHoveredCellPosition()
    {
        var building = toPlaceBuildingResource.BuildingScene.Instantiate<Node2D>();
        ySortRoot.AddChild(building);

        building.GlobalPosition = hoveredGridArea.Position * 64;
        building.GetFirstNodeOfType<BuildingAnimatorComponent>()?.PlayInAnimation();
        currentlyUsedResourceCount += toPlaceBuildingResource.ResourceCost;

        ChangeState(State.Normal);
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void DestroyBuildingAtHoveredCellPosition()
    {
        var rootCell = hoveredGridArea.Position;
        var buildingComponent = BuildingComponent.GetValidBuildingComponents(this)
            .FirstOrDefault((buildingComponent) =>
            {
                return buildingComponent.BuildingResource.IsDeletable && buildingComponent.IsTileInBuildingArea(rootCell);
            });
        if (buildingComponent == null) return;

        currentlyUsedResourceCount -= buildingComponent.BuildingResource.ResourceCost;
        buildingComponent.Destroy();
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void ClearBuildingGhost()
    {
        gridManager.ClearHighlightedTiles();

        if (IsInstanceValid(buildingGhost))
        {
            buildingGhost.QueueFree();
        }
        buildingGhost = null;
    }

    private bool IsBuildingPlaceableAtArea(Rect2I tileArea)
    {
        var allTilesBuildable = gridManager.IsTileAreaBuildable(tileArea);
        return allTilesBuildable && AvailableResourceCount >= toPlaceBuildingResource.ResourceCost;
    }

    private void UpdateHoveredGridArea()
    {
        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                UpdateGridDisplay();
                break;
            default:
                break;
        }
    }

    private void ChangeState(State toState)
    {
        // if (toState == currentState) return;

        switch (currentState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                ClearBuildingGhost();
                toPlaceBuildingResource = null;
                break;
        }

        currentState = toState;

        switch (toState)
        {
            case State.Normal:
                break;
            case State.PlacingBuilding:
                buildingGhost = buildingGhostScene.Instantiate<BuildingGhost>();
                ySortRoot.AddChild(buildingGhost);
                break;
        }
    }

    private void OnResourceTilesUpdated(int resourceCount)
    {
        currentResourceCount = resourceCount;
        EmitSignal(SignalName.AvailableResourceCountChanged, AvailableResourceCount);
    }

    private void OnBuildingResourceSelected(BuildingResource buildingResource)
    {
        ChangeState(State.PlacingBuilding);
        hoveredGridArea.Size = buildingResource.Dimensions;
        var buildingSprite = buildingResource.SpriteScene.Instantiate<Sprite2D>();
        buildingGhost.AddSpriteNode(buildingSprite);
        buildingGhost.SetDimensions(buildingResource.Dimensions);
        buildingGhostDimensions = buildingResource.Dimensions;
        toPlaceBuildingResource = buildingResource;
        UpdateGridDisplay();
    }
}
