using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>BuildingPanel 탭 전환 — Content/title_tab 버튼 ↔ Content/UI_* 페이지.</summary>
[DisallowMultipleComponent]
public sealed class GuildPanelController : MonoBehaviour
{
    public enum GuildPanelTab
    {
        Home,
        Storage,
        Members,
        GateDispatch,
        Facility,
        Option
    }

    private static readonly (GuildPanelTab tab, string folderName)[] TabFolders =
    {
        (GuildPanelTab.Home, "Home"),
        (GuildPanelTab.Storage, "Storage"),
        (GuildPanelTab.Members, "Members"),
        (GuildPanelTab.GateDispatch, "Gate_dispatch"),
        (GuildPanelTab.Facility, "Facility"),
        (GuildPanelTab.Option, "Option")
    };

    private readonly Dictionary<GuildPanelTab, GameObject> _tabPages = new();
    private readonly Dictionary<GuildPanelTab, Button> _tabButtons = new();
    private GuildPanelTab _activeTab = GuildPanelTab.Home;
    private bool _wired;

    public GuildPanelTab ActiveTab => _activeTab;

    private void Awake()
    {
        CacheTabPages();
        WireTabButtons();
    }

    public void PrepareForOpen()
    {
        CacheTabPages();
        WireTabButtons();
        SelectTab(GuildPanelTab.Home);
    }

    public void SelectTab(GuildPanelTab tab)
    {
        _activeTab = tab;

        foreach (var pair in _tabPages)
            pair.Value?.SetActive(pair.Key == tab);
    }

    private void CacheTabPages()
    {
        _tabPages.Clear();
        _tabPages[GuildPanelTab.Home] =
            BuildingPanelLayoutFit.FindPanelTransform(transform, "Content/UI_Home")?.gameObject;
    }

    private void WireTabButtons()
    {
        if (_wired)
            return;

        var wiredAny = false;

        foreach (var (tab, folderName) in TabFolders)
        {
            var button = BuildingPanelLayoutFit
                .FindPanelTransform(transform, $"Content/title_tab/{folderName}/Button")
                ?.GetComponent<Button>();
            if (button == null)
                continue;

            _tabButtons[tab] = button;
            var captured = tab;
            button.onClick.AddListener(() => SelectTab(captured));
            wiredAny = true;
        }

        _wired = wiredAny;
    }
}
