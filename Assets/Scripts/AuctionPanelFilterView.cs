using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>경매 패널 필터 UI — 등급 / 최소 입찰가 / 최소 에너지.</summary>
[DisallowMultipleComponent]
public sealed class AuctionPanelFilterView : MonoBehaviour
{
    private TMP_Dropdown _gradeDropdown;
    private TMP_InputField _minBidInput;
    private TMP_InputField _minEnergyInput;
    private Button _searchButton;
    private Button _resetButton;

    private AuctionPanelFilterCriteria _applied = AuctionPanelFilterCriteria.Empty;

    public AuctionPanelFilterCriteria AppliedCriteria => _applied;

    public event Action Applied;

    private void Awake()
    {
        CacheUiRefs();
        GameUIFont.ApplyDropdown(_gradeDropdown);
        GameUIFont.ApplyInputField(_minBidInput);
        GameUIFont.ApplyInputField(_minEnergyInput);
        WireButtons();
        ConfigureNumericInputs();
    }

    public void RefreshGradeOptions(GateDatabase database, int buildingLevel)
    {
        if (_gradeDropdown == null || database == null)
            return;

        GameUIFont.ApplyDropdown(_gradeDropdown);

        var unlock = database.GetUnlock(buildingLevel);
        var maxGrade = unlock?.maxUnlockedGrade ?? GateGrade.F;

        var options = new List<TMP_Dropdown.OptionData> { new("전체") };
        for (var grade = GateGrade.F; grade <= maxGrade; grade++)
            options.Add(new($"{GateGradeUtility.GetDisplayName(grade)} 등급"));

        _gradeDropdown.ClearOptions();
        _gradeDropdown.AddOptions(options);

        if (_gradeDropdown.value >= options.Count)
            _gradeDropdown.SetValueWithoutNotify(0);

        GameUIFont.ApplyDropdown(_gradeDropdown);
        _gradeDropdown.RefreshShownValue();
    }

    public void ResetToDefaults()
    {
        if (_gradeDropdown != null)
        {
            _gradeDropdown.SetValueWithoutNotify(0);
            GameUIFont.ApplyDropdown(_gradeDropdown);
            _gradeDropdown.RefreshShownValue();
        }

        if (_minBidInput != null)
            _minBidInput.SetTextWithoutNotify(string.Empty);

        if (_minEnergyInput != null)
            _minEnergyInput.SetTextWithoutNotify(string.Empty);

        _applied = AuctionPanelFilterCriteria.Empty;
    }

    private void CacheUiRefs()
    {
        _gradeDropdown = FindUi("Content/Dropdown")?.GetComponent<TMP_Dropdown>();
        _minBidInput = FindInputField("Content/Searchbar/coin")
                       ?? FindInputField("Content/Search_min/coin");
        _minEnergyInput = FindInputField("Content/Searchbar/energy")
                          ?? FindInputField("Content/Search_min/energy");
        _searchButton = FindUi("Content/Search")?.GetComponent<Button>();
        _resetButton = FindUi("Content/Reset")?.GetComponent<Button>();

        if (_gradeDropdown == null)
            Debug.LogWarning("AuctionPanelFilterView: Content/Dropdown not found.", this);
        if (_minBidInput == null)
            Debug.LogWarning("AuctionPanelFilterView: min bid InputField not found.", this);
        if (_minEnergyInput == null)
            Debug.LogWarning("AuctionPanelFilterView: min energy InputField not found.", this);
    }

    private TMP_InputField FindInputField(string containerPath)
    {
        var container = FindUi(containerPath);
        return container != null ? container.GetComponentInChildren<TMP_InputField>(true) : null;
    }

    private Transform FindUi(string relativePath) =>
        AuctionPanelLayoutFit.FindPanelTransform(transform, relativePath);

    private void WireButtons()
    {
        if (_searchButton != null)
            _searchButton.onClick.AddListener(OnSearchClicked);

        if (_resetButton != null)
            _resetButton.onClick.AddListener(OnResetClicked);
    }

    private void ConfigureNumericInputs()
    {
        ConfigureNumericInput(_minBidInput);
        ConfigureNumericInput(_minEnergyInput);
    }

    private static void ConfigureNumericInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.lineType = TMP_InputField.LineType.SingleLine;
    }

    private void OnSearchClicked()
    {
        _applied = BuildCriteriaFromUi();
        Applied?.Invoke();
    }

    private void OnResetClicked()
    {
        ResetToDefaults();
        Applied?.Invoke();
    }

    private void OnDestroy()
    {
        if (_searchButton != null)
            _searchButton.onClick.RemoveListener(OnSearchClicked);

        if (_resetButton != null)
            _resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private AuctionPanelFilterCriteria BuildCriteriaFromUi()
    {
        GateGrade? grade = null;
        if (_gradeDropdown != null && _gradeDropdown.value > 0)
            grade = (GateGrade)(_gradeDropdown.value - 1);

        return new AuctionPanelFilterCriteria(
            grade,
            TryParseMinValue(_minBidInput),
            TryParseMinValue(_minEnergyInput));
    }

    private static int? TryParseMinValue(TMP_InputField input)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text))
            return null;

        var trimmed = input.text.Trim().Replace(",", string.Empty);
        return int.TryParse(trimmed, out var value) ? value : null;
    }
}

public readonly struct AuctionPanelFilterCriteria
{
    public static AuctionPanelFilterCriteria Empty => new(null, null, null);

    public GateGrade? Grade { get; }
    public int? MinBidPrice { get; }
    /// <summary>최소 에너지 % (UI 바와 동일 기준).</summary>
    public int? MinEnergyPercent { get; }

    public AuctionPanelFilterCriteria(GateGrade? grade, int? minBidPrice, int? minEnergyPercent)
    {
        Grade = grade;
        MinBidPrice = minBidPrice;
        MinEnergyPercent = minEnergyPercent;
    }

    public static bool Matches(GateAuctionLotRuntime lot, AuctionPanelFilterCriteria criteria, GateDatabase database)
    {
        if (lot == null)
            return false;

        if (criteria.Grade.HasValue && lot.Lot.grade != criteria.Grade.Value)
            return false;

        if (criteria.MinBidPrice.HasValue && lot.Lot.auction.bidBandMin < criteria.MinBidPrice.Value)
            return false;

        if (criteria.MinEnergyPercent.HasValue)
        {
            var percent = database != null
                ? database.GetEnergyDisplayPercent(lot.Lot)
                : lot.Lot.energy * 100f / GateDatabase.EnergyPercentMax;

            if (percent < criteria.MinEnergyPercent.Value)
                return false;
        }

        return true;
    }
}
