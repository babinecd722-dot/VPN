# MONSTER Panel (BONELAB MelonLoader mod)

Страница **MONSTER Panel** в BoneMenu, два тумблера (без кнопок — вкл/выкл):

- **Invincible** — бессмертие. Игрок не получает урон и не умирает.
- **Monster Damage** — ваншот с жёстким отбросом:
  - **враги-гуманоиды** (PuppetMaster) — мгновенно убиваются и **отлетают** в сторону удара;
  - **объекты/ящики** (Health) — уничтожаются с одного касания;
  - **игроки в Fusion** — сетевой урон по игроку подменяется на максимум (ваншот по сети).
- **Remote Kill** — наводишь рукой на игрока (толстый луч + зелёный крестик), зажимаешь grip → максимальный урон по всем частям тела цели по сети. Одна цель на руку, дальность ~40 м.

Работает и для ближнего боя, и для стрельбы.

## Teleport (только в LabFusion)

Появляется отдельная подстраница **Teleport** в BoneMenu, если загружен LabFusion.
Список других игроков обновляется при каждом открытии страницы (плюс кнопка **Refresh**).
Выбираешь игрока → подстраница с двумя действиями:

- **Teleport to player** — телепортируешься к игроку. Свой риг синхронизируется по сети штатно — работает надёжно.
- **Bring player to me** — притянуть игрока к себе. Best-effort: позицией чужого рига
  владеет его клиент, поэтому его может «отдёрнуть» назад. Твой телепорт к нему — надёжнее.

Точка приземления берётся лучом вниз от физ-рига цели (ноги на полу), с небольшим отступом,
чтобы не оказаться внутри игрока.

## Как работает (Harmony-префиксы, типизированно)

- Бессмертие → `Player_Health.TAKEDAMAGE / ApplyKillDamage / Death` (префикс возвращает `false`).
- Враги → `PuppetMasta.SubBehaviourHealth.TakeDamage` → зовём `Kill()` + раскидываем
  риг задетого тела через `attack.collider` (скорость `velocity` всем Rigidbody от корня).
- Объекты → `Health.TAKEDAMAGE` → `Death()`.
- Игроки (Fusion) → `LabFusion.Patching.PlayerDamageReceiverPatches.ReceiveAttack(ref Attack attack)` →
  ставим `attack.damage` в максимум до того, как Fusion отправит урон по сети.

Всё собрано **типизированно** против реальных сборок с твоих очков:
`Il2CppSLZ.Marrow.dll`, `LabFusion.dll` (v1.14.2), `BoneLib.dll` v3.1.4,
`MelonLoader.dll` v0.6.5, `Il2CppInterop.Runtime`. Сигнатуры методов сверены с
метаданными. Компиляция — 0 ошибок.

## Установка

1. `MonsterPanel.dll` → `MelonLoader/Mods/` (замени старый!).
2. BONELAB → BoneMenu → **MONSTER Panel**.
3. В `Latest.log` проверь строки «пропатчен …»: `SubBehaviourHealth.TakeDamage`,
   `Health.TAKEDAMAGE`, `Player_Health.*`, и (если Fusion) `PlayerDamageReceiverPatches.ReceiveAttack`.

## Нюансы / не проверено на устройстве

- Бессмертие уже подтверждено тобой в игре. Остальное собрано верно, но рантайм-тест
  на очках не делался.
- **Fusion:** префикс `ReceiveAttack` бустит урон для любой атаки, проходящей через
  ресивер. Рекомендую в PvP держать **Invincible** тоже включённым (на случай, если
  ресивер сработает и на входящий по тебе урон).
- Если по какому-то врагу/объекту не сработает — скинь `Latest.log`, добавлю нужный тип.

## Пересборка

Референсы берутся из `third_party/BONELAB-libs/` (Il2CppAssemblies + LabFusion) и
NuGet `Il2CppInterop.Runtime`. См. `MonsterPanel.csproj`.
