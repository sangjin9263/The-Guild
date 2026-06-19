using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>경매 Lot 카드 1장 공통.</summary>
public interface IAuctionLotRowView
{
    void Bind(GateAuctionLotRuntime lot, GateDatabase database);
    void Clear();
    void RefreshInteractiveState();
}
