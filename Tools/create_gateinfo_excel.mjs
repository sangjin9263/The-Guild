import { writeFileSync, mkdirSync } from "fs";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import XLSX from "xlsx";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = join(__dirname, "..", "Assets", "Data");
const outPath = join(outDir, "Gateinfo.xlsx");

mkdirSync(outDir, { recursive: true });

const NOTES_COLUMN = "#notes";
const SPAWN_WEIGHT_SUM_COLUMN = "#spawn_weight_sum";

function note(text) {
  if (!text) return "";
  return text.startsWith("#") ? text : `# ${text}`;
}

const ALL_GRADES = ["F", "E", "D", "C", "B", "A", "S", "SS", "SSS"];
const TIER_WEIGHT_PERCENT = [60, 30, 10];

const UNLOCK_CONFIG = [
  {
    level: 1,
    maxGrade: "B",
    energyBarMax: 100,
    auctionTabs: "grade_only",
    notes: note("F~B 해금, spawn 피크 F~D"),
  },
  {
    level: 2,
    maxGrade: "A",
    energyBarMax: 130,
    auctionTabs: "grade_only",
    notes: "",
  },
  {
    level: 3,
    maxGrade: "S",
    energyBarMax: 170,
    auctionTabs: "grade_only",
    notes: note("spawn 피크 C~B"),
  },
  {
    level: 4,
    maxGrade: "SS",
    energyBarMax: 230,
    auctionTabs: "grade_and_unified",
    notes: note("통합 경매장 해금, spawn 피크 B~A"),
  },
  {
    level: 5,
    maxGrade: "SSS",
    energyBarMax: 300,
    auctionTabs: "grade_and_unified",
    notes: note("spawn 피크 B~A, SSS 희귀"),
  },
];

const SPAWN_WEIGHTS_BY_LEVEL = {
  1: { F: 35, E: 28, D: 20, C: 12, B: 5 },
  2: { F: 25, E: 22, D: 20, C: 18, B: 10, A: 5 },
  3: { F: 15, E: 14, D: 16, C: 18, B: 20, A: 12, S: 5 },
  4: { F: 10, E: 10, D: 12, C: 15, B: 20, A: 20, S: 8, SS: 5 },
  5: { F: 5, E: 8, D: 12, C: 18, B: 22, A: 22, S: 8, SS: 4, SSS: 1 },
};

function gradesUpTo(maxGrade) {
  const idx = ALL_GRADES.indexOf(maxGrade);
  return ALL_GRADES.slice(0, idx + 1);
}

function buildUnlockRows() {
  const header = [
    "building_level",
    "unlocked_grades",
    "max_unlocked_grade",
    "energy_bar_max",
    "auction_tabs",
    NOTES_COLUMN,
  ];
  const rows = UNLOCK_CONFIG.map((cfg) => [
    cfg.level,
    gradesUpTo(cfg.maxGrade).join(","),
    cfg.maxGrade,
    cfg.energyBarMax,
    cfg.auctionTabs,
    cfg.notes,
  ]);
  return [header, ...rows];
}

function buildSpawnWeightRows() {
  const header = [
    "building_level",
    "grade",
    "spawn_weight",
    SPAWN_WEIGHT_SUM_COLUMN,
    NOTES_COLUMN,
  ];
  const rows = [header];

  for (const [level, weights] of Object.entries(SPAWN_WEIGHTS_BY_LEVEL)) {
    const sum = Object.values(weights).reduce((a, b) => a + b, 0);
    for (const [grade, weight] of Object.entries(weights)) {
      rows.push([Number(level), grade, weight, sum, ""]);
    }
  }

  return rows;
}

function tierRow(
  grade,
  tierId,
  band,
  energyMin,
  energyMax,
  notes = "",
) {
  return [
    grade,
    tierId,
    band,
    energyMin,
    energyMax,
    TIER_WEIGHT_PERCENT[tierId],
    tierId + 1,
    "",
    notes,
  ];
}

const README = [
  ["Gateinfo — 던전 게이트 데이터"],
  [""],
  ["Import 규칙:"],
  ["  · 시트 이름이 # 로 시작하면 전체 시트 스킵 (이 시트 #ReadMe 포함)"],
  ["  · 컬럼 이름이 # 로 시작하면 해당 컬럼 전체 스킵 (아래 셀 값도 Import 안 함)"],
  ["  · 예: #notes, #spawn_weight_sum — Excel에서만 보는 검증/메모용"],
  [""],
  ["데이터 시트:"],
  ["  GateGrades / GateEnergyTiers / GateHints / GateAuctionEconomy"],
  ["  GateUnlock / GateSpawnWeights"],
  [""],
  ["경매 (코드): energy<=100 Ebay, energy>100 English, override 우선"],
  ["energy 롤 (코드): 100 미만 step 5 | 100 이상 10 단위 (Excel 컬럼 없음)"],
  [""],
  ["해금: Lv1 F~B | Lv2 F~A | Lv3 F~S | Lv4 F~SS | Lv5 F~SSS"],
  [""],
  ["spawn 순서:"],
  ["  1. GateSpawnWeights → grade (합 100)"],
  ["  2. GateEnergyTiers.tier_weight → tier (60/30/10 %)"],
  ["  3. energy 롤 → auction → GateHints"],
  [""],
  ["재생성: node Tools/create_gateinfo_excel.mjs"],
];

const GRADES = [
  ["grade", "energy_min", "energy_max", "grade_band", NOTES_COLUMN],
  ["F", 40, 70, "하위", ""],
  ["E", 50, 80, "하위", ""],
  ["D", 60, 90, "하위", ""],
  ["C", 70, 100, "중위", ""],
  ["B", 80, 150, "중위", ""],
  ["A", 90, 170, "중위", note("tier0 110+ English")],
  ["S", 100, 200, "상위", note("100=Ebay only")],
  ["SS", 150, 250, "상위", ""],
  ["SSS", 200, 300, "최상위", ""],
];

const ENERGY_TIER_RANGES = [
  ["F", "하위", [40, 50], [55, 60], [65, 70]],
  ["E", "하위", [50, 60], [65, 70], [75, 80]],
  ["D", "하위", [60, 70], [75, 80], [85, 90]],
  ["C", "중위", [70, 80], [85, 90], [95, 100]],
  ["B", "중위", [80, 100], [110, 130], [140, 150]],
  ["A", "중위", [90, 120], [130, 150], [160, 170]],
  ["S", "상위", [100, 150], [160, 180], [190, 200]],
  ["SS", "상위", [150, 200], [210, 230], [240, 250]],
  ["SSS", "최상위", [200, 240], [250, 290], [290, 300]],
];

const ENERGY_TIER_NOTES = {
  "A,0": note("110~120 롤 시 English (100+ 는 10단위)"),
  "S,0": note("energy=100 일 때만 Ebay"),
};

const ENERGY_TIERS = [
  [
    "grade",
    "tier_id",
    "grade_band",
    "energy_min",
    "energy_max",
    "tier_weight",
    "display_order",
    "auction_type_override",
    NOTES_COLUMN,
  ],
  ...ENERGY_TIER_RANGES.flatMap(([grade, band, t0, t1, t2]) => [
    tierRow(grade, 0, band, t0[0], t0[1], ENERGY_TIER_NOTES[`${grade},0`] ?? ""),
    tierRow(grade, 1, band, t1[0], t1[1]),
    tierRow(grade, 2, band, t2[0], t2[1]),
  ]),
];

const HINTS = [
  [
    "energy_min",
    "energy_max",
    "hint_display1",
    "hint_display2",
    "hint_display3",
    NOTES_COLUMN,
  ],
  [40, 50, "조용하다.", "아무 일 없어 보인다.", "바람만 스친다.", ""],
  [55, 60, "무난하다.", "평범한 느낌의 던전이다.", "특별할 것 없다.", ""],
  [
    65,
    70,
    "미세한 기운이 느껴진다.",
    "살짝 싸한 냄새가 난다.",
    "기운이 스친다.",
    "",
  ],
  [75, 80, "피부가 따끔하다.", "어딘가 어수선하다.", "거센 기운이 스친다.", ""],
  [85, 90, "안이 울리는 느낌이다.", "공기가 무거워진다.", "귀가 먹먹하다.", ""],
  [95, 100, "신경이 곤두선다.", "압박이 느껴진다.", "뭔가 잘못됐다.", ""],
  [
    110,
    130,
    "으드득거리는 소리가 난다.",
    "숨이 가빠진다.",
    "안개가 맴돈다.",
    "",
  ],
  [
    140,
    150,
    "불안한 예감이 든다.",
    "뒤통수가 서늘하다.",
    "누가 보고 있다.",
    "",
  ],
  [160, 180, "땅이 울린다.", "공기가 떨린다.", "심장이 빨라진다.", ""],
  [
    190,
    200,
    "가슴이 조여든다.",
    "소름이 돋는 기운이다.",
    "다리가 무거워진다.",
    "",
  ],
  [
    210,
    230,
    "도망치고 싶어진다.",
    "본능이 경계한다.",
    "이유를 모르게 두렵다.",
    "",
  ],
  [240, 249, "발이 떨어지지 않는다.", "숨이 막힌다.", "시야가 좁아진다.", ""],
  [250, 290, "불길한 기운이다.", "맥이 끊긴다.", "돌이킬 수 없다.", ""],
  [300, 300, "무언가 있다.", "끝이 보이지 않는다.", "가까이 있다.", ""],
];

const ECONOMY = [
  [
    "grade",
    "tier_id",
    "starting_price_min",
    "starting_price_max",
    "reward_gold_min",
    "reward_gold_max",
    "clear_time_sec",
    "ebay_duration_sec",
    "bid_increment",
    "english_round_sec",
    NOTES_COLUMN,
  ],
  ["F", 0, 30, 50, 80, 150, 1200, 60, 5, 15, ""],
  ["F", 1, 50, 70, 120, 200, 1500, 60, 8, 15, ""],
  ["F", 2, 70, 100, 150, 280, 1800, 75, 10, 15, ""],
  ["E", 0, 50, 80, 100, 200, 1800, 60, 10, 15, ""],
  ["E", 1, 80, 120, 150, 300, 2400, 75, 15, 15, ""],
  ["E", 2, 120, 160, 220, 380, 2700, 75, 15, 15, ""],
  ["D", 0, 100, 150, 200, 400, 2400, 60, 15, 15, ""],
  ["D", 1, 150, 220, 350, 550, 3000, 75, 20, 15, ""],
  ["D", 2, 220, 320, 500, 800, 3600, 90, 25, 15, ""],
  ["C", 0, 180, 260, 400, 700, 3000, 60, 25, 15, ""],
  ["C", 1, 260, 360, 600, 950, 3600, 75, 30, 15, ""],
  ["C", 2, 360, 500, 850, 1300, 4200, 90, 40, 15, ""],
  ["B", 0, 280, 380, 500, 900, 3600, 60, 30, 15, ""],
  ["B", 1, 380, 500, 750, 1100, 4200, 75, 40, 15, ""],
  ["B", 2, 500, 700, 900, 1400, 4800, 90, 50, 15, ""],
  ["A", 0, 400, 550, 800, 1200, 4200, 60, 40, 15, ""],
  ["A", 1, 550, 750, 1100, 1600, 4800, 75, 50, 15, ""],
  ["A", 2, 750, 1000, 1500, 2200, 5400, 90, 75, 15, ""],
  ["S", 0, 600, 850, 1200, 1800, 4800, 90, 60, 15, ""],
  ["S", 1, 850, 1150, 1700, 2500, 5400, 90, 80, 15, ""],
  ["S", 2, 1150, 1600, 2400, 3500, 6000, 90, 100, 15, ""],
  ["SS", 0, 900, 1250, 1800, 2700, 5400, 90, 80, 15, ""],
  ["SS", 1, 1250, 1700, 2600, 3800, 6000, 90, 100, 15, ""],
  ["SS", 2, 1700, 2300, 3600, 5200, 6600, 90, 125, 15, ""],
  ["SSS", 0, 1400, 1900, 2800, 4200, 6000, 90, 100, 15, ""],
  ["SSS", 1, 1900, 2600, 4000, 5800, 6600, 90, 125, 15, ""],
  ["SSS", 2, 2600, 3500, 5500, 8000, 7200, 90, 150, 15, ""],
];

const wb = XLSX.utils.book_new();
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(README), "#ReadMe");
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(GRADES), "GateGrades");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(ENERGY_TIERS),
  "GateEnergyTiers",
);
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(HINTS), "GateHints");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(ECONOMY),
  "GateAuctionEconomy",
);
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(buildUnlockRows()), "GateUnlock");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildSpawnWeightRows()),
  "GateSpawnWeights",
);

writeFileSync(outPath, XLSX.write(wb, { type: "buffer", bookType: "xlsx" }));
console.log(`Created: ${outPath}`);
