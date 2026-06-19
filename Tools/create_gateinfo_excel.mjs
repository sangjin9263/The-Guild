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
const ENERGY_PERCENT_MAX = 200;
const ENERGY_ROLL_STEP = 5;
/** tier_id가 이 값이면 English (등급 무관). */
const ENGLISH_TIER_ID = 2;
/** 이 등급 이상이면 tier 무관 English. */
const ENGLISH_MIN_GRADE = "S";

const ARCHETYPES = ["gold", "mineral", "equipment", "artifact", "mutation"];
const ARCHETYPE_WEIGHT_SUM_COLUMN = "#weight_sum";

const UNLOCK_CONFIG = [
  {
    level: 1,
    maxGrade: "B",
    auctionTabs: "grade_only",
    notes: note(
      `F~B 해금 · 최대 에너지=${ENERGY_PERCENT_MAX} (에너지 %=energy/${ENERGY_PERCENT_MAX})`,
    ),
  },
  {
    level: 2,
    maxGrade: "A",
    auctionTabs: "grade_only",
    notes: "",
  },
  {
    level: 3,
    maxGrade: "S",
    auctionTabs: "grade_only",
    notes: note("spawn 피크 C~B"),
  },
  {
    level: 4,
    maxGrade: "SS",
    auctionTabs: "grade_and_unified",
    notes: note("통합 경매장 해금, spawn 피크 B~A"),
  },
  {
    level: 5,
    maxGrade: "SSS",
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
    "auction_tabs",
    NOTES_COLUMN,
  ];
  const rows = UNLOCK_CONFIG.map((cfg) => [
    cfg.level,
    gradesUpTo(cfg.maxGrade).join(","),
    cfg.maxGrade,
    cfg.auctionTabs,
    cfg.notes,
  ]);
  return [header, ...rows];
}

/** @type {Record<string, number>} */
const ARTIFACT_WEIGHT_BY_GRADE = {
  F: 0,
  E: 0,
  D: 0,
  C: 3,
  B: 3,
  A: 5,
  S: 7,
  SS: 9,
  SSS: 10,
};

/** @returns {Record<string, number>} */
function archetypeWeightsFor(grade, tierId) {
  const gradeIdx = ALL_GRADES.indexOf(grade);
  const artifact = ARTIFACT_WEIGHT_BY_GRADE[grade] ?? 0;

  let gold = 35;
  let mineral = 25;
  let equipment = 25;
  let mutation = 10;

  if (artifact === 0) {
    // F~D: 유물 없음 — 빠진 5% → gold +3, mineral +2
    gold += 3;
    mineral += 2;
  } else {
    // C~SSS: 등급별 artifact, 기본 5% 대비 gold 조정
    gold += 5 - artifact;
  }

  // F~D tier2: 광물·장비 ↑ (역전 스파이스용)
  if (tierId === 2 && gradeIdx <= 2) {
    gold -= 10;
    mineral += 5;
    equipment += 5;
  }

  return { gold, mineral, equipment, artifact, mutation };
}

function buildArchetypeRows() {
  const header = [
    "grade",
    "tier_id",
    "archetype",
    "weight",
    ARCHETYPE_WEIGHT_SUM_COLUMN,
    NOTES_COLUMN,
  ];
  const rows = [header];

  for (const grade of ALL_GRADES) {
    for (let tierId = 0; tierId < 3; tierId++) {
      const weights = archetypeWeightsFor(grade, tierId);
      const sum = Object.values(weights).reduce((a, b) => a + b, 0);

      for (const archetype of ARCHETYPES) {
        rows.push([
          grade,
          tierId,
          archetype,
          weights[archetype],
          sum,
          archetype === "gold" ? note("보상 상세 → Dungeoninfo") : "",
        ]);
      }
    }
  }

  return rows;
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

function resolveBidType(grade, tierId) {
  const gradeIdx = ALL_GRADES.indexOf(grade);
  const minIdx = ALL_GRADES.indexOf(ENGLISH_MIN_GRADE);
  if (tierId === ENGLISH_TIER_ID) return "English";
  if (gradeIdx >= minIdx) return "English";
  return "Ebay";
}

/** @deprecated GateEnergyTiers.auction_type_override — resolveBidType 과 동일 */
const resolveAuctionTypeOverride = resolveBidType;

function auctionTypeNote(grade, tierId) {
  if (tierId === ENGLISH_TIER_ID)
    return note(`tier${ENGLISH_TIER_ID} → English`);
  const gradeIdx = ALL_GRADES.indexOf(grade);
  const minIdx = ALL_GRADES.indexOf(ENGLISH_MIN_GRADE);
  if (gradeIdx >= minIdx)
    return note(`${ENGLISH_MIN_GRADE}+ → English`);
  return "";
}

function tierRow(
  grade,
  tierId,
  energyMin,
  energyMax,
  notes = "",
) {
  const auctionOverride = resolveAuctionTypeOverride(grade, tierId);
  const typeNote = auctionTypeNote(grade, tierId);
  const combinedNotes = [notes, typeNote].filter(Boolean).join(" · ");
  return [
    grade,
    tierId,
    energyMin,
    energyMax,
    TIER_WEIGHT_PERCENT[tierId],
    tierId + 1,
    auctionOverride,
    combinedNotes,
  ];
}

const README = [
  ["Gateinfo — 입장권·경매장 데이터"],
  [""],
  ["Import 규칙:"],
  ["  · 시트/컬럼 이름이 # 으로 시작하면 Import 스킵 (#ReadMe, #gap 등)"],
  [""],
  ["Import 시트 (순서 무관):"],
  ["  GateConstants / GateGradeBands / GateGrades / GateEnergyTiers"],
  ["  Auction / EnglishTimerTiers / EnglishAiBehavior / GateBrokerHints / GateArchetypes / GateBrokerPricing"],
  ["  GateUnlock / GateSpawnWeights"],
  [""],
  ["던전 보상 (드랍·클리어 골드·mutation) → Dungeoninfo.xlsx"],
  ["아이템 마스터 → Iteminfo.xlsx"],
  [""],
  [`에너지 %: energy / ${ENERGY_PERCENT_MAX} × 100 (GateConstants.energy_percent_max)`],
  [`  energy 롤: ${ENERGY_ROLL_STEP} 단위 고정 (min~max, 등급·구간 무관)`],
  ["  F tier0만 5~25 (폭 20) · SSS tier2 = 200 고정"],
  ["  tier2(N) = tier0(N+1) 브릿지 · 같은 tier_id면 상위 등급 energy 구간이 더 높음"],
  ["  건물 Lv와 무관 — 같은 Lot은 항상 같은 %"],
  [""],
  ["Lot 생성 순서 (코드):"],
  ["  1. GateSpawnWeights → grade"],
  ["  2. GateEnergyTiers → tier → energy 롤"],
  ["  3. GateArchetypes → 주 성향 (입장권 라벨)"],
  ["  4. Auction[grade,tier] → bid_type · bid_band · ai_count (English/Ebay 공용 lookup)"],
  ["  5. bid_type: tier2 → English | S+ 등급 → English · 그 외 Ebay"],
  [""],
  ["경매 UI:"],
  ["  · 무료 힌트 없음 — 등급 + 에너지 % + 입찰가"],
  ["  · 정보상: GateBrokerPricing + GateBrokerHints (tier_id)"],
  [""],
  ["GateBrokerHints:"],
  ["  archetype × tier_id(0~2) · up_arrow = tier_id + 1"],
  [""],
  ["GateArchetypes:"],
  ["  artifact F~D=0 | C=3 | B=3 | A=5 | S=7 | SS=9 | SSS=10"],
  ["  F~D tier2: mineral/equipment ↑ · weight 합=100"],
  [""],
  ["Auction:"],
  ["  grade × tier_id · bid_type(English/Ebay) · bid_band_min~max · ai_count_min~max"],
  ["  · bid_type 규칙 = GateConstants english_min_grade / english_tier_id 와 동일"],
  ["  · opening_bid·입찰가·힌트: bid_band (Ebay·English 공용)"],
  ["  · English: ai_count rnd → 카드 AI N명 · Ebay: 결과 AI 3명(코드 고정)"],
  [""],
  ["EnglishTimerTiers:"],
  ["  bid_count_gt 초과 시 timer_sec 적용 (내림차순 매칭)"],
  ["  · 0→60초(1분) · 4→45 · 7→30 · 10→15"],
  [""],
  ["EnglishAiBehavior:"],
  ["  grade별 English 라이브 AI 입찰 패턴 (Ebay 미사용)"],
  ["  · 1회 입찰 = step_count rnd × english_bid_increment"],
  ["  · 유저가 방금 1등이면 + player_counter_step rnd (추가 step)"],
  ["  · react_delay 후 counter_bid_chance% 로 입찰 시도 · 상한 english_ai_max_steps_per_bid"],
  [""],
  ["GateConstants (경매 규칙):"],
  [`  english_min_grade (${ENGLISH_MIN_GRADE}+ → English)`],
  [`  english_tier_id (tier${ENGLISH_TIER_ID} → English, 등급 무관)`],
  ["  ebay_bid_increment · english_bid_increment (없으면 ebay와 동일)"],
  ["  english_ai_max_steps_per_bid · english_ai_eval_interval_sec"],
  [""],
  ["GateEnergyTiers.auction_type_override:"],
  ["  · 행별 English/Ebay — 코드 규칙과 동기 (수동 override 가능)"],
  [""],
  ["해금: Lv1 F~B | Lv2 F~A | Lv3 F~S | Lv4 F~SS | Lv5 F~SSS"],
  [""],
  ["재생성: node Tools/create_gateinfo_excel.mjs"],
];

const GRADE_ICON_NOTE = note(
  "icon_sprite 비움=자동(gate_img_auction/{grade}). 값 있으면 해당 슬라이스 이름으로 override",
);

const GRADES = [
  ["grade", "icon_sprite", NOTES_COLUMN],
  ["F", "", GRADE_ICON_NOTE],
  ["E", "", note("icon_sprite 비움 → gate_img_auction E 슬라이스")],
  ["D", "", note("icon_sprite 비움 → gate_img_auction D 슬라이스")],
  ["C", "", note("icon_sprite 비움 → gate_img_auction C 슬라이스")],
  ["B", "", note("icon_sprite 비움 → gate_img_auction B 슬라이스")],
  ["A", "", note("icon_sprite 비움 → gate_img_auction A 슬라이스")],
  ["S", "", note("icon_sprite 비움 → gate_img_auction S 슬라이스")],
  ["SS", "", note("icon_sprite 비움 → gate_img_auction SS 슬라이스")],
  ["SSS", "", note("icon_sprite 비움 → gate_img_auction SSS 슬라이스")],
];

const ENERGY_TIER_RANGES = [
  ["F", [5, 25], [30, 35], [40, 45]],
  ["E", [40, 45], [50, 55], [60, 65]],
  ["D", [60, 65], [70, 75], [80, 85]],
  ["C", [80, 85], [90, 95], [100, 105]],
  ["B", [100, 105], [110, 115], [120, 125]],
  ["A", [120, 125], [130, 135], [140, 145]],
  ["S", [140, 145], [150, 155], [160, 165]],
  ["SS", [160, 165], [170, 175], [180, 185]],
  ["SSS", [180, 185], [190, 195], [200, 200]],
];

const ENERGY_TIER_NOTES = {
  "F,0": note("F tier0만 5~25 (입문 분산)"),
  "SSS,2": note("tier2 energy 200 고정 (100%)"),
};

const ENERGY_TIERS = [
  [
    "grade",
    "tier_id",
    "energy_min",
    "energy_max",
    "tier_weight",
    "display_order",
    "auction_type_override",
    NOTES_COLUMN,
  ],
  ...ENERGY_TIER_RANGES.flatMap(([grade, t0, t1, t2]) => [
    tierRow(grade, 0, t0[0], t0[1], ENERGY_TIER_NOTES[`${grade},0`] ?? ""),
    tierRow(grade, 1, t1[0], t1[1]),
    tierRow(grade, 2, t2[0], t2[1]),
  ]),
];

/** @type {Record<string, [number, number]>} grade → [ai_count_min, ai_count_max] (English 카드 표시용) */
const AI_COUNT_BY_GRADE = {
  F: [2, 3],
  E: [2, 3],
  D: [3, 4],
  C: [3, 4],
  B: [3, 5],
  A: [4, 5],
  S: [4, 6],
  SS: [5, 7],
  SSS: [6, 8],
};

/** bid_count_gt 초과 시 timer_sec (English 라이브 경매) */
const ENGLISH_TIMER_TIERS = [
  [0, 60, note("시작 1분 · bid count ≤4")],
  [4, 45, note("bid count >4 → 45초")],
  [7, 30, note("bid count >7 → 30초")],
  [10, 15, note("bid count >10 → 15초")],
];

/** @type {[string, number, number, number, string][]} grade, tier_id, min, max, notes */
const AUCTION_RAW = [
  ["F", 0, 30, 50, ""],
  ["F", 1, 50, 70, ""],
  ["F", 2, 70, 100, ""],
  ["E", 0, 50, 80, ""],
  ["E", 1, 80, 120, ""],
  ["E", 2, 120, 160, ""],
  ["D", 0, 100, 150, ""],
  ["D", 1, 150, 220, ""],
  ["D", 2, 220, 320, ""],
  ["C", 0, 180, 260, ""],
  ["C", 1, 260, 360, ""],
  ["C", 2, 360, 500, ""],
  ["B", 0, 280, 380, ""],
  ["B", 1, 380, 500, ""],
  ["B", 2, 500, 700, ""],
  ["A", 0, 400, 550, ""],
  ["A", 1, 550, 750, ""],
  ["A", 2, 750, 1000, ""],
  ["S", 0, 600, 850, ""],
  ["S", 1, 850, 1150, ""],
  ["S", 2, 1150, 1600, ""],
  ["SS", 0, 900, 1250, ""],
  ["SS", 1, 1250, 1700, ""],
  ["SS", 2, 1700, 2300, ""],
  ["SSS", 0, 1400, 1900, ""],
  ["SSS", 1, 1900, 2600, ""],
  ["SSS", 2, 2600, 3500, ""],
];

function buildAuctionRows() {
  const header = [
    "grade",
    "tier_id",
    "bid_type",
    "bid_band_min",
    "bid_band_max",
    "ai_count_min",
    "ai_count_max",
    NOTES_COLUMN,
  ];

  return [
    header,
    ...AUCTION_RAW.map(([grade, tierId, bandMin, bandMax, extraNotes]) => {
      const bidType = resolveBidType(grade, tierId);
      const [aiMin, aiMax] = AI_COUNT_BY_GRADE[grade];
      const typeNote = auctionTypeNote(grade, tierId);
      const aiNote =
        bidType === "English"
          ? note(`English AI ${aiMin}~${aiMax}명 rnd · 플레이어 제외`)
          : note("Ebay · ai_count 미사용(결과 AI 3명 코드 고정)");
      const combinedNotes = [extraNotes, typeNote, aiNote].filter(Boolean).join(" · ");
      return [grade, tierId, bidType, bandMin, bandMax, aiMin, aiMax, combinedNotes];
    }),
  ];
}

function buildEnglishTimerTierRows() {
  return [
    ["bid_count_gt", "timer_sec", NOTES_COLUMN],
    ...ENGLISH_TIMER_TIERS.map(([bidCountGt, timerSec, notes]) => [
      bidCountGt,
      timerSec,
      notes,
    ]),
  ];
}

/**
 * English 라이브 AI 1회 입찰량 (step × english_bid_increment):
 *   steps = rnd(step_count_min, step_count_max)
 *   + (유저가 현재 1등이면 rnd(player_counter_step_min, player_counter_step_max))
 *   → min(..., english_ai_max_steps_per_bid)
 *   new_bid = min(current_high + steps × increment, bid_band_max)
 *
 * @type {Record<string, {
 *   stepMin: number, stepMax: number,
 *   playerCounterMin: number, playerCounterMax: number,
 *   reactMin: number, reactMax: number,
 *   counterChance: number,
 *   notes?: string
 * }>}
 */
const ENGLISH_AI_BEHAVIOR_BY_GRADE = {
  F: {
    stepMin: 1,
    stepMax: 2,
    playerCounterMin: 0,
    playerCounterMax: 1,
    reactMin: 10,
    reactMax: 18,
    counterChance: 35,
    notes: note("저가·소극 · +5~15G"),
  },
  E: {
    stepMin: 1,
    stepMax: 2,
    playerCounterMin: 0,
    playerCounterMax: 1,
    reactMin: 9,
    reactMax: 16,
    counterChance: 40,
  },
  D: {
    stepMin: 1,
    stepMax: 3,
    playerCounterMin: 1,
    playerCounterMax: 2,
    reactMin: 8,
    reactMax: 14,
    counterChance: 45,
    notes: note("유저 역입찰 시 +1~2 step"),
  },
  C: {
    stepMin: 1,
    stepMax: 3,
    playerCounterMin: 1,
    playerCounterMax: 2,
    reactMin: 7,
    reactMax: 12,
    counterChance: 50,
  },
  B: {
    stepMin: 2,
    stepMax: 4,
    playerCounterMin: 1,
    playerCounterMax: 3,
    reactMin: 6,
    reactMax: 11,
    counterChance: 55,
    notes: note("+10~35G · 중급"),
  },
  A: {
    stepMin: 2,
    stepMax: 4,
    playerCounterMin: 2,
    playerCounterMax: 3,
    reactMin: 5,
    reactMax: 10,
    counterChance: 60,
  },
  S: {
    stepMin: 2,
    stepMax: 5,
    playerCounterMin: 2,
    playerCounterMax: 4,
    reactMin: 4,
    reactMax: 9,
    counterChance: 68,
    notes: note("S+ Eng · +10~45G"),
  },
  SS: {
    stepMin: 3,
    stepMax: 6,
    playerCounterMin: 2,
    playerCounterMax: 4,
    reactMin: 3,
    reactMax: 8,
    counterChance: 75,
  },
  SSS: {
    stepMin: 3,
    stepMax: 8,
    playerCounterMin: 3,
    playerCounterMax: 5,
    reactMin: 2,
    reactMax: 6,
    counterChance: 82,
    notes: note("최상 · +15~65G · 빠른 반응"),
  },
};

function buildEnglishAiBehaviorRows() {
  const header = [
    "grade",
    "step_count_min",
    "step_count_max",
    "player_counter_step_min",
    "player_counter_step_max",
    "react_delay_sec_min",
    "react_delay_sec_max",
    "counter_bid_chance_pct",
    NOTES_COLUMN,
  ];

  return [
    header,
    ...ALL_GRADES.map((grade) => {
      const row = ENGLISH_AI_BEHAVIOR_BY_GRADE[grade];
      return [
        grade,
        row.stepMin,
        row.stepMax,
        row.playerCounterMin,
        row.playerCounterMax,
        row.reactMin,
        row.reactMax,
        row.counterChance,
        row.notes ?? "",
      ];
    }),
  ];
}

const GRADE_BAND_BY_GRADE = {
  F: "low",
  E: "low",
  D: "mid",
  C: "mid",
  B: "mid",
  A: "high",
  S: "high",
  SS: "top",
  SSS: "top",
};

/** @type {Record<string, number>} */
const BROKER_PRICING_PCT = {
  F: 8,
  E: 8,
  D: 7,
  C: 7,
  B: 6,
  A: 6,
  S: 5,
  SS: 5,
  SSS: 5,
};

function buildConstantsRows() {
  return [
    ["key", "value", NOTES_COLUMN],
    [
      "energy_percent_max",
      String(ENERGY_PERCENT_MAX),
      note("에너지 % = energy / value × 100"),
    ],
    [
      "english_min_grade",
      ENGLISH_MIN_GRADE,
      note("이 등급 이상 → English (tier 무관)"),
    ],
    [
      "english_tier_id",
      String(ENGLISH_TIER_ID),
      note("이 tier_id → English (등급 무관)"),
    ],
    ["ebay_bid_increment", "5", note("Ebay 입찰 ± step")],
    [
      "english_bid_increment",
      "5",
      note("English 입찰 최소 상향 단위 · Import 시 없으면 ebay와 동일"),
    ],
    [
      "english_ai_max_steps_per_bid",
      "8",
      note("AI 1회 입찰 step 상한 (× increment)"),
    ],
    [
      "english_ai_eval_interval_sec",
      "1",
      note("AI 입찰 판정 폴링 간격(초)"),
    ],
  ];
}

function buildGradeBandRows() {
  return [
    ["grade", "grade_band", NOTES_COLUMN],
    ...ALL_GRADES.map((grade) => [
      grade,
      GRADE_BAND_BY_GRADE[grade],
      grade === "F" ? GRADE_BAND_NOTE : "",
    ]),
  ];
}

function buildBrokerPricingRows() {
  return [
    ["grade", "hint_cost_pct_of_min_bid", NOTES_COLUMN],
    ...ALL_GRADES.map((grade) => [
      grade,
      BROKER_PRICING_PCT[grade],
      grade === "F"
        ? note("힌트 가격 ≈ bid_band_min × pct / 100 (Lot당 1회)")
        : "",
    ]),
  ];
}

const GRADE_BAND_NOTE = note("low=F~E | mid=D~B | high=A~S | top=SS~SSS");

const HINT_INFO_BY_ARCHETYPE = {
  gold: "골드 보상",
  mineral: "광물 보상",
  equipment: "장비 보상",
  artifact: "유물 보상",
  mutation: "변이 보상",
};

const UP_ARROW_BY_TIER_ID = [1, 2, 3];

function brokerHintRow(archetype, tierId, hintText, notes = "") {
  return [
    archetype,
    tierId,
    hintText,
    HINT_INFO_BY_ARCHETYPE[archetype] ?? "",
    UP_ARROW_BY_TIER_ID[tierId] ?? 1,
    notes,
  ];
}

/** 정보상 유료 힌트 — 5성향 × tier_id(0~2) */
const BROKER_HINTS = [
  [
    "archetype",
    "tier_id",
    "hint_text",
    "hint_info",
    "up_arrow",
    NOTES_COLUMN,
  ],
  brokerHintRow("gold", 0, "월급은 줄 수 있겠네요.", note("tier0")),
  brokerHintRow("gold", 1, "이 정도면 입찰가 좀 올려도 되겠는데요?"),
  brokerHintRow("gold", 2, "길드장님, 이건 다들 탐내는 물건입니다."),
  brokerHintRow("mineral", 0, "땅은 나쁘지 않습니다."),
  brokerHintRow("mineral", 1, "경호원보다 곡괭이가 더 필요할지도 모릅니다."),
  brokerHintRow("mineral", 2, "이 정도면 광산이 게이트 안에 있는 수준입니다."),
  brokerHintRow("equipment", 0, "수리비 정도는 회수 가능할 것 같습니다."),
  brokerHintRow("equipment", 1, "무기 장사꾼들이 관심을 보이고 있습니다."),
  brokerHintRow("equipment", 2, "명장 공방에서 먼저 연락이 왔습니다."),
  brokerHintRow("artifact", 0, "먼지는 좀 털어볼 가치가 있겠네요."),
  brokerHintRow("artifact", 1, "박물관 쪽에서 먼저 연락이 왔습니다."),
  brokerHintRow("artifact", 2, "이건 팔지 말고 전시해야 할 수도 있습니다."),
  brokerHintRow("mutation", 0, "숫자가 조금 이상하게 움직입니다."),
  brokerHintRow("mutation", 1, "게이트 보고서가 세 번 수정됐다고 합니다."),
  brokerHintRow("mutation", 2, "측정기가 측정을 거부했습니다."),
];

const wb = XLSX.utils.book_new();
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(README), "#ReadMe");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildConstantsRows()),
  "GateConstants",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildGradeBandRows()),
  "GateGradeBands",
);
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(GRADES), "GateGrades");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(ENERGY_TIERS),
  "GateEnergyTiers",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildAuctionRows()),
  "Auction",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildEnglishTimerTierRows()),
  "EnglishTimerTiers",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildEnglishAiBehaviorRows()),
  "EnglishAiBehavior",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(BROKER_HINTS),
  "GateBrokerHints",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildArchetypeRows()),
  "GateArchetypes",
);
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildBrokerPricingRows()),
  "GateBrokerPricing",
);
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(buildUnlockRows()), "GateUnlock");
XLSX.utils.book_append_sheet(
  wb,
  XLSX.utils.aoa_to_sheet(buildSpawnWeightRows()),
  "GateSpawnWeights",
);

writeFileSync(outPath, XLSX.write(wb, { type: "buffer", bookType: "xlsx" }));
console.log(`Created: ${outPath}`);
