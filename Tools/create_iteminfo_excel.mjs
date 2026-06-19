import { writeFileSync, mkdirSync } from "fs";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import XLSX from "xlsx";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outDir = join(__dirname, "..", "Assets", "Data");
const outPath = join(outDir, "Iteminfo.xlsx");

mkdirSync(outDir, { recursive: true });

const NOTES_COLUMN = "#notes";

function note(text) {
  if (!text) return "";
  return text.startsWith("#") ? text : `# ${text}`;
}

const README = [
  ["Iteminfo — 아이템 마스터 데이터"],
  [""],
  ["Import 규칙:"],
  ["  · 시트/컬럼 이름이 # 으로 시작하면 Import 스킵 (#ReadMe, #notes 등)"],
  ["  · Unity: The Guild/Data/Import Iteminfo"],
  [""],
  ["Import 시트:"],
  ["  Items — 아이템 정의 (item_id 고유)"],
  ["  ItemEffects — 유물·패시브 효과 (item_id → Items 참조)"],
  [""],
  ["item_kind (Items):"],
  ["  mineral | weapon | armor | accessory | artifact"],
  ["  (추후 consumable, material 등 확장 가능)"],
  [""],
  ["icon_sprite:"],
  ["  비움 → Import 시 Item Icon_{item_id} 규칙 (미구현 시 null)"],
  ["  값 있으면 해당 스프라이트 이름으로 override"],
  [""],
  ["sell_gold:"],
  ["  0 = 판매 불가 (유물 등)"],
  [""],
  ["Dungeoninfo 연동:"],
  ["  LootPools.item_key → Items.item_key · display_name/sell_gold는 여기서 조회"],
  [""],
  ["재생성: node Tools/create_iteminfo_excel.mjs"],
];

const ITEMS = [
  [
    "item_id",
    "item_kind",
    "display_name",
    "icon_sprite",
    "sell_gold",
    "stack_max",
    NOTES_COLUMN,
  ],
  // mineral
  ["ore_iron", "mineral", "철광석", "", 12, 99, note("하위 광물")],
  ["ore_silver", "mineral", "은광석", "", 35, 99, ""],
  ["ore_mithril", "mineral", "미스릴 원석", "", 90, 99, ""],
  ["crystal_mana", "mineral", "마력결정", "", 150, 50, note("희귀")],
  // equipment
  ["wpn_rusty_sword", "weapon", "녹슨 장검", "", 80, 1, ""],
  ["wpn_short_bow", "weapon", "짧은 활", "", 70, 1, ""],
  ["arm_leather_vest", "armor", "가죽 조끼", "", 65, 1, ""],
  ["acc_lucky_charm", "accessory", "행운의 부적", "", 120, 1, ""],
  // artifact (sell_gold=0)
  [
    "art_guild_exp_5",
    "artifact",
    "길드 경험치 +5%",
    "",
    0,
    1,
    note("C+ 게이트 유물 풀"),
  ],
  ["art_bid_fee_minus_3", "artifact", "입찰 수수료 -3%", "", 0, 1, ""],
  ["art_equip_drop_plus_2", "artifact", "장비 드랍률 +2%", "", 0, 1, ""],
  [
    "art_clear_time_plus_5",
    "artifact",
    "클리어 시간 +5%",
    "",
    0,
    1,
    note("테스트용 패시브"),
  ],
];

const ITEM_EFFECTS = [
  ["item_id", "effect_id", "value", NOTES_COLUMN],
  [
    "art_guild_exp_5",
    "guild_exp_pct",
    5,
    note("길드 경험치 획득 +%"),
  ],
  [
    "art_bid_fee_minus_3",
    "bid_fee_pct",
    -3,
    note("입찰 수수료 % (음수=할인)"),
  ],
  [
    "art_equip_drop_plus_2",
    "equip_drop_pct",
    2,
    note("장비 드랍 확률 +%p"),
  ],
  [
    "art_clear_time_plus_5",
    "clear_time_pct",
    5,
    note("클리어 제한 시간 +%"),
  ],
];

const wb = XLSX.utils.book_new();
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(README), "#ReadMe");
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(ITEMS), "Items");
XLSX.utils.book_append_sheet(wb, XLSX.utils.aoa_to_sheet(ITEM_EFFECTS), "ItemEffects");

writeFileSync(outPath, XLSX.write(wb, { type: "buffer", bookType: "xlsx" }));
console.log(`Created: ${outPath}`);
