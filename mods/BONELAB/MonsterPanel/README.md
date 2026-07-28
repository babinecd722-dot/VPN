# MONSTER Panel (BONELAB MelonLoader mod)

## Tracking (2.30.25+)

- Offline friend card: **no Join row**
- Already with them in Fusion lobby → **"In lobby"** instead of Join
- Online watch every **~15s** (API cooldown 10s): offline→online → Fusion popup like player joined (`Name Online` / `is online.`, `SaveToMenu=false`)
- Join success logic from 2.30.23 unchanged

## Anti-OOB (2.30.28+)

Silent Fusion shield against the **Whoops / far out of bounds** kick (no BoneMenu text):
- Blocks the reload→spawn→OOB→reload cascade (crash-crash-crash before load finishes)
- Scene grace + recover cooldown + lockdown after burst recovers
- Suppresses the Whoops popup; blocks `Disconnect("Left Bounds")`
- Sanitizes `PlayerRepTeleport` and local teleports

Uses LabFusion from Checkerb0ard 0.0.5 (EOS), not Steam.

## Host tools unlock (2.30.39+)

Silent (no BoneMenu) — stock Fusion tools work even when the lobby locks them:
- **Dev Tools / Spawn Gun / Nimbus** — bypass `FusionDevTools` permission + gamemode disable for the local player
- **Constrainer** — keep the gun when `Constrainer` permission is locked; allow player-constrain locally (`PlayerConstraining`)

Same pattern as Slow Mo / Despawn All unlocks.

---

Страница **MONSTER Panel** в BoneMenu, два тумблера (без кнопок — вкл/выкл):

- **Invincible** — бессмертие локального игрока (2.30.32+: TAKEDAMAGE/Dying/Death/OnReceivedDamage/insta-death + tick heal; чужих не трогает).
- **Monster Damage** — ваншот с жёстким отбросом:
  - **враги-гуманоиды** (PuppetMaster) — мгновенно убиваются и **отлетают** в сторону удара;
  - **объекты/ящики** (Health) — уничтожаются с одного касания;
  - **игроки в Fusion** — сетевой урон по игроку подменяется на максимум (ваншот по сети).
- **Kill Aura** — подстраница-настройки урона по игрокам, **без наведения и крестиков**:
  - **ENABLE** — мастер-выключатель (включил → цель(и) начинают получать урон);
  - **Mode: ALL players** — бить всех / только выбранного;
  - **Speed (hits/sec)** — ползунок 1…60: как часто шлём урон (60 ≈ моментальная смерть);
  - **Ignore distance** — бить независимо от расстояния;
  - **Lightning FX** — **сетевая** молния (видят все): спавним электро-объект из реестра
    (`NetworkAssetSpawner`) **ровно в грудь** цели — теперь точно по игроку, а не «рядом»
    (раньше был offset над головой, объект падал сбоку). Один удар за залп (round-robin),
    авто-удаление по таймеру (`Despawn`), кап одновременных — без лагов. Требует активный
    сервер и электро-крейт в реестре (ищем по названию: lightning/electric/…);
  - **список игроков** с пометкой **[HOST]** — клик выбирает одиночную цель и включает ауру.
  - Технически урон уходит через `PlayerDamageReceiver.ReceiveAttack` с проставленным `attack.proxy`
    от своего рига — только тогда патч LabFusion шлёт `SendPlayerDamage` владельцу. Если хост лобби
    выключил PvP-урон (`NetworkCombatManager.CanAttack` = false), урон не пройдёт — это серверная настройка.
- **Infinite Ammo** — бесконечный **запас** патронов в инвентаре: перезарядка всегда доступна, а сам магазин расходуется штатно (стреляй → перезаряжай без конца). Патчится `AmmoInventory.GetCartridgeCount`.
- **Tank Mode** — тебя **нельзя схватить/поднять** (отключаем `AvatarGrip` на своём риге). Массу/руки не трогаем — рост, движение и удары как обычно.
- **Anti-Constrainer** (всегда вкл, без UI) — снимает **ЧУЖИЕ** скрепы **Constrainer** с тебя и предметов рядом (радиус ~3.5 м, раз в 0.35 с). Владельца берём из `NetworkConstraint.Cache → NetworkEntity.OwnerID`: **свои скрепы не трогаем** (`OwnerID.IsMe` или владельца не определить → оставляем). Если кто-то приконстрейнил твоё оружие — освобождается; свои постройки держатся. Бьём точечно по `ConstraintTracker.DeleteConstraint()` (штатное снятие, синхронится через LabFusion), не трогая джойнты уровня (двери/петли).
- **Disarm** — у **всех игроков рядом** (радиус ~15 м) раз в 0.5 с **вырывает оружие**, **без наведения**. Сначала **забираем владение сетевой сущностью** предмета (`NetworkEntityManager.TakeOwnership` через `IMarrowEntityExtender.Cache`) — иначе позицией ствола владеет чужой клиент и швырок сразу перетирается синхронизацией. Забрав владение, хват срывается и предмет улетает. Тела игроков не трогаются.

> Kill Aura / Disarm работают только в **Fusion** (в одиночке некого задевать) — раздел
> активен, только когда LabFusion загружен. Наведение и крестики убраны за ненадобностью:
> цель берётся из списка сетевых игроков напрямую.

### Контра против игроков с их god mode

Урон по такому игроку **не проходит** — его клиент сам отклоняет урон (P2P, обойти нельзя).
Поэтому «убить» их нельзя, но можно **физически издеваться**: схватить и швырнуть ванильно,
**Disarm** (вырвать оружие), плюс **Tank** (тебя самого не схватят в ответ).
Это то, что бессмертие не спасает.

**Очистить чужой инвентарь по сети — нельзя.** Инвентарь принадлежит клиенту игрока (P2P).
Сообщения `InventorySlotDropMessage` в LabFusion есть, но они для твоих легитимных действий
(взял ствол из чужой кобуры). Подделывать их для удалённого вайпа — хак, который легко
крашит игру и непроверяем; поэтому не делаем. Забирать оружие — только физически (Disarm/захват).

Работает и для ближнего боя, и для стрельбы. Monster Damage по объектам теперь ставит им **максимальный урон** (`Health.TAKEDAMAGE` → max), а не просто Death().

## Ник: скрытие и цветные DEV-пресеты (Fusion)

Подстраница **Nickname** — меняет твой ник, который видят другие над твоим персонажем
(ник синхронизируется по сети как метаданные). Nametag рисуется через TextMeshPro,
а он понимает rich-text теги `<color=…>`, поэтому доступны цветные имена:

- **MONSTER (red / green / gold / cyan / pink)** — цветное имя `MONSTER`.
- **DEV (gold)** — золотой `DEV`.
- **Hide (empty)** — пустой ник (пробел; «пусто» LabFusion откатывает на username).
- **Reset to default** — вернуть обычный ник.

Лимит имени в LabFusion — **32 символа**, а теги его «съедают», поэтому по-буквенную
радугу вписать нельзя (`<color=#ff4040>M</color>…` слишком длинно). Пресеты подобраны ≤32.
Работает только в лобби Fusion.

> Нюанс: рисует ли клиент чужой nametag — решает он сам (его настройка Nametags), так что
> «железно» убрать тег нельзя. Но текст имени мы меняем/убираем. Если у тебя стоит галка
> «richText» off на конкретном шрифте — теги покажутся как текст; проверь на устройстве и скажи.

## Teleport (2.30.31+, LabFusion)

Классическое меню: список игроков → **Teleport to player** / **Bring player to me**.
- Go to: поза/ноги + `LocalPlayer.TeleportToPosition`
- Bring: всегда шлёт `PlayerRepTeleport` через MessageRelay (работает и не-хостом, без прав Teleportation)
- Ники с `<color>` как раньше; Tracking сюда не смешан

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
