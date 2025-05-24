using Game.Manager;
using Game.Resources.Building;
using Godot;

namespace Game.UI;

public partial class GameUI : CanvasLayer
{
    [Signal]
    public delegate void BuildingResourceSelectedEventHandler(BuildingResource buildingResource);

    [Export]
    private BuildingResource[] buildingResources;
    [Export]
    private PackedScene buildingSectionScene;
    [Export]
    private BuildingManager buildingManager;

    private VBoxContainer buildingSectionContainer;
    private Label resourceCountLabel;

    public override void _Ready()
    {
        buildingSectionContainer = GetNode<VBoxContainer>("%BuildingSectionContainer");
        resourceCountLabel = GetNode<Label>("%ResourceLabel");
        CreateBuildingSections();

        buildingManager.AvailableResourceCountChanged += OnAvailableResourceCountChanged;
    }

    public void HideUI()
    {
        Visible = false;
    }

    private void CreateBuildingSections()
    {
        foreach (var buildingResource in buildingResources)
        {
            var buildingSection = buildingSectionScene.Instantiate<BuildingSection>();
            buildingSectionContainer.AddChild(buildingSection);
            buildingSection.SetBuildingResource(buildingResource);

            buildingSection.SelectBuildingPressed += () =>
            {
                EmitSignal(SignalName.BuildingResourceSelected, buildingResource);
            };
        }
    }

    private void OnAvailableResourceCountChanged(int availableResourceCount)
    {
        resourceCountLabel.Text = $"{availableResourceCount}";
    }
}
