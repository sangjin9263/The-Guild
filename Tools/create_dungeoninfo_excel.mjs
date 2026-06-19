import { writeFileSync, mkdirSync } from "fs";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import XLSX from "xlsx";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = join(__dirname, "..", "Assets", "Data");
const outPath = join(outDir, "Dungeoninfo.xlsx");

mkdirSync(outDir, { recursive: true });

const NOTES_COLUMN = "#notes";
const POOL_WEIGHT_SUM_COLUMN = "#pool_weight_sum";

function note(text) {
  if (!text) return "";
  return text.startsWith("#") ? text : `# ${text}`;
}

/** Gateinfo Auction bid_band와 동기 — 클리어 골드 placeholder 생성용 */
const BID_BAND_REF = [
  ["F", 0, 30, 50],
  ["F", 1, 50, 70],
  ["F", 2, 70, 100],
  ["E", 0, 50, 80],
  ["E", 1, 80, 120],
  ["E", 2, 120, 160],
  ["D", 0, 100, 150],
  ["D", 1, 150, 220],
  ["D", 2, 220, 320],
  ["C", 0, 180, 260],
  ["C", 1, 260, 360],
  ["C", 2, 360, 500],
  ["B", 0, 280, 380],
  ["B", 1, 380, 500],
  ["B", 2, 500, 700],
  ["A", 0, 400, 550],
  ["A", 1, 550, 750],
  ["A", 2, 750, 1000],
  ["S", 0, 600, 850],
  ["S", 1, 850, 1150],
  ["S", 2, 1150, 1600],
  ["SS", 0, 900, 1250],
  ["SS", 1, 1250, 1700],
  ["SS", 2, 1700, 2300],
  ["SSS", 0, 1400, 1900],
  ["SSS", 1, 1900, 2600],
  ["SSS", 2, 2600, 3500],
];

const README = [
  ["Dungeoninfo — 던전 클리어 보상 데이터"],
  [""],
  ["Import 규칙:"],
  ["  · 시트/컬럼 이름이 # 으로 시작하면 Import 스킵"],
  ["  · Unity: The Guild/Data/Import Dungeoninfo"],
  [""],
  ["Import 시트:"],
  ["  LootPools / ArchetypeLoot / ClearRewards"],
  ["  MutationRewardMix / MutationModifiers / DungeonConstants"],
  [""],
  ["연동:"],
  ["  GateLot.archetype → ArchetypeLoot → 보상 종류"],
  ["  LootPools.item_key → Iteminfo.Items"],
  ["  archetype=gold → ClearRewards (grade × tier_id)"],
  [""],
  ["ClearRewards:"],
  ["  placeholder: gold_min=max(bid_band_min×2), gold_max=bid_band_max×3"],
  [""],
  ["재생성: node Tools/create_dungeoninfo_excel.mjs"],
];

const LOOT_POOLS = [
  [
    "pool_id",
    "item_key",
    "weight",
    "quantity_min",
    "quantity_max",
    POOL_WEIGHT_SUM_COLUMN,
    NOTES_COLUMN,
  ],
  ["pool_mineral", "ore_iron", 40, 2, 5, 100, note("하위 광물")],
  ["pool_mineral", "ore_silver", 30, 1, 3, 100, ""],
  ["pool_mineral", "ore_gold", 20, 1, 2, 100, ""],
  ["pool_mineral", "crystal_mana", 10, 1, 1, 100, note("희귀")],
  ["pool_equipment", "normal_sword", 35, 1, 1, 100, ""],
  ["pool_equipment", "normal_bow", 25, 1, 1, 100, ""],
  ["pool_equipment", "leather_armor", 25, 1, 1, 100, ""],
  ["pool_equipment", "leather_helmet", 15, 1, 1, 100, ""],
  ["pool_artifact", "artifact_1", 50, 1, 1, 100, note("낡은 길드 인장")],
  ["pool_artifact", "artifact_2", 50, 1, 1, 100, note("녹슨 전리품 주머니")],
];

const ARCHETYPE_LOOT = [
  ["archetype", "reward_kind", "loot_pool_id", NOTES_COLUMN],
  ["gold", "gold", "", note("ClearRewards 참조")],
  ["mineral", "loot_pool", "pool_mineral", ""],
  ["equipment", "loot_pool", "pool_equipment", ""],
  ["artifact", "loot_pool", "pool_artifact", ""],
  ["mutation", "mutation", "", note("MutationRewardMix + MutationModifiers")],
];

function buildClearRewardRows() {
  const header = [
    "grade",
    "tier_id",
    "gold_min",
    "gold_max",
    "clear_time_sec",
    NOTES_COLUMN,
  ];
  const rows = [
    header,
    ...BID_BAND_REF.map(([grade, tierId, bidMin, bidMax]) => [
      grade,
      tierId,
      bidMin * 2,
      bidMax * 3,
      1800 + tierId * 600,
      tierId === 0 ? note("placeholder: bid_band ×2 / ×3") : "",
    ]),
  ];
  return rows;
}

const MUTATION_REWARD_MIX = [
  [
    "grade_min",
    "grade_max",
    "hidden_archetype",
    "weight",
    "loot_pool_id",
    NOTES_COLUMN,
  ],
  ["F", "D", "gold", 55, "", note("F~D artifact 없음")],
  ["F", "D", "mineral", 25, "pool_mineral", ""],
  ["F", "D", "equipment", 20, "pool_equipment", ""],
  ["C", "SSS", "gold", 45, "", ""],
  ["C", "SSS", "mineral", 25, "pool_mineral", ""],
  ["C", "SSS", "equipment", 20, "pool_equipment", ""],
  ["C", "SSS", "artifact", 10, "pool_artifact", note("C+ 유물")],
];

const MUTATION_MODIFIERS = [
  [
    "modifier_tier",
    "weight",
    "difficulty_mult",
    "reward_mult",
    NOTES_COLUMN,
  ],
  [
    "standard",
    70,
    1.15,
    1.25,
    note("clear_time /= difficulty_mult, 보상 × reward_mult"),
  ],
  ["volatile", 25, 1.28, 1.4, ""],
  ["extreme", 5, 1.45, 1.6, note("아주 가끔")],
];

const DUNGEON_CONSTANTS = [
  ["key", "value", NOTES_COLUMN],
  ["gold_roll_step", "5", note("클리어 골드 step")],
  ["loot_roll_count", "1", note("풀 1회당 아이템 종류 수")],
];

const wb = XLSX.utils.book_new();
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(README), "#ReadMe");
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(LOOT_POOLS), "LootPools");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(ARCHETYPE_LOOT),
  "ArchetypeLoot",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildClearRewardRows()),
  "ClearRewards",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(MUTATION_REWARD_MIX),
  "MutationRewardMix",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(MUTATION_MODIFIERS),
  "MutationModifiers",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(DUNGEON_CONSTANTS),
  "DungeonConstants",
);

writeFileSync(outPath, XLSX.write(wb, { type: "buffer", bookType: "xlsx" }));
console.log(`Created: ${outPath}`);
