# VR CONTROLER

Файловый менеджер для Meta Quest (Quest 2 / 3 / 3S / Pro) с полным доступом ко всему хранилищу, включая `Android/data` и `Android/obb`.

**Один APK — портативный.** Отдельный Shizuku ставить не нужно: shell-доступ встроен через Wireless Debugging.

Весь код открыт в этом репозитории.

## Возможности

- Полный доступ ко всему `/sdcard` через «Управление всеми файлами»
- Доступ к `Android/data` и `Android/obb` через встроенный shell-daemon (права ADB)
- Копирование, перемещение, переименование, удаление, создание папок, мультивыбор
- Распаковка ZIP (моды) прямо в нужную папку
- Встроенный текстовый редактор (`repositories.txt` для Fusion и т.п.)
- Ярлыки: Загрузки, Android/data, BONELAB
- Тёмный VR-интерфейс с крупными элементами

## Установка

1. Включи режим разработчика на Quest (Meta Horizon на телефоне) — у тебя уже есть.
2. Установи APK через SideQuest / `adb install`.
3. Запусти **VR CONTROLER** из *Неизвестные источники*.
4. Разреши «Управление всеми файлами».

## Android/data (встроенный privilege, без Shizuku)

Horizon OS блокирует `Android/data` для обычных приложений. VR CONTROLER поднимает свой shell-daemon через Wireless Debugging — **отдельный APK Shizuku не нужен**.

### Первый раз (pairing, один раз)

1. Параметры → Система → Параметры разработчика → **Wireless debugging** — включи.
2. В VR CONTROLER нажми **Подключить** / **Ввести код**.
3. В Wireless debugging открой **Pair device with pairing code**.
4. Введи 6 цифр в диалоге приложения (порт находится по mDNS сам).
5. После pairing приложение само поднимет daemon — ярлык Android/data заработает.

### После перезагрузки очков

Wireless Debugging нужно включить снова, затем в приложении нажать **Подключить** (повторный pairing не нужен, пока не сбросишь данные приложения).

Пока privilege не подключён, менеджер работает как обычный (всё, кроме `Android/data` / `Android/obb`).

## Моды BONELAB Fusion

1. Скачай мод (zip) браузером Quest → `Download`.
2. VR CONTROLER → «Загрузки» → тап по zip — распаковка.
3. Выдели папку мода → «Вырез.» → «BONELAB» → `Mods` → «Встав.».
4. Репозитории Fusion: «BONELAB» → `repositories.txt` — встроенный редактор.

## Сборка

```bash
./gradlew assembleRelease
```

JDK 17, Android SDK 34. Подпись: `zipalign` + `apksigner`.

## Технологии privilege-слоя

- Wireless ADB pairing/client (адаптация протокола из [Shizuku](https://github.com/RikkaApps/Shizuku), Apache-2.0)
- `libadb.so` (SPAKE2) из релиза Shizuku
- Встроенный `FileDaemon` через `app_process` с shell UID
- Quest-friendly in-app pairing (без notification RemoteInput — на Horizon OS v83+ он сломан)

## Лицензии стороннего кода

См. `app/src/main/java/moe/shizuku/manager/adb/NOTICE.txt`.
